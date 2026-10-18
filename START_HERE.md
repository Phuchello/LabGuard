# Bắt đầu với LabGuard

Đọc [README](README.md) để biết phần nào đã chạy; xem [PROJECT_STATE](PROJECT_STATE.md) để đối chiếu lịch sử. Base hiện hành sau PR #11 là `70481fa`. GK là một buổi trình bày đề xuất theo lịch giảng viên, không phải hạn hoàn thành LabGuard; kế hoạch dưới đây áp dụng cho toàn đồ án.

## Cài và chạy

Windows, SDK .NET 9; PowerShell 7 cho script tái tạo. Chạy `dotnet build LabGuard.sln`, `dotnet test LabGuard.sln` hoặc [script verification](docs/agent_deployment/verify_agent.ps1). 195 tests là kết quả Windows local đã báo cáo từ PR #11, không phải CI.

- Agent: theo [docs/agent_deployment/README.md](docs/agent_deployment/README.md), tạo config rồi chọn validate, collect-once hoặc development probe. Launcher không còn chỉ in status. Không có mode monitoring bỏ qua session/AUTH.
- Central: `dotnet run --project src/LabGuard.Central` mở listener 5000; nhận HELLO không chứng minh auth/lưu SQL.
- Admin: `dotnet run --project src/LabGuard.Admin` mở layout hiện có, chưa chứng minh data binding/quy trình xử lý sự cố đầy đủ.
- TcpEcho: `dotnet run --project tools/TcpEcho`.
- Mock: mở [mock harness](tools/mock_harness); không thay Central nghiệp vụ hoặc task Relay.

## Phúc — Agent & Protocol

Mở [Program.cs](src/LabGuard.Agent/Program.cs), [AgentConsoleApplication.cs](src/LabGuard.Agent/AgentConsoleApplication.cs), [AgentSettings.cs](src/LabGuard.Agent/AgentSettings.cs), [AgentStartupTests.cs](tests/LabGuard.Agent.Tests/AgentStartupTests.cs). A-001..A-004 giữ lịch sử XONG ở cấp component. PR #11 bổ sung startup/config/local collection/probe; không đóng A-005/A-006.

Tiếp theo: phối hợp Quang hoàn thiện TLS/session/HELLO/AUTH, reader/liveness, A-005 reconnect và A-006 ReplayBuffer/ACK; rồi mới ghép MetricsService/HeartbeatService vào monitoring. Kiểm tra silent disconnect, shutdown, replay không trùng và LAN thật trước Service. Không sửa hợp đồng đã đóng băng để vượt gate.

## Quang — Central & SQL Server

Mở [CentralServer.cs](src/LabGuard.Central/CentralServer.cs), [schema.sql](database/schema.sql), [Central tests](tests/LabGuard.Central.Tests). B-001 XONG; B-005 chỉ có DDL và tham chiếu package, persistence còn đang làm. B-002 AUTH là gate của telemetry; rules, RBAC, transaction, dedup và BATCH_ACK sau commit còn cần hiện thực/kiểm chứng.

B-010 bổ sung mapping context có thẩm quyền cho Assistant sau khi các chức năng cốt lõi sẵn sàng. Dữ liệu thiết bị/sự cố phải qua Central; thiếu history/timestamp phải hiện rõ. Hai giờ dự kiến của B-010 không bao gồm hoàn thành core đang thiếu.

## Vy — Admin WinForms

Mở [DashboardForm.cs](src/LabGuard.Admin/DashboardForm.cs) và [Designer](src/LabGuard.Admin/DashboardForm.Designer.cs). C-001 layout 0.1 đang làm; C-002 kết nối Central, grids và workflow C-003..C-011 chưa nghiệm thu.

C-012 nhúng logic Ngọc vào chi tiết sự cố, giữ selection/device identity, permission, OCC và handler hiện hành. Không query Agent hoặc SQL trực tiếp. Ba giờ dự kiến ghép UI không bao gồm hoàn thiện networking/workflow còn thiếu. Thiết kế phải phân biệt connectivity, monitoring condition và quy trình xử lý sự cố.

## Ngọc — Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố

Giữ D-001 nền tảng TCP, D-002 Echo đã XONG và D-003 Relay chưa nghiệm thu. Mở [WBS](docs/internal/WBS_TASKS.csv), [phân công](docs/internal/TEAM_ASSIGNMENTS.md), [Admin](src/LabGuard.Admin) và [TcpEcho](tools/TcpEcho).

D-007..D-010 là phần việc chính thức, đều CHƯA LÀM: rà nguồn/missing/stale evidence → thiết kế quy tắc → thư viện logic nội bộ → kiểm tra tích hợp với B-010/C-012. Assistant nằm trong LabGuard Admin và trình bày **Bằng chứng → Nhận định → Hướng kiểm tra → Thao tác xử lý**. Gợi ý có giới hạn, không tự sửa máy hoặc tự Resolve sự cố.

Giai đoạn đầu dùng required device/incident data qua Central và hiện rõ evidence thiếu. History SQL qua optional GET_METRICS/METRICS_RESULT chỉ xét khi được chọn và có backend; không tạo history/timestamp giả. Gateway/Collector là P1 tùy chọn; Simulator giữ placeholder. Không tạo executable Troubleshooting riêng.

## Kế hoạch thực hiện toàn đồ án

1. Kết nối TCP, giao thức và giao diện ban đầu.
2. Thu thập, lưu dữ liệu, heartbeat và phục hồi kết nối.
3. Phát hiện sự cố, quản lý xử lý và hỗ trợ quyết định tích hợp.
4. Tích hợp, xử lý lỗi và kiểm thử thực tế.
5. Mở rộng có điều kiện sau khi phần cốt lõi ổn định.

Các giai đoạn là tương đối; WBS giữ dependencies/offsets lịch sử, không biến 19–26/10 thành nhật ký báo cáo hằng ngày. Theo dõi 40 task trong [Excel](LabGuard_Task_Tracker.xlsx); [dependency map](docs/internal/DEPENDENCY_MAP.md) nêu gate tích hợp.

## Handbook

[Master](docs/pdf/LabGuard_Tong_Quan_A-Z.pdf), [Phúc](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_A.pdf), [Quang](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_B.pdf), [Vy](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_C.pdf), [Ngọc](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_D.pdf). Tài liệu giáo dục phân biệt hiện trạng với kế hoạch; nguồn và hướng dẫn build ở [docs/pdf](docs/pdf/README.md).
