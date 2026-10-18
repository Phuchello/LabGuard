# 5 sổ tay hướng dẫn LabGuard

Mốc đối chiếu ngày 18/10/2026: main `d4e20ad` sau PR #13; PR #14 đang Draft. Hai tập Tổng quan A–Z và Thành viên D được cập nhật trong PR #14. Đây là tài liệu học và kế hoạch triển khai, không phải báo cáo GK đã duyệt.

**Số trang PDF thực tế:** tổng quan 32, A 23, B 17, C 15, D 21; tổng 108 trang, tính cả bìa và mục lục. Lượt sửa cuối PR #14 biên dịch lại hai tập Tổng quan/D và kiểm tra trực quan toàn bộ 53 trang: mục lục, dấu tiếng Việt, bảng, sơ đồ và ngắt trang. Log XeLaTeX cuối không có lỗi font/ký tự thiếu, tham chiếu chưa giải quyết hoặc cảnh báo vượt khung. Ba tập A/B/C giữ nguyên nguồn và PDF; không ghi nhận một lượt kiểm tra mới cho ba tập này.

| Tập | PDF | Người/phần việc |
|---|---|---|
| 1 | [Tổng quan A–Z](LabGuard_Tong_Quan_A-Z.pdf) | Cả nhóm: kiến trúc, hiện trạng và phối hợp |
| 2 | [Thành viên A](LabGuard_Huong_Dan_Thanh_Vien_A.pdf) | Võ Trọng Phúc: Agent & Protocol |
| 3 | [Thành viên B](LabGuard_Huong_Dan_Thanh_Vien_B.pdf) | Nguyễn Bá Quang: Central & SQL Server |
| 4 | [Thành viên C](LabGuard_Huong_Dan_Thanh_Vien_C.pdf) | Lý Tuyết Vy: Admin WinForms |
| 5 | [Thành viên D](LabGuard_Huong_Dan_Thanh_Vien_D.pdf) | Đỗ Minh Ngọc: Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố tích hợp trong LabGuard |

Hai tập Tổng quan/D dùng [sync_status_pr14.tex](source/sync_status_pr14.tex). Ba tập A/B/C tiếp tục dùng [sync_status.tex](source/sync_status.tex), giữ mốc lịch sử đi kèm PDF cũ. PR #11 là mốc bổ sung launcher/config/DeviceId/local collect/development probe, không phải thay đổi gần nhất. 195 bài kiểm thử Windows tại máy là kết quả đã báo cáo từ PR #11, không phải GitHub CI hoặc lượt chạy mới trong PR #14. Monitoring vẫn bị chặn chờ TLS/session/AUTH.

P0 ưu tiên Windows; Linux/macOS chỉ xét sau khi P0 ổn định. Dữ liệu gần nhất từ P0 không chứng minh có chuỗi ba mẫu liên tiếp. `GET_METRICS/METRICS_RESULT` là tùy chọn, phụ thuộc nhóm chấp thuận và B/C xác minh nguồn/handler. `DataQuality` là hợp đồng nội bộ dự kiến, tách khỏi trạng thái kết nối do Central quyết định. Ngưỡng độ mới metric, ánh xạ trạng thái DTO/đặc tả và các phụ thuộc B-010/C-012 vẫn cần nhóm chốt; tài liệu không sửa wire contract hoặc schema.

Chức năng hỗ trợ xử lý sự cố nhúng trong quy trình xử lý sự cố Admin, dùng dữ liệu Central có thẩm quyền, phối hợp B/C. Nội dung Gateway/Collector trong tập D chỉ là kiến thức P1 tùy chọn; Simulator vẫn placeholder. Các ví dụ/pseudocode và tiêu chí kiểm tra tương lai không chứng minh tính năng đã hoàn tất. Xem [trạng thái dự án](../../PROJECT_STATE.md), [phân công](../internal/TEAM_ASSIGNMENTS.md), [Agent deployment](../agent_deployment/README.md).

## Rebuild và kiểm tra

Cần XeLaTeX với font Times New Roman/Consolas và các package trong [labguard_pdf.sty](source/labguard_pdf.sty). Dùng PATH hoặc biến môi trường `LABGUARD_XELATEX` để chọn chương trình chạy riêng, không hardcode đường dẫn cá nhân.

```powershell
./docs/pdf/build.ps1 -OutputDirectory scratch/handbook-candidates -Handbook Overview,D
```

Bỏ `-Handbook` để dựng đủ năm tập; có thể chọn `Overview`, `A`, `B`, `C`, `D`. Script chạy ba lượt mỗi nguồn được chọn, dừng nếu compiler trả lỗi và chỉ lấy PDF mới trong thư mục làm việc riêng. Staging chỉ chép nguồn `.tex/.sty`, tránh MiKTeX đọc nhầm `.aux/.toc` cũ từ thư mục nguồn. Mặc định tạo thư mục GUID dưới scratch; không sửa trực tiếp PDF tracked, không xóa lịch sử hoặc tự kết luận visual PASS. Logs/aux giữ trong staging để điều tra khi cần, không commit.

Trước khi thay PDF chính thức: kiểm tra log lỗi/glyph/overfull, render từng cuốn, xem trang hiện trạng và mọi trang thay đổi; đối chiếu nội dung/TOC/bảng/sơ đồ. Chỉ chép các candidate đã kiểm tra vào tên PDF tương ứng ở bảng trên; không thay các tập không được chỉnh sửa. Không để nguồn mới đi cùng PDF cũ mà không ghi rõ mismatch.

Bằng chứng lượt kiểm tra tài liệu PR #14 nằm trong mô tả Draft PR; [PROJECT_STATE.md](../../PROJECT_STATE.md) giữ lịch sử trạng thái dự án. Không chạy lại unit test ứng dụng trong lượt sửa tài liệu này. Không sửa schema/giao thức hoặc tự đổi .NET 9 sang .NET 10.
