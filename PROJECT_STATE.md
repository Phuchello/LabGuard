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

## 2. KẾT QUẢ KIỂM THỬ HIỆN TẠI (AUTOMATED TEST COUNT)

**Tổng số kiểm thử tự động:** **77 / 77 tests PASSED (100%)**
- `LabGuard.Protocol.Tests`: **20 / 20 passed** (Kiểm thử FrameCodec, DTO serialize/deserialize).
- `LabGuard.Agent.Tests`: **5 / 5 passed** (Kiểm thử `ReplayBuffer` FIFO, drop-oldest, cờ `isReplay`).
- `LabGuard.Central.Tests`: **52 / 52 passed** (Kiểm thử RuleEngine streak, ClientConnection, DatabaseRepository, OCC Conflict 409, Idempotency và luồng sự cố TCP).

*Minh chứng giao diện & báo cáo:*
- Ảnh chụp màn hình Dashboard Milestone 4: [docs/screenshots/dashboard_milestone4.png](docs/screenshots/dashboard_milestone4.png)
- Báo cáo chi tiết các mốc trước: `docs/history/product-milestones/`

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
