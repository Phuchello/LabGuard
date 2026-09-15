# LABGUARD — VIỆC CẦN LÀM (TEAM TODO BOARD)

Bảng phân chia công việc thực tế cho 3 thành viên nhóm trong giai đoạn tự chủ lập trình (Team Takeover).

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
