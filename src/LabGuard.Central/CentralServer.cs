using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LabGuard.Protocol;
using LabGuard.Protocol.Serialization; 

namespace LabGuard.Central
{
    public class CentralServer
    {
        private readonly TcpListener _listener;
        private readonly DatabaseManager _db;

        public CentralServer()
        {
            _listener = new TcpListener(IPAddress.Any, 5000);
            _db = new DatabaseManager(); 
        }

        public async Task StartAsync(CancellationToken ct)
        {
            _listener.Start();
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
            catch (OperationCanceledException) { }
            finally { _listener.Stop(); }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            try
            {
                using NetworkStream stream = client.GetStream();

                while (!ct.IsCancellationRequested)
                {
                    byte[]? frame = await FrameCodec.ReadFrameAsync(stream, ct: ct);
                    if (frame == null) break;

                    var env = ProtocolSerializer.DeserializeEnvelope(frame);

                    if (env != null && EnvelopeValidator.Validate(env).IsValid)
                    {
                        Console.WriteLine($"[Central] Nhan tin: {env.MessageType} tu {env.DeviceId}");

                        if (env.MessageType == MessageTypes.IncidentOpen)
                        {
                            await _db.SaveIncidentAsync(env.DeviceId, "CanhBaoCPU", "High");
                        }
                    }
                }
            }
            catch (Exception) { }
            finally { client.Close(); }
        }
    }
}