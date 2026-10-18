# Kế hoạch triển khai Agent Windows

Một binary dùng lại cho nhiều máy, mỗi máy có một cấu hình riêng. Phúc phụ trách launcher/cấu hình/publish; Quang phụ trách phiên Central, lưu SQL và ACK; Vy kiểm tra góc nhìn Admin khi nguồn dữ liệu sẵn sàng; Ngọc kiểm tra tình huống lỗi mạng/trợ lý tích hợp và giữ TCP Tools. Đây là phối hợp phần việc; chưa gửi tin hoặc thay đổi GitHub thay các thành viên.

## Cấu hình và khởi động tối thiểu

1. Tạo cấu hình bằng `--init-config <file> --central-host <host> [--central-port <port>]`. Sinh DeviceId ngẫu nhiên một lần vào file, từ chối ghi đè; binary không chứa MAY-03 hoặc IP minh họa. Tạo riêng trên mỗi máy, không nhân bản file đã có identity cùng image máy.
2. `--config <file> --validate-config`: kiểm tra JSON, DeviceId, host/port, nhịp lấy mẫu/heartbeat, timeout, tham chiếu biến môi trường xác thực và logging. Không đọc/ghi token vào cấu hình hoặc log. Lưu cấu hình ở thư mục mà người vận hành có quyền thích hợp.
3. `--collect-once`: dùng WindowsMetricsCollector hiện có, chỉ lấy số liệu tại máy; không kết nối/gửi dữ liệu. Constructor tạo mốc CPU, launcher đợi khoảng 200 ms rồi đọc delta. Không kết luận NORMAL hoặc nghiệm thu độ chính xác từ một mẫu ngắn.
4. `--probe`: chỉ mở TCP đến endpoint đã cấu hình, giữ phiên trong khoảng chẩn đoán ngắn rồi đóng; không phải monitoring, không gửi token/heartbeat/metric. Có thể gửi HELLO không bí mật để Central hiện có đọc và ghi nhận; queue thành công không phải xác nhận từ Central. Nhật ký và kiểm tra phía Central phải phân biệt các mốc.
5. Chạy bình thường chưa được bật monitoring: báo thiếu TLS/session/AUTH, dừng thay vì gửi dữ liệu chưa xác thực. Khi Phúc/Quang hoàn tất phiên, thứ tự sẽ là cấu hình → collector/transport → TLS/HELLO/AUTH → chạy MetricsService/HeartbeatService → hủy hai vòng → đóng transport. Chưa hiện thực reconnect/replay thì không được ghi chúng đang hoạt động.

DeviceId trong file ổn định qua các lần chạy; GUID giảm khả năng trùng ngẫu nhiên, không thay việc quản lý danh tính ở Central. Hai PC sao chép cùng config sẽ vẫn trùng: cần cấp riêng và kiểm tra Central từ chối phiên trùng khi handler đó có. BootId cho mẻ phải sinh mới mỗi lần chạy, không dùng DeviceId thay BootId; launcher hiện tại không gửi mẻ giám sát.

## Các giai đoạn có thể chứng minh

| Giai đoạn | Việc làm | Điều kiện/kết quả phải ghi đúng |
|---|---|---|
| A — một PC Console | Tạo cấu hình, kiểm tra invalid/valid, đọc số liệu Windows, probe và Ctrl+C | Chỉ local collection/transport; monitoring chờ phiên xác thực |
| B — Central ở host LAN khác | Cho phép host/port trong firewall phù hợp; dùng cùng package đổi cấu hình | Cần hai host thật và LAN được phép; loopback không chứng minh giai đoạn này |
| C — nhiều Agent | Tạo riêng identity/config cho từng PC; Quang quan sát kết nối | Nhiều process loopback chứng minh phân biệt identity trong process test, không thay hai PC thật |
| D — package Windows | Publish Release win-x64 self-contained, không trim; thử executable ngoài source tree | Copy cả thư mục publish; thử trên máy không có runtime riêng sau này; tự cập nhật runtime đóng gói |
| E — Windows Service | Chỉ đánh giá sau startup/shutdown/log/reconnect đã kiểm chứng | Chưa cài Service. Cài đặt có thể cần quyền admin; chạy với tài khoản/quyền tối thiểu phù hợp |
| F — phòng máy | Triển khai lên tập PC được trường/quản trị viên cho phép | Chưa có phép/host trong lượt này, không triển khai tự động |

Lệnh publish dự kiến: `dotnet publish src/LabGuard.Agent/LabGuard.Agent.csproj -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -o <output>`. Cấu hình riêng ngoài executable; không nhúng token. Probe không phải kết quả end-to-end SQL.

Tham khảo: [Microsoft publishing overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/) giải thích self-contained gồm runtime/phụ thuộc và vẫn cần cập nhật; [Windows Service hosting](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service) dành cho giai đoạn E, không là yêu cầu cài ngay.

## Chuyển toàn solution sang .NET 10

SDK 10 chưa có. Lập riêng nhánh thay đổi sau khi được chọn: cài/chọn SDK phù hợp môi trường; rà mọi csproj, NuGet và Windows Forms; retarget đồng bộ, restore/build/test toàn solution, mở Designer/Admin trên Windows và publish lại. Kiểm tra cả công cụ/mock harness, không chỉ Agent. Chưa có bằng chứng compatibility/build/test net10 nên không đánh dấu đã migrate; không hứa bộ net9 vẫn phù hợp production sau ngày hết hỗ trợ.
