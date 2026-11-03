# LabGuard — Việc tiếp theo

Nguồn kế hoạch: [WBS 40 task](docs/internal/WBS_TASKS.csv), [tracker Excel](LabGuard_Task_Tracker.xlsx), [dependencies](docs/internal/DEPENDENCY_MAP.md). Tên người phụ trách theo [quyết định 17/10/2026](docs/internal/TEAM_ASSIGNMENTS.md). [PROJECT_STATE](PROJECT_STATE.md) giữ lịch sử nghiệm thu.

## Việc điều phối thêm từ QA 03/11/2026 (đề xuất; không đổi WBS)

- [ ] Nhóm xác định baseline source local/secure và review hashes, contract trước khi đưa vào nhánh tích hợp.
- [ ] A/B/C chốt DB-01, C3, C4; C1/C2 trước các lệnh incident có mutation.
- [ ] A/B/C tiếp tục Foundations: Agent TLS/AUTH → SQL COMMIT/ACK → Admin authorized read và P0 NIC/SERVICE_STATUS.
- [x] **Evidence riêng**: Windows QA chạy 89/89 unit tests EvidenceEvaluator ngày 03/11; không dùng dấu x này để tự đánh dấu D-009 hoặc các task chính thức hoàn thành.
- [ ] D/A/B/C xin duyệt scope E0 Runner + 2 máy lab + DNS/HTTPS test + fault rollback; chưa chạy probes.
- [ ] Nhóm review [dự thảo GK](docs/gk/REPORT_DRAFT_2026-11-03.md), [bằng chứng](docs/gk/STATUS_2026-11-03.md) và [checklist](docs/internal/GK_TEAM_REVIEW_2026-11-03.md).

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
- [ ] B-008 — TC-01/TC-02/TC-03 **CHƯA LÀM**: môi trường Testcontainers opt-in, schema SQL Server 2022 cô lập, rồi persistence/chống trùng/rollback/commit-before-ACK sau mã B-005. `Testcontainers.MsSql` 4.16.0 là bản tham chiếu chờ kiểm tra tương thích. Unit test thường không cần Docker; production không dùng Docker. Bổ sung **7–10 giờ công chờ Quang rà soát**, không thay 2,5 giờ lịch sử. Mốc và tiêu chí tại [kế hoạch](docs/internal/DEPENDENCY_MAP.md#công-nghệ-đã-duyệt--chưa-hiện-thực), đã gắn vào WBS/Excel; schema test không hoàn thành B-005.

## Vy — Admin WinForms

- [ ] C-001 đang làm: DashboardForm 0.1 là prototype có thể bỏ; nhóm dự kiến thiết kế lại Admin WinForms, không phải giữ bố cục/control cũ. UI mới chưa hiện thực. C-002 kết nối Central; C-003..C-008, C-010/C-011 giữ chức năng thiết bị/sự cố/quyền/kiểm tra xung đột cập nhật.
- [ ] C-012: nhúng chức năng hỗ trợ xử lý sự cố trong chi tiết sự cố; 3 giờ ghép không bao gồm kết nối/quy trình xử lý còn thiếu.
- [ ] C-003/C-004 — SC-01/SC-02/SC-03 **CHƯA LÀM**: thành phần ScottPlot CPU/RAM dùng lại, tích hợp Admin thiết kế lại, rồi telemetry thật sau C-002/xác thực và nguồn Central hợp lệ. `ScottPlot.WinForms` 5.1.59 là bản tham chiếu chờ kiểm tra tương thích. Giữ chức năng danh sách thiết bị/quản lý sự cố, tách render khỏi nhận TCP; không tự thêm bản tin/API lịch sử hoặc lệnh điều khiển ngoài phạm vi. Bổ sung **6–9 giờ công chung chờ Vy rà soát**, không cộng hai lần hoặc thay 3 + 3 giờ lịch sử; xem [mốc và nghiệm thu](docs/internal/DEPENDENCY_MAP.md#công-nghệ-đã-duyệt--chưa-hiện-thực).

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
