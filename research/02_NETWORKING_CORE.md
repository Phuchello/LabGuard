# LABGUARD — NGHIÊN CỨU SÂU TẦNG GIAO VẬN & KỸ THUẬT LẬP TRÌNH TCP SOCKET
**Chuyên đề:** Networking Core — Socket, Byte Stream, Message Framing, Concurrency & Reliability  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Tác giả:** Lead Technical Researcher & Network Software Architect (Antigravity)  
**Trạng thái tài liệu:** Đã cập nhật sau vòng Audit của ChatGPT Work — Checkpoint 1  
**Tài liệu tham chiếu chuẩn (Tier 1):**
- IETF RFC 9293 §3.7: *Transmission Control Protocol (TCP) — Data Communication* (https://www.rfc-editor.org/rfc/rfc9293.html#section-3.7).
- IETF RFC 8259 §7: *The JavaScript Object Notation (JSON) Data Interchange Format — Strings* (https://www.rfc-editor.org/rfc/rfc8259.html#section-7).
- IETF RFC 1700 / STD 2: *Network Byte Order (Big-Endian)*.
- Microsoft Learn: `System.Net.Sockets.NetworkStream` (https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream).
- Microsoft Learn: `System.Net.Sockets.Socket.Connected` (https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.connected).
- Microsoft Learn: *How to: Make thread-safe calls to Windows Forms controls* (https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-make-thread-safe-calls).
- Microsoft Learn: `rowversion (Transact-SQL)` (https://learn.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql).
- UIT NT106: *Giáo trình Thực hành LTM căn bản* (NXB ĐHQG-HCM, 2022).
- `[REFERENCE]` UIT NT106: *Đề cương đáp án vấn đáp LTM tham khảo* (`van-dap-mon-lap-trinh-mang-1.pdf`).

---

## 1. BẢN CHẤT CỦA GIAO THỨC TCP (TRANSMISSION CONTROL PROTOCOL)

### 1.1. TCP cung cấp những gì? (RFC 9293 §3.7)
Theo RFC 9293, TCP là một giao thức ở tầng Giao vận (Transport Layer) cung cấp 4 đặc tính nền tảng:
1. **Connection-oriented (Hướng kết nối):** Trước khi truyền dữ liệu, hai đầu cuối phải bắt tay 3 bước (3-way handshake: SYN $\rightarrow$ SYN-ACK $\rightarrow$ ACK) để thiết lập trạng thái kết nối và đàm phán số thứ tự tuần tự ban đầu (Initial Sequence Number - ISN).
2. **Reliable (Tin cậy):** Mọi byte dữ liệu gửi đi đều được theo dõi bằng số thứ tự (Sequence Number). Bên nhận phản hồi bằng số xác nhận (Acknowledgment Number - ACK). Nếu bên gửi không nhận được ACK sau một khoảng thời gian chờ (Retransmission Timeout - RTO), dữ liệu sẽ được gửi lại tự động. Các gói tin trùng lặp sẽ bị loại bỏ ở tầng giao vận.
3. **Ordered (Có thứ tự):** Mạng IP định tuyến từng gói tin (packet) độc lập, các gói tin có thể đến đích lộn xộn (Out-of-order). TCP lưu tạm các phân đoạn đến trước vào bộ đệm nhận (Receive Buffer) và chỉ giao cho tầng ứng dụng dòng byte theo đúng thứ tự truyền ban đầu.
4. **Full-Duplex Byte Stream (Dòng byte hai chiều độc lập):** Hai bên có thể đồng thời vừa gửi vừa nhận dữ liệu qua cùng một kết nối socket.

---

## 2. VẤN ĐỀ CỐT LÕI: TCP LÀ BYTE STREAM, KHÔNG CÓ RANH GIỚI MESSAGE

### 2.1. Quy trình sư phạm: Tại sao một lần `ReadAsync` không tương ứng với một Message?

#### Bước 1: VẤN ĐỀ THỰC TẾ
Một sinh viên viết chương trình Agent gửi một bản tin JSON cấu hình máy về Central Server:
```json
{"version":1,"messageType":"METRICS_BATCH","requestId":"REQ-01","deviceId":"PC-01","sentAtUtc":"2026-09-13T07:00:00Z","isReplay":false,"payload":{"cpu":45.2,"ram":68.0}}
```
Ở Server, sinh viên viết:
```csharp
byte[] buffer = new byte[1024];
int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
var metric = JsonSerializer.Deserialize<EnvelopeDto>(json);
```
Khi chạy trên `localhost`, code có thể chạy bình thường vì độ trễ bằng 0 và kích thước dữ liệu nhỏ. Nhưng khi đưa ra mạng LAN hoặc chạy với nhiều kết nối đồng thời:
- Thỉnh thoảng Server báo lỗi `JsonException` vì chuỗi JSON bị thiếu ngoặc đóng (chỉ mới đọc được một nửa).
- Thỉnh thoảng Server báo lỗi `JsonException` vì đọc phải hai chuỗi JSON dính liền nhau: `{"version":1...}{"version":1...}`.

Sinh viên đặt câu hỏi: *"Tại sao em gửi nguyên một chuỗi JSON mà bên nhận lại đọc ra nửa chuỗi hoặc hai chuỗi dính nhau?"*

#### Bước 2: TRỰC GIÁC (ANALOGY)
Hãy tưởng tượng **TCP như một đường ống dẫn nước**, và dữ liệu của bạn là **những giọt nước màu xanh (Message 1) và đỏ (Message 2)**.
- Khi bạn đổ vào đầu ống một cốc nước màu xanh rồi đổ tiếp một cốc nước màu đỏ.
- Ở đầu ra, nước chảy thành một **dòng liên tục**. Chiếc cốc hứng của bạn (Receive Buffer) có thể:
  - Hứng được một nửa cốc nước màu xanh (nếu van ngắt sớm $\rightarrow$ hiện tượng **Partial Read**).
  - Hứng được một cốc chứa toàn bộ nước xanh lẫn một phần nước đỏ (hiện tượng **Packet Coalescing / Dính gói**).
Đường ống TCP chỉ đảm bảo chuyển từng giọt nước đầy đủ và đúng thứ tự; đường ống **hoàn toàn không có ranh giới của chiếc cốc**.

#### Bước 3: VÍ DỤ SỐ & BYTE STREAM MINH HỌA
Giả sử Agent gửi liên tiếp 2 message:
- **Message A (20 bytes):** `{"msg":"Heartbeat"}`
- **Message B (24 bytes):** `{"msg":"AlertCPUHigh"}`

Tầng IP và bộ đệm TCP của hệ điều hành gom/chia dữ liệu tùy thuộc vào MTU (thường 1500 bytes), tình trạng nghẽn mạng và thuật toán Nagle:

```
[Bên Gửi] ---- Gửi 20 bytes ----> [TCP Send Buffer] ---- Mạng IP ----> [TCP Recv Buffer]
          ---- Gửi 24 bytes ----> 
```

Tại Server, khi ứng dụng gọi `stream.ReadAsync(buffer, 0, 1024)`:
- **Tình huống 1 (Partial Read - Đọc thiếu):**
  Lần đọc 1 nhận 12 bytes: `{"msg":"Hear`
  Lần đọc 2 nhận tiếp 8 bytes: `tbeat"}`
- **Tình huống 2 (Concatenated Messages - Dính gói):**
  Lần đọc 1 nhận trọn vẹn cả 44 bytes:
  `{"msg":"Heartbeat"}{"msg":"AlertCPUHigh"}`

#### Bước 4: TÊN THUẬT NGỮ KỸ THUẬT
- **Byte Stream (Dòng byte):** Chuỗi tuần tự các byte không có ranh giới phân định thông điệp ở tầng giao vận.
- **Message Boundary (Ranh giới thông điệp):** Điểm đánh dấu bắt đầu và kết thúc của một đơn vị dữ liệu logic ở tầng ứng dụng.
- **Partial Read (Đọc một phần / Đọc dở dang):** Thao tác đọc trả về ít byte hơn một thông điệp ứng dụng hoàn chỉnh.
- **Packet Coalescing (Dính gói):** Nhiều thông điệp ứng dụng nằm nối tiếp nhau trong cùng một bộ đệm đọc.
- **Message Framing (Phân khung thông điệp):** Giao thức tầng ứng dụng định nghĩa quy tắc đóng gói để tách dòng byte thành các thông điệp độc lập.

---

## 3. SO SÁNH CÁC PHƯƠNG PHÁP FRAMING & QUYẾT ĐỊNH CỦA LABGUARD

### 3.1. Phân tích kỹ thuật các phương pháp Framing

| Tiêu chí | Delimiter-based (Ký tự phân tách, ví dụ `\n`) | Fixed-length (Kích thước cố định) | Length-prefix (Tiền tố độ dài) |
|:---|:---|:---|:---|
| **Cơ chế** | Thêm một ký tự đặc biệt ở cuối mỗi message (ví dụ: `\n`, `\0`, `\r\n`). | Mỗi message luôn có kích thước cố định đúng $K$ bytes. | Gửi $M$ byte biểu diễn độ dài $N$ của dữ liệu, ngay sau đó là $N$ byte payload. |
| **Tính hợp lệ với JSON** | **HOÀN TOÀN CÓ THỂ DÙNG ĐƯỢC** nếu JSON được serialize trên một dòng (single-line JSON). Theo **RFC 8259 §7**, ký tự xuống dòng bên trong chuỗi bắt buộc phải được escape thành `\n` (`\\n`). Do đó, nhận định "JSON chứa xuống dòng làm mọi giao thức newline đều sai" là **không chính xác**. | Khó áp dụng cho JSON vì kích thước JSON biến thiên liên tục; phải đệm padding lãng phí. | **Cực kỳ phù hợp**: Tách bạch kích thước payload độc lập với nội dung dữ liệu bên trong. |
| **Lý do LabGuard chọn Length-prefix** | Cần duyệt quét từng byte để tìm ký tự phân tách; nếu muốn mở rộng sang payload nhị phân (như Protocol Buffers, nén gzip) thì delimiter rất dễ vỡ. | Không hỗ trợ message có kích thước linh hoạt (telemetry batch thay đổi số lượng máy). | 1. **Contract nhị phân rõ ràng:** Biết trước chính xác kích thước $N$ để cấp phát bộ đệm một lần duy nhất.<br>2. **Kiểm soát parser tốt hơn:** Không cần quét tìm ký tự phân tách.<br>3. **Chống DoS chủ động:** Kiểm tra $N \leq MaxFrameSize$ trước khi cấp phát bộ nhớ. |

### 3.2. Cấu trúc Khung truyền LabGuard (Frame Specification)
Giao thức tầng ứng dụng LabGuard quy định:
- **Header:** 4 byte nguyên không dấu `uint32` theo trật tự byte mạng **Big-Endian** (RFC 1700), biểu diễn độ dài $N$ của phần Envelope.
- **Payload:** Đúng $N$ byte dữ liệu chuỗi JSON được mã hóa theo chuẩn **UTF-8**.
- **Lưu ý chuẩn xác:** $N$ **chỉ tính riêng kích thước của chuỗi JSON envelope UTF-8**, hoàn toàn KHÔNG tính 4 byte header tiền tố. Tổng số byte truyền trên đường dây là $4 + N$ bytes.

```
+-----------------------------+-------------------------------------------------------+
| 4 Bytes (uint32 Big-Endian) | N Bytes (JSON Envelope UTF-8)                        |
| Length N                    | {"version":1,"messageType":"...","sentAtUtc":"..."}   |
+-----------------------------+-------------------------------------------------------+
|<------- Header (4B) ------->|<------------------- Payload (N B) ------------------->|
```

### 3.3. Cấu trúc Envelope JSON chuẩn của LabGuard
Mọi thông điệp trao đổi giữa Agent, Central và Admin đều được bọc trong một cấu trúc Envelope thống nhất:
```json
{
  "version": 1,
  "messageType": "METRIC_BATCH",
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "deviceId": "PC-LAB1-07",
  "sentAtUtc": "2026-09-13T07:15:30.1234567Z",
  "payload": {
    "bootId": "a1b2c3d4-0000-0000-0000-000000000001",
    "batchId": 142,
    "isReplay": false,
    "cpuPercent": 85.5,
    "memoryUsedBytes": 8589934592,
    "memoryTotalBytes": 17179869184,
    "uptimeSeconds": 14250,
    "services": [
      { "port": 8080, "isAvailable": true, "responseTimeMs": 12 }
    ]
  }
}
```
- **`messageType`:** Bắt buộc sử dụng tên wire chuẩn theo danh mục viết hoa (Uppercase Enum) khôi phục 100% từ `03_PROTOCOL_SPEC.md`:
  - Telemetry & Liveness: `HEARTBEAT`, `HEARTBEAT_ACK`, `METRIC_BATCH`, `BATCH_ACK`, `SERVICE_STATUS`.
  - Authentication & Session: `LOGIN`, `LOGIN_RESULT`.
  - Subscription & Snapshot: `SUBSCRIBE_DEVICES`, `SUBSCRIBE_INCIDENTS`, `SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END`.
  - Realtime Events: `DEVICE_UPDATE`, `INCIDENT_OPEN`, `INCIDENT_UPDATE`, `HEALTH_UPDATE`.
  - Incident Actions & Management: `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`, `COMMAND_RESULT`.
  - *Tuyệt đối không dùng các tên tự sáng tác lệch spec như `LOGIN_REQ`, `LOGIN_RESP`, `INCIDENT_EVENT`, `METRIC_EVENT`, `INCIDENT_CMD_REQ`.*
- **`sentAtUtc`:** Bắt buộc dùng tên trường `sentAtUtc` theo chuẩn ISO-8601 UTC (ví dụ `YYYY-MM-DDTHH:mm:ss.fffffffZ`), **không dùng `timestamp`** để tránh nhầm lẫn với Unix epoch hoặc kiểu `rowversion`.
- **`isReplay`:** Nằm **bên trong payload** của `METRIC_BATCH` và `SERVICE_STATUS`, không nằm ở envelope ngoài. Mặc định là `false`. Khi mạng chập chờn, client gửi lại bản tin chưa nhận được ACK thì bật `isReplay: true` làm cờ gợi ý cho server.
- **Cơ chế Khử trùng lặp (Deduplication) & Lưu trữ bền vững (Persistence):**
  - **Batch Telemetry:** Khóa chống trùng lặp tại server là bộ ba: **`DeviceId + BootId + BatchId`**. Khóa này được lưu bền vững vào bảng SQL Server `ProcessedBatches` với TTL 24 giờ. Nếu nhận được batch trùng lặp, server bỏ qua bước ghi CSDL và gửi ngay `BATCH_ACK` xác nhận để Agent giải phóng hàng đợi replay.
  - **Command Mutating (Admin):** Khóa chống trùng lặp là **`UserId + CommandId`** đi kèm kiểm tra mã băm nội dung `RequestDigest` (SHA-256) được lưu bền vững vào bảng SQL Server `ProcessedCommands` với TTL 7 ngày để ngăn chặn thực thi lặp và phát hiện payload bị thay đổi.
  - **Phân định rạch ròi:** Ingest Ring Buffer (`System.Threading.Channels.Channel<T>`) chỉ là hàng đợi bộ nhớ RAM có giới hạn (Bounded Queue) để điều tiết áp lực ghi đĩa (backpressure) giữa mạng và CSDL, **không phải** nơi lưu dedup. Các bảng `AuditLogs` và `IncidentHistory` phục vụ truy vết kiểm toán con người, **không phải** bảng idempotency kỹ thuật.
  - **`requestId`:** Định danh duy nhất cho mỗi frame (UUID v4) dùng để đối soát tương quan (Correlation ID giữa Request và Response/ACK), **không thay thế** khóa khử trùng lặp nghiệp vụ.
  - **`isReplay`:** Là cờ chỉ báo hỗ trợ chẩn đoán, **không phải là khóa** chống trùng lặp.

### 3.4. Giới hạn an toàn `MaxFrameSize` (256 KiB) chống tấn công DoS
- **Vấn đề an ninh mạng (Denial of Service via RAM Exhaustion):** Nếu client độc hại gửi 4 byte độ dài giả mạo: `0x7F FF FF FF` (~2.1 GB). Nếu Server không kiểm tra và gọi `new byte[N]`, máy chủ sẽ ném `OutOfMemoryException` và sập toàn bộ dịch vụ.
- **Quy tắc LabGuard:**
  - Hằng số: `MaxFrameSize = 256 * 1024` (256 KiB).
  - Nếu $N == 0$ hoặc $N > MaxFrameSize$: Server từ chối ngay lập tức, ghi log cảnh báo an ninh và đóng kết nối socket mà không cấp phát bộ nhớ. Giúp bảo vệ tài nguyên bộ nhớ trước các frame bất thường.

---

## 4. HIỆN THỰC THUẬT TOÁN ĐỌC ĐỦ BYTES & KHẲNG ĐỊNH ĐÚNG VỀ `FrameCodec`

### 4.1. Đính chính khẳng định kỹ thuật về `FrameCodec`
> **[SỬA ĐỔI THEO AUDIT CHATGPT WORK]:**
> Bản nghiên cứu trước đây từng viết: *"loại bỏ 100% JsonException"*. Khẳng định này là **chưa chính xác**.
> **Khẳng định chuẩn xác:**
> - `FrameCodec` xử lý triệt để việc phân mảnh dữ liệu tầng giao vận (Partial Read và Concatenated Frames), đảm bảo khi trả về một mảng `byte[]`, mảng đó chứa chính xác $N$ byte của một frame hoàn chỉnh.
> - `FrameCodec` **KHÔNG THỂ** ngăn ngừa các lỗi: chuỗi JSON bên trong bị sai cú pháp (Malformed JSON), đối tác ngắt kết nối giữa chừng khi đang truyền dở $N$ byte (Unexpected EOF), frame length vượt ngưỡng an toàn, hoặc lỗi logic tuần tự hóa. Những trường hợp này ứng dụng vẫn phải bắt và xử lý bằng các khối `try-catch` chuyên biệt (`JsonException`, `EndOfStreamException`, `InvalidDataException`).

### 4.2. Mã nguồn minh họa `FrameCodec` (`[ILLUSTRATIVE CODE]`)

```csharp
public static class FrameCodec
{
    public const int HeaderSize = 4; // 4 bytes uint32
    public const int MaxFrameSize = 256 * 1024; // 256 KiB limit

    /// <summary>
    /// Đọc chính xác `count` bytes từ stream vào buffer.
    /// Lặp lại việc đọc cho đến khi nhận đủ số byte yêu cầu hoặc kênh truyền bị đóng (EOF).
    /// </summary>
    public static async Task ReadExactlyBytesAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int totalBytesRead = 0;
        while (totalBytesRead < count)
        {
            int bytesRead = await stream.ReadAsync(
                buffer.AsMemory(offset + totalBytesRead, count - totalBytesRead), 
                ct);

            if (bytesRead == 0)
            {
                // Khi yêu cầu đọc số byte > 0 mà ReadAsync trả về 0: Kênh truyền đã đóng (FIN)
                throw new EndOfStreamException(
                    $"Kênh truyền bị đóng bất ngờ khi đang đọc frame. Đã nhận {totalBytesRead}/{count} bytes.");
            }

            totalBytesRead += bytesRead;
        }
    }

    /// <summary>
    /// Đọc một frame hoàn chỉnh từ stream.
    /// Trả về null nếu kênh truyền được đóng êm đềm (Graceful EOF) ngay trước frame mới.
    /// </summary>
    public static async Task<byte[]?> ReadFrameAsync(Stream stream, CancellationToken ct)
    {
        byte[] headerBuffer = new byte[HeaderSize];

        // 1. Đọc byte đầu tiên của header.
        int initialRead = await stream.ReadAsync(headerBuffer.AsMemory(0, HeaderSize), ct);
        if (initialRead == 0)
        {
            return null; // Graceful EOF từ đối tác
        }

        // Đọc cho đủ 4 byte nếu xảy ra partial read trên header
        if (initialRead < HeaderSize)
        {
            await ReadExactlyBytesAsync(stream, headerBuffer, initialRead, HeaderSize - initialRead, ct);
        }

        // 2. Giải mã độ dài N theo chuẩn Big-Endian
        uint payloadLength = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(headerBuffer);

        // 3. Kiểm tra tính hợp lệ và bounds check chống DoS
        if (payloadLength == 0)
        {
            throw new InvalidDataException("Frame không hợp lệ: Độ dài payload bằng 0.");
        }
        if (payloadLength > MaxFrameSize)
        {
            throw new InvalidDataException(
                $"Frame vượt kích thước tối đa: {payloadLength} bytes > {MaxFrameSize} bytes.");
        }

        // 4. Đọc chính xác N byte payload
        byte[] payloadBuffer = new byte[payloadLength];
        await ReadExactlyBytesAsync(stream, payloadBuffer, 0, (int)payloadLength, ct);

        return payloadBuffer;
    }

    /// <summary>
    /// Đóng gói và ghi một frame ra stream với tiền tố 4 byte Big-Endian.
    /// </summary>
    public static async Task WriteFrameAsync(Stream stream, byte[] payload, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Length == 0)
        {
            throw new ArgumentException("Payload không được rỗng.", nameof(payload));
        }
        if (payload.Length > MaxFrameSize)
        {
            throw new ArgumentException($"Payload vượt quá giới hạn {MaxFrameSize} bytes.", nameof(payload));
        }

        byte[] frame = new byte[HeaderSize + payload.Length];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(0, HeaderSize), (uint)payload.Length);
        Buffer.BlockCopy(payload, 0, frame, HeaderSize, payload.Length);

        await stream.WriteAsync(frame.AsMemory(), ct);
        await stream.FlushAsync(ct);
    }
}
```

---

## 5. CÁC NGUYÊN LÝ ĐỒNG THỜI & TIN CẬY (CONCURRENCY & RELIABILITY)

### 5.1. Mẫu thiết kế Single Writer per Socket
- **Tài liệu Microsoft Learn chính thức:**
  > *"Read and write operations can be performed simultaneously on an instance of the NetworkStream class... but simultaneous read or simultaneous write operations are not allowed."* (https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream)
- **Bản chất kỹ thuật:**
  - `NetworkStream` không hỗ trợ nhiều thao tác ghi đồng thời. Nếu nhiều `Task` (ví dụ luồng gửi Heartbeat và luồng gửi Telemetry) cùng gọi `WriteAsync` trên một socket, điều này tạo ra **tranh chấp truy cập (race condition)** trên dòng dữ liệu ngầm, dẫn đến nguy cơ các mẩu byte của các frame khác nhau bị ghi xen kẽ (interleaved), làm sai lệch framing ở phía nhận.
  - Cần phân biệt rõ: Một `Task` trong C# là một đơn vị công việc bất đồng bộ (asynchronous work item), được thực thi trên ThreadPool của .NET runtime, hoàn toàn **không đồng nhất với một hệ điều hành Thread (OS Thread)**.
- **Giải pháp:**
  - Mỗi kết nối socket duy trì một vòng lặp ghi bất đồng bộ duy nhất (`Single Writer Loop`).
  - Sử dụng hàng đợi có giới hạn (`Bounded Channel`):
    ```csharp
    var options = new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait, // Áp dụng backpressure: Chờ khi hàng đợi đầy
        SingleReader = true,                   // Tối ưu hóa: Chỉ có 1 writer loop đọc
        SingleWriter = false                   // Nhiều task có thể cùng đẩy frame vào hàng đợi
    };
    Channel<byte[]> _outgoingChannel = Channel.CreateBounded<byte[]>(options);
    ```
  - Giới hạn kích thước hàng đợi ngăn chặn hiện tượng rò rỉ bộ nhớ RAM (Memory Leak) khi mạng bị tắc nghẽn.

### 5.2. Bản chất cờ `Socket.Connected` và `Read` trả về `0`
- **`Socket.Connected`:**
  - Theo Microsoft Learn (https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.connected), thuộc tính `Connected` chỉ phản ánh trạng thái của kết nối socket **tính đến thời điểm của thao tác I/O gần nhất**.
  - Nếu đối tác bị mất nguồn, đứt cáp mạng hoặc sập tiến trình đột ngột, cờ `Connected` phía Server vẫn trả về `true` cho đến khi có một thao tác `Send` hoặc `Receive` thất bại.
  - Do đó, `Socket.Connected` **hoàn toàn không phải là bằng chứng xác thực liveness hiện tại** của Agent.
- **`ReadAsync` trả về giá trị `0`:**
  - Chỉ có ý nghĩa báo hiệu đối tác đã thực hiện đóng kết nối êm đẹp (Graceful Close qua cờ TCP FIN) khi ứng dụng yêu cầu đọc số byte $> 0$.
  - Cần phân biệt rõ với các trường hợp lỗi mạng đột ngột (ném ra `SocketException`), timeout (ném ra `TimeoutException`), hoặc tác vụ bị hủy (ném ra `OperationCanceledException`).

### 5.3. Quy chuẩn phát hiện Offline & Phục hồi (Liveness Rules)
Đồng bộ chuẩn xác với Blueprint gốc:
- **Chu kỳ gửi Heartbeat:** Mỗi **5 giây** một lần.
- **Quy tắc phát hiện Offline:** Central Server đánh giá liveness dựa trên đồng hồ đơn điệu (Monotonic Clock, dùng `Stopwatch.GetTimestamp()` hoặc `Environment.TickCount64` để tránh sai lệch khi đổi giờ hệ thống NTP). Một thiết bị bị coi là `OFFLINE` khi:
  $$\text{Thời gian trôi qua kể từ Heartbeat hợp lệ gần nhất} > 20 \text{ giây}$$
- **Bộ quét định kỳ (Liveness Scheduler Tick):** Thiết kế quét mỗi $\leq 1$ giây (`[DESIGN TARGET]`).
- **Thời gian chuyển đổi dự phòng Collector Failover (P1):** Chỉ tiêu thiết kế đạt p95 $\leq 45$ giây (`[DESIGN TARGET]`, chưa đo thực nghiệm).
  - *Kiến trúc P1 Collector:* Sinh viên C phát triển duy nhất một ứng dụng Collector (`LabGuard.Collector.exe`); Sinh viên A triển khai 02 instances của binary này trên các cổng khác nhau (5001, 5002) sau L4 Gateway để kiểm thử cân bằng tải và failover.
- **Phân biệt rạch ròi 4 mốc thời gian:**
  1. *Detection Time (Thời gian phát hiện lỗi):* 20 giây mất heartbeat.
  2. *Reconnect Time (Thời gian Agent kết nối lại):* Thời gian lùi lũy tiến (Exponential backoff).
  3. *Recovery Time (Thời gian phục hồi dịch vụ):* Đủ số mẫu trạng thái tốt để đóng incident.
  4. *Failover Time (Thời gian chuyển sang server khác):* Đứt kết nối Collector $\rightarrow$ ngắt session cũ $\rightarrow$ kết nối node mới.

### 5.4. Quy chuẩn phát hiện sự cố CPU & Service (Rule Engine)
Đồng bộ chuẩn xác với Blueprint gốc:
- **Chu kỳ lấy mẫu telemetry:** **5 giây / lần**.
- **Kích hoạt sự cố `HIGH_CPU`:** Khi mức sử dụng CPU $\geq 90\%$ trong **3 mẫu hợp lệ liên tiếp** (khoảng thời gian giữa mẫu đầu và mẫu cuối là **10 giây**, tương ứng các mốc $t=0\text{s}, 5\text{s}, 10\text{s}$; không diễn giải là "15 giây liên tục").
- **Phục hồi sự cố (Recovery):** Khi mức sử dụng CPU $\leq 75\%$ trong **3 mẫu hợp lệ liên tiếp** (trải qua 10 giây).
- **Trực giác kỹ thuật:** Vùng trễ giữa 75% và 90% (khoảng cách 15% Deadband) ngăn ngừa hiện tượng **Alert Flapping** (cảnh báo bật/tắt liên hồi) khi CPU dao động quanh ngưỡng 90%.
- **Sự cố cổng dịch vụ `SERVICE_DOWN`:** Kích hoạt sau **2 lần thăm dò (TCP probe) thất bại liên tiếp** (timeout 2 giây/lần, chu kỳ 5 giây); tự động chuyển sang trạng thái phục hồi sau **2 lần thăm dò thành công liên tiếp**.

### 5.5. Kiến trúc phân tách trạng thái hai lớp (Dual-State Architecture)
Hệ thống phân tách rạch ròi giữa trạng thái vật lý/kỹ thuật và quy trình can thiệp của quản trị viên:
1. **`ConditionState` (Trạng thái điều kiện kỹ thuật do Rule Engine tự động đánh giá):**
   - `ACTIVE`: Điều kiện bất thường đang diễn ra (ví dụ CPU $\ge 90\%$, cổng dịch vụ sập, mất heartbeat).
   - `RECOVERED`: Điều kiện bất thường đã kết thúc (ví dụ CPU $\le 75\%$, cổng dịch vụ mở lại).
   - `UNKNOWN`: Mất kết nối telemetry hoặc không đủ dữ liệu đánh giá.
2. **`WorkflowState` (Trạng thái quy trình công việc do Admin/Operator thao tác):**
   - `OPEN`: Sự cố mới mở, chưa có người nhận trách nhiệm.
   - `ACKNOWLEDGED`: Admin/Operator đã xem và ghi nhận sự cố.
   - `IN_PROGRESS`: Đang tiến hành xử lý kỹ thuật.
   - `RECOVERED`: Hệ thống tự động ghi nhận máy trạm đã trở lại bình thường nhưng chờ Admin nghiệm thu.
   - `RESOLVED`: Admin xác nhận hoàn tất xử lý và đóng phiếu sự cố.
- **Ý nghĩa:** Tách hai biến độc lập giúp hệ thống xử lý trơn tru các ca biên: máy tự phục hồi trước khi Admin kịp bấm ACK, hoặc lỗi tái phát (`relapse`) khi phiếu đang trong tiến trình xử lý (`IN_PROGRESS`).

---

## 6. SỰ AN TOÀN ĐA LUỒNG TRÊN GIAO DIỆN WINFORMS

- **Nguyên lý UI Thread:** Windows Forms hoạt động theo kiến trúc Single Threaded Apartment (STA) với vòng lặp thông điệp Win32 (Message Loop). Mọi thao tác truy cập trực tiếp vào thuộc tính của Control từ một Background Task / Socket Thread đều vi phạm an toàn luồng.
- **Cách xử lý chuẩn xác:**
  - Không khẳng định chỉ có 2 API. Tùy thuộc vào phiên bản .NET:
    * `Control.Invoke`: Gọi đồng bộ, chặn luồng gọi cho đến khi UI thread xử lý xong.
    * `Control.BeginInvoke`: Gọi bất đồng bộ (fire-and-forget), đẩy thông điệp vào hàng đợi UI mà không chặn luồng mạng.
    * `Control.InvokeAsync` (.NET 9+): Hỗ trợ lập trình `await` bất đồng bộ hoàn toàn trên UI thread.
  - **Lưu ý về `CheckForIllegalCrossThreadCalls = false`:** Lệnh này chỉ tắt cơ chế ném ngoại lệ `InvalidOperationException` ở chế độ Debug của .NET runtime. Nó **hoàn toàn không làm cho code trở nên an toàn luồng**. Các tranh chấp truy cập trên bộ nhớ đồ họa GDI/User32 vẫn xảy ra và có thể gây treo giao diện hoặc lỗi hiển thị ngẫu nhiên.

---

## 7. BẢN CHẤT KIỂU DỮ LIỆU `rowversion` TRONG SQL SERVER

- Theo tài liệu Microsoft Learn (https://learn.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql):
  - `rowversion` (tên cũ là `timestamp`) có kích thước cố định **8 byte**.
  - Nó **hoàn toàn không liên quan đến ngày giờ hay thời gian**.
  - Mỗi khi một dòng trong bảng được `INSERT` hoặc `UPDATE` (kể cả khi update với cùng giá trị cũ), SQL Server sẽ tự động gán cho cột `rowversion` một giá trị nhị phân tự tăng duy nhất mới trong phạm vi toàn bộ cơ sở dữ liệu.
  - Kiểu dữ liệu `binary(8)` thông thường **không có tính năng tự tăng này**.
- **Ứng dụng trong LabGuard:** Áp dụng cho cơ chế **Optimistic Concurrency Control (OCC)**:
  ```sql
  UPDATE Incidents 
  SET Status = 'ACKNOWLEDGED', AcknowledgedBy = @AdminId
  WHERE IncidentId = @IncidentId AND RowVersion = @OldRowVersion;
  ```
  Nếu có 2 Admin cùng bấm nút: Admin nào đến trước sẽ cập nhật thành công và đổi `rowversion`. Admin đến sau với `OldRowVersion` cũ sẽ nhận về `0 rows affected`, Server phát hiện xung đột và trả mã lỗi `CONFLICT`.

---

## 8. BỘ 5 KỊCH BẢN KIỂM THỬ GIAO THỨC (FRAMING TEST SUITE)

> **Lưu ý đối soát (Audit Notice):**
> Bộ 5 test case dưới đây đã được tác giả (Antigravity) lập trình và chạy kiểm thử bằng mã nguồn C# độc lập tại thư mục `scratch/test_frame_codec/Program.cs` (.NET SDK 9.0.318).
> Kết quả **5/5 PASSED** được báo cáo là kết quả chạy thực nghiệm phía tác giả, được cung cấp kèm mã nguồn đầy đủ và log lệnh để Auditor (ChatGPT Work) có thể sao chép và tái lập độc lập.
> Nhóm không đưa ra các tuyên bố mang tính tuyệt đối hóa không có căn cứ (như "chính xác tuyệt đối", "chống mọi hình thức DoS", "ổn định tuyệt đối"). Mục tiêu là kiểm chứng logic xử lý framing, partial read và coalescing trong các điều kiện kiểm thử giả lập.


```
+-----------------------------------------------------------------------------------------+
| TEST TC-NET-01: Partial Read trên Header (4 byte độ dài bị chia thành 2 lần đọc)       |
+-----------------------------------------------------------------------------------------+
| SETUP    | Dùng NetworkStream giả lập hoặc MemoryStream phân đoạn. Frame dài 120 bytes. |
| ACTION   | Phía nhận ép luồng chỉ trả 2 byte đầu tiên của header, sau đó trả nốt 2 byte |
|          | còn lại của header, rồi trả 120 byte payload JSON.                           |
| EXPECTED | FrameCodec ghép đủ 4 byte, giải mã ra đúng số 120, đọc trọn vẹn 120 byte.    |
| EVIDENCE | Test case trong xUnit test suite (xem TestHarness bên dưới).                 |
+-----------------------------------------------------------------------------------------+

+-----------------------------------------------------------------------------------------+
| TEST TC-NET-02: Partial Read trên Payload (Dữ liệu JSON bị phân mảnh nhiều lần đọc)   |
+-----------------------------------------------------------------------------------------+
| SETUP    | Gói tin JSON 200 bytes được chia thành 3 mẩu: 50 bytes, 80 bytes, 70 bytes.  |
| ACTION   | Phía nhận gọi `ReadFrameAsync`.                                              |
| EXPECTED | Hàm lặp đọc 3 lần cho đến khi đủ 200 bytes; JSON deserializer parse thành    |
|          | công không ném JsonException.                                                |
| EVIDENCE | Test case trong xUnit test suite (xem TestHarness bên dưới).                 |
+-----------------------------------------------------------------------------------------+

+-----------------------------------------------------------------------------------------+
| TEST TC-NET-03: Concatenated Frames (Hai frame nằm liền kề trong cùng 1 lần đọc buffer)|
+-----------------------------------------------------------------------------------------+
| SETUP    | Ghi liền 2 frame vào Stream: Frame A (50 bytes) + Frame B (80 bytes).        |
| ACTION   | Bộ đệm đọc nhận 130 bytes cùng lúc. Gọi `ReadFrameAsync` lần 1, rồi lần 2.   |
| EXPECTED | Lần 1 trả về đúng 50 byte của Frame A; lần 2 đọc tiếp đúng 80 byte Frame B.  |
| EVIDENCE | Test case trong xUnit test suite (xem TestHarness bên dưới).                 |
+-----------------------------------------------------------------------------------------+

+-----------------------------------------------------------------------------------------+
| TEST TC-NET-04: Malformed & Oversized Frame (Độ dài bằng 0 hoặc vượt quá 256 KiB)      |
+-----------------------------------------------------------------------------------------+
| SETUP    | Client giả mạo gửi header có độ dài N = 500,000 bytes (> 256 KiB).           |
| ACTION   | Phía nhận gọi `ReadFrameAsync`.                                              |
| EXPECTED | Ném InvalidDataException ngay tại bước đọc header, KHÔNG cấp phát 500 KB RAM.|
| EVIDENCE | Test case trong xUnit test suite (xem TestHarness bên dưới).                 |
+-----------------------------------------------------------------------------------------+

+-----------------------------------------------------------------------------------------+
| TEST TC-NET-05: Truncated Stream / Mất kết nối khi đang đọc dở Payload                 |
+-----------------------------------------------------------------------------------------+
| SETUP    | Header báo frame dài 100 bytes, nhưng sau 30 bytes stream bị ngắt (EOF).     |
| ACTION   | Hàm `ReadExactlyBytesAsync` tiếp tục chờ đọc 70 byte còn lại.                |
| EXPECTED | ReadAsync trả về 0; ném EndOfStreamException báo rõ thiếu 70 byte.           |
| EVIDENCE | Test case trong xUnit test suite (xem TestHarness bên dưới).                 |
+-----------------------------------------------------------------------------------------+
```

---

## 9. CÁC CÂU HỎI VẤN ĐÁP CỐT LÕI (VIVA DEFENSE QUESTIONS)

1. **Thầy/Cô hỏi:** *"Tại sao em không dùng ký tự xuống dòng `\n` để kết thúc message cho đơn giản giống bài thực hành Lab 3 trên lớp?"*
   - **Trả lời:** Dạ thưa Thầy/Cô, về mặt lý thuyết, nếu chuẩn hóa JSON trên một dòng thì ký tự `\n` bên trong chuỗi string đã được escape thành `\n` theo RFC 8259 §7 nên delimiter vẫn có thể hoạt động được. Tuy nhiên, nhóm em chọn **Length-Prefix (4-byte uint32 Big-Endian)** vì:
     1. Length-prefix cung cấp một contract nhị phân tường minh: bên nhận biết trước chính xác độ dài $N$ để cấp phát bộ đệm một lần duy nhất, tránh việc phải quét duyệt từng byte trong RAM để tìm ký tự kết thúc.
     2. Cho phép kiểm soát an ninh chủ động: nếu $N > 256\text{ KiB}$, server từ chối ngay lập tức để chống tấn công cạn kiệt bộ nhớ DoS.
     3. Dễ dàng mở rộng trong tương lai nếu nhóm muốn chuyển sang truyền dữ liệu nhị phân hoặc nén dữ liệu mà không lo bị trùng ký tự delimiter.

2. **Thầy/Cô hỏi:** *"Nếu mạng chập chờn, 4 byte độ dài header bị chia làm hai lần đọc (mỗi lần 2 byte) thì code của em xử lý thế nào?"*
   - **Trả lời:** Dạ, trong hàm `FrameCodec.ReadFrameAsync`, nhóm không gọi `ReadAsync` một lần rồi parse ngay. Thay vào đó, nhóm dùng hàm phụ trợ `ReadExactlyBytesAsync` với vòng lặp `while (totalBytesRead < count)`. Nếu lần đầu hệ điều hành chỉ trả về 2 byte, biến `totalBytesRead` ghi nhận 2 và vòng lặp tiếp tục gọi `ReadAsync` để nhận nốt 2 byte còn lại vào đúng vị trí offset của mảng 4-byte header trước khi gọi `BinaryPrimitives.ReadUInt32BigEndian`.

3. **Thầy/Cô hỏi:** *"Tại sao `socket.Connected` phía Server vẫn bằng `true` khi máy Agent đã bị rút dây mạng?"*
   - **Trả lời:** Dạ vì theo tài liệu của Microsoft, thuộc tính `Socket.Connected` chỉ phản ánh trạng thái của socket tại lần thao tác I/O gần nhất chứ không gửi gói tin thăm dò ra đường truyền theo thời gian thực. Khi máy Agent rút cáp, tầng TCP chưa kịp trao đổi gói tin FIN hay RST. Do đó, phía Server phải dùng cơ chế **Heartbeat tầng ứng dụng**: nếu quá 20 giây kể từ nhịp tim hợp lệ gần nhất (tính theo đồng hồ đơn điệu) mà Server không nhận được gói tin mới thì Server sẽ chủ động đánh dấu Agent là `OFFLINE`.
