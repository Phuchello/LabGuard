# Rà soát mức sẵn sàng của mã nguồn — 18/10/2026

Đối chiếu GitHub **Phuchello/LabGuard**, main `260759b23d960994f75c7b15455915b68d0c5f8d`, ngày 18/10/2026. Đây là mốc rà soát trước sửa launcher, ban đầu chỉ đọc GitHub. [Nguồn main được pin](https://github.com/Phuchello/LabGuard/tree/260759b23d960994f75c7b15455915b68d0c5f8d). Nhánh review được tạo riêng từ chính main này sau khi người điều phối cho phép commit/push và Draft PR.

Checkout gốc được giữ nguyên ở `docs/progress-sync-20261015` (`dab8dad`), có thay đổi tài liệu phân công trước lượt này; không checkout/reset hoặc ghi đè chúng. Mười một tệp mã/cấu hình trọng tâm đọc từ main giống local sau chuẩn hóa xuống dòng. WBS trên main có 34 task; local có 40 do bổ sung kế hoạch hỗ trợ xử lý sự cố chưa push. Cả hai ghi 6 XONG/2 ĐANG LÀM; số chưa làm lần lượt 26/32. GitHub search các mục đang mở không trả issue/PR. Tracker và kết quả kiểm thử cũ không tự chứng minh deployment readiness.

## Nguồn thực tế trước sửa launcher

| Thành phần | Đã có | Còn thiếu cho triển khai |
|---|---|---|
| Agent Program.cs | In thông báo rồi thoát | Đọc cấu hình, identity, vận hành Console, hủy tác vụ, log; câu “Central chưa hiện thực” đã lỗi thời |
| AgentSocketClient | ConnectAsync, bounded outbox 256, một vòng ghi, ngắt/hủy; phát hiện lỗi writer | Không có reader phiên, TLS, HELLO/AUTH thành công, ACK dispatcher hoặc reconnect. `IsConnected` không chứng minh phiên đã xác thực hoặc liveness khi peer đóng im lặng |
| MetricsCollector | Win32 CPU delta/RAM/uptime | Cần máy Windows và kiểm tra quyền thực tế; mẫu CPU đầu cần được hiểu đúng là mốc khởi tạo |
| MetricsService/HeartbeatService | Đóng gói và gửi định kỳ, Start/Run/Stop | Program chưa ghép; không tự kiểm tra AUTH. Không được khởi chạy chỉ vì socket đã connect. Default PC-AGENT-01 trong component không được dùng làm identity của bộ triển khai |
| Central Program.cs/Server | Listener đa client, Ctrl+C, FrameCodec, log và EnvelopeReceived | Chưa phản hồi HELLO_ACK/AUTH_RESULT; chưa TLS/session/RBAC/liveness/SQL repository, transaction, dedup hoặc BATCH_ACK; Program không nối event vào pipeline nghiệp vụ |
| SQL | DDL và tham chiếu Microsoft.Data.SqlClient 7.0.3 | Package/DDL tồn tại không có nghĩa telemetry đã commit; chưa chạy pipeline TCP→SQL |

Tách năm mốc: **TCP connected → authenticated session → telemetry received → transaction committed → application ACK received**. Hiện Central có thể đọc frame, nhưng các mốc sau không được suy từ log kết nối. METRIC_BATCH/HEARTBEAT không được tự bật ở launcher trước khi phiên đúng đã sẵn sàng.

## Phần sửa an toàn trước

Thay màn hình trạng thái Agent bằng launcher có cấu hình được kiểm tra, sinh DeviceId một lần khi tạo cấu hình riêng cho từng PC, kiểm tra cấu hình, lấy số liệu cục bộ và probe TCP được yêu cầu rõ ràng. Probe không phải chế độ giám sát: chỉ xác minh transport với Central hiện có, không gửi token/telemetry và không giả xác thực. Chế độ giám sát bình thường dừng rõ ràng vì chưa có phiên TLS/AUTH. Không thêm chế độ bỏ qua xác thực để thu telemetry.

Không sửa networking core, DTO, protocol, SQL hoặc Central ở nhánh công việc Agent này. Không sửa WBS để biến công việc launcher thành A-005/A-006 hoàn tất. Không đổi báo cáo/PPTX GK.

## SDK và lifecycle

Máy hiện có SDK **9.0.318**, runtime 9.0.20; chưa có SDK 10. `dotnet --info` trong sandbox báo AccessDenied khi dò Service Control Manager; danh sách SDK vẫn đọc được. Kết quả build/test thật được ghi riêng sau khi chạy, không lấy từ số PASS trong README.

.NET 9 hết hỗ trợ **10/11/2026**; .NET 10 LTS tới **14/11/2028**, theo [Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). Giữ target hiện tại để kiểm tra thay đổi launcher; chuyển toàn solution sang .NET 10 là công việc riêng cần SDK, restore/build/test, WinForms designer và package compatibility được kiểm chứng. Không nâng riêng Agent.

Thông báo Agent phủ nhận Central đã có listener là lỗi thời và đã được thay trong launcher. WBS ghi tiến độ component, không phải bằng chứng nghiệm thu toàn tuyến; các tracker và lịch sử nghiệm thu được giữ nguyên. Kết quả sau sửa, bao gồm giới hạn peer đóng im lặng đã quan sát thực tế, nằm trong `VALIDATION_EVIDENCE.md`.
