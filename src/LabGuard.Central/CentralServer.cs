using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LabGuard.Protocol;

namespace LabGuard.Central;

/// <summary>
/// Máy chủ trung tâm (Central Server) lắng nghe kết nối TCP từ các Agent và Admin Client.
/// Nhiệm vụ B-001: Mở cổng TCP 5000, chấp nhận đa kết nối, đọc gói tin qua FrameCodec.ReadEnvelopeAsync,
/// hiển thị DeviceId và MessageType, và xử lý lỗi ngắt kết nối an toàn.
/// </summary>
public class CentralServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly int _port;
    private readonly ConcurrentDictionary<Task, bool> _activeClientTasks = new();
    private readonly CancellationTokenSource _internalCts = new();
    private readonly TaskCompletionSource _startedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _isDisposed;

    /// <summary>
    /// Cổng TCP thực tế máy chủ đang lắng nghe (hữu ích khi chỉ định port 0 trong kiểm thử tự động).
    /// </summary>
    public int Port
    {
        get
        {
            try
            {
                return _listener.LocalEndpoint is IPEndPoint ep ? ep.Port : _port;
            }
            catch
            {
                return _port;
            }
        }
    }

    /// <summary>
    /// Trạng thái máy chủ đang hoạt động lắng nghe.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Chờ đến khi máy chủ đã mở socket TCP và sẵn sàng tiếp nhận kết nối.
    /// </summary>
    public Task WaitForStartAsync(CancellationToken ct = default) => _startedTcs.Task.WaitAsync(ct);

    /// <summary>
    /// Sự kiện kích hoạt khi nhận được một WireEnvelope hợp lệ từ client.
    /// </summary>
    public event Action<WireEnvelope<JsonElement>, EndPoint?>? EnvelopeReceived;

    /// <summary>
    /// Sự kiện kích hoạt khi client gửi dữ liệu sai định dạng JSON hoặc vi phạm giao ước khung tin.
    /// </summary>
    public event Action<EndPoint?, string>? MalformedMessageRejected;

    /// <summary>
    /// Sự kiện kích hoạt khi có client mới kết nối.
    /// </summary>
    public event Action<EndPoint?>? ClientConnected;

    /// <summary>
    /// Sự kiện kích hoạt khi client ngắt kết nối.
    /// </summary>
    public event Action<EndPoint?>? ClientDisconnected;

    /// <summary>
    /// Khởi tạo CentralServer với cổng chỉ định (mặc định 5000 theo kiến trúc LabGuard).
    /// </summary>
    /// <param name="port">Cổng lắng nghe (5000 cho production, 0 cho dynamic port trong test).</param>
    /// <param name="bindAddress">Địa chỉ IP lắng nghe (mặc định IPAddress.Any).</param>
    public CentralServer(int port = 5000, IPAddress? bindAddress = null)
    {
        _port = port;
        _listener = new TcpListener(bindAddress ?? IPAddress.Any, port);
    }

    /// <summary>
    /// Bắt đầu lắng nghe và vòng lặp tiếp nhận các kết nối TCP client.
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _internalCts.Token);
        var token = linkedCts.Token;

        _listener.Start();
        IsRunning = true;
        _startedTcs.TrySetResult();
        Console.WriteLine("=== LABGUARD CENTRAL SERVER ===");
        Console.WriteLine($"[Central] May chu dang lang nghe tren cong TCP {Port}...");

        try
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                EndPoint? remoteEp = null;
                try
                {
                    remoteEp = client.Client.RemoteEndPoint;
                }
                catch { }

                Console.WriteLine($"\n[Central] [+] Thiet bi moi ket noi: {remoteEp}");
                ClientConnected?.Invoke(remoteEp);

                // Quản lý tác vụ xử lý client để dọn dẹp sạch sẽ khi dừng server
                var clientTask = Task.Run(() => HandleClientAsync(client, token), token);
                _activeClientTasks.TryAdd(clientTask, true);
                _ = clientTask.ContinueWith(t => _activeClientTasks.TryRemove(t, out _), TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[Central] May chu dang tat an toan...");
        }
        finally
        {
            IsRunning = false;
            try
            {
                _listener.Stop();
            }
            catch { }

            // Chờ các kết nối client hiện có hoàn tất an toàn trong thời gian cho phép
            if (!_activeClientTasks.IsEmpty)
            {
                try
                {
                    await Task.WhenAny(Task.WhenAll(_activeClientTasks.Keys), Task.Delay(2000)).ConfigureAwait(false);
                }
                catch { }
            }
            Console.WriteLine("[Central] May chu da dung.");
        }
    }

    /// <summary>
    /// Xử lý một luồng TCP client: đọc khung tin bằng FrameCodec.ReadEnvelopeAsync,
    /// hiển thị DeviceId và MessageType, xử lý lỗi ngắt kết nối an toàn không làm sập server.
    /// </summary>
    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        EndPoint? remoteEp = null;
        try
        {
            remoteEp = client.Client.RemoteEndPoint;
            using NetworkStream stream = client.GetStream();

            while (!ct.IsCancellationRequested)
            {
                WireEnvelope<JsonElement>? env;
                try
                {
                    env = await FrameCodec.ReadEnvelopeAsync(stream, ct: ct).ConfigureAwait(false);
                }
                catch (InvalidDataException ex)
                {
                    Console.WriteLine($"[Central] [!] Phat hien goi tin rac hoac sai dinh dang tu {remoteEp}: {ex.Message}");
                    MalformedMessageRejected?.Invoke(remoteEp, ex.Message);
                    break; // Đóng kết nối khi luồng nhị phân bị hỏng framing
                }
                catch (EndOfStreamException ex)
                {
                    Console.WriteLine($"[Central] [-] Thiet bi {remoteEp} ngat ket noi dot ngot giua frame: {ex.Message}");
                    break;
                }

                if (env == null)
                {
                    Console.WriteLine($"[Central] [-] Thiet bi {remoteEp} ngat ket noi hop le.");
                    break;
                }

                // Hiển thị DeviceId và MessageType theo yêu cầu B-001
                if (env.MessageType == MessageTypes.HelloAgent)
                {
                    Console.WriteLine($"[Central] Da nhan HELLO_AGENT tu may sinh vien: {env.DeviceId} (RequestId: {env.RequestId})");
                }
                else if (env.MessageType == MessageTypes.Login)
                {
                    Console.WriteLine($"[Central] Da nhan LOGIN tu quan tri vien: {env.DeviceId} (RequestId: {env.RequestId})");
                }
                else if (env.MessageType == MessageTypes.Heartbeat)
                {
                    Console.WriteLine($"[Central] Da nhan HEARTBEAT tu thiet bi: {env.DeviceId} (RequestId: {env.RequestId})");
                }
                else if (env.MessageType == MessageTypes.MetricBatch)
                {
                    Console.WriteLine($"[Central] Da nhan METRIC_BATCH tu thiet bi: {env.DeviceId} (RequestId: {env.RequestId})");
                }
                else
                {
                    Console.WriteLine($"[Central] Nhan tin: {env.MessageType} tu {env.DeviceId} (RequestId: {env.RequestId})");
                }

                EnvelopeReceived?.Invoke(env, remoteEp);
            }
        }
        catch (OperationCanceledException)
        {
            // Kết thúc bình thường khi máy chủ tắt
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Central] Loi mang tu {remoteEp}: {ex.Message}");
        }
        finally
        {
            ClientDisconnected?.Invoke(remoteEp);
            try
            {
                client.Close();
            }
            catch { }
        }
    }

    /// <summary>
    /// Dừng máy chủ và ngắt lắng nghe.
    /// </summary>
    public void Stop()
    {
        _internalCts.Cancel();
        try
        {
            _listener.Stop();
        }
        catch { }
    }

    /// <summary>
    /// Giải phóng tài nguyên và dừng máy chủ bất đồng bộ.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Stop();
        _internalCts.Dispose();
        await Task.CompletedTask;
    }
}