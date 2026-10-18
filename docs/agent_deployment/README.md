# Agent deployment readiness — review 18/10/2026

Launcher Console đã thay màn hình status cũ: cấu hình JSON strict, DeviceId tạo một lần, collector Windows tại máy, TCP/HELLO probe qua core hiện có, cancellation và log giới hạn. **Chưa phải Agent monitoring hoàn chỉnh:** mặc định dừng exit 5; chưa TLS/session/AUTH, reconnect, telemetry→SQL hoặc ACK.

- [Audit main và giới hạn thực tế](SOURCE_READINESS_AUDIT.md).
- [Triển khai theo giai đoạn A–F](AGENT_DEPLOYMENT_PLAN.md).
- [Tập thay đổi nhỏ nhất và ownership](MINIMAL_CHANGE_LIST.md).
- [Phụ thuộc, .NET 10 và kế hoạch test](DEPENDENCY_AND_TEST_PLAN.md).
- [Kết quả thật và phần chưa kiểm chứng](VALIDATION_EVIDENCE.md).

## Chạy lại trên Windows

Từ repo root, dùng PowerShell 7 và SDK .NET 9 tương thích:

```powershell
./docs/agent_deployment/verify_agent.ps1
./docs/agent_deployment/verify_agent.ps1 -Publish
```

Script build/test toàn solution; tùy chọn publish self-contained win-x64 và chạy smoke config/local collection/fail-closed. Mỗi lần tạo thư mục riêng dưới `scratch/agent-readiness/`; không đóng ZIP, không ghi Git, không cài Service hoặc kết nối host từ xa. Logs/TRX/artifacts ở local, không commit. Restore/publish có thể cần truy cập NuGet. SDK 10 chưa được kiểm chứng và không retarget riêng Agent.

## Cấu hình từng máy

Sau khi publish, copy **toàn bộ** thư mục publish. Tại thư mục đó:

```powershell
$centralHost = Read-Host 'Central development host được phép'
./LabGuard.Agent.exe --init-config ./config/agentsettings.json --central-host $centralHost --central-port 5000
./LabGuard.Agent.exe --config ./config/agentsettings.json --validate-config
./LabGuard.Agent.exe --config ./config/agentsettings.json --collect-once
./LabGuard.Agent.exe --config ./config/agentsettings.json --probe
```

Tạo config riêng trên mỗi PC, không clone file đã có DeviceId. Init từ chối ghi đè. `agentsettings.example.json` cố ý không có identity/host thật và cần điền hoặc init trước khi dùng. `AuthenticationTokenEnvironmentVariable` chỉ là tên tham chiếu, launcher chưa đọc secret. Không lưu token vào JSON/log. Giới hạn quyền đọc/sửa config theo tài khoản vận hành.

`--probe` dùng TCP thường chỉ trên endpoint phát triển được phép, gửi HELLO không bí mật, không heartbeat/metric/token. `HelloQueued` không phải HELLO_ACK; đối chiếu log Central mới biết frame được nhận. Exit 0 chỉ là lệnh hoàn tất; peer đóng im lặng có thể chưa được phát hiện. Ctrl+C đóng transport, không bảo đảm drain/delivery hàng đợi.

Log file tùy chọn là đường dẫn tương đối với thư mục config hoặc absolute. Append có giới hạn; khi đầy ngừng ghi file và báo Console, không xóa hoặc rotate lịch sử. Exit codes: 0 command completed, 2 config/CLI, 3 runtime, 4 TCP, 5 session unavailable, 130 launcher cancellation. PowerShell/PTY có thể trả mã wrapper khác khi Ctrl+C.

## Ranh giới PR

Phúc/Agent và tests liên quan; reference Central chỉ trong test để dùng listener thật. Không sửa Central, Admin, protocol, schema, WBS hoặc tài liệu GK. Những thay đổi phân công/WBS và báo cáo SQL có sẵn ở checkout gốc được giữ ngoài PR. Người điều phối cho phép nhánh review và Draft PR; chưa merge.

Logs/TRX gốc, snapshot nguồn, checksums GK, package cũ và bằng chứng terminal được giữ local ở checkout gốc `docs/agent_deployment/evidence/` và `deliverables/`; không bị xóa để làm gọn PR. Review trên GitHub dùng diff, test source, kết quả tóm tắt và script tái tạo, không cần artifacts lớn hay đường dẫn cá nhân.
