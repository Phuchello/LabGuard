# LabGuard — Giám sát phòng máy tập trung

Đồ án NT106, Nhóm 5, UIT. LabGuard dự kiến thu thập thông tin máy trạm qua Agent, xử lý tại Central và hỗ trợ quản trị trong Admin WinForms. Thiết kế tổng thể đang được hiện thực từng phần; chưa có luồng giám sát từ Agent qua Central đến Admin được nghiệm thu.

## Hiện trạng sau PR #11

Mốc nguồn: [PR #11](https://github.com/Phuchello/LabGuard/pull/11), merge `70481fa`, ngày 18/10/2026. [PROJECT_STATE.md](PROJECT_STATE.md) giữ lịch sử và ranh giới nghiệm thu.

| Phần | Đã có | Chưa hoàn tất |
|---|---|---|
| Protocol | Envelope, FrameCodec, DTO và fixtures dùng chung | Hợp đồng đóng băng; không sửa tùy tiện |
| Agent | Launcher Console, JSON config, DeviceId mỗi installation, collector CPU/RAM/uptime Windows, development TCP/HELLO probe | Chế độ giám sát bị chặn có chủ đích tới khi có TLS/session/AUTH; A-005 reconnect, A-006 ReplayBuffer, ServiceMonitor và Windows Service chưa hoàn tất |
| Central | Listener nhiều client, framing và đọc HELLO | Authentication, liveness, rules, persistence SQL và ACK sau commit |
| SQL Server | DDL ba bảng đã merge | B-005 C# persistence/tích hợp SQL chưa nghiệm thu; seed chưa có dữ liệu |
| Admin | Layout WinForms 0.1 | C-001 đang làm; binding, kết nối Central và quy trình xử lý sự cố còn trong kế hoạch |
| Công cụ kiểm thử TCP | TcpEcho; mock_harness kiểm thử giao thức độc lập | Relay chưa nghiệm thu; mock không chứng minh Central nghiệp vụ đã chạy |
| Hỗ trợ phân tích, xử lý sự cố (Troubleshooting Assistant) | Phân công chính thức và kế hoạch tích hợp | Chưa hiện thực: nằm trong quy trình xử lý sự cố Admin, dùng dữ liệu có thẩm quyền qua Central |
| Mở rộng tùy chọn | Giữ placeholder Simulator | Gateway/Collector và sinh tải chỉ xét sau khi phần cốt lõi ổn định |

Kết nối TCP thành công chưa chứng minh đã xác thực ứng dụng, nhận số liệu, ghi SQL hoặc nhận ACK ứng dụng. Chế độ thử trả exit 0 chỉ xác nhận lệnh kết thúc; chưa có phần đọc phản hồi/kiểm tra mất kết nối để luôn phát hiện phía bên kia đã đóng kết nối.

## Chạy kiểm tra trên Windows

Cần SDK .NET 9 và PowerShell 7. Không cần cài Windows Service để thử Console. Hướng dẫn đầy đủ: [Agent deployment](docs/agent_deployment/README.md).

```powershell
dotnet build LabGuard.sln
dotnet test LabGuard.sln
dotnet run --project src/LabGuard.Agent -- --init-config ./scratch/agentsettings.json --central-host 127.0.0.1 --central-port 5000
dotnet run --project src/LabGuard.Agent -- --config ./scratch/agentsettings.json --validate-config
dotnet run --project src/LabGuard.Agent -- --config ./scratch/agentsettings.json --collect-once
```

Để probe local, chạy Central trong Console riêng rồi chạy Agent:

```powershell
dotnet run --project src/LabGuard.Central
# Console khác:
dotnet run --project src/LabGuard.Agent -- --config ./scratch/agentsettings.json --probe
```

127.0.0.1 chỉ dùng thử trên cùng máy; tạo cấu hình riêng và địa chỉ máy chủ được phép cho mỗi PC. Init không ghi đè DeviceId. Không đưa cấu hình có định danh hoặc bí mật vào Git. Chạy Agent không chọn chế độ thử sẽ dừng với exit 5: chế độ giám sát đang bị chặn theo thiết kế hiện tại. Không tự triển khai lên máy trường khi chưa được phép.

**Verification được báo cáo từ PR #11:** 195 tests passed trên Windows (Protocol 107, Agent 64, Central 9, MockHarness 15), build 0 warning/error. Đây là kết quả local, không phải GitHub CI và không chứng minh SQL/ACK/LAN/Service đã sẵn sàng. [Chi tiết và script chạy lại](docs/agent_deployment/VALIDATION_EVIDENCE.md).

## Phân công chính thức

Nguồn: quyết định trực tiếp của người điều phối ngày 17/10/2026, ghi tại [TEAM_ASSIGNMENTS.md](docs/internal/TEAM_ASSIGNMENTS.md).

| Mã | Thành viên | Trách nhiệm |
|---|---|---|
| A | Võ Trọng Phúc | Agent & Protocol |
| B | Nguyễn Bá Quang | Central & SQL Server |
| C | Lý Tuyết Vy | Admin WinForms |
| D | Đỗ Minh Ngọc | Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố |

Assistant là kế hoạch phát triển chính thức của Ngọc, phối hợp Quang/Vy; không phải ứng dụng TCP chẩn đoán độc lập. Gateway/Collector là mở rộng tùy chọn.

## Tài liệu và kế hoạch

- [START_HERE.md](START_HERE.md): bắt đầu theo phần việc.
- [FILE_MAP.md](FILE_MAP.md): file đang có và thành phần còn dự kiến.
- [TODO.md](TODO.md), [WBS 40 task](docs/internal/WBS_TASKS.csv), [tracker Excel](LabGuard_Task_Tracker.xlsx): kế hoạch/trạng thái; không nâng A-005/A-006 thành DONE do có launcher.
- [Ownership](docs/internal/OWNERSHIP_MATRIX.md), [dependencies](docs/internal/DEPENDENCY_MAP.md).
- [Năm handbook và cách rebuild PDF](docs/pdf/README.md): tài liệu học/triển khai; các ví dụ tương lai không phải bằng chứng đã chạy.

.NET 10 là công việc migration riêng cho toàn solution. PR housekeeping này giữ .NET 9, schema và giao thức hiện tại.
