using System;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LabGuard.Protocol;

Console.Title = "FakeAgent - Mock Client";
Console.WriteLine("==========================================");
Console.WriteLine("       LABGUARD - FAKE AGENT CLIENT       ");
Console.WriteLine("==========================================");

int port = 6000;
using var client = new TcpClient();
Console.WriteLine($"[FakeAgent] Dang ket noi toi FakeCentral tai 127.0.0.1:{port}...");

try
{
    await client.ConnectAsync("127.0.0.1", port);
    Console.WriteLine("[FakeAgent] Ket noi thanh cong!\n");

    using var stream = client.GetStream();

    for (int i = 1; i <= 5; i++)
    {
        using var doc = JsonDocument.Parse($"{{\"sequence\": {i}, \"status\": \"ACTIVE\"}}");
        var heartbeat = WireEnvelope<JsonElement>.Create(
            messageType: MessageTypes.Heartbeat,
            deviceId: "AGENT-DEV-01",
            payload: doc.RootElement.Clone()
        );

        // 1. Dong khung 4 byte Big-Endian va gui qua stream
        byte[] frameBytes = FrameCodec.Encode(heartbeat);
        await stream.WriteAsync(frameBytes, CancellationToken.None);
        await stream.FlushAsync();
        Console.WriteLine($"[FakeAgent GUI #{i}] Loai: {heartbeat.MessageType} | ReqId: {heartbeat.RequestId}");

        // 2. Nhan phan hoi ACK tu Server
        var ack = await FrameCodec.ReadEnvelopeAsync(stream, ct: CancellationToken.None);
        if (ack != null)
        {
            Console.WriteLine($"[FakeAgent NHAN #{i}] Nhan ACK: {ack.MessageType} (RequestId: {ack.RequestId})\n");
        }

        await Task.Delay(2000);
    }

    Console.WriteLine("[FakeAgent] Da hoan thanh 5 luot gui. Nhan phim bat ky de thoat.");
    Console.ReadKey();
}
catch (Exception ex)
{
    Console.WriteLine($"[FakeAgent Loi]: {ex.Message}");
}