using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LabGuard.Protocol; 
namespace LabGuard.Central
{
    public class CentralServer
    {
        private readonly TcpListener _listener;

        public CentralServer()
        {
            _listener = new TcpListener(IPAddress.Any, 5000);
        }

        public async Task StartAsync(CancellationToken ct)
        {
            _listener.Start();
            Console.WriteLine("=== LABGUARD CENTRAL SERVER ===");
            Console.WriteLine("[Central] May chu dang lang nghe tren cong TCP 5000...");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(ct);
                    Console.WriteLine($"\n[Central] [+] Thiet bi moi ket noi: {client.Client.RemoteEndPoint}");

                    _ = Task.Run(() => HandleClientAsync(client, ct), ct);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Central] May chu dang tat an toan...");
            }
            finally
            {
                _listener.Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            try
            {
                using NetworkStream stream = client.GetStream();

                while (!ct.IsCancellationRequested)
                {
                    byte[]? frame = await FrameCodec.ReadFrameAsync(stream, ct: ct);

                    if (frame == null)
                    {
                        Console.WriteLine($"[Central] [-] Thiet bi ngat ket noi.");
                        break;
                    }

                    var env = ProtocolSerializer.DeserializeEnvelope<JsonElement>(frame);

                    if (env != null && EnvelopeValidator.Validate(env).IsValid)
                    {
                        if (env.MessageType == MessageTypes.HelloAgent)
                        {
                            Console.WriteLine($"[Central] Da nhan HELLO_AGENT tu may sinh vien: {env.DeviceId}");
                        }
                        else if (env.MessageType == MessageTypes.Login)
                        {
                            Console.WriteLine($"[Central] Da nhan LOGIN tu quan tri vien: {env.DeviceId}");
                        }
                        else
                        {
                            Console.WriteLine($"[Central] Nhan tin: {env.MessageType} tu {env.DeviceId}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[Central] [!] Phat hien goi tin rac hoac sai dinh dang!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Central] Loi mang: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}