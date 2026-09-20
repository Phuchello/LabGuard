# LABGUARD — BÁO CÁO HIỆN TRẠNG DỰ ÁN (PROJECT STATE)

**Dự án:** LABGUARD — Hệ thống giám sát phòng máy tính tập trung thời gian thực  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Thời điểm cập nhật:** 15/09/2026  
**Chế độ làm việc:** Team Takeover Mode (Nhóm sinh viên trực tiếp lập trình, AI hỗ trợ Mentor/Review/Debug)  

---

## 1. TỔNG QUAN TÌNH TRẠNG SẢN PHẨM (CURRENT PRODUCT STATE)

- **Milestone 1–3:** Đã hoàn thành (Completed).
  - Kết nối TCP Socket với Frame Length-Prefix (UInt32 Big-Endian) + UTF-8 JSON.
  - Bắt tay `HELLO`, xác thực `AUTH`, duy trì nhịp tim `HEARTBEAT` (5s), phát hiện `DEVICE_OFFLINE` (liveness timeout > 20s).
  - Đọc chỉ số phần cứng Windows thực tế (CPU qua `GetSystemTimes`, RAM qua `GlobalMemoryStatusEx`, Uptime qua `TickCount64`).
  - Lưu trữ CSDL SQL Server (`dbo.MetricSamples`), chống trùng lặp (`dbo.ProcessedBatches`).
  - Tự động kết nối lại theo số mũ (Exponential Backoff 1s..30s ±10% jitter) và hàng đợi lưu đệm 50 gói tin (`ReplayBuffer`) có cờ `isReplay`.
  - Rule Engine phát hiện bất thường theo chuỗi mẫu vi phạm: `HIGH_CPU` (>=90%, phục hồi <=75%), `HIGH_MEMORY` (>=85%, phục hồi <=70%), `SERVICE_DOWN` (thất bại 2 lần), `DEVICE_OFFLINE` (>20s).

- **Milestone 4:** Đã có mã nguồn thực thi; bản vá cuối cùng do nhóm sinh viên tự làm đang chờ hoàn thiện (Implementation exists; final team-owned patch pending).
  - Vòng đời sự cố State Machine: `OPEN` -> `ACKNOWLEDGED` -> `IN_PROGRESS` -> `RESOLVED`.
  - Kiểm soát tranh chấp lạc quan (OCC) sử dụng `RowVersion` 8-byte hex.
  - Chống trùng lặp lệnh bằng `RequestDigest` SHA-256 (`(UserId, CommandId)`).
  - Giao diện Admin WinForms với các nút hành động `ACK`, `Assign`, `Start`, `Resolve`, huy hiệu trạng thái màu và cơ chế khóa nút tự động.

---

- **Task LG-A-001 (Wire Contract & Protocol Fixtures & Final Freeze Patch):** Đã hoàn thành (DONE).
  - Đóng băng giao ước wire protocol: Envelope đúng 6 trường cố định (`version`, `messageType`, `requestId`, `deviceId`, `sentAtUtc`, `payload`), cấm trường lạ và kiểm tra phân biệt hoa thường nghiêm ngặt ở outer envelope (`ValidateRawJson` / `ValidateAndParse`).
  - Kiểm tra kiểu dữ liệu `payload`: Bắt buộc `JsonValueKind.Object` (từ chối string, number, boolean, array, null, undefined).
  - Chuẩn hóa lỗi phiên bản: `version != 1` trả về mã lỗi canonical `ERR_INVALID_VERSION` duy nhất.
  - Chuẩn hóa lỗi JSON hỏng: JSON sai cú pháp trả về mã lỗi canonical `ERR_MALFORMED_JSON` trên toàn bộ pipeline.
  - Ràng buộc mốc thời gian UTC: `sentAtUtc` bắt buộc chứa ký hiệu chỉ định UTC rõ ràng (`Z` hoặc `+00:00`), từ chối chuỗi không múi giờ.
  - Đóng khung TCP: UInt32 Big-Endian 4-byte header biểu diễn duy nhất payload byte count; max payload 262,144 bytes; xử lý luồng byte TCP (phân mảnh & dính gói); `FrameCodec.ReadEnvelopeAsync` xác thực nghiêm ngặt và ném `InvalidDataException` khi vi phạm.
  - Đúng 16 fixtures chuẩn canonical (theo chỉ tiêu WBS_TASKS.csv): `HEARTBEAT`, `HEARTBEAT_ACK`, `METRIC_BATCH`, `BATCH_ACK`, `SERVICE_STATUS`, `HELLO_AGENT`, `HELLO_ACK`, `AUTH_AGENT`, `AUTH_RESULT`, `AGENT_GOODBYE`, `GOODBYE_ACK`, `LOGIN`, `LOGIN_RESULT`, `INCIDENT_ACK`, `COMMAND_RESULT`, `ERROR` (minified, không BOM, <= 262,144 bytes, deserialize vào DTO tương ứng).
  - Phân định rõ ràng: 16 canonical fixtures đóng băng giao ước cốt lõi vs. kiểm thử tương thích 69 bản tin toàn catalog.

- **Task LG-A-002 (FrameCodec & partial-read / message-framing verification):** Đã hoàn thành (DONE).
  - Hiện thực và chuẩn hóa `FrameCodec.cs`:
    - Tiền tố độ dài 4-byte UInt32 Big-Endian (`BinaryPrimitives.ReadUInt32BigEndian` / `WriteUInt32BigEndian`).
    - Hằng số `MaxPayloadSize = 262_144` bytes (256 KiB) và alias `MaxFrameSize = 262_144` theo đúng interface contract trong WBS.
    - Phương thức phụ trợ tích lũy byte `ReadExactlyBytesAsync(Stream, byte[], offset, count, ct)` xử lý triệt để hiện tượng phân mảnh TCP (TCP partial reads), ném `EndOfStreamException` khi kênh truyền bị đóng bất ngờ.
    - Kiểm tra giới hạn nghiêm ngặt: Từ chối frame 0 byte (`InvalidDataException`) và từ chối frame vượt quá 262,144 bytes TRƯỚC KHI cấp phát bộ nhớ đệm payload (`InvalidDataException`), chống tấn công cạn kiệt RAM (DoS/OOM).
    - Đóng kết nối an toàn: Trả về `null` khi gặp Graceful EOF tại ranh giới frame; ném `EndOfStreamException` khi gặp EOF giữa header hoặc giữa payload.
    - Tách frame độc lập: Phân tách hoàn hảo khi nhiều frame dính liền trong một luồng stream (Packet Coalescing).
  - Bộ kiểm thử tự động toàn diện `FrameCodecTests.cs` (100% pass) kiểm chứng đầy đủ 9 kịch bản quy định trong WBS và kịch bản phụ trợ `ReadExactlyBytesAsync`.
  - Chương trình kiểm chứng độc lập `scratch/test_frame_codec/Program.cs` và log thực thi `scratch/test_frame_codec/test_frame_codec_run.log` (10/10 scenarios passed).
  - Đồng bộ Task Tracker `LabGuard_Task_Tracker.xlsx`: LG-A-002 / TEST-A02 chuyển sang `Xong` / `PASS`.

---

## 2. KẾT QUẢ KIỂM THỬ HIỆN TẠI (AUTOMATED TEST COUNT)

**Tổng số kiểm thử tự động:** **165 / 165 tests PASSED (100%)**
- `LabGuard.Protocol.Tests`: **107 / 107 passed** (Kiểm thử FrameCodec, ReadExactlyBytesAsync, chunking 1-byte header/payload, coalescing 3 frames, boundary 262,144 bytes, WireEnvelope, raw JSON validation, 8 yêu cầu wire contract, 16 canonical fixtures minified/no-BOM/DTO deserialization, strict casing, duplicate keys, non-object payloads, strict uppercase 'Z'/'+00:00' UTC designator, ReadEnvelopeAsync strictness).
- `LabGuard.Agent.Tests`: **5 / 5 passed** (Kiểm thử `ReplayBuffer` FIFO, drop-oldest, cờ `isReplay`).
- `LabGuard.Central.Tests`: **53 / 53 passed** (Kiểm thử RuleEngine streak, ClientConnection, DatabaseRepository, OCC Conflict 409, Idempotency, luồng sự cố TCP và từ chối trường lạ qua kết nối TCP thực tế).

*Minh chứng giao diện & báo cáo:*
- Ảnh chụp màn hình Dashboard Milestone 4: [docs/screenshots/dashboard_milestone4.png](docs/screenshots/dashboard_milestone4.png)
- Báo cáo chi tiết các mốc trước: `docs/history/product-milestones/`

---

## 2.1. CHECKPOINT AN TOÀN & HÀNH ĐỘNG TIẾP THEO (CHECKPOINT & NEXT ACTION)

- **Last Safe Checkpoint:** `LG-A-002 CLOSED` (165/165 automated tests pass, zero regressions, zero warnings).
- **Exact Next Action:** Triển khai tiếp nhiệm vụ **`LG-A-003` — Agent TCP connection và writer queue** theo tiến trình Master WBS (`WBS_TASKS.csv`). Không nhảy cóc trực tiếp sang RBAC.

---

## 3. CÁC HẠNG MỤC CẦN HOÀN THIỆN (KNOWN PENDING WORK)

Nhóm sinh viên sẽ tiếp quản và hoàn thiện các hạng mục sau:
1. **Server-side RBAC Enforcement:** Kiểm tra ma trận phân quyền trong `ClientConnection.cs`:
   - `Viewer`: Chỉ đọc/xem danh sách, không được thay đổi sự cố.
   - `Operator`: Được phép `INCIDENT_ACK`, `INCIDENT_START`, `INCIDENT_RESOLVE` (không được `INCIDENT_ASSIGN`).
   - `Admin`: Đầy đủ quyền xử lý bao gồm cả `INCIDENT_ASSIGN`.
2. **Lưu vết `AssignedToUserId`:** Lưu thông tin kỹ thuật viên tiếp nhận sự cố vào bảng `dbo.Incidents` và ghi nhật ký vào bảng `dbo.IncidentHistory`.
3. **Chuẩn hóa mã lỗi `ERR_CONCURRENCY_VIOLATION`:** Trả về đúng mã lỗi canonical quy định trong giao thức khi có xung đột `RowVersion`.
4. **Cô lập CSDL kiểm thử (`LabGuardTestDb`):** Hiện tại bộ test tích hợp `LabGuard.Central.Tests` vẫn đang dùng chung CSDL `LabGuardDb`. Cần cấu hình và tách riêng sang CSDL kiểm thử `LabGuardTestDb`.
5. **Hiển thị Người xử lý trên Admin Dashboard:** Bổ sung cột hoặc nhãn hiển thị kỹ thuật viên được phân công trên bảng sự cố WinForms.

---

## 4. CÁC VẤN ĐỀ VÀ RỦI RO ĐÃ BIẾT (KNOWN ISSUES / BLOCKERS)

| STT | Vấn đề / Rủi ro | Tình trạng hiện tại | Hướng giải quyết của nhóm |
| :---: | :--- | :--- | :--- |
| 1 | `LabGuardTestDb` chưa được cô lập hoàn toàn | Bộ test `LabGuard.Central.Tests` vẫn kết nối trực tiếp vào `LabGuardDb`. | Sinh viên B cấu hình connection string riêng cho test harness. |
| 2 | Đọc chỉ số phần cứng qua Win32 API | Chỉ hoạt động trên môi trường Windows. | Đúng phạm vi phòng máy UIT; bổ sung Linux `/proc` nếu mở rộng P1. |
| 3 | Điều kiện đóng sự cố (`Resolve`) | Server từ chối đóng nếu điều kiện vật lý còn `ACTIVE` hoặc ghi chú giải quyết bị trống. | Ràng buộc nghiệp vụ đúng thiết kế; Sinh viên C hiển thị thông báo lỗi rõ ràng trên UI. |
