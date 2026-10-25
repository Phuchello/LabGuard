# Kiểm tra bản trình bày LabGuard GK

Ngày lập: 25/10/2026

Nhánh: `codex/labguard-gk-storyline-rework-20261025`

Mốc repository đối chiếu: `90562c5bbbccac6022cc6658195aa227ee2b6cbe` (`origin/main` khi bắt đầu công việc).

## Nguồn và phạm vi

- Nguồn PowerPoint được chọn: `LabGuard_Nhom5_GK_Keynote(7).pptx`.
- SHA-256 nguồn: `27C5CF2BE9EE722CB2A684A06C27CA6B1A03301AEBE09B68DB9AF742FA089444`.
- Bản nguồn đã được sao chép nguyên trạng vào thư mục này để có thể dựng lại; SHA-256 bản sao phải trùng với nguồn trên.
- Chỉ thêm deliverable và script trong `deliverables/presentation_rework_20261025/`. Không sửa mã ứng dụng, giao thức, schema, WBS, báo cáo GK hay PowerPoint gốc.

## Ánh xạ trước và sau

| Slide mới | Nguồn / nội dung | Lý do |
|---|---|---|
| 1 | Slide 1 cũ — nhận diện LabGuard, viết lại lời mở đầu | Nêu bài toán trước kiến trúc và giữ tên môn, nhóm, thành viên. |
| 2 | Slide 4 cũ — phòng máy isometric và nhấn MAY-03 | Đưa tình huống giả định lên đầu; tiêu đề mới nói rõ quy mô và máy cần chú ý. Giữ hình vector và nhịp làm nổi bật có sẵn. |
| 3 | Slide 2 cũ — vẽ lại topology | Phân biệt Ethernet vật lý với TCP logic; thể hiện Central là nơi duy nhất truy cập SQL. |
| 4 | Mới — từ mẫu CPU đến TCP frame | Bổ sung nội dung lập trình mạng: JSON, prefix 4 byte Big-Endian, giới hạn payload và đọc đủ frame. |
| 5 | Mới — Central, giao dịch SQL và ACK | Nối METRIC_BATCH với chống lưu lặp, commit và BATCH_ACK; thay bảng ba bảng rời bằng một luồng xử lý. |
| 6 | Slide Admin cũ — tinh gọn lại | Giữ ý tưởng WinForms, tách trạng thái và đưa bằng chứng trước gợi ý; ghi rõ đây là giao diện phác thảo. |
| 7 | Mới — tiến độ có căn cứ | Phân biệt phần đã có ở mức thành phần, đang làm và chưa tích hợp/kiểm chứng đầu-cuối. |
| 8 | Slide lộ trình cũ — làm lại theo phụ thuộc | Giữ bốn giai đoạn phát triển toàn đồ án, thêm trách nhiệm chính thức của bốn thành viên. |
| 9 | Mới — kết luận | Tóm tắt ba ý chính và xin góp ý, không đưa thêm chức năng. |

Nội dung phân vai cũ được chuyển vào slide 8. Cận cảnh MAY-03 lặp lại và danh sách bảng dữ liệu tách rời được bỏ vì không thêm bước giải thích mới.

## Luồng trình bày và thời lượng ghi chú

Tình huống MAY-03 → topology → framing TCP → xác thực/lưu/ACK → cách Admin dự kiến trình bày bằng chứng → tiến độ thật → thứ tự triển khai và chủ sở hữu → kết luận.

| Slide | Thời lượng |
|---|---:|
| 1–9 | 15, 30, 45, 40, 40, 55, 35, 25, 15 giây |
| **Tổng** | **300 giây** |

Đã mở lại PPTX để xác nhận có 9 slide và 9 bộ speaker notes. Mỗi bộ ghi chú có TIME, SAY, POINT, TRANSITION và lưu ý kỹ thuật khi cần. Slide 2 có CLICK CUE tương ứng hiệu ứng có sẵn.

## Đối chiếu kỹ thuật

- Agent và Admin là TCP client; Central là TCP server. Ethernet vật lý đi theo hình sao qua switch. Các nét TCP và Central–SQL là đường truyền logic, không phải dây riêng đến switch. Admin/Agent không truy cập SQL trực tiếp.
- Cổng TCP 5000 là cổng ứng dụng đề xuất. TCP 1433 chỉ áp dụng nếu SQL Server được cấu hình dùng cổng tĩnh.
- TCP cung cấp byte stream có thứ tự, không tạo ranh giới bản tin. Prefix 4 byte Big-Endian là framing của LabGuard; N là số byte JSON payload, không gồm prefix và tối đa 256 KiB.
- Theo thiết kế, Central xác thực và kiểm tra dữ liệu, dùng `DeviceId + BootId + BatchId` để nhận diện batch gửi lại, ghi `ProcessedBatches`/`MetricSamples` trong giao dịch SQL rồi chỉ gửi `BATCH_ACK` sau COMMIT. Đây chưa phải pipeline đã nghiệm thu đầu-cuối.
- Ba bảng hiện có được giữ đúng tên: `ProcessedBatches`, `MetricSamples`, `Incidents`. Ngưỡng CPU ≥90% qua ba mẫu liên tiếp là quy tắc dự kiến/minh họa, không phải kết quả thử nghiệm.
- Tiến độ lấy từ WBS đối chiếu trên baseline: 40 nhiệm vụ, 6 `XONG`, 2 `ĐANG LÀM`, 32 `CHƯA LÀM`. Con số 195 bài đạt là lượt kiểm tra Windows cục bộ lịch sử ghi tại PR #11, không phải CI hoặc kiểm thử đầu-cuối mới.
- Thành viên: Võ Trọng Phúc — Protocol/Agent; Nguyễn Bá Quang — Central/SQL Server; Lý Tuyết Vy — Admin WinForms; Đỗ Minh Ngọc — công cụ kiểm thử truyền thông TCP và hỗ trợ phân tích, xử lý sự cố tích hợp trong LabGuard.

Chưa được thể hiện là đã hoàn tất: phiên/xác thực, ghi SQL bền vững và ACK, reconnect/replay, quy trình sự cố, Admin binding và tích hợp Agent → Central → SQL → Admin. RFC telemetry/sự cố còn là bản nháp chờ quyết định. Không có triển khai 40 máy, số liệu benchmark hay kết quả thực nghiệm mới được tuyên bố.

## Kiểm tra hình ảnh và tệp

- 9 PNG xem trước, mỗi trang 2560×1440; contact sheet 3×3 được mở và rà soát. Các trang PDF đã được kết xuất lại để kiểm tra trực quan; tiếng Việt có dấu hiển thị, không thấy chữ bị cắt hoặc chồng lên nhau. Slide 2 được kiểm tra lại sau khi đổi tiêu đề.
- PDF có đúng 9 trang, tỷ lệ 16:9. Đây là PDF tĩnh độ phân giải cao dựng từ PNG vì bộ xuất PDF của môi trường báo thiếu module font Helvetica và PowerPoint/LibreOffice không có sẵn. PDF không giữ text/vector có thể chọn; PPTX là bản chỉnh sửa chính.
- PPTX mở lại được bằng ArtifactTool: 9 slide, 9 notes; có shape và text native. Kiểm tra Open XML: 0 đối tượng ảnh trên cả 9 slide; slide phòng máy chứa 219 shape, giữ minh họa vector có thể chỉnh sửa.
- Slide 2 có transition `fade`, 7 `animEffect`, một chuỗi click-effect; 7 ID mục tiêu đã được đối chiếu tồn tại. Không có motion path. Các slide còn lại không có hiệu ứng đối tượng.
- **Chưa xác minh được phát lại trong PowerPoint Slide Show trên Windows**: PowerPoint Desktop và LibreOffice không khả dụng trong môi trường này. Kiểm tra XML và ảnh tĩnh không thay thế việc phát thử thật; cần người rà soát mở PPTX trong PowerPoint để xác nhận nhịp click.
- ArtifactTool cảnh báo glyph coverage không thể kiểm tra với font subset nhúng; trong PNG/PDF đã kết xuất, dấu tiếng Việt nhìn đúng. Chưa có xác nhận từ PowerPoint Desktop.

## Tái tạo

`build_rework.mjs` dựng deck từ `LabGuard_Nhom5_GK_Keynote_SOURCE.pptx`; `restore_native_animation.ps1` khôi phục timing/transition tương ứng cho slide 2; `render_final.mjs` mở lại và xuất PNG; `export_preview_assets.py` tạo PDF tĩnh và contact sheet. Có thể đặt `LABGUARD_SOURCE_PPTX` để chọn nguồn khác và `LABGUARD_ARTIFACT_TOOL_MODULE` để trỏ tới module ArtifactTool của môi trường đang dùng. Các lệnh dựng/kiểm tra phụ thuộc Node.js, ArtifactTool, Python, Pillow và ReportLab.

Không chạy build/test ứng dụng vì nhiệm vụ chỉ tạo và rà soát tài liệu trình bày.
