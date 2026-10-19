using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;

namespace LabGuard.Agent;

/// <summary>
/// Dịch vụ định kỳ phát tín hiệu nhịp tim (HEARTBEAT) từ Agent tới Central Server (Task LG-A-004).
/// Chu kỳ chuẩn: 5 giây một lần (sử dụng PeriodicTimer).
/// Tăng dần SequenceNumber một cách đơn điệu, lấy Uptime hiện tại và đẩy vào hàng đợi của AgentSocketClient.
/// Tuyệt đối KHÔNG đưa bản tin Heartbeat vào ReplayBuffer (theo quyết định kiến trúc CP2-003).
/// </summary>
public sealed class HeartbeatService : IDisposable
{
    /// <summary>
    /// Chu kỳ phát nhịp tim mặc định theo đặc tả kiến trúc (5 giây).
    /// </summary>
    public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

    private readonly AgentSocketClient _client;
    private readonly IMetricsCollector _collector;
    private readonly string _deviceId;
    private readonly TimeSpan _period;

    private long _sequenceNumber;
    private Task? _executingTask;
    private CancellationTokenSource? _cts;
    private bool _isDisposed;

    /// <summary>
    /// Số thứ tự nhịp tim hiện tại (bắt đầu từ 0 và tăng lên 1 ở lần gửi đầu tiên).
    /// </summary>
    public long CurrentSequenceNumber => Interlocked.Read(ref _sequenceNumber);

    /// <summary>
    /// Tên máy trạm gửi nhịp tim.
    /// </summary>
    public string DeviceId => _deviceId;

    /// <summary>
    /// Chu kỳ gửi nhịp tim.
    /// </summary>
    public TimeSpan Period => _period;

    /// <summary>
    /// Khởi tạo HeartbeatService.
    /// </summary>
    /// <param name="client">Client quản lý kết nối socket TCP và hàng đợi xuất tuần tự (A-003).</param>
    /// <param name="collector">Bộ thu thập thông tin hệ thống để lấy uptime (A-004).</param>
    /// <param name="deviceId">Tên máy trạm gửi heartbeat.</param>
    /// <param name="period">Chu kỳ định kỳ (mặc định 5s).</param>
    public HeartbeatService(
        AgentSocketClient client,
        IMetricsCollector collector,
        string deviceId = "PC-AGENT-01",
        TimeSpan? period = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        _deviceId = string.IsNullOrWhiteSpace(deviceId) ? "PC-AGENT-01" : deviceId;
        _period = period ?? DefaultPeriod;
    }

    /// <summary>
    /// Phát một bản tin nhịp tim ngay lập tức:
    /// - Tăng SequenceNumber đơn điệu
    /// - Lấy Uptime hiện tại từ collector
    /// - Đóng gói HeartbeatPayload và gửi qua hàng đợi AgentSocketClient
    /// </summary>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Đối tượng HeartbeatPayload chuẩn vừa được gửi đi.</returns>
    public async Task<HeartbeatPayload> SendHeartbeatAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        long seq = Interlocked.Increment(ref _sequenceNumber);
        long uptime = _collector.ReadUptimeSeconds();

        var payload = new HeartbeatPayload
        {
            SequenceNumber = seq,
            UptimeSeconds = uptime
        };

        var envelope = WireEnvelope<HeartbeatPayload>.Create(
            MessageTypes.Heartbeat,
            _deviceId,
            payload);

        await _client.SendEnvelopeAsync(envelope, ct).ConfigureAwait(false);

        return payload;
    }

    /// <summary>
    /// Vòng lặp định kỳ gửi nhịp tim sử dụng PeriodicTimer.
    /// </summary>
    /// <param name="ct">Token hủy tác vụ.</param>
    public async Task RunAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        using var timer = new PeriodicTimer(_period);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                bool tick = await timer.WaitForNextTickAsync(ct).ConfigureAwait(false);
                if (!tick)
                {
                    break;
                }

                await SendHeartbeatAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Kích hoạt vòng lặp phát nhịp tim trong background task.
    /// </summary>
    public void Start(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_cts != null)
        {
            throw new InvalidOperationException("Service is already started. Await StopAsync before starting again.");
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _cts.Token;
        _executingTask = Task.Run(() => RunAsync(token), token);
    }

    /// <summary>
    /// Dừng vòng lặp phát nhịp tim.
    /// </summary>
    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_executingTask != null)
            {
                try
                {
                    await _executingTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Hủy bình thường
                }
            }
            _cts.Dispose();
            _cts = null;
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
    }
}
