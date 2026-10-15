using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using LabGuard.Protocol;

// Cho phép assembly kiểm thử truy cập constructor nội bộ (test seam) mà không public lên production
[assembly: InternalsVisibleTo("LabGuard.Agent.Tests")]

namespace LabGuard.Agent;

/// <summary>
/// Quản lý kết nối TCP Socket từ Agent tới Central Server và hàng đợi gửi tuần tự (A-003).
/// Áp dụng mẫu kiến trúc Single Writer Loop với Bounded Channel:
/// - Nhiều thành phần (Heartbeat, Metrics, Service) có thể đẩy frame đồng thời vào hàng đợi.
/// - Chỉ DUY NHẤT một vòng lặp ngầm (writer loop) được phép ghi dữ liệu xuống NetworkStream.
/// - Ngăn chặn hoàn toàn hiện tượng xé khung hoặc đan xen byte (Frame Interleaving) trên TCP socket.
/// </summary>
public sealed class AgentSocketClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Sức chứa cố định của hàng đợi gửi theo quyết định kiến trúc CP2-001 (256 items).
    /// </summary>
    public const int DefaultOutboxCapacity = 256;

    private readonly int _outboxCapacity;
    private TcpClient? _tcpClient;
    private NetworkStream? _networkStream;
    private Channel<byte[]>? _outboxChannel;
    private Task? _writerTask;
    private CancellationTokenSource? _cts;
    private int _isDisposed;

    // Được đặt thành 1 khi writer loop thoát do lỗi mạng (không phải do huỷ chủ động).
    // Dùng để IsConnected trả false và các EnqueueFrameAsync tiếp theo ném lỗi ngay lập tức.
    private volatile bool _writerFaulted;

    /// <summary>
    /// Constructor production: luôn dùng đúng 256 items theo CP2-001.
    /// Không cho phép truyền capacity tuỳ ý từ bên ngoài để đảm bảo bất biến kiến trúc.
    /// </summary>
    public AgentSocketClient() : this(DefaultOutboxCapacity) { }

    /// <summary>
    /// Constructor nội bộ chỉ dành cho kiểm thử (test seam).
    /// Cho phép truyền capacity nhỏ để tạo ra backpressure có thể kiểm soát được trong test.
    /// KHÔNG dùng trong production — chỉ truy cập được từ assembly LabGuard.Agent.Tests.
    /// </summary>
    internal AgentSocketClient(int outboxCapacity)
    {
        if (outboxCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outboxCapacity), "Sức chứa hàng đợi phải lớn hơn 0.");
        }

        _outboxCapacity = outboxCapacity;
    }

    /// <summary>
    /// Kiểm tra trạng thái kết nối TCP hiện tại của Agent.
    /// Trả về false ngay khi writer loop bị lỗi mạng, ngay cả khi TcpClient.Connected chưa cập nhật.
    /// </summary>
    public bool IsConnected
    {
        get
        {
            if (_isDisposed != 0) return false;
            if (_writerFaulted) return false;
            var client = _tcpClient;
            return client != null && client.Connected && _networkStream != null;
        }
    }

    /// <summary>
    /// Sức chứa tối đa của hàng đợi gửi (Outbox).
    /// </summary>
    public int OutboxCapacity => _outboxCapacity;

    /// <summary>
    /// Số lượng frame hiện đang xếp hàng chờ gửi trong hàng đợi.
    /// </summary>
    public int QueuedCount => _outboxChannel?.Reader.Count ?? 0;

    /// <summary>
    /// Kết nối tới Central Server qua TCP Socket và khởi động Writer Loop chạy ngầm.
    /// Nếu kết nối thất bại, ném ngoại lệ ngay lập tức và KHÔNG tự động thử lại (A-003).
    /// </summary>
    /// <param name="host">Địa chỉ IP hoặc hostname của Central Server.</param>
    /// <param name="port">Cổng dịch vụ của Central Server (ví dụ: 5000).</param>
    /// <param name="ct">Token hủy tác vụ kết nối.</param>
    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed != 0, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);

        if (IsConnected)
        {
            throw new InvalidOperationException("Agent đã có kết nối TCP đang hoạt động.");
        }

        // Tạo mới TcpClient và kết nối tới server
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port, ct).ConfigureAwait(false);
            _networkStream = client.GetStream();
            _tcpClient = client;
        }
        catch
        {
            // Đóng kết nối an toàn nếu ConnectAsync thất bại và ném lỗi lên phía gọi
            client.Dispose();
            throw;
        }

        // Tạo hàng đợi bằng factory nội bộ để test có thể dùng chính factory đó mà kiểm chứng.
        // Toàn bộ cấu hình Bounded + FullMode.Wait + SingleReader nằm trong CreateOutbox.
        _outboxChannel = CreateOutbox(_outboxCapacity);
        _writerFaulted = false;

        // Khởi động Background Writer Task chuyên trách việc ghi dữ liệu xuống socket
        _cts = new CancellationTokenSource();
        _writerTask = Task.Run(() => WriterLoopAsync(_networkStream, _outboxChannel, _cts.Token));
    }

    /// <summary>
    /// Đưa một frame nhị phân đã đóng khung [4-byte Big-Endian length + JSON payload] vào hàng đợi gửi.
    /// Nếu hàng đợi bị đầy (đạt giới hạn), tác vụ sẽ tạm dừng (await) chờ đến khi có chỗ trống (Backpressure).
    /// Nếu writer loop đã bị lỗi mạng, ném OperationCanceledException ngay lập tức thay vì chờ mãi.
    /// </summary>
    /// <param name="frame">Mảng byte của frame hoàn chỉnh.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    public async ValueTask EnqueueFrameAsync(byte[] frame, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed != 0, this);
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Length == 0)
        {
            throw new ArgumentException("Không thể gửi frame có độ dài bằng 0 (vi phạm giao thức).", nameof(frame));
        }

        if (!IsConnected || _outboxChannel == null)
        {
            throw new InvalidOperationException("Agent chưa kết nối tới Central Server, không thể gửi tin.");
        }

        // Kết hợp token từ người gọi với token của writer CTS.
        // Nếu writer loop bị lỗi mạng, _cts sẽ bị cancel → WriteAsync sẽ throw thay vì chờ mãi.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts?.Token ?? CancellationToken.None);
        await _outboxChannel.Writer.WriteAsync(frame, linkedCts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Đóng gói WireEnvelope bằng FrameCodec.Encode và đưa vào hàng đợi gửi tuần tự.
    /// Tái sử dụng toàn bộ thư viện giao thức chuẩn A-001 / A-002 mà không viết lại.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của payload.</typeparam>
    /// <param name="envelope">Đối tượng phong bì chuẩn 6 trường.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    public async ValueTask SendEnvelopeAsync<T>(WireEnvelope<T> envelope, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        // FrameCodec.Encode tự động tuần tự hóa JSON UTF-8 và gắn tiền tố 4 byte Big-Endian
        byte[] frameBytes = FrameCodec.Encode(envelope);
        await EnqueueFrameAsync(frameBytes, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Ngắt kết nối TCP và dừng vòng lặp Writer một cách êm đềm.
    /// Không được gọi khi _isDisposed = 1 vì Dispose/DisposeAsync sẽ xử lý trực tiếp.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_isDisposed != 0) return;

        // Báo cho hàng đợi biết không còn item mới được thêm vào
        _outboxChannel?.Writer.TryComplete();

        // Phát tín hiệu dừng tới Writer Loop
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }

        // Chờ Writer Loop kết thúc an toàn (tránh treo vô hạn bằng timeout / cancellation)
        if (_writerTask != null)
        {
            try
            {
                await _writerTask.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Bỏ qua ngoại lệ hủy hợp lệ
            }
            catch (Exception)
            {
                // Bỏ qua lỗi kết nối đã đứt khi ngắt kết nối
            }
            _writerTask = null;
        }

        // Đóng dòng dữ liệu và socket
        _networkStream?.Dispose();
        _networkStream = null;

        _tcpClient?.Dispose();
        _tcpClient = null;

        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// Factory nội bộ tạo Bounded Channel dùng cho Outbox của Agent.
    /// production ConnectAsync gọi hàm này, test cũng gọi hàm này để kiểm chứng
    /// ĐÚNG cấu hình channel được dùng trong production (Bounded + FullMode.Wait + SingleReader).
    /// </summary>
    /// <param name="capacity">Số lượng frame tối đa trong hàng đợi (production = 256).</param>
    internal static Channel<byte[]> CreateOutbox(int capacity)
    {
        // Cấu hình FROZEN theo quyết định kiến trúc CP2-001:
        // - BoundedChannelFullMode.Wait: khi đầy producer bị chặn (backpressure), không drop frame.
        // - SingleWriter = false: nhiều producer đồng thời được phép.
        // - SingleReader = true: đúng một WriterLoop đọc ra socket.
        return Channel.CreateBounded<byte[]>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });
    }

    /// <summary>
    /// VÒNG LẶP GHI DUY NHẤT (SINGLE WRITER LOOP):
    /// Đây là NƠI DUY NHẤT trong toàn bộ Agent được phép gọi `stream.WriteAsync`.
    /// Đọc từng frame từ ChannelReader và ghi trọn vẹn từng frame xuống NetworkStream.
    ///
    /// Khi WriteAsync thất bại do lỗi mạng:
    /// - Đánh dấu _writerFaulted = true → IsConnected trả false ngay lập tức.
    /// - Hoàn tất Channel.Writer với exception → các WriteAsync của producer sẽ throw, không chờ mãi.
    /// - Cancel _cts → các linked token trong EnqueueFrameAsync cũng thoát nhanh.
    /// </summary>
    private async Task WriterLoopAsync(NetworkStream stream, Channel<byte[]> channel, CancellationToken ct)
    {
        try
        {
            // Lặp chờ cho đến khi có dữ liệu hoặc kênh bị đóng
            while (await channel.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                while (channel.Reader.TryRead(out byte[]? frame))
                {
                    if (frame is { Length: > 0 })
                    {
                        // Ghi trọn vẹn một frame xuống socket TCP.
                        // NetworkStream không có bộ đệm nội bộ — WriteAsync đẩy thẳng xuống OS.
                        // KHÔNG gọi FlushAsync vì nó là no-op trên NetworkStream.
                        await stream.WriteAsync(frame, ct).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Kết thúc bình thường khi có yêu cầu dừng (CancellationToken bị hủy)
        }
        catch (Exception ex)
        {
            // Lỗi mạng thực sự (ví dụ: server đóng kết nối đột ngột).
            // Cần thông báo cho các producer đang chờ biết để chúng không bị treo mãi.

            // 1. Đánh dấu kết nối bị lỗi → IsConnected trả false
            _writerFaulted = true;

            // 2. Hoàn tất channel với exception → WriteAsync của producer sẽ throw ChannelClosedException
            channel.Writer.TryComplete(ex);

            // 3. Hủy CTS → LinkedToken trong EnqueueFrameAsync cũng thoát nhanh nếu đang WaitToReadAsync
            try { _cts?.Cancel(); } catch { /* đã bị dispose rồi thì bỏ qua */ }
        }
    }

    /// <summary>
    /// Giải phóng tài nguyên đồng bộ (sync).
    /// Hủy CTS, đóng stream và socket ngay lập tức mà không chờ writer loop kết thúc.
    /// Phù hợp khi không thể await (ví dụ: trong finalizer hoặc using block sync).
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

        // Hoàn tất channel để writer loop và các producer thoát ra khỏi await
        _outboxChannel?.Writer.TryComplete();

        try
        {
            _cts?.Cancel();
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
            _cts?.Dispose();
        }
        catch
        {
            // Bỏ qua lỗi phát sinh trong quá trình giải phóng tài nguyên
        }
    }

    /// <summary>
    /// Giải phóng tài nguyên bất đồng bộ (async).
    /// Chờ writer loop kết thúc sạch trước khi đóng stream và socket.
    /// Phù hợp dùng với `await using` — KHÔNG gọi Dispose() trước rồi mới DisposeAsync().
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

        // Hoàn tất channel để writer loop thoát ra khỏi WaitToReadAsync
        _outboxChannel?.Writer.TryComplete();

        // Hủy token để WriterLoopAsync thoát nhanh nếu đang chờ ghi
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }

        // Chờ writer loop kết thúc hoàn toàn trước khi đóng stream/socket
        // (tránh ObjectDisposedException bên trong writer loop)
        if (_writerTask != null)
        {
            try
            {
                await _writerTask.ConfigureAwait(false);
            }
            catch
            {
                // Writer loop đã xử lý lỗi nội bộ rồi, bỏ qua ở đây
            }
        }

        _networkStream?.Dispose();
        _tcpClient?.Dispose();
        _cts?.Dispose();
    }
}
