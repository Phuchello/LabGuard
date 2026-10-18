# Phụ thuộc và kế hoạch kiểm tra

Chạy baseline trước sửa và chạy lại sau sửa. Chỉ ghi PASS khi lệnh thực chạy thành công; nếu môi trường chặn thì ghi BLOCKED/NOT RUN, không chuyển thành kết quả phần mềm.

## Phụ thuộc triển khai

`Cấu hình/identity → TCP → TLS/HELLO/AUTH → thu/gửi heartbeat + metric → Central xác minh/lưu transaction → BATCH_ACK → replay đúng mẻ`. Socket connect không thể thay các mốc ở sau. Khi SQL chưa có repository, không chạy ca lưu thành công giả. Auth và reconnect chưa có thì chưa nghiệm thu giám sát toàn tuyến hoặc Service.

## Các ca trong môi trường hiện tại

- JSON sai/field lạ/trùng, thiếu DeviceId hoặc endpoint, port/interval ngoài phạm vi: lỗi rõ, không mở socket.
- Init cấu hình tạo ID riêng và không ghi đè; đọc lại cùng file giữ ID; hai file mới khác ID. Không khẳng định phát hiện cấu hình bị clone trên hai máy nếu chưa có registry Central.
- Collection trên Windows dùng collector thật; fixture chỉ dùng unit test, không thay số liệu thực hoặc đối soát Windows reference.
- Central không chạy: probe lỗi kết nối/timeout; không tự reconnect, không báo authenticated.
- CentralServer thật trên loopback: nhận kết nối và HELLO nếu probe gửi; không có metric/heartbeat/auth token. Hai identity riêng có thể được quan sát. Dùng event test chỉ để chứng minh nhận frame, không nối SQL giả.
- Ctrl+C/cancellation khi đang chờ: thoát có kiểm soát, đóng socket, log rõ; lỗi writer/network không biến thành thành công của monitoring.
- Build/test cả solution với SDK hiện có; publish self-contained và chạy executable nếu runtime pack sẵn sàng. Ghi môi trường/exit code/log/TRX thực.

## Các ca cần điều kiện sau này

| Ca | Điều kiện | Giới hạn hiện tại |
|---|---|---|
| Reconnect sau ngắt LAN | Session reader, liveness và reconnect được hiện thực | NOT IMPLEMENTED; TcpClient.Connected có thể cũ khi peer đóng im lặng |
| Metric đã lưu | Quang có auth + repository/transaction SQL thật | NOT IMPLEMENTED; log frame không chứng minh commit |
| Lost ACK và không trùng | ACK dispatcher + replay buffer + dedup/TTL | NOT IMPLEMENTED trong launcher, không dùng mock PASS thay toàn tuyến |
| Hai máy LAN | Hai host được phép, cấu hình/firewall | NOT RUN nếu chỉ có một host |
| Windows Service | Startup/shutdown/log/reconnect ổn; quyền cài đặt | NOT INSTALLED; không yêu cầu admin cho Console |
| .NET 10 | SDK10 và rà toàn solution/NuGet/WinForms | Chưa có SDK10; không retarget riêng Agent |
| PC phòng máy | Cho phép quản trị viên và danh sách máy | Không tự triển khai |

Sau sửa, ghi exact command, SDK/runtime, test counts/exit codes, phạm vi loopback và hash package. Giữ nguồn baseline main được pin và trạng thái canonical; chỉ đưa phần Agent lên nhánh review theo ủy quyền, không merge.

## Đánh giá phụ thuộc .NET 10 trước khi retarget

Inventory toàn repo có **13 csproj**, trong đó **12 thuộc LabGuard.sln**; công cụ `tools/validation/test_frame_codec` nằm ngoài solution. Tất cả hiện là net9.0, riêng Admin net9.0-windows/UseWindowsForms. Các project: Admin, Agent, Central, Protocol, Simulator; bốn project test; FakeAgent, FakeCentral, TcpEcho; và test_frame_codec ngoài solution. Có thể đối chiếu trực tiếp các csproj trong repo, không cần snapshot nguồn trùng lặp.

- Central dùng Microsoft.Data.SqlClient 7.0.3; test dùng Microsoft.NET.Test.Sdk 17.12.0, xunit 2.9.2, xunit.runner.visualstudio 2.8.2 và coverlet.collector 6.0.2. Restore/build hiện tại thành công chỉ xác nhận target .NET 9 trên máy này.
- Agent không thêm package mới. Reference Central mới chỉ nằm trong test để thử listener thực tế.
- SDK 10 chưa cài: chưa restore/build/test net10, chưa xác minh dependency transitive/native SqlClient theo RID, chưa mở WinForms Designer dưới SDK10. Không kết luận những package hiện có tương thích hay không tương thích chỉ từ số phiên bản.
- Lượt migration cần retarget thống nhất 13 project và tham chiếu liên quan, kiểm tra restore warnings/asset selection và advisory, build/test toàn solution, build công cụ ngoài solution, kiểm tra Admin/Designer trên Windows rồi publish/run lại các executable. Thay version NuGet chỉ khi kết quả thực tế yêu cầu, không cập nhật hàng loạt thiếu bằng chứng.

Kết quả của lượt hiện tại được chốt ở `VALIDATION_EVIDENCE.md`; các mục NOT RUN/NOT IMPLEMENTED ở trên vẫn giữ nguyên.
