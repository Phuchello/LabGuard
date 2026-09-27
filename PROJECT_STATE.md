# LABGUARD — BÁO CÁO HIỆN TRẠNG DỰ ÁN (PROJECT STATE)

**Dự án:** LABGUARD — Hệ thống giám sát phòng máy tính tập trung thời gian thực  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Thời điểm cập nhật:** 27/09/2026  
**Chế độ làm việc:** Team Takeover Mode (Nhóm 4 sinh viên A, B, C, D trực tiếp lập trình, AI hỗ trợ Mentor/Review/Debug)  
**Tài liệu nhập môn cho nhóm:** [START_HERE.md](START_HERE.md) & [FILE_MAP.md](FILE_MAP.md) (Tài liệu kỹ thuật nội bộ lưu trữ tại `docs/internal/`)  

---

## 1. TỔNG QUAN TÌNH TRẠNG SẢN PHẨM (CURRENT PRODUCT STATE)

> [!IMPORTANT]
> **Hiện trạng thực tế của mã nguồn:**  
> - **ĐÃ HOÀN THÀNH & NGHIỆM THU:** Nhiệm vụ **LG-A-001** (Wire Contract & 16 Fixtures), **LG-A-002** (`FrameCodec.cs` & xử lý phân mảnh TCP) và **LG-A-003** (`AgentSocketClient.cs` & hàng đợi Single-Writer 256). Tổng: **123/123 tests PASS** (Protocol 107, Agent 16).
> - **CÁC PHÂN HỆ CHƯA TRIỂN KHAI:** `src/LabGuard.Central/`, `src/LabGuard.Admin/`, `src/LabGuard.Simulator/` chỉ chứa khung sườn tối thiểu (`Program.cs`, `DashboardForm.cs`). Sinh viên B/C/D sẽ tự tay lập trình từng tính năng.
> - **CÁC THÀNH VIÊN SẼ TỰ TAY LẬP TRÌNH TỪNG TÍNH NĂNG:** Sinh viên sẽ trực tiếp tạo mới và lập trình các file tính năng theo đúng nhiệm vụ được phân công trong bảng theo dõi `LabGuard_Task_Tracker.xlsx`.

- **Chi tiết hiện trạng từng phân hệ:**
  - `src/LabGuard.Protocol/`: **HOÀN THÀNH 100%**. Gồm `FrameCodec.cs`, `WireEnvelope.cs`, `EnvelopeValidator.cs`, `MessageTypes.cs`, `ErrorCodes.cs`, các DTOs và `ProtocolSerializer.cs`. Đã đóng băng giao ước.
  - `src/LabGuard.Agent/`: **A-003 HOÀN THÀNH**. Có `AgentSocketClient.cs` (BoundedChannel 256, Single-Writer Loop, `CreateOutbox` factory, DisposeAsync đúng, xử lý lỗi ghi) và `Program.cs` khung sườn. 16/16 Agent tests PASS. Từ A-004 trở đi là **CHƯA LÀM** — Sinh viên A tự tay thực hiện.
  - `src/LabGuard.Central/`: **KHUNG SƯỜN CHỜ TRIỂN KHAI**. Chưa có TCP listener, chưa có bắt tay xác thực, chưa có liveness monitor, chưa có rule engine hay database repository. Sinh viên B sẽ bắt đầu từ B-001 (`CentralServer.cs`).
  - `src/LabGuard.Admin/`: **KHUNG SƯỜN GIAO DIỆN CHỜ TRIỂN KHAI**. Đã có form rỗng `DashboardForm.cs`. Chưa có kết nối mạng `AdminClient.cs`, chưa có bảng DataGridView và các nút điều khiển. Sinh viên C sẽ bắt đầu từ C-001 và C-002.
  - `src/LabGuard.Gateway/` & `src/LabGuard.Collector/`: **CHƯA TỒN TẠI TRONG SRC**. Là phân hệ mở rộng dự kiến cho Thành viên D sau khi hệ thống cốt lõi của A, B, C ổn định. D hiện thực hành socket trong `tools/`.
  - `database/`: **KHUNG CHỜ (PLACEHOLDER)**. `schema.sql` và `seed.sql` hiện là khung chờ; Sinh viên B sẽ tự tay hoàn thiện trong B-005.

- **Hiện trạng nhiệm vụ chính thức của các thành viên (Authoritative Team Status):**
  - **Sinh viên A (Agent & Protocol):** Đã xong **A-001** và **A-002**. Từ **A-003 trở đi là CHƯA LÀM** (Bắt đầu với A-003: `AgentSocketClient.cs` với `BoundedChannelOptions(256)` và `FullMode.Wait`).
  - **Sinh viên B (Central & CSDL):** **100% nhiệm vụ là CHƯA LÀM** (Bắt đầu với B-001: `CentralServer.cs` mở cổng TCP 5000).
  - **Sinh viên C (Admin UI):** **100% nhiệm vụ là CHƯA LÀM** (Bắt đầu với C-001 thiết kế WinForms và C-002 tạo `AdminClient.cs`).
  - **Sinh viên D (Gateway & Collector):** **100% nhiệm vụ là CHƯA LÀM** (Bắt đầu với D-001 đọc hiểu giao thức và D-002 viết TCP Echo trong `tools/`).

---

- **Task LG-A-001 (Wire Contract & Protocol Fixtures & Final Freeze Patch):** Đã hoàn thành (DONE).
  - Đóng băng giao ước wire protocol: Envelope đúng 6 trường cố định (`version`, `messageType`, `requestId`, `deviceId`, `sentAtUtc`, `payload`), cấm trường lạ và kiểm tra phân biệt hoa thường nghiêm ngặt ở outer envelope (`ValidateRawJson` / `ValidateAndParse`).
  - Kiểm tra kiểu dữ liệu `payload`: Bắt buộc `JsonValueKind.Object` (từ chối string, number, boolean, array, null, undefined).
  - Chuẩn hóa lỗi phiên bản: `version != 1` trả về mã lỗi canonical `ERR_INVALID_VERSION` duy nhất.
  - Chuẩn hóa lỗi JSON hỏng: JSON sai cú pháp trả về mã lỗi canonical `ERR_MALFORMED_JSON` trên toàn bộ pipeline.
  - Ràng buộc mốc thời gian UTC: `sentAtUtc` bắt buộc chứa ký hiệu chỉ định UTC rõ ràng (`Z` hoặc `+00:00`), từ chối chuỗi không múi giờ.
  - Đóng khung TCP: UInt32 Big-Endian 4-byte header biểu diễn duy nhất payload byte count; max payload 262,144 bytes; xử lý luồng byte TCP (phân mảnh & dính gói); `FrameCodec.ReadEnvelopeAsync` xác thực nghiêm ngặt và ném `InvalidDataException` khi vi phạm.
  - Đúng 16 fixtures chuẩn canonical: `HEARTBEAT`, `HEARTBEAT_ACK`, `METRIC_BATCH`, `BATCH_ACK`, `SERVICE_STATUS`, `HELLO_AGENT`, `HELLO_ACK`, `AUTH_AGENT`, `AUTH_RESULT`, `AGENT_GOODBYE`, `GOODBYE_ACK`, `LOGIN`, `LOGIN_RESULT`, `INCIDENT_ACK`, `COMMAND_RESULT`, `ERROR` (minified, không BOM, <= 262,144 bytes, deserialize vào DTO tương ứng).

- **Task LG-A-002 (FrameCodec & partial-read / message-framing verification):** Đã hoàn thành (DONE).
  - Hiện thực và chuẩn hóa `FrameCodec.cs`:
    - Tiền tố độ dài 4-byte UInt32 Big-Endian (`BinaryPrimitives.ReadUInt32BigEndian` / `WriteUInt32BigEndian`).
    - Hằng số `MaxPayloadSize = 262_144` bytes (256 KiB) và alias `MaxFrameSize = 262_144` theo đúng interface contract trong WBS.
    - Phương thức phụ trợ tích lũy byte `ReadExactlyBytesAsync(Stream, byte[], offset, count, ct)` xử lý triệt để hiện tượng phân mảnh TCP (TCP partial reads), ném `EndOfStreamException` khi kênh truyền bị đóng bất ngờ.
    - Kiểm tra giới hạn nghiêm ngặt: Từ chối frame 0 byte (`InvalidDataException`) và từ chối frame vượt quá 262,144 bytes TRƯỚC KHI cấp phát bộ nhớ đệm payload (`InvalidDataException`), chống tấn công cạn kiệt RAM (DoS/OOM).
    - Đóng kết nối an toàn: Trả về `null` khi gặp Graceful EOF tại ranh giới frame; ném `EndOfStreamException` khi gặp EOF giữa header hoặc giữa payload.
    - Tách frame độc lập: Phân tách hoàn hảo khi nhiều frame dính liền trong một luồng stream (Packet Coalescing).
  - Bộ kiểm thử tự động toàn diện `FrameCodecTests.cs` (100% pass) kiểm chứng đầy đủ các kịch bản quy định trong WBS và kịch bản phụ trợ `ReadExactlyBytesAsync`.
  - Chương trình kiểm chứng độc lập `tools/validation/test_frame_codec/Program.cs` (10/10 scenarios passed).
  - Đồng bộ Task Tracker `LabGuard_Task_Tracker.xlsx`: LG-A-002 / TEST-A02 chuyển sang `Xong` / `PASS`.

- **Task LG-A-003 (Agent TCP connection và writer queue):** Đã hoàn thành (DONE).
  - Tạo mới `AgentSocketClient.cs` trong `src/LabGuard.Agent/`:
    - Quản lý vòng đời `TcpClient` và `NetworkStream` rõ ràng.
    - Hàng đợi xuất `Channel<byte[]>` có chặn (Bounded Channel) dung lượng đúng 256 items (`DefaultOutboxCapacity = 256`), chế độ tràn `BoundedChannelFullMode.Wait` (áp lực ngược / Backpressure), `SingleWriter = false`, `SingleReader = true`.
    - Vòng lặp ghi duy nhất (`WriterLoopAsync`): Nơi duy nhất gọi `NetworkStream.WriteAsync`, đọc tuần tự từng frame từ hàng đợi và ghi trọn vẹn xuống socket, ngăn ngừa hoàn toàn hiện tượng Frame Interleaving.
    - Tái sử dụng trọn vẹn `FrameCodec.Encode(envelope)` và `FrameCodec.Encode(payload)` từ thư viện giao thức A-001/A-002 mà không lặp lại mã nguồn.
    - Xử lý lỗi ngắt kết nối an toàn, dừng êm đềm (cancellation), không tự động kết nối lại khi chưa đến nhiệm vụ A-005.
  - Bộ kiểm thử tự động `AgentSocketClientTests.cs` (16/16 tests PASS, 100%) kiểm chứng toàn diện các kịch bản với máy chủ giả lập nội bộ `FakeTcpServer` trên loopback. Bao gồm: `Scenario8b` (DisposeAsync trực tiếp), `Scenario10` (writer fault → IsConnected=false), `PublicConstructor_AlwaysUses256Capacity`, backpressure qua `CreateOutbox` factory.
  - Đồng bộ Task Tracker `LabGuard_Task_Tracker.xlsx`: LG-A-003 / TEST-A03 chuyển sang `Xong` / `PASS`.

---

## 2. KẾT QUẢ KIỂM THỬ HIỆN TẠI (AUTOMATED TEST COUNT)

**Tổng số kiểm thử tự động hiện tại:** **123 / 123 tests PASSED (100%)**
- `LabGuard.Protocol.Tests`: **107 / 107 passed** (Kiểm thử FrameCodec, ReadExactlyBytesAsync, chunking 1-byte header/payload, coalescing 3 frames, boundary 262,144 bytes, WireEnvelope, raw JSON validation, 8 yêu cầu wire contract, 16 canonical fixtures minified/no-BOM/DTO deserialization, strict casing, duplicate keys, non-object payloads, strict uppercase 'Z'/'+00:00' UTC designator, ReadEnvelopeAsync strictness).
- `LabGuard.Agent.Tests`: **16 / 16 passed** (Kiểm thử AgentSocketClient kết nối TCP, gửi đơn frame, tuần tự, nhiều producer đồng thời, single writer, backpressure qua CreateOutbox factory, cancellation, dispose đồng bộ, DisposeAsync trực tiếp với server EOF, lỗi ghi mạng IsConnected=false, lỗi kết nối không auto-reconnect, public constructor 256).
- `LabGuard.Central.Tests`: **0 tests** (Chờ Thành viên B triển khai Central Server từ B-001 trở đi).

---

## 2.1. CHECKPOINT AN TOÀN & HÀNH ĐỘNG TIẾP THEO (CHECKPOINT & NEXT ACTION)

- **Last Safe Checkpoint:** `LG-A-003 ADVERSARIAL PASS CLOSED` (123/123 automated tests pass, zero regressions, zero warnings). Branch: `feat/lg-a-003-writer-queue`, commit `5c9ada1`.
- **Exact Next Action:** Sinh viên A triển khai tiếp nhiệm vụ **`LG-A-004 [CHƯA LÀM — TIẾP THEO]` — Thu thập CPU, RAM, Uptime hệ thống** (`MetricsCollector.cs`, `MetricsService.cs`).

---

## 2.2. TỔ CHỨC DỰ ÁN CHO NHÓM 4 THÀNH VIÊN (CANONICAL TEAM PLAN)

Toàn bộ dự án được tổ chức theo kế hoạch chuẩn hóa 34 nhiệm vụ (phù hợp với `LabGuard_Task_Tracker.xlsx`, `TODO.md`, `START_HERE.md` và tài liệu hướng dẫn các thành viên):

- **Thành viên A (Máy trạm Agent & Giao thức):** 9 nhiệm vụ (`A-001` đến `A-009`)
  - A-001 [XONG]: 16 bản tin JSON mẫu chuẩn.
  - A-002 [XONG]: Đóng khung gói tin TCP 4 byte độ dài (`FrameCodec.cs`).
  - A-003 [XONG]: Kết nối TCP và hàng đợi gửi tuần tự (`AgentSocketClient.cs`, capacity 256, FullMode.Wait).
  - A-004 [CHƯA LÀM — TIẾP THEO]: Đo CPU, RAM và Uptime thật từ Windows (`MetricsCollector.cs`, `MetricsService.cs`).
  - A-005 [CHƯA LÀM]: Tự động kết nối lại khi rớt mạng (`AgentSocketClient.cs` với Exponential Backoff + Jitter).
  - A-006 [CHƯA LÀM]: Bộ nhớ giữ dữ liệu ngoại tuyến (`ReplayBuffer.cs`, tối đa 50 batches, `METRIC_BATCH` duy nhất, FIFO Drop-Oldest, `isReplay = true`).
  - A-007 [CHƯA LÀM]: Giám sát cổng dịch vụ mạng 80 và 8080 (`ServiceMonitor.cs`).
  - A-008 [CHƯA LÀM]: Xử lý ngắt kết nối đột ngột và đóng socket an toàn.
  - A-009 [CHƯA LÀM]: Thử nghiệm kiểm tra dịch vụ mạng trên máy ảo Ubuntu / môi trường thực tế.

- **Thành viên B (Máy chủ Central & CSDL):** 9 nhiệm vụ (`B-001` đến `B-009`)
  - B-001 [BẮT ĐẦU LÀM]: Mở cổng Socket TCP 5000 đón nhận kết nối (`CentralServer.cs`).
  - B-002 [CHƯA LÀM]: Bắt tay và xác thực máy con qua `HELLO` và `AUTH` (`ClientConnection.cs`).
  - B-003 [CHƯA LÀM]: Tiếp nhận nhịp tim và theo dõi trạng thái sống (`LivenessMonitor.cs`, deadline 20s).
  - B-004 [CHƯA LÀM]: Phân quyền tài khoản máy chủ RBAC (`ClientConnection.cs` / `AuthService.cs`).
  - B-005 [CHƯA LÀM]: Lập trình CSDL & lưu số liệu máy vào SQL Server (`DatabaseRepository.cs`, `schema.sql`).
  - B-006 [CHƯA LÀM]: Bộ quy tắc phát hiện sự cố (`RuleEngine.cs` với Hysteresis, CPU >= 90%, RAM >= 85%).
  - B-007 [CHƯA LÀM]: Vòng đời sự cố và chống sửa đè OCC bằng `RowVersion` (`DatabaseRepository.cs`).
  - B-008 [CHƯA LÀM]: Tách riêng CSDL kiểm thử `LabGuardTestDb` khỏi `LabGuardDb`.
  - B-009 [CHƯA LÀM]: Ghi nhận UserId kỹ thuật viên xử lý vào bảng sự cố (`dbo.Incidents`, `dbo.IncidentHistory`).

- **Thành viên C (Giao diện quản trị Admin WinForms):** 10 nhiệm vụ (`C-001` đến `C-008`, `C-010`, `C-011`; nhiệm vụ `C-009` được thu hồi/dự phòng)
  - C-001 [BẮT ĐẦU LÀM]: Thiết kế màn hình chính WinForms (`DashboardForm.cs`).
  - C-002 [BẮT ĐẦU LÀM]: Kết nối ngầm tới máy chủ Central (`AdminClient.cs`).
  - C-003 [CHƯA LÀM]: Bảng danh sách thiết bị DataGridView (`DashboardForm.cs`).
  - C-004 [CHƯA LÀM]: Bảng danh sách sự cố DataGridView (`DashboardForm.cs`).
  - C-005 [CHƯA LÀM]: Nút bấm tiếp nhận sự cố ACK (`DashboardForm.cs`).
  - C-006 [CHƯA LÀM]: Nút bấm bắt đầu sửa Start (`DashboardForm.cs`).
  - C-007 [CHƯA LÀM]: Nút bấm hoàn tất sửa Resolve (`DashboardForm.cs`).
  - C-008 [CHƯA LÀM]: Thêm cột và nút phân công người sửa Assign (`DashboardForm.cs`).
  - C-010 [CHƯA LÀM]: Khóa nút bấm theo phân quyền khi đăng nhập Viewer (`DashboardForm.cs`).
  - C-011 [CHƯA LÀM]: Hiển thị thông báo lỗi thân thiện khi máy chủ từ chối thao tác (`DashboardForm.cs`).

- **Thành viên D (Tầng mở rộng Gateway & Collector):** 6 nhiệm vụ (`D-001` đến `D-006`)
  - D-001 [BẮT ĐẦU LÀM]: Chạy bộ kiểm thử giao thức (`dotnet test`), đọc hiểu `FrameCodec.cs`, xem xét 16 tệp mẫu JSON trong fixtures, nắm vững phong bì 6 trường và đóng khung TCP 4 byte.
  - D-002 [BẮT ĐẦU LÀM]: Viết chương trình Console TCP Echo trong thư mục `tools/` để làm quen socket.
  - D-003 [CHƯA LÀM]: Thử nghiệm chuyển tiếp gói tin TCP Relay (`tools/`, `BoundedChannelOptions(100)`, `FullMode.Wait`).
  - D-004 [CHƯA LÀM / DỰ KIẾN]: Tạo dự án `LabGuard.Gateway` (bộ cân bằng tải L4 TCP).
  - D-005 [CHƯA LÀM / DỰ KIẾN]: Tạo dự án `LabGuard.Collector` (trạm gom dữ liệu phân tán).
  - D-006 [CHƯA LÀM / DỰ KIẾN]: Viết logic sinh tải 20–50 máy trạm cho `LabGuard.Simulator`.

---

## 3. CÁC QUY TẮC KỸ THUẬT QUAN TRỌNG ĐÃ THỐNG NHẤT (APPROVED ARCHITECTURAL RULES)

1. **Giao ước Wire Protocol:** Cố định đúng 6 trường outer envelope, phân biệt hoa thường nghiêm ngặt, payload bắt buộc là JSON Object.
2. **Đóng khung TCP (Framing):** 4-byte UInt32 Big-Endian length prefix, kích thước frame tối đa 262,144 bytes (256 KiB).
3. **Hàng đợi gửi Agent (A-003):** `BoundedChannelOptions(256)`, `BoundedChannelFullMode.Wait`, đảm bảo Single Writer trên socket.
4. **Bộ đệm ReplayBuffer (A-006):** Giới hạn tối đa **50 batches** (tương đương 250 giây dữ liệu), chỉ lưu trữ `METRIC_BATCH` ngoại tuyến, FIFO Drop-Oldest khi đầy, gắn cờ `isReplay = true`, **không** lưu `HEARTBEAT`, **không** lưu `SERVICE_STATUS`.
5. **Cơ chế Liveness (B-003):** Đánh giá nhịp tim dựa trên đồng hồ đơn điệu Stopwatch máy chủ; ngưỡng phát hiện mất kết nối là 20 giây (quá 3 chu kỳ heartbeat 15s + 5s ân hạn).
6. **Thuật toán RuleEngine (B-006):** Chống dao động Hysteresis; kích hoạt cảnh báo quá tải khi vi phạm liên tiếp 3 mẫu (streak 3), phục hồi khi đạt liên tiếp 3 mẫu an toàn.
7. **Kiểm soát tương tranh OCC (B-007 & C-011):** Sử dụng cột `RowVersion` 8-byte token; khi phát hiện xung đột trả về mã lỗi canonical `ERR_CONCURRENCY_VIOLATION` (HTTP 409 Conflict).
8. **An toàn luồng giao diện WinForms (C-001..C-007):** Bắt buộc chuyển luồng qua `BeginInvoke` khi cập nhật control từ luồng mạng ngầm.

---

## 4. CÁC VẤN ĐỀ VÀ RỦI RO ĐÃ BIẾT (KNOWN ISSUES / BLOCKERS)

| STT | Vấn đề / Rủi ro | Tình trạng hiện tại | Hướng giải quyết của nhóm |
| :---: | :--- | :--- | :--- |
| 1 | `LabGuardTestDb` chưa được cô lập | Bộ test tích hợp tương lai cần môi trường CSDL riêng để không ảnh hưởng dữ liệu chính. | Sinh viên B cấu hình connection string riêng cho test harness trong task B-008. |
| 2 | Đọc chỉ số phần cứng qua Win32 API | Hiện chỉ áp dụng cho môi trường Windows. | Phù hợp hoàn toàn với phạm vi phòng máy UIT. |
| 3 | Điều kiện giải quyết sự cố (`Resolve`) | Server từ chối đóng nếu điều kiện vật lý còn vi phạm hoặc ghi chú giải quyết bị trống. | Ràng buộc nghiệp vụ đúng thiết kế; Sinh viên C hiển thị thông báo thân thiện trên WinForms. |
