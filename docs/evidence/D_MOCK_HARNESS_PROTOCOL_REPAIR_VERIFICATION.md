# BẰNG CHỨNG KIỂM THỬ VÀ NGHIỆM THU: MOCK HARNESS PROTOCOL REPAIR

- **Nhiệm vụ:** `LG-D — Sửa đổi và chuẩn hóa Mock Harness (FakeAgent & FakeCentral) theo Frozen Protocol`
- **Môn học:** NT106 — Lập trình mạng (Đồ án giám sát phòng máy LabGuard)
- **Thời điểm kiểm thử:** 15/10/2026
- **Thành viên phụ trách:** Thành viên D (Scale & Test Harness)
- **Vị trí mã nguồn:**
  - `tools/mock_harness/FakeAgent/Program.cs`
  - `tools/mock_harness/FakeCentral/Program.cs`
  - `tests/LabGuard.MockHarness.Tests/MockHarnessTests.cs`

---

## 1. Tóm tắt các vấn đề đã sửa đổi

1. **Khắc phục cấu trúc gói tin của FakeAgent:**
   - Thay thế hoàn toàn JSON payload thủ công chứa `sequence` và `status`.
   - Sử dụng DTO chính thức `HeartbeatPayload` với `SequenceNumber` và `UptimeSeconds`.
   - Đóng gói và tuần tự hóa qua `WireEnvelope<HeartbeatPayload>` và `FrameCodec.Encode()`.
   - Đọc phản hồi `HEARTBEAT_ACK` bằng `FrameCodec.ReadEnvelopeAsync()`.
   - **Khắc phục triệt để lỗi P1 (Missing Field Masking do Default Initializer):**
     - Thẩm định trực tiếp đối tượng `JsonElement` thô của payload **TRƯỚC KHI** deserialize vào `HeartbeatAckPayload`.
     - Bắt buộc trường `serverTimeUtc` phải tồn tại tường minh trong JSON, có kiểu String, không rỗng và là định dạng ISO-8601 UTC hợp lệ (`Z` hoặc `+00:00`).
     - Bắt buộc trường `acknowledgedSequence` phải tồn tại tường minh trong JSON, có kiểu Number số nguyên không âm (`>= 0`).
     - Từ chối ngay lập tức mọi payload thiếu trường, null, sai kiểu hoặc sai sequence trước khi chạm vào DTO (loại bỏ hoàn toàn trường hợp payload `{"acknowledgedSequence":1}` được chấp nhận do giá trị khởi tạo mặc định của C#).
   - Loại bỏ `Console.ReadKey()` để tránh treo các kịch bản kiểm thử tự động.
   - Thêm cơ chế timeout hữu hạn (Linked Token Source) để không bị treo vĩnh viễn khi thiếu ACK.

2. **Khắc phục xử lý gói tin của FakeCentral:**
   - Đọc toàn bộ gói tin qua `FrameCodec.ReadEnvelopeAsync()`, tự động kiểm tra tính hợp lệ của phong bì 6 trường theo chuẩn wire contract.
   - Chỉ phản hồi `HEARTBEAT_ACK` đối với bản tin `HEARTBEAT`.
   - Kiểm tra rõ ràng sự tồn tại và kiểu số hợp lệ của `sequenceNumber` và `uptimeSeconds` trước khi deserialize.
   - Sử dụng `HeartbeatAckPayload` với `MessageTypes.HeartbeatAck`.
   - Gán `acknowledgedSequence` bằng đúng `sequenceNumber` nhận được và `serverTimeUtc` bằng thời gian hiện tại của máy chủ.
   - Giữ nguyên `requestId` của bản tin gửi lên để client đối soát (request/response correlation).
   - Với các loại bản tin không hỗ trợ hoặc payload sai, ghi log và **tuyệt đối không gửi ACK thành công giả mạo**.
   - Bắt và xử lý an toàn mọi ngoại lệ ngắt kết nối (EOF, ConnectionReset), frame sai (`InvalidDataException`), và hỗ trợ dừng server êm đẹp (Graceful Shutdown).

3. **Tương thích dự án và môi trường:**
   - Cả hai dự án `FakeAgent.csproj` và `FakeCentral.csproj` đã được hạ target từ `net10.0` về `net9.0`, đồng bộ 100% với môi trường chung của toàn bộ solution LabGuard.
   - Tích hợp đầy đủ vào `LabGuard.sln` mà không loại bỏ `TcpEcho` hay bất kỳ dự án nào khác.

---

## 2. Kết quả kiểm thử tự động (LabGuard.MockHarness.Tests — 15/15 PASS)

Bộ kiểm thử tự động xUnit trong `tests/LabGuard.MockHarness.Tests/MockHarnessTests.cs` bao phủ 15 kịch bản độc lập (bao gồm 9 kịch bản nghiệp vụ chính và 6 kịch bản kiểm tra dị dạng payload ACK):

| STT | Kịch bản kiểm thử | Mô tả & Cách thức xác minh | Kết quả |
|:---:|---|---|:---:|
| 1 | **Valid HEARTBEAT -> correct HEARTBEAT_ACK** | Gửi `HeartbeatPayload` hợp lệ (`Seq: 42, Uptime: 1234`). FakeCentral phản hồi `HeartbeatAckPayload` với đúng `RequestId`, `AckSeq: 42`, và `ServerTimeUtc` chuẩn UTC. | **PASS** |
| 2 | **Five sequential heartbeat requests** | Chạy `FakeAgent` gửi 5 heartbeat tuần tự tới `FakeCentral`. Tất cả 5 lượt đều nhận ACK hợp lệ, hoàn thành chu trình. | **PASS** |
| 3 | **Two simultaneous TCP clients** | Client A kết nối gửi Heartbeat và giữ kết nối mở; Client B kết nối và nhận ACK tức thì mà không bị chặn; Client A tiếp tục gửi lượt 2 thành công. | **PASS** |
| 4 | **Multiple length-prefixed frames coalesced** | Ghép 3 frames liên tiếp vào 1 mảng byte duy nhất trên TCP stream. Server tách và giải mã đúng cả 3 frames, gửi trả 3 ACK riêng biệt. | **PASS** |
| 5 | **Fragmented TCP header and payload** | Cắt nhỏ gói tin thành từng byte rải rác kèm delay. Cơ chế `ReadExactlyBytesAsync` tích lũy đủ dữ liệu và giải mã chính xác. | **PASS** |
| 6 | **Invalid payload -> no false success ACK** | Gửi HEARTBEAT thiếu trường, sai kiểu dữ liệu hoặc bản tin không hỗ trợ (`PING`). Server không gửi ACK giả mạo, client timeout 0 byte. | **PASS** |
| 7 | **Wrong ACK requestId / sequence / UTC detected** | Tạo server giả gửi ACK sai RequestId, sai Sequence hoặc sai định dạng UTC. `FakeAgent` phát hiện lỗi và trả mã lỗi `ExitCode = 1`. | **PASS** |
| 8 | **Missing serverTimeUtc rejected** | Server trả payload `{"acknowledgedSequence": 1}` thiếu trường `serverTimeUtc`. FakeAgent thẩm định raw JSON và từ chối (`ExitCode = 1`). | **PASS** |
| 9 | **Null serverTimeUtc rejected** | Server trả payload `{"acknowledgedSequence": 1, "serverTimeUtc": null}`. FakeAgent từ chối (`ExitCode = 1`). | **PASS** |
| 10 | **Invalid timestamp rejected** | Server trả payload với `serverTimeUtc: "invalid-timestamp"`. FakeAgent từ chối (`ExitCode = 1`). | **PASS** |
| 11 | **Missing acknowledgedSequence rejected** | Server trả payload `{"serverTimeUtc": "..."}` thiếu trường `acknowledgedSequence`. FakeAgent từ chối (`ExitCode = 1`). | **PASS** |
| 12 | **Wrong acknowledgedSequence rejected** | Server trả `acknowledgedSequence: 999` (sai lệch so với sequence gửi). FakeAgent từ chối (`ExitCode = 1`). | **PASS** |
| 13 | **Valid ACK succeeds** | Server trả đầy đủ `acknowledgedSequence` và `serverTimeUtc` UTC hợp lệ. FakeAgent chấp nhận (`ExitCode = 0`). | **PASS** |
| 14 | **Normal and abrupt disconnection** | Thử nghiệm ngắt kết nối bình thường (EOF/Shutdown) và đột ngột (TCP RST via `LingerOption(true, 0)`). Server không sập và tiếp tục phục vụ client tiếp theo. | **PASS** |
| 15 | **Clean tool shutdown** | Gửi tín hiệu hủy `CancellationTokenSource`. Server kết thúc vòng lặp, giải phóng listener và trả lại cổng cho hệ điều hành. | **PASS** |

---

## 3. Bằng chứng chạy thực tế FakeCentral và FakeAgent trên Localhost

Thực thi chạy trực tiếp 2 tiến trình trên Windows qua cổng `6005`:

### Log phía FakeAgent:
```text
==========================================
       LABGUARD - FAKE AGENT CLIENT       
==========================================
[FakeAgent] Đang kết nối tới FakeCentral tại 127.0.0.1:6005...
[FakeAgent] Kết nối thành công!

[FakeAgent GỬI #1] Loại: HEARTBEAT | Seq: 1 | ReqId: d5a0ba0d-6298-4339-a769-af6c0a775c48
[FakeAgent NHẬN #1] Nhận ACK hợp lệ! AcknowledgedSeq: 1 | ServerTimeUtc: 2026-10-15T05:27:06.1477458Z

[FakeAgent GỬI #2] Loại: HEARTBEAT | Seq: 2 | ReqId: 721d609b-5ae0-4f76-90d8-921d018a317b
[FakeAgent NHẬN #2] Nhận ACK hợp lệ! AcknowledgedSeq: 2 | ServerTimeUtc: 2026-10-15T05:27:07.1803762Z

[FakeAgent GỬI #3] Loại: HEARTBEAT | Seq: 3 | ReqId: d70240a3-7d54-41cf-81d7-c92c7d491b5e
[FakeAgent NHẬN #3] Nhận ACK hợp lệ! AcknowledgedSeq: 3 | ServerTimeUtc: 2026-10-15T05:27:08.1926765Z

[FakeAgent GỬI #4] Loại: HEARTBEAT | Seq: 4 | ReqId: 7c4265fe-79db-4275-9cce-e727df5a2615
[FakeAgent NHẬN #4] Nhận ACK hợp lệ! AcknowledgedSeq: 4 | ServerTimeUtc: 2026-10-15T05:27:09.2077741Z

[FakeAgent GỬI #5] Loại: HEARTBEAT | Seq: 5 | ReqId: 93d350ea-2dd9-4c38-a538-cfa96556efcd
[FakeAgent NHẬN #5] Nhận ACK hợp lệ! AcknowledgedSeq: 5 | ServerTimeUtc: 2026-10-15T05:27:10.2101594Z

[FakeAgent] Đã hoàn thành 5 lượt gửi heartbeat và xác thực ACK thành công.
Agent ExitCode: 0
```

### Log phía FakeCentral:
```text
==========================================
       LABGUARD - FAKE CENTRAL SERVER     
==========================================
[FakeCentral] Đang lắng nghe tại 127.0.0.1:6005...
[FakeCentral] Nhấn Ctrl+C để dừng.

[FakeCentral] [+] Kết nối mới từ: 127.0.0.1:63505
[FakeCentral NHẬN] Loại: HEARTBEAT       | Device: AGENT-DEV-01 | ReqId: d5a0ba0d-6298-4339-a769-af6c0a775c48
[FakeCentral GỬI] -> Đã gửi HEARTBEAT_ACK cho ReqId: d5a0ba0d-6298-4339-a769-af6c0a775c48 (AckSeq: 1)
[FakeCentral NHẬN] Loại: HEARTBEAT       | Device: AGENT-DEV-01 | ReqId: 721d609b-5ae0-4f76-90d8-921d018a317b
[FakeCentral GỬI] -> Đã gửi HEARTBEAT_ACK cho ReqId: 721d609b-5ae0-4f76-90d8-921d018a317b (AckSeq: 2)
[FakeCentral NHẬN] Loại: HEARTBEAT       | Device: AGENT-DEV-01 | ReqId: d70240a3-7d54-41cf-81d7-c92c7d491b5e
[FakeCentral GỬI] -> Đã gửi HEARTBEAT_ACK cho ReqId: d70240a3-7d54-41cf-81d7-c92c7d491b5e (AckSeq: 3)
[FakeCentral NHẬN] Loại: HEARTBEAT       | Device: AGENT-DEV-01 | ReqId: 7c4265fe-79db-4275-9cce-e727df5a2615
[FakeCentral GỬI] -> Đã gửi HEARTBEAT_ACK cho ReqId: 7c4265fe-79db-4275-9cce-e727df5a2615 (AckSeq: 4)
[FakeCentral NHẬN] Loại: HEARTBEAT       | Device: AGENT-DEV-01 | ReqId: 93d350ea-2dd9-4c38-a538-cfa96556efcd
[FakeCentral GỬI] -> Đã gửi HEARTBEAT_ACK cho ReqId: 93d350ea-2dd9-4c38-a538-cfa96556efcd (AckSeq: 5)
[FakeCentral] [-] Client 127.0.0.1:63505 đã ngắt kết nối bình thường (EOF).
[FakeCentral] [-] Đã giải phóng kết nối: 127.0.0.1:63505
```

---

## 4. Kết quả kiểm tra toàn hệ thống

- **dotnet build LabGuard.sln:**
  ```text
  Build succeeded.
      0 Warning(s)
      0 Error(s)
  ```
- **dotnet test LabGuard.sln:**
  - `LabGuard.Protocol.Tests`: **107 / 107 Passed**
  - `LabGuard.Agent.Tests`: **34 / 34 Passed**
  - `LabGuard.Central.Tests`: **9 / 9 Passed**
  - `LabGuard.MockHarness.Tests`: **15 / 15 Passed**
  - **TỔNG CỘNG: 165 / 165 Passed (100%)** — Toàn bộ 150 test cũ được bảo toàn nguyên vẹn, 15 test Mock Harness đạt 100%.
