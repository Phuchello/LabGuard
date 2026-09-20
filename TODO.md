# LABGUARD — VIỆC CẦN LÀM (TEAM TODO BOARD)

Bảng phân chia công việc thực tế cho 3 thành viên nhóm trong giai đoạn tự chủ lập trình (Team Takeover).

## 0. Đã hoàn thành gần nhất (Recently Completed)
- [x] **LG-A-001 — Wire Contract & Protocol Fixtures & Final Freeze Patch:**
  - [x] Đóng băng và tài liệu hóa chuẩn khung truyền: 6 trường envelope cố định, 4-byte Big-Endian length prefix, max payload 262,144 bytes, bảng mã lỗi và ma trận kiểm tra trong `CONTRACT_MATRIX.md`.
  - [x] Triển khai `Encode`, `Decode`, `ReadEnvelopeAsync` trong `FrameCodec.cs` (xác thực nghiêm ngặt qua `EnvelopeValidator.ValidateAndParse`, ném `InvalidDataException` khi vi phạm).
  - [x] Triển khai xác thực nghiêm ngặt phong bì raw JSON `ValidateRawJson` và `ValidateAndParse` trong `EnvelopeValidator.cs` (kiểm tra phân biệt hoa thường, cấm trường trùng lặp, payload phải là JSON object, version != 1 trả về `ERR_INVALID_VERSION` duy nhất, bắt buộc ký hiệu UTC `Z` hoặc `+00:00`).
  - [x] Áp dụng `ValidateAndParse` trước khi phân phối / deserialize trên toàn bộ socket pipeline của Central (`ClientConnection.cs`), Agent (`AgentSocketClient.cs`), và Admin (`AdminClient.cs`).
  - [x] Chuẩn hóa mã lỗi canonical: `ERR_MALFORMED_JSON` cho JSON sai cú pháp, `ERR_INVALID_VERSION` cho phiên bản khác 1.
  - [x] Tạo đầy đủ đúng 16 fixtures chuẩn canonical (WBS_TASKS.csv): `heartbeat.json`, `heartbeat_ack.json`, `metric_batch.json`, `batch_ack.json`, `service_status.json`, `hello_agent.json`, `hello_ack.json`, `auth_agent.json`, `auth_result.json`, `agent_goodbye.json`, `goodbye_ack.json`, `login.json`, `login_result.json`, `incident_ack.json`, `command_result.json`, `error.json` và cập nhật `ProtocolFixtures.cs`.
  - [x] Xây dựng bộ kiểm thử tự động toàn diện `WireContractAndFixturesTests.cs` (99/99 tests pass) và kiểm thử từ chối trường lạ qua kết nối TCP thực tế (53/53 tests pass).
- [x] **LG-A-002 — FrameCodec & partial-read / message-framing verification:**
  - [x] Hiện thực `FrameCodec.cs` với `ReadExactlyBytesAsync(Stream, byte[], offset, count, ct)` tích lũy byte qua vòng lặp, xử lý phân mảnh TCP (partial reads).
  - [x] Tiền tố độ dài 4-byte UInt32 Big-Endian, `MaxPayloadSize = 262_144` bytes (256 KiB) và alias `MaxFrameSize = 262_144`.
  - [x] Kiểm tra giới hạn: từ chối frame 0 byte (`InvalidDataException`) và frame > 262,144 bytes TRƯỚC KHI cấp phát bộ nhớ đệm payload (`InvalidDataException`).
  - [x] Xử lý ngắt dòng: Graceful EOF tại ranh giới frame trả về `null`; EOF giữa chừng (mid-header hoặc mid-payload) ném `EndOfStreamException`.
  - [x] Phân tách độc lập khi nhiều frame dính liền (coalesced frames).
  - [x] Bộ kiểm thử tự động `FrameCodecTests.cs` (107/107 passed) và kịch bản độc lập `scratch/test_frame_codec/Program.cs` (10/10 passed).
  - [x] Cập nhật bảng theo dõi `LabGuard_Task_Tracker.xlsx`: LG-A-002 / TEST-A02 chuyển sang `Xong` / `PASS`.
- **Last Safe Checkpoint:** `LG-A-002 CLOSED` (165/165 tests pass trên toàn bộ Solution, 0 warnings).
- **Exact Next Action:** A — Agent / Protocol: Triển khai tiếp nhiệm vụ **`LG-A-003` — Agent TCP connection và writer queue** theo tiến trình Master WBS (`WBS_TASKS.csv`). Không nhảy cóc trực tiếp sang RBAC.

---

## A — Agent / Simulator / Mạng thực tế
- [ ] Thử nghiệm ngắt/nối mạng thực tế: Chạy Agent, ngắt Central trong 15-30 giây rồi bật lại; kiểm tra Agent tự động backoff và gửi bù đủ các gói tin từ `ReplayBuffer`.
- [ ] Cấu hình `ServiceMonitor.cs` để probe cổng dịch vụ trên máy ảo Ubuntu (port 80 hoặc 22); kiểm tra kích hoạt sự cố `SERVICE_DOWN` khi dừng dịch vụ trên máy ảo.
- [ ] Chạy công cụ giả lập `LabGuard.Simulator` với 10-20 agents gửi telemetry đồng thời vào Central Server để kiểm tra độ ổn định.

## B — Central / Database / RBAC
- [ ] Bổ sung kiểm tra quyền truy cập (Server-side RBAC) trong `ClientConnection.cs` theo chuẩn ma trận:
  - `Viewer`: Chỉ đọc/xem danh sách, không có quyền thao tác sự cố.
  - `Operator`: Được phép `INCIDENT_ACK`, `INCIDENT_START`, `INCIDENT_RESOLVE` (KHÔNG được `INCIDENT_ASSIGN`).
  - `Admin`: Toàn quyền thao tác bao gồm cả `INCIDENT_ASSIGN`.
- [ ] Lưu vết người xử lý sự cố (`AssignedToUserId`): Cập nhật vào bảng `dbo.Incidents` và ghi nhật ký vào bảng `dbo.IncidentHistory` trong `DatabaseRepository.cs`.
- [ ] Chuẩn hóa mã lỗi OCC canonical: Trả về mã lỗi `ERR_CONCURRENCY_VIOLATION` khi phát hiện xung đột `RowVersion`.
- [ ] Cô lập CSDL kiểm thử tự động (`LabGuardTestDb`): Tách cấu hình connection string để `LabGuard.Central.Tests` không chạy trực tiếp trên CSDL thật `LabGuardDb`.

## C — Admin WinForms / UI & UX
- [ ] Thêm cột "Người xử lý" (`Assigned To`) trên DataGridView danh sách sự cố trong `DashboardForm.cs`.
- [ ] Xây dựng hộp thoại gán việc trực quan khi bấm nút `Assign`, cho phép nhập hoặc chọn tài khoản kỹ thuật viên.
- [ ] Cải thiện trải nghiệm thao tác sự cố (UI Polish & UX): Hiển thị `MessageBox` thân thiện khi có lỗi (xung đột dữ liệu 409, lỗi quyền 403, hoặc đóng sự cố khi máy còn vi phạm).

## Việc cả nhóm
- [ ] Chạy thử kịch bản phối hợp hoàn chỉnh (End-to-End Demo): Bật SQL -> Central -> Agent -> Admin -> Kích hoạt sự cố -> Kỹ thuật viên xử lý -> Đóng sự cố.
- [ ] Chạy toàn bộ bộ kiểm thử tự động (`dotnet test LabGuard.sln`) đảm bảo 100% pass trước khi nghiệm thu.
- [ ] Cập nhật nhật ký công việc vào file `LabGuard_Task_Tracker.xlsx`.

## Sau này / nếu còn thời gian
- [ ] Bổ sung đọc chỉ số CPU/RAM trên hệ điều hành Linux (đọc qua `/proc/stat` và `/proc/meminfo`).
- [ ] Thêm biểu đồ đường theo dõi chỉ số trên giao diện Admin.
- [ ] Đóng gói file chạy portable cho Agent trên máy trạm.
