# LABGUARD — Quyết định phân công thành viên Nhóm 5

Ngày ghi nhận: **17/10/2026**, múi giờ Asia/Saigon.

Nguồn xác nhận thực tế: **quyết định trực tiếp của người điều phối dự án trong cuộc trao đổi ngày 17/10/2026**. Người điều phối xác nhận rõ đây là quyết định phân công chính thức, không phải đề xuất chờ duyệt. Tài liệu này ghi nhận quyết định đó; không phải biên bản họp, chữ ký của các thành viên khác hoặc bằng chứng đồng thuận tập thể được tự tạo.

## Ánh xạ để đối chiếu

| Workstream | Thành viên | MSSV | Phân công chính thức | Trạng thái |
|---|---|---|---|---|
| A | Võ Trọng Phúc | 25521452 | Protocol & Agent | Chính thức theo quyết định người điều phối ngày 17/10/2026 |
| B | Nguyễn Bá Quang | 25521524 | Central Server & SQL Server | Chính thức theo cùng quyết định |
| C | Lý Tuyết Vy | 25522107 | Admin WinForms | Chính thức theo cùng quyết định |
| D | Đỗ Minh Ngọc | 25521203 | Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố tích hợp trong LabGuard | Chính thức theo cùng quyết định; chức năng hỗ trợ xử lý sự cố thuộc kế hoạch phát triển của Ngọc, chưa khẳng định đã lập trình |

## Nhiệm vụ và giới hạn

- Giữ nguyên các nhiệm vụ cốt lõi đã có trong WBS. Theo quyết định mới, bổ sung D-007 đến D-010 cho kế hoạch chức năng hỗ trợ xử lý sự cố và B-010/C-012 cho phần phối hợp tích hợp; không xóa hoặc đổi nội dung 34 dòng nhiệm vụ trước đó. Ownership Matrix được cập nhật tên, phần thư viện nội bộ và trách nhiệm tích hợp tương ứng.
- D giữ LG-D-001 (nền tảng TCP/fixtures), LG-D-002 (TCP Echo), LG-D-003 (TCP Relay). Gateway, Collector, Simulator vẫn theo điều kiện P1 hiện có, không tự trở thành bắt buộc.
- Công việc chính thức mới của Ngọc là logic hỗ trợ phân tích bằng chứng và gợi ý xử lý **nhúng trong LabGuard Admin**, phối hợp Quang/Vy, nhận dữ liệu có thẩm quyền qua Central. Không xây thành ứng dụng TCP độc lập và không chuyển phần cốt lõi của A/B/C sang D.
- Ước lượng 2 giờ cho B / 3 giờ cho C là phần ghép bổ sung sau khi các chức năng cốt lõi sẵn sàng, không bao gồm việc hoàn thiện chức năng cốt lõi còn thiếu. Cần rà soát lịch và công sức phần cốt lõi trước khi cam kết tích hợp; đây là điều kiện kỹ thuật, không phải tình trạng chờ phê duyệt phân công.
- `approved_member_mapping.json` của các bộ bàn giao trước là metadata, **không phải nguồn phê duyệt độc lập**. Nguồn có thẩm quyền ở lượt này là quyết định trực tiếp của người điều phối nêu trên.
- Chưa có biên bản họp, chữ ký hoặc hồ sơ đồng thuận tập thể được cung cấp. Không ghi “cả nhóm đã duyệt”. Khi có hồ sơ thật, bổ sung đường dẫn/ngày/nguồn để đối chiếu; không tự tạo lịch sử phê duyệt.

Phần liên quan trong báo cáo GK và slide ghi **phân công chính thức, kế hoạch thực hiện**. GK vẫn là đề xuất thiết kế sơ bộ, chưa chứng minh chức năng hỗ trợ xử lý sự cố được hiện thực hay nghiệm thu. Việc kích hoạt GET_METRICS/METRICS_RESULT tùy chọn và mọi sửa đổi hợp đồng/schema vẫn là quyết định kỹ thuật riêng; lượt này không sửa schema, protocol hoặc mã ứng dụng.
