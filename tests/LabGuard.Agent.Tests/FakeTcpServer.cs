using System.Net;
using System.Net.Sockets;
using LabGuard.Protocol;

namespace LabGuard.Agent.Tests;

/// <summary>
/// Máy chủ TCP giả lập phục vụ riêng cho các bài kiểm thử Agent (A-003).
/// Chỉ lắng nghe trên giao tiếp loopback (127.0.0.1) với cổng ngẫu nhiên do hệ điều hành cấp.
/// Tuyệt đối không chứa mã nguồn sản xuất của Central Server.
/// </summary>
internal sealed class FakeTcpServer : IAsyncDisposable, IDisposable
{
    private readonly TcpListener _listener;
    private readonly List<TcpClient> _acceptedClients = new();
    private readonly object _lock = new();
    private bool _disposed;

    public FakeTcpServer()
    {
        // Gán IP 127.0.0.1 và cổng 0 để HĐH tự chọn cổng khả dụng
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
    }

    public string Host => "127.0.0.1";
    public int Port { get; }

    /// <summary>
    /// Đón nhận một kết nối từ Agent client.
    /// </summary>
    public async Task<TcpClient> AcceptClientAsync(CancellationToken ct = default)
    {
        var client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
        lock (_lock)
        {
            _acceptedClients.Add(client);
        }
        return client;
    }

    /// <summary>
    /// Đọc đúng N frames từ client sử dụng chuẩn đóng khung FrameCodec (A-002).
    /// </summary>
    public async Task<List<byte[]>> ReadFramesAsync(TcpClient client, int count, CancellationToken ct = default)
    {
        var stream = client.GetStream();
        var frames = new List<byte[]>(count);

        for (int i = 0; i < count; i++)
        {
            byte[]? payload = await FrameCodec.ReadFrameAsync(stream, ct: ct).ConfigureAwait(false);
            if (payload == null)
            {
                break; // Clean EOF
            }
            frames.Add(payload);
        }

        return frames;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            foreach (var client in _acceptedClients)
            {
                client.Dispose();
            }
            _acceptedClients.Clear();
        }

        _listener.Stop();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
