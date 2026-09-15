# BẢNG MA TRẬN KIỂM ĐỊNH KỸ THUẬT STUDENT MODE V3.1 (STUDENT_MODE_CONTENT_QA.md)

**Dự án:** LABGUARD — Lightweight Network Monitoring & Incident Management System  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Phiên bản tài liệu:** V3.1 — Final Technical Correction Pass  
**Ngày lập:** 13/09/2026  
**Nguồn đối chiếu chuẩn (SSOT):** `03_PROTOCOL_SPEC.md`, `CONTRACT_MATRIX.md`, `WBS_TASKS.csv`, `PROJECT_STATE.md`, `DECISIONS.md`.

---

## 1. MỤC TIÊU KIỂM ĐỊNH (AUDIT SCOPE)
Ma trận này ghi nhận toàn bộ các hạng mục hiệu chỉnh kỹ thuật từ **Student Mode V3** sang **Student Mode V3.1**, nhằm bảo đảm gói tài liệu và công cụ dành cho sinh viên:
1. **Giữ nguyên 100% triết lý Student Mode:** Trực quan, dạng thẻ (card-based), ngôn ngữ đời thường, prompt AI may đo, không bảng nhiều cột gây ngợp.
2. **Loại bỏ 100% sai lệch kỹ thuật (Technical Drift):** Đồng bộ tuyệt đối với bộ đặc tả gốc Checkpoint 1 (SSOT).
3. **Không hứa hẹn điểm số, trung tính học thuật:** Khuyến khích hiểu sâu kiến trúc và bảo vệ đồ án thực chất.

---

## 2. MA TRẬN ĐỐI SOÁT THEO TỪNG DANH MỤC HIỆU CHỈNH

| Hạng mục | Vị trí / Task ID | Hiện trạng cũ (V3 Drift) | Chuẩn hóa mới (V3.1 Correction) | Căn cứ SSOT |
|:---|:---|:---|:---|:---|
| **PROTOCOL** | Sheet 99, Guide P1, AI Prompts | Xuất hiện các tên message cũ: `COLLECTOR_REGISTER`, `COLLECTOR_FORWARD` | Thay bằng tập bản tin P1 chuẩn: `HELLO_COLLECTOR`, `AUTH_COLLECTOR`, `FORWARD_OPEN`, `FORWARD_DATA`, `FORWARD_RESULT`, `FORWARD_CLOSE`, `CLOSE_SESSION`, `COLLECTOR_HEARTBEAT`, `HEALTH_PROBE`, `HEALTH_RESULT` | `03_PROTOCOL_SPEC.md` Mục 3 & 4.5 |
| **PROTOCOL** | LG-B-011, LG-C-010, Sheet 99 | Dùng tên truy vấn cũ: `QUERY_INCIDENT_HISTORY`, `QUERY_METRICS_HISTORY` | Thay bằng cặp Request/Response chuẩn: `GET_HISTORY` / `HISTORY_PAGE`, `GET_METRICS` / `METRICS_RESULT`, `LIST_INCIDENTS` / `INCIDENT_PAGE` | `03_PROTOCOL_SPEC.md` Mục 2.7 & 2.8 |
| **PROTOCOL** | LG-B-008, Sheet 99, Guide P9 | Bản tin phát sóng dùng `INCIDENT_CREATED` hoặc `METRIC_UPDATE` | Thay bằng danh mục Event chuẩn: `INCIDENT_OPEN`, `INCIDENT_UPDATE`, `DEVICE_UPDATE` | `03_PROTOCOL_SPEC.md` Mục 2.10 |
| **PROTOCOL** | Phản hồi lỗi giao thức | Nhắc tới `ERROR_RESPONSE` | Chuẩn hóa thành `ERROR` (kèm mã lỗi và `requestId`) | `03_PROTOCOL_SPEC.md` Mục 2.11; `CONTRACT_MATRIX.md` Mục III.11 |
| **ERROR CODE** | LG-B-009, LG-C-008, LG-C-016, Sheet 05, 07, Guide | Dùng mã lỗi `CONCURRENCY_CONFLICT` | Chuẩn hóa thành mã lỗi canonical: `ERR_CONCURRENCY_VIOLATION` (409 Conflict) | `CONTRACT_MATRIX.md` Mục VI |
| **ERROR CODE** | Hướng dẫn lỗi và test case | Dùng `AUTH_FAILED` | Chuẩn hóa thành mã lỗi canonical: `ERR_UNAUTHENTICATED` (401) hoặc `ERR_UNAUTHORIZED` (403) | `CONTRACT_MATRIX.md` Mục VI |
| **ERROR CODE** | Task LG-A-002, LG-C-002 | Dùng lỗi ngoại lệ thô | Chuẩn hóa danh mục: `ERR_STREAM_TRUNCATED` (`EndOfStreamException`), `ERR_FRAME_TOO_LARGE` ($N > 256\text{ KiB}$) | `CONTRACT_MATRIX.md` Mục VI |
| **STATE** | LG-B-008, LG-C-008, Sheet 99, Guide | Dùng trạng thái `NEW`, `OPEN/NEW`, `ACKED` | Chuẩn hóa bộ trạng thái: `ConditionState`: `ACTIVE`, `RECOVERED`, `UNKNOWN`; `WorkflowState`: `OPEN`, `ACKNOWLEDGED`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED` | `03_PROTOCOL_SPEC.md` Mục 2.10 & 4.2; `CONTRACT_MATRIX.md` |
| **DATABASE** | LG-B-001, Sheet 00, Sheet 02, Guide P4 | Yêu cầu sinh viên tạo 8 bảng CSDL ngay ngày đầu tiên khi chưa chốt schema ở CP1 | Chỉnh sửa: TẠM THỜI CHƯA TẠO FULL SCHEMA; chỉ cài SQL Server, kiểm tra kết nối C#, tạo database `LabGuard_Dev` rỗng, chuẩn bị migration skeleton; bảng chính thức chốt tại CP2. Thêm hộp cảnh báo `⛔ CHỜ CHECKPOINT 2 TRƯỚC KHI TẠO SCHEMA THẬT` | `PROJECT_STATE.md`, Quyết định đóng băng CP1 |
| **TASK ORDER** | Sheet 00, Sheet 01 (SV A) | Nhảy cóc vào LG-A-002, bỏ qua việc chốt hợp đồng LG-A-001 | Sắp xếp lại: SV A bắt đầu từ `LG-A-001` (Chốt wire contract & fixtures JSON); sau khi xong mới làm `LG-A-002` (`FrameCodec`) và B, C mới hiện thực socket phụ thuộc | `WBS_TASKS.csv` & `PARALLEL_WORK_PLAN.md` |
| **TASK ORDER** | Sheet 00 (SV B) | Nhiệm vụ ngày 1 ghi: "Khởi tạo 8 bảng CSDL và tài khoản Admin mẫu" | Đổi thành: "Chuẩn bị SQL Server + kiểm tra kết nối .NET + migration skeleton [Chờ CP2 tạo bảng]" | B-001 Scope Correction |
| **DYNAMIC UI** | Sheet `00_Bat_Dau_O_Day` | Hardcode cố định chỉ hiển thị Tuần 1 và task cố định | Thêm ô chọn tuần động tại ô `D5` (Dropdown `W1`..`W5`), bảng lộ trình 5 tuần trực quan; nguyên tắc phối hợp ngày 1 rõ ràng | Yêu cầu cải tiến UX V3.1 |
| **LOG POLICY** | LG-A-011, Sheet 01, Sheet 99, Guide | Một số chỗ còn sót định mức cũ `5 MB / 3 files` | Khóa duy nhất một chính sách: `MaxFileSize = 10 MB`, `MaxFiles = 5`, tổng thư mục log `<= 50 MB` [CP2 TO VERIFY] | `PROJECT_STATE.md`, Rev 4.1 Log Policy |
| **ANALOGY** | LG-A-004 (TLS Client) | Bị lệch sang: "bác sĩ đo huyết áp, nhịp tim..." (là của metric/heartbeat) | Sửa chuẩn xác: "Gọi điện qua đường dây đã khóa mã và kiểm tra đúng người ở đầu bên kia (TLS handshake & chứng chỉ số)" | Đối soát nội dung task A-004 |
| **ANALOGY** | LG-A-005 (CPU/RAM/NIC) | Bị lệch sang: "ghi âm khi mất sóng..." (là của replay) | Sửa chuẩn xác: "Cụm đồng hồ xe máy hiển thị tốc độ, xăng và quãng đường (Đo định kỳ CPU %, RAM, NIC byte, Uptime)" | Đối soát nội dung task A-005 |
| **ANALOGY** | LG-A-006 (Heartbeat) | Bị lệch sang: "đường hầm bí mật..." (là của TLS) | Sửa chuẩn xác: "Nhân viên trực cứ 5 giây nhắn 'em vẫn ở đây' và định kỳ gửi kiện hàng số liệu về kho (Heartbeat 5s & batch sender)" | Đối soát nội dung task A-006 |
| **ANALOGY** | LG-A-007 (TCP Probe) | Chưa sát bản chất | Sửa chuẩn xác: "Bảo vệ đi gõ cửa từng phòng xem bên trong có ai trả lời không (TCP connect probe allowlist <= 5 cổng, timeout 2s)" | Đối soát nội dung task A-007 |
| **ANALOGY** | LG-A-008 (Simulator) | Chưa sát bản chất | Sửa chuẩn xác: "Thuê 20 diễn viên đóng vai 20 máy tính để thử xem máy chủ có chịu nổi không (Simulator 10/20/50 socket)" | Đối soát nội dung task A-008 |
| **ANALOGY** | LG-A-010 (Failure Injection) | Chưa sát bản chất | Sửa chuẩn xác: "Cố tình rút dây điện hoặc cắt mạng trong lúc thử nghiệm để xem hệ thống xử lý sự cố thế nào" | Đối soát nội dung task A-010 |
| **ANALOGY** | LG-A-011 (Rolling Log) | Chưa sát bản chất | Sửa chuẩn xác: "Camera hành trình tự xóa video cũ khi bộ nhớ đầy (10 MB/file, tối đa 5 file, tổng <= 50 MB)" | Đối soát nội dung task A-011 |
| **ANALOGY** | Toàn bộ 60 thẻ task | Nhiều ví dụ đời thường bị dịch chuyển chỉ số (shifted index) | Rà soát và viết lại toàn bộ 60 analogy khớp 100% bản chất nghiệp vụ từng task | 60/60 task alignment |
| **FAILURE HELP** | Toàn bộ 60 thẻ task (Mục `if_error`) | Boilerplate chung chung: "1. Kiểm tra kết nối mạng/CSDL. 2. Bẫy lỗi... 3. Soát lại..." lặp lại 60 lần | Thay bằng 3 bước xử lý lỗi cụ thể, có câu lệnh thực tế (`Test-NetConnection`, `ping`, `wireshark`, `dotnet test`, `Stopwatch`, `InvokeRequired`, `RowVersion`...) | Yêu cầu loại bỏ generic help |
| **CP2 DEFAULT** | Ingest buffer, hysteresis, probe, schema | Chưa gắn nhãn tham số dự kiến | Gắn nhãn tường minh `[CP2 TO VERIFY]` cho: BoundedChannel 1000 items, probe timeout 2000ms, hysteresis 3 samples, purge batch 5000 rows, SQL index latency < 50ms | Technical boundary gate |
| **SCOPE GATE** | Guide P2, Sheet 05, Sheet 00 | Chưa nêu rõ quy mô giờ công tại cổng M4 | Nêu rõ: Cổng M4 bảo vệ 164.5h MVP cam kết; nếu chập chờn thì DEFER 47.0h P1 Collector để tập trung kiểm thử | `PARALLEL_WORK_PLAN.md` |
| **WORDING** | Toàn bộ tài liệu | Chứa câu hứa hẹn điểm số: "chắc chắn bị liệt", "giảng viên sẽ chấm 9-10", "100% mượt", "không bao giờ" | Chuyển sang giọng văn trung tính học thuật: "Ưu tiên một MVP ổn định, có minh chứng và thành viên hiểu rõ code hơn việc mở rộng thêm P1 nhưng chưa kiểm thử đủ" | Chuẩn mực sư phạm đại học |

---

## 3. KẾT LUẬN KIỂM ĐỊNH NỘI BỘ
Toàn bộ 12 hạng mục kỹ thuật đã được rà soát và khắc phục hoàn toàn trên cả 4 thành phẩm:
1. `LabGuard_Student_Task_Tracker_v3_1.xlsx`: 10 sheets, 0 lỗi công thức, 0 disallowed terms, đạt kiểm tra Microsoft Excel COM.
2. `LabGuard_Huong_Dan_Cho_Nhom_v3_1.docx`: Đầy đủ 9 phần + phụ lục, định dạng chuẩn Unicode Segoe UI.
3. `LabGuard_Huong_Dan_Cho_Nhom_v3_1.pdf`: Xuất trực tiếp qua Word COM, 116 trang, 0 disallowed terms, layout chỉn chu.
4. Gói Checkpoint 1 baseline gốc được bảo vệ nguyên vẹn, không có sự xáo trộn hay thay đổi kiến trúc.
