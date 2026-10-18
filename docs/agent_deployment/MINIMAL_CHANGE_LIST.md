# Danh sách thay đổi tối thiểu

Phạm vi nhánh review: workstream **Phúc/Agent** và test Agent, tài liệu triển khai. Phần Quang chỉ mô tả đầu vào cần bàn giao; không sửa Central, schema, protocol, WBS hoặc tài liệu GK. Người điều phối đã cho phép commit/push lên nhánh review và tạo Draft PR; chưa cho phép merge hoặc ghi trực tiếp main.

| Tệp/phần | Thay đổi đã thực hiện, có kiểm tra local |
|---|---|
| Agent/Program.cs | Async Console entry point, parse lệnh, Ctrl+C, exit codes; bỏ thông báo Central chưa hiện thực |
| Agent cấu hình | JSON strict, init không ghi đè, DeviceId ổn định trong file, host/port không hardcode, timeout/intervals hợp lệ, tham chiếu xác thực và log |
| Agent launcher | Validate, collection cục bộ và TCP probe qua AgentSocketClient; normal monitoring fail-closed vì chưa có TLS/AUTH |
| Agent logging | Timestamp/event/mức log rõ ràng, console + file tùy chọn có giới hạn kích thước; không token/raw payload hoặc tuyên bố commit/ACK giả |
| Agent csproj/sample | Copy mẫu cấu hình khi publish, không thêm framework/dependency mới hoặc đổi net9 |
| Agent.Tests | Kiểm tra config/identity/launcher/cancel/actual Central probe; thêm project reference Central chỉ trong test nếu cần |
| docs/agent_deployment | Bốn tài liệu này, hướng dẫn chạy lại và tóm tắt kết quả thật; log/TRX/artifacts giữ local, không đưa vào PR |

Không sửa AgentSocketClient, MetricsCollector, MetricsService, HeartbeatService hoặc mạng lõi trong bước launcher. Không tự thêm reader/TLS/auth/reconnect/replay dưới nhãn “startup”. Không thêm bypass monitoring chưa xác thực.

Quang cần bàn giao riêng: phiên TLS và HELLO/AUTH, identity binding, đọc/điều phối lệnh; liveness; transaction lưu mẻ/mẫu và dedup; ACK sau commit; hành vi SQL lỗi. Phúc nối reader/session và reconnect vào transport theo hợp đồng hiện có khi có thể kiểm chứng. Vy/Ngọc chỉ dựa dữ liệu Central được phép sau đó.

PR chứa diff tám file Agent/test và tài liệu Markdown/hướng dẫn tái tạo; không suy PASS của component thành readiness triển khai. Danh mục thực tế, kết quả và giới hạn ở `VALIDATION_EVIDENCE.md`.
