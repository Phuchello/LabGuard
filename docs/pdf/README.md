# Năm handbook LabGuard

Cập nhật nguồn/PDF sau PR #11 (main `70481fa`) và quyết định phân công 17/10/2026. Đây là tài liệu học và kế hoạch triển khai, không phải báo cáo GK đã duyệt; GK/PPTX không được sửa trong housekeeping.

**Bộ PDF đã đồng bộ với nguồn cuối:** build thành công và kiểm tra ảnh render đủ 98 trang (tổng quan 28, A 23, B 17, C 15, D 15). Không có lỗi glyph/tham chiếu hoặc chữ bị cắt; một cảnh báo vượt dòng 0,63 pt ở A đã được kiểm tra. Kết quả và giới hạn tại [PROJECT_STATE.md](../../PROJECT_STATE.md).

| Tập | PDF | Người/phần việc |
|---|---|---|
| 1 | [Tổng quan A–Z](LabGuard_Tong_Quan_A-Z.pdf) | Cả nhóm: kiến trúc, hiện trạng và phối hợp |
| 2 | [Thành viên A](LabGuard_Huong_Dan_Thanh_Vien_A.pdf) | Võ Trọng Phúc: Agent & Protocol |
| 3 | [Thành viên B](LabGuard_Huong_Dan_Thanh_Vien_B.pdf) | Nguyễn Bá Quang: Central & SQL Server |
| 4 | [Thành viên C](LabGuard_Huong_Dan_Thanh_Vien_C.pdf) | Lý Tuyết Vy: Admin WinForms |
| 5 | [Thành viên D](LabGuard_Huong_Dan_Thanh_Vien_D.pdf) | Đỗ Minh Ngọc: Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố tích hợp trong LabGuard |

Các tập dùng chung [sync_status.tex](source/sync_status.tex) để mô tả đúng launcher/config/DeviceId/local collect/development probe và monitoring đang bị chặn chờ TLS/session/AUTH. 195 tests Windows local được báo cáo từ PR #11, không phải GitHub CI. Những số 34/165 hoặc Windows reference trong hồ sơ cũ được giữ như lịch sử, không phải verification mới.

Chức năng hỗ trợ xử lý sự cố nhúng trong quy trình xử lý sự cố Admin, dùng dữ liệu Central có thẩm quyền, phối hợp B/C. Nội dung Gateway/Collector trong tập D chỉ là kiến thức P1 tùy chọn; Simulator vẫn placeholder. Các ví dụ/pseudocode và tiêu chí kiểm tra tương lai không chứng minh tính năng đã hoàn tất. Xem [trạng thái dự án](../../PROJECT_STATE.md), [phân công](../internal/TEAM_ASSIGNMENTS.md), [Agent deployment](../agent_deployment/README.md).

## Rebuild và kiểm tra

Cần XeLaTeX với font Times New Roman/Consolas và các package trong [labguard_pdf.sty](source/labguard_pdf.sty). Dùng PATH hoặc biến môi trường `LABGUARD_XELATEX` để chọn chương trình chạy riêng, không hardcode đường dẫn cá nhân.

```powershell
./docs/pdf/build.ps1 -OutputDirectory scratch/handbook-candidates
```

Script chạy ba lượt mỗi nguồn, dừng nếu compiler trả lỗi và chỉ lấy PDF mới trong work directory riêng. Mặc định tạo thư mục GUID dưới scratch; không sửa trực tiếp PDF tracked, không xóa lịch sử hoặc tự kết luận visual PASS. Logs/aux giữ trong staging để điều tra khi cần, không commit.

Trước khi thay PDF chính thức: kiểm tra log lỗi/glyph/overfull, render từng cuốn, xem trang hiện trạng và mọi trang thay đổi; đối chiếu nội dung/TOC/bảng/sơ đồ. Chỉ copy năm candidate đã kiểm tra vào các tên PDF ở bảng trên. Không để nguồn mới đi cùng PDF cũ mà không ghi rõ mismatch.

Kết quả build, kiểm tra hình thức và tình trạng đồng bộ nguồn/PDF được ghi tại [PROJECT_STATE.md](../../PROJECT_STATE.md), kèm summary trong Draft PR. Không sửa schema/giao thức hoặc tự đổi .NET 9 sang .NET 10.
