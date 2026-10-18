# Bản đồ file LabGuard

Đối chiếu main sau PR #11 (`70481fa`). Các liên kết dưới đây trỏ tới file/thư mục đang có; tên module tương lai chỉ là kế hoạch, không phải file đã tồn tại.

| Vùng | File/đường dẫn đang có | Vai trò và giới hạn |
|---|---|---|
| Khởi chạy Agent | [Program.cs](src/LabGuard.Agent/Program.cs), [AgentConsoleApplication.cs](src/LabGuard.Agent/AgentConsoleApplication.cs) | Console bất đồng bộ, hủy tác vụ; kiểm tra cấu hình/thu số liệu/thử TCP. Giám sát mặc định bị chặn |
| Cấu hình/nhật ký Agent | [AgentSettings.cs](src/LabGuard.Agent/AgentSettings.cs), [AgentLog.cs](src/LabGuard.Agent/AgentLog.cs), [mẫu JSON](src/LabGuard.Agent/agentsettings.example.json) | Tạo DeviceId một lần; kiểm tra địa chỉ, chu kỳ, tham chiếu bí mật; ghi nhật ký có giới hạn |
| Phần cốt lõi Agent | [AgentSocketClient.cs](src/LabGuard.Agent/AgentSocketClient.cs), [MetricsCollector.cs](src/LabGuard.Agent/MetricsCollector.cs) | TCP/hàng đợi gửi; thu CPU/RAM/thời gian hoạt động qua Win32. Chưa đọc phản hồi/xác thực/kết nối lại |
| Gửi định kỳ | [MetricsService.cs](src/LabGuard.Agent/MetricsService.cs), [HeartbeatService.cs](src/LabGuard.Agent/HeartbeatService.cs) | Có thành phần và kiểm thử; chương trình khởi chạy chưa bật gửi số liệu/nhịp tim trước AUTH |
| Giao thức | [FrameCodec.cs](src/LabGuard.Protocol/FrameCodec.cs), [WireEnvelope.cs](src/LabGuard.Protocol/WireEnvelope.cs), [cấu trúc trao đổi (DTO)](src/LabGuard.Protocol/Dtos), [mã hóa/giải mã](src/LabGuard.Protocol/Serialization) | Hợp đồng dùng chung, đóng băng |
| Central Server | [Program.cs](src/LabGuard.Central/Program.cs), [CentralServer.cs](src/LabGuard.Central/CentralServer.cs) | Tiếp nhận nhiều kết nối và đọc khung; chưa hoàn tất luồng xác thực/SQL/ACK |
| SQL Server | [schema.sql](database/schema.sql), [seed.sql](database/seed.sql) | Đã có lệnh định nghĩa bảng (DDL); chưa có dữ liệu khởi tạo hoặc tài khoản/mật khẩu mẫu |
| Admin | [Program.cs](src/LabGuard.Admin/Program.cs), [DashboardForm.cs](src/LabGuard.Admin/DashboardForm.cs), [Designer](src/LabGuard.Admin/DashboardForm.Designer.cs), [tài nguyên](src/LabGuard.Admin/Properties/Resources.resx) | Bố cục 0.1; Vy phụ trách giao diện. Quy trình quản lý và hỗ trợ xử lý sự cố chưa hiện thực |
| Simulator | [Program.cs](src/LabGuard.Simulator/Program.cs) | Giữ bản khung; chưa phải công cụ sinh tải hoạt động |
| Công cụ kiểm thử truyền thông TCP | [TcpEcho](tools/TcpEcho), [bộ mô phỏng](tools/mock_harness), [kiểm tra khung](tools/validation/test_frame_codec) | Echo đã có trên main; mô phỏng kiểm tra độc lập, không thay bộ xử lý nghiệp vụ thật |
| Kiểm thử | [Protocol](tests/LabGuard.Protocol.Tests), [Agent](tests/LabGuard.Agent.Tests), [Central](tests/LabGuard.Central.Tests), [MockHarness](tests/LabGuard.MockHarness.Tests) | 195 bài đạt trên Windows được báo cáo từ PR #11, không phải GitHub CI |

## Tài liệu

- [Agent deployment](docs/agent_deployment/README.md): hướng dẫn chạy thật, [verification script](docs/agent_deployment/verify_agent.ps1) và [bằng chứng](docs/agent_deployment/VALIDATION_EVIDENCE.md).
- [WBS](docs/internal/WBS_TASKS.csv), [Excel](LabGuard_Task_Tracker.xlsx), [phân công](docs/internal/TEAM_ASSIGNMENTS.md), [ownership](docs/internal/OWNERSHIP_MATRIX.md), [dependency map](docs/internal/DEPENDENCY_MAP.md).
- [Protocol spec](docs/spec/PROTOCOL_SPEC.md), [contract matrix](docs/spec/CONTRACT_MATRIX.md), [architecture decisions](docs/spec/ARCHITECTURE_DECISIONS.md): hợp đồng đã đóng băng, housekeeping không sửa.
- [Bộ năm sổ tay hướng dẫn LabGuard và cách biên dịch PDF](docs/pdf/README.md), [nguồn LaTeX](docs/pdf/source), [build.ps1](docs/pdf/build.ps1).
- [Hồ sơ kỹ thuật lịch sử](docs/evidence): giữ bằng chứng cũ và mốc nghiệm thu, không dùng số test cũ làm hiện trạng mới.

## Tên còn dự kiến

ReplayBuffer, ServiceMonitor, phần đọc phản hồi/xác thực phiên, DatabaseRepository, RuleEngine, AdminClient và chức năng hỗ trợ xử lý sự cố là phần chưa hoàn tất. Chức năng hỗ trợ dự kiến là thư viện/logic bên trong Admin do Ngọc phụ trách; Vy giữ giao diện, Quang cung cấp dữ liệu qua Central. Các tên thành phần dự kiến không chứng minh đã có file hoặc chức năng hoạt động.

Nhiệm vụ chính thức của Đỗ Minh Ngọc: **Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố tích hợp trong LabGuard**. Chức năng hỗ trợ nằm trong quy trình xử lý sự cố của Admin, dùng dữ liệu có thẩm quyền qua Central; chưa được lập trình.

Gateway/Collector chưa có project, chỉ là P1 tùy chọn. Simulator vẫn placeholder. Build outputs, logs/TRX/cache và deliverables lớn ở local không phải nguồn cần commit.
