# Bản đồ file LabGuard

Đối chiếu main sau PR #11 (`70481fa`). Các liên kết dưới đây trỏ tới file/thư mục đang có; tên module tương lai chỉ là kế hoạch, không phải file đã tồn tại.

| Vùng | File/đường dẫn đang có | Vai trò và giới hạn |
|---|---|---|
| Agent startup | [Program.cs](src/LabGuard.Agent/Program.cs), [AgentConsoleApplication.cs](src/LabGuard.Agent/AgentConsoleApplication.cs) | Async Console, cancellation; validate/collect/probe. Monitoring mặc định bị chặn |
| Agent config/log | [AgentSettings.cs](src/LabGuard.Agent/AgentSettings.cs), [AgentLog.cs](src/LabGuard.Agent/AgentLog.cs), [mẫu JSON](src/LabGuard.Agent/agentsettings.example.json) | Init DeviceId riêng một lần, kiểm tra endpoint/intervals, secret reference và bounded append log |
| Agent core | [AgentSocketClient.cs](src/LabGuard.Agent/AgentSocketClient.cs), [MetricsCollector.cs](src/LabGuard.Agent/MetricsCollector.cs) | TCP/outbox writer; Win32 CPU/RAM/uptime. Chưa reader/auth/reconnect |
| Agent producers | [MetricsService.cs](src/LabGuard.Agent/MetricsService.cs), [HeartbeatService.cs](src/LabGuard.Agent/HeartbeatService.cs) | Có component gửi định kỳ và test, launcher chưa bật trước AUTH |
| Protocol | [FrameCodec.cs](src/LabGuard.Protocol/FrameCodec.cs), [WireEnvelope.cs](src/LabGuard.Protocol/WireEnvelope.cs), [DTO](src/LabGuard.Protocol/Dtos), [Serialization](src/LabGuard.Protocol/Serialization) | Hợp đồng dùng chung, đóng băng |
| Central | [Program.cs](src/LabGuard.Central/Program.cs), [CentralServer.cs](src/LabGuard.Central/CentralServer.cs) | Listener đa client và đọc frame; chưa pipeline auth/SQL/ACK |
| SQL | [schema.sql](database/schema.sql), [seed.sql](database/seed.sql) | DDL có sẵn; seed chỉ placeholder, không có tài khoản/mật khẩu mẫu |
| Admin | [Program.cs](src/LabGuard.Admin/Program.cs), [DashboardForm.cs](src/LabGuard.Admin/DashboardForm.cs), [Designer](src/LabGuard.Admin/DashboardForm.Designer.cs), [resources](src/LabGuard.Admin/Properties/Resources.resx) | Layout 0.1; C sở hữu UI. Incident workflow/Assistant chưa hiện thực |
| Simulator | [Program.cs](src/LabGuard.Simulator/Program.cs) | Placeholder được giữ; chưa phải công cụ sinh tải chạy thật |
| TCP Tools | [TcpEcho](tools/TcpEcho), [mock harness](tools/mock_harness), [frame validator](tools/validation/test_frame_codec) | Echo đã merge; mocks kiểm tra độc lập, không thay backend thật |
| Tests | [Protocol](tests/LabGuard.Protocol.Tests), [Agent](tests/LabGuard.Agent.Tests), [Central](tests/LabGuard.Central.Tests), [MockHarness](tests/LabGuard.MockHarness.Tests) | 195 tests Windows được báo cáo từ PR #11, không phải CI |

## Tài liệu

- [Agent deployment](docs/agent_deployment/README.md): hướng dẫn chạy thật, [verification script](docs/agent_deployment/verify_agent.ps1) và [evidence](docs/agent_deployment/VALIDATION_EVIDENCE.md).
- [WBS](docs/internal/WBS_TASKS.csv), [Excel](LabGuard_Task_Tracker.xlsx), [phân công](docs/internal/TEAM_ASSIGNMENTS.md), [ownership](docs/internal/OWNERSHIP_MATRIX.md), [dependency map](docs/internal/DEPENDENCY_MAP.md).
- [Protocol spec](docs/spec/PROTOCOL_SPEC.md), [contract matrix](docs/spec/CONTRACT_MATRIX.md), [architecture decisions](docs/spec/ARCHITECTURE_DECISIONS.md): hợp đồng đã đóng băng, housekeeping không sửa.
- [PDF handbook index/build](docs/pdf/README.md), [LaTeX source](docs/pdf/source), [build.ps1](docs/pdf/build.ps1).
- [Hồ sơ kỹ thuật lịch sử](docs/evidence): giữ bằng chứng cũ và mốc nghiệm thu, không dùng số test cũ làm hiện trạng mới.

## Tên còn dự kiến

ReplayBuffer, ServiceMonitor, session reader/auth, DatabaseRepository, RuleEngine, AdminClient và Assistant là phần chưa hoàn tất. Dự kiến code Assistant là thư viện/logic dùng bên trong Admin với D phụ trách, C sở hữu host UI và B sở hữu nguồn Central. Không tạo đường dẫn giả như thể file đã có.

Nhiệm vụ chính thức của Đỗ Minh Ngọc: **Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố**. Chức năng hỗ trợ nằm trong quy trình xử lý sự cố của Admin, dùng dữ liệu có thẩm quyền qua Central; chưa được lập trình.

Gateway/Collector chưa có project, chỉ là P1 tùy chọn. Simulator vẫn placeholder. Build outputs, logs/TRX/cache và deliverables lớn ở local không phải nguồn cần commit.
