using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LabGuard.Tools.TcpEcho;

/// <summary>
/// Chương trình TCP Echo Server đơn giản (Nhiệm vụ LG-D-002).
/// Sử dụng TcpListener, TcpClient, NetworkStream theo kiến thức môn NT106 (Lập trình mạng).
/// Client gửi chuỗi bất kỳ thì Server gửi trả lại đúng chuỗi đó.
/// Xử lý an toàn khi Client ngắt kết nối mà không làm sập Server.
/// </summary>
public class Program
{
    private static readonly CancellationTokenSource _cts = new();

    public static async Task Main(string[] args)
    {
        int port = 5050;
        if (args.Length > 0 && int.TryParse(args[0], out int customPort))
        {
            port = customPort;
        }

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n[EchoServer] Đang dừng máy chủ...");
            _cts.Cancel();
        };

        await RunServerAsync(port, _cts.Token);
    }

    /// <summary>
    /// Khởi động TCP Echo Server tại cổng chỉ định.
    /// </summary>
    public static async Task RunServerAsync(int port, CancellationToken ct = default)
    {
        TcpListener listener = new(IPAddress.Any, port);

        try
        {
            listener.Start();
            int actualPort = ((IPEndPoint)listener.LocalEndpoint).Port;
            Console.WriteLine("==================================================");
            Console.WriteLine($"[EchoServer] TCP Echo Server đang lắng nghe tại cổng {actualPort}...");
            Console.WriteLine("[EchoServer] Nhấn Ctrl+C để dừng.");
            Console.WriteLine("==================================================");

            while (!ct.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // Xử lý mỗi client trên một task riêng biệt để hỗ trợ đa kết nối đồng thời
                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[EchoServer] Nhận tín hiệu dừng máy chủ.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EchoServer] Lỗi máy chủ: {ex.Message}");
        }
        finally
        {
            try
            {
                listener.Stop();
            }
            catch { }
            Console.WriteLine("[EchoServer] Máy chủ đã đóng.");
        }
    }

    /// <summary>
    /// Xử lý kết nối từ một Client: đọc chuỗi và gửi trả (Echo) lại chính xác chuỗi đó.
    /// Bắt các ngoại lệ mạng an toàn để không làm sập máy chủ khi client ngắt đột ngột.
    /// </summary>
    public static async Task HandleClientAsync(TcpClient client, CancellationToken ct = default)
    {
        EndPoint? remoteEp = null;
        try
        {
            remoteEp = client.Client.RemoteEndPoint;
            Console.WriteLine($"[EchoServer] [+] Client đã kết nối: {remoteEp}");

            using NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];

            while (!ct.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                }
                catch (IOException ex) when (ex.InnerException is SocketException se &&
                                            (se.SocketErrorCode == SocketError.ConnectionReset ||
                                             se.SocketErrorCode == SocketError.ConnectionAborted))
                {
                    Console.WriteLine($"[EchoServer] [-] Client {remoteEp} đã ngắt kết nối đột ngột: {se.SocketErrorCode}");
                    break;
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"[EchoServer] [-] Lỗi I/O từ {remoteEp}: {ex.Message}");
                    break;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"[EchoServer] [-] Lỗi Socket từ {remoteEp}: {ex.Message}");
                    break;
                }

                // bytesRead == 0 tương đương với client đóng kết nối (EOF / FIN)
                if (bytesRead == 0)
                {
                    Console.WriteLine($"[EchoServer] [-] Client {remoteEp} đã ngắt kết nối bình thường (EOF).");
                    break;
                }

                string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"[EchoServer] [Nhận từ {remoteEp} ({bytesRead} bytes)]: {text.TrimEnd()}");

                // Gửi trả lại chính xác những gì đã nhận (Echo)
                await stream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
                await stream.FlushAsync(ct).ConfigureAwait(false);
                Console.WriteLine($"[EchoServer] [Gửi trả {remoteEp} ({bytesRead} bytes)]: {text.TrimEnd()}");
            }
        }
        catch (OperationCanceledException)
        {
            // Máy chủ tắt, kết thúc êm đẹp
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EchoServer] [!] Lỗi xử lý client {remoteEp}: {ex.Message}");
        }
        finally
        {
            try
            {
                client.Close();
            }
            catch { }
            Console.WriteLine($"[EchoServer] [-] Đã giải phóng kết nối cho client: {remoteEp}");
        }
    }
}
