# Bắt đầu với LabGuard

Đọc [README](README.md) để biết phần nào đã chạy; xem [PROJECT_STATE](PROJECT_STATE.md) để đối chiếu lịch sử. Base hiện hành sau PR #11 là `70481fa`. GK là một buổi trình bày đề xuất theo lịch giảng viên, không phải hạn hoàn thành LabGuard; kế hoạch dưới đây áp dụng cho toàn đồ án.

## Cài và chạy

Windows, SDK .NET 9; PowerShell 7 để chạy script tái tạo. Chạy `dotnet build LabGuard.sln`, `dotnet test LabGuard.sln` hoặc [script kiểm tra](docs/agent_deployment/verify_agent.ps1). 195 bài kiểm thử đạt là kết quả Windows tại máy đã báo cáo từ PR #11, không phải hệ thống kiểm thử tự động GitHub CI.

- Agent: theo [docs/agent_deployment/README.md](docs/agent_deployment/README.md), tạo cấu hình rồi chọn kiểm tra cấu hình, thu số liệu một lần hoặc thử kết nối TCP. Chương trình khởi chạy không còn chỉ in trạng thái. Không có chế độ giám sát bỏ qua phiên kết nối và xác thực AUTH.
- Central: `dotnet run --project src/LabGuard.Central` tiếp nhận TCP trên cổng 5000; nhận HELLO không chứng minh đã xác thực hoặc lưu SQL.
- Admin: `dotnet run --project src/LabGuard.Admin` mở bố cục hiện có, chưa chứng minh đã liên kết dữ liệu hoặc hoàn tất quy trình xử lý sự cố.
- TcpEcho: `dotnet run --project tools/TcpEcho`.
- Mô phỏng: mở [bộ kiểm thử mô phỏng](tools/mock_harness); không thay Central nghiệp vụ hoặc nhiệm vụ chuyển tiếp TCP.

## Phúc — Agent & Protocol

Mở [Program.cs](src/LabGuard.Agent/Program.cs), [AgentConsoleApplication.cs](src/LabGuard.Agent/AgentConsoleApplication.cs), [AgentSettings.cs](src/LabGuard.Agent/AgentSettings.cs), [AgentStartupTests.cs](tests/LabGuard.Agent.Tests/AgentStartupTests.cs). A-001..A-004 giữ lịch sử XONG ở cấp thành phần. PR #11 bổ sung khởi chạy, cấu hình, thu số liệu cục bộ và thử TCP; không hoàn tất A-005/A-006.

Tiếp theo: phối hợp Quang hoàn thiện bảo mật TLS, phiên kết nối và HELLO/AUTH; bổ sung đọc phản hồi, phát hiện mất kết nối, A-005 kết nối lại và A-006 ReplayBuffer/ACK. Sau đó mới ghép MetricsService/HeartbeatService vào chế độ giám sát. Kiểm tra phía bên kia đóng kết nối, tắt chương trình, gửi lại không trùng và LAN thật trước khi triển khai Windows Service. Không sửa hợp đồng đóng băng để bỏ qua điều kiện này.

## Quang — Central & SQL Server

Mở [CentralServer.cs](src/LabGuard.Central/CentralServer.cs), [schema.sql](database/schema.sql), [kiểm thử Central](tests/LabGuard.Central.Tests). B-001 XONG; B-005 mới có định nghĩa bảng và tham chiếu thư viện, phần lưu dữ liệu còn đang làm. B-002 AUTH là điều kiện nhận số liệu. Quy tắc cảnh báo, phân quyền theo vai trò (RBAC), giao dịch SQL, chống trùng và BATCH_ACK sau khi ghi thành công còn cần hiện thực/kiểm chứng.

B-010 bổ sung ánh xạ dữ liệu ngữ cảnh có thẩm quyền cho chức năng hỗ trợ xử lý sự cố sau khi các chức năng cốt lõi sẵn sàng. Dữ liệu thiết bị/sự cố phải qua Central; thiếu lịch sử/thời điểm phải hiện rõ. Hai giờ dự kiến của B-010 không bao gồm hoàn thành phần cốt lõi đang thiếu.

## Vy — Admin WinForms

Mở [DashboardForm.cs](src/LabGuard.Admin/DashboardForm.cs) và [Designer](src/LabGuard.Admin/DashboardForm.Designer.cs). C-001 bố cục 0.1 đang làm; C-002 kết nối Central, các bảng hiển thị và quy trình xử lý C-003..C-011 chưa nghiệm thu.

C-012 nhúng logic của Ngọc vào chi tiết sự cố, giữ thiết bị đang chọn, định danh, quyền, kiểm tra xung đột cập nhật (OCC) và bộ xử lý lệnh hiện hành. Không truy vấn Agent hoặc SQL trực tiếp. Ba giờ dự kiến ghép giao diện không bao gồm hoàn thiện kết nối/quy trình xử lý còn thiếu. Phải phân biệt kết nối Agent, điều kiện cảnh báo và trạng thái xử lý sự cố.

## Ngọc — Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố tích hợp trong LabGuard

Giữ D-001 nền tảng TCP, D-002 gửi lại dữ liệu (Echo) đã XONG và D-003 chuyển tiếp dữ liệu (Relay) chưa nghiệm thu. Mở [WBS](docs/internal/WBS_TASKS.csv), [phân công](docs/internal/TEAM_ASSIGNMENTS.md), [Admin](src/LabGuard.Admin) và [TcpEcho](tools/TcpEcho). Các công cụ phục vụ kiểm tra kết nối, gửi, nhận và chuyển tiếp dữ liệu.

D-007..D-010 là phần việc chính thức, đều CHƯA LÀM: rà nguồn và bằng chứng thiếu/cũ → thiết kế quy tắc → thư viện logic nội bộ → kiểm tra tích hợp với B-010/C-012. Chức năng hỗ trợ xử lý sự cố nằm trong LabGuard Admin và trình bày **Bằng chứng → Nhận định → Hướng kiểm tra → Thao tác xử lý**. Gợi ý có giới hạn, không tự sửa máy hoặc tự Resolve sự cố.

Giai đoạn đầu dùng dữ liệu thiết bị/sự cố bắt buộc qua Central và hiện rõ bằng chứng thiếu. Lịch sử SQL qua GET_METRICS/METRICS_RESULT chỉ xét khi nhóm chọn phần tùy chọn này và bộ xử lý đã sẵn sàng; không tạo lịch sử/thời điểm giả. Gateway/Collector là mở rộng tùy chọn sau phần cốt lõi; Simulator giữ bản khung. Không tạo chương trình hỗ trợ xử lý sự cố độc lập.

## Kế hoạch thực hiện toàn đồ án

1. Kết nối TCP, giao thức và giao diện ban đầu.
2. Thu thập, lưu dữ liệu, heartbeat và phục hồi kết nối.
3. Phát hiện sự cố, quản lý xử lý và hỗ trợ quyết định tích hợp.
4. Tích hợp, xử lý lỗi và kiểm thử thực tế.
5. Mở rộng có điều kiện sau khi phần cốt lõi ổn định.

Các giai đoạn là tương đối; WBS giữ dependencies/offsets lịch sử, không biến 19–26/10 thành nhật ký báo cáo hằng ngày. Theo dõi 40 task trong [Excel](LabGuard_Task_Tracker.xlsx); [dependency map](docs/internal/DEPENDENCY_MAP.md) nêu điều kiện tích hợp.

## Bộ năm sổ tay hướng dẫn LabGuard

[Master](docs/pdf/LabGuard_Tong_Quan_A-Z.pdf), [Phúc](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_A.pdf), [Quang](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_B.pdf), [Vy](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_C.pdf), [Ngọc](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_D.pdf). Tài liệu giáo dục phân biệt hiện trạng với kế hoạch; nguồn và hướng dẫn build ở [docs/pdf](docs/pdf/README.md).
