# LabGuard — Việc tiếp theo

Nguồn kế hoạch: [WBS 40 task](docs/internal/WBS_TASKS.csv), [tracker Excel](LabGuard_Task_Tracker.xlsx), [dependencies](docs/internal/DEPENDENCY_MAP.md). Tên người phụ trách theo [quyết định 17/10/2026](docs/internal/TEAM_ASSIGNMENTS.md). [PROJECT_STATE](PROJECT_STATE.md) giữ lịch sử nghiệm thu.

## Đã có, không mở lại task để ghi tiến độ giả

- [x] A-001..A-004: giao thức, đóng khung, truyền dữ liệu/hàng đợi gửi và thu/gửi số liệu định kỳ ở cấp thành phần.
- [x] B-001: Central tiếp nhận TCP (PR #7).
- [x] D-002: TCP Echo (PR #8); bộ kiểm thử mô phỏng bổ trợ đã có trên main qua PR #9.
- [x] PR #11: Agent khởi chạy/cấu hình/DeviceId/thu số liệu cục bộ/thử TCP. **Không đồng nghĩa A-005/A-006 XONG.**

## Phúc — Agent & Protocol

- [ ] Phối hợp Quang: đọc phản hồi phiên kết nối, TLS/HELLO/AUTH; chỉ giám sát khi phiên hợp lệ.
- [ ] A-005 kết nối lại với thời gian chờ tăng dần; A-006 ReplayBuffer/ACK; A-007 kiểm tra dịch vụ; A-008/A-009 lỗi mạng/LAN theo WBS.
- [ ] Windows Service sau khi khởi chạy/tắt/ghi nhật ký/kết nối lại được xác minh; không yêu cầu Windows Service để thử Console.

## Quang — Central & SQL Server

- [ ] B-002..B-004: xác thực AUTH, phát hiện mất kết nối, phân quyền theo vai trò (RBAC).
- [ ] B-005 đang làm: lưu dữ liệu SQL, giao dịch/chống trùng và ACK sau khi ghi thành công; định nghĩa bảng đã có.
- [ ] B-006..B-009: quy tắc cảnh báo, quản lý sự cố/kiểm tra xung đột cập nhật (OCC) và kiểm tra SQL.
- [ ] Trước B-009: giải quyết yêu cầu lịch sử AssignedToUserId/IncidentHistory không khớp schema AssigneeId hiện tại; không tự thêm cột/bảng hoặc đánh dấu DONE.
- [ ] B-010: dữ liệu ngữ cảnh có thẩm quyền cho chức năng hỗ trợ xử lý sự cố; 2 giờ ghép bổ sung không bao gồm phần cốt lõi chưa sẵn sàng.

## Vy — Admin WinForms

- [ ] C-001 đang làm; C-002 kết nối Central; C-003..C-008, C-010/C-011 bảng hiển thị/quy trình xử lý/quyền/kiểm tra xung đột cập nhật.
- [ ] C-012: nhúng chức năng hỗ trợ xử lý sự cố trong chi tiết sự cố; 3 giờ ghép không bao gồm kết nối/quy trình xử lý còn thiếu.

## Ngọc — Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố tích hợp trong LabGuard

- [ ] D-001 hồ sơ hiểu TCP/mẫu JSON và D-003 chuyển tiếp TCP; giữ lịch sử D-002 XONG.
- [ ] D-007 rà nguồn và bằng chứng thiếu/cũ; D-008 thiết kế quy tắc; D-009 logic nội bộ Admin; D-010 tích hợp và xác minh cùng B/C.
- [ ] Luồng Bằng chứng → Nhận định → Hướng kiểm tra → Thao tác xử lý; tích hợp trong Admin, dùng dữ liệu qua Central, không tự sửa máy hoặc đóng sự cố.
- [ ] Lịch sử SQL qua GET_METRICS/METRICS_RESULT là tùy chọn riêng; trước mắt hiện rõ dữ liệu thiếu.
- [ ] D-004..D-006 Gateway/Collector/Simulator chỉ xét sau khi phần cốt lõi ổn định, không phải cam kết chính của Ngọc.

## Toàn nhóm

- [ ] Tích hợp năm mốc: kết nối TCP → xác thực → nhận số liệu → ghi SQL thành công → nhận ACK, với bằng chứng cho từng mốc.
- [ ] Khi mã đã sẵn sàng, kiểm tra nhiều máy LAN được phép, mất mạng, kết nối lại, mất ACK/gửi lại/chống trùng và lỗi SQL.
- [ ] Nâng cấp .NET 10 toàn bộ dự án là việc riêng; lượt này giữ nguyên nền tảng đích .NET 9.

Kiểm thử được báo cáo từ PR #11: **195 bài đạt trên Windows tại máy**, không phải GitHub CI. Chạy lại bằng [script](docs/agent_deployment/verify_agent.ps1). Không biến kế hoạch toàn đồ án thành lịch chuẩn bị PowerPoint 19–26/10.
