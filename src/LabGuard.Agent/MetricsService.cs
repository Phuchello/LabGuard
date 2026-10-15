using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;

namespace LabGuard.Agent;

/// <summary>
/// Dịch vụ định kỳ thu thập chỉ số CPU, RAM, Uptime và đóng gói gửi về Central Server (Task LG-A-004).
/// Chu kỳ chuẩn: 5 giây một lần (sử dụng PeriodicTimer).
/// Đóng gói payload chuẩn MetricBatchPayload và đẩy vào hàng đợi AgentSocketClient (Single-Writer).
/// </summary>
public sealed class MetricsService : IDisposable
{
    /// <summary>
    /// Chu kỳ thu thập mặc định theo đặc tả kiến trúc (5 giây).
    /// </summary>
    public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

    private readonly AgentSocketClient _client;
    private readonly IMetricsCollector _collector;
    private readonly string _deviceId;
    private readonly TimeSpan _period;

    private Task? _executingTask;
    private CancellationTokenSource? _cts;
    private bool _isDisposed;

    /// <summary>
    /// Mã định danh phiên khởi động (BootId).
    /// Đảm bảo tính nhất quán (stable) trong suốt vòng đời của Agent process/session,
    /// phục vụ cho cơ chế dedup và ReplayBuffer sau này (A-006).
    /// </summary>
    public string BootId { get; }

    /// <summary>
    /// Mã định danh máy trạm gửi bản tin.
    /// </summary>
    public string DeviceId => _deviceId;

    /// <summary>
    /// Chu kỳ thu thập đang áp dụng.
    /// </summary>
    public TimeSpan Period => _period;

    /// <summary>
    /// Khởi tạo dịch vụ thu thập số liệu định kỳ.
    /// </summary>
    /// <param name="client">Client quản lý socket TCP và hàng đợi gửi tuần tự (A-003).</param>
    /// <param name="collector">Bộ thu thập chỉ số hệ thống (A-004).</param>
    /// <param name="deviceId">Tên máy trạm.</param>
    /// <param name="period">Chu kỳ định kỳ (mặc định 5s).</param>
    /// <param name="bootId">Tùy chọn BootId (mặc định sinh GUID v4 duy nhất một lần).</param>
    public MetricsService(
        AgentSocketClient client,
        IMetricsCollector collector,
        string deviceId = "PC-AGENT-01",
        TimeSpan? period = null,
        string? bootId = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        _deviceId = string.IsNullOrWhiteSpace(deviceId) ? "PC-AGENT-01" : deviceId;
        _period = period ?? DefaultPeriod;
        BootId = string.IsNullOrWhiteSpace(bootId) ? Guid.NewGuid().ToString("D") : bootId;
    }

    /// <summary>
    /// Thu thập chỉ số hệ thống ngay lập tức, đóng gói METRIC_BATCH và đẩy vào hàng đợi của AgentSocketClient.
    /// </summary>
    /// <param name="ct">Token hủy tác vụ.</param>
    /// <returns>Đối tượng MetricBatchPayload chuẩn vừa được gửi đi.</returns>
    public async Task<MetricBatchPayload> SendMetricBatchAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        // 1. Thu thập các chỉ số từ collector
        double cpuPercent = _collector.ReadCpuPercent();
        MemorySnapshot memory = _collector.ReadMemoryBytes();
        long uptimeSeconds = _collector.ReadUptimeSeconds();

        // 2. Tính % RAM vật lý đã sử dụng: (Used / Total) * 100
        double ramPercent = memory.TotalPhysicalBytes > 0
            ? ((double)memory.UsedPhysicalBytes / memory.TotalPhysicalBytes) * 100.0
            : 0.0;
        ramPercent = Math.Clamp(ramPercent, 0.0, 100.0);

        // 3. Khởi tạo mốc thời gian UTC tường minh chung cho các mẫu đo trong chu kỳ này
        string recordedAtUtc = DateTime.UtcNow.ToString("O");

        // 4. Khởi tạo danh sách mẫu đo chuẩn (SampleCount = 2: CPU và RAM)
        var samples = new List<MetricSampleDto>
        {
            new()
            {
                MetricName = "CPU",
                MetricValue = Math.Round(cpuPercent, 2),
                Unit = "%",
                RecordedAtUtc = recordedAtUtc
            },
            new()
            {
                MetricName = "RAM",
                MetricValue = Math.Round(ramPercent, 2),
                Unit = "%",
                RecordedAtUtc = recordedAtUtc
            }
        };

        // 5. Đóng gói MetricBatchPayload theo đúng giao ước DTO
        var payload = new MetricBatchPayload
        {
            BootId = BootId,                           // BootId cố định trong suốt phiên làm việc của Agent
            BatchId = Guid.NewGuid().ToString("D"),    // BatchId sinh mới duy nhất cho từng mẻ
            SampleCount = samples.Count,
            UptimeSeconds = uptimeSeconds,
            IsReplay = false,                          // Chưa triển khai ReplayBuffer (A-006), luôn false trong A-004
            Samples = samples
        };

        // 6. Đặt vào phong bì chuẩn 6 trường và gửi qua AgentSocketClient
        var envelope = WireEnvelope<MetricBatchPayload>.Create(
            MessageTypes.MetricBatch,
            _deviceId,
            payload);

        await _client.SendEnvelopeAsync(envelope, ct).ConfigureAwait(false);

        return payload;
    }

    /// <summary>
    /// Bắt đầu vòng lặp định kỳ thu thập và gửi số liệu bằng PeriodicTimer.
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

                await SendMetricBatchAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Kích hoạt vòng lặp chạy ngầm trong background task.
    /// </summary>
    public void Start(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _executingTask = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
    }

    /// <summary>
    /// Dừng vòng lặp nền nếu đang chạy.
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
