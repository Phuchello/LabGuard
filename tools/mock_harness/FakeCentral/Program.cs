using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LabGuard.Protocol;

Console.Title = "FakeCentral - Mock Server";
Console.WriteLine("==========================================");
Console.WriteLine("       LABGUARD - FAKE CENTRAL SERVER     ");
Console.WriteLine("==========================================");

int port = 6000;
var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();
Console.WriteLine($"[FakeCentral] Dang lang nghe tai 127.0.0.1:{port}...");

while (true)
{
    try
    {
        var client = await listener.AcceptTcpClientAsync();
        Console.WriteLine($"\n[FakeCentral] Ket noi moi tu: {client.Client.RemoteEndPoint}");

        _ = Task.Run(async () =>
        {
            using (client)
            using (var stream = client.GetStream())
            {
                while (true)
                {
                    try
                    {
                        // 1. Doc truc tiep va parse WireEnvelope tu stream qua FrameCodec
                        var envelope = await FrameCodec.ReadEnvelopeAsync(stream, ct: CancellationToken.None);
                        if (envelope == null) break;

                        Console.WriteLine($"[FakeCentral NHAN] Loai: {envelope.MessageType,-15} | Device: {envelope.DeviceId} | ReqId: {envelope.RequestId}");

                        // 2. Tao ban tin phan hoi ACK bang ham Create chuan cua WireEnvelope
                        using var doc = JsonDocument.Parse("{\"status\":\"SUCCESS\"}");
                        var ackEnvelope = WireEnvelope<JsonElement>.Create(
                            messageType: "HEARTBEAT_ACK",
                            deviceId: "CENTRAL-MOCK",
                            payload: doc.RootElement.Clone(),
                            requestId: envelope.RequestId
                        );

                        // 3. Dong khung va gui tra lai
                        byte[] ackFrame = FrameCodec.Encode(ackEnvelope);
                        await stream.WriteAsync(ackFrame, CancellationToken.None);
                        await stream.FlushAsync();
                        Console.WriteLine($"[FakeCentral GUI] -> Da gui phan hoi ACK cho RequestId: {envelope.RequestId}");
                    }
                    catch (EndOfStreamException)
                    {
                        Console.WriteLine("[FakeCentral] Agent da dong ket noi an toan.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FakeCentral Loi]: {ex.Message}");
                        break;
                    }
                }
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FakeCentral Listener Error]: {ex.Message}");
        break;
    }
}