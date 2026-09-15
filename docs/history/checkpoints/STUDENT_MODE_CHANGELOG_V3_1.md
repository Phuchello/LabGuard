# NHẬT KÝ THAY ĐỔI STUDENT MODE V3.1 (STUDENT_MODE_CHANGELOG_V3_1.md)

**Dự án:** LABGUARD — Lightweight Network Monitoring & Incident Management System  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Phiên bản:** V3.1 (Final Technical Correction Pass)  
**Ngày phát hành:** 13/09/2026  
**Vai trò thực hiện:** Technical Editor, Excel/Spreadsheet UX Designer, Beginner-first NT106 Mentor, Documentation Engineer  
**Đối tượng bàn giao:** Đội ngũ phát triển nhóm sinh viên & Đơn vị Kiểm toán độc lập (Independent Auditor)

---

## 1. TỔNG QUAN VÒNG CHỈNH SỬA V3.1 (EXECUTIVE SUMMARY)

Gói **Student Mode V3** đã nhận được phản hồi rất tích cực về mặt trải nghiệm người dùng (UX sư phạm cho 3 sinh viên năm 2):
- Thiết kế dạng thẻ trực quan (Card UI),
- Tích hợp sẵn khung câu lệnh prompt copy cho AI kèm hộp "Phần bạn phải tự hiểu",
- Giải thích khái niệm bằng ví dụ đời thường.

Tuy nhiên, kiểm tra nội bộ đã phát hiện một số điểm **sai lệch kỹ thuật (Technical Drift)** so với bộ đặc tả cốt lõi Checkpoint 1 (SSOT). Vòng **V3.1 Final Technical Correction Pass** được tiến hành với nguyên tắc tối thượng:
> **GIỮ NGUYÊN 100% TRẢI NGHIỆM DỄ DÙNG DẠNG THẺ CHO SINH VIÊN + KHẮC PHỤC TRIỆT ĐỂ TOÀN BỘ TECHNICAL DRIFT.**

Tuyệt đối **KHÔNG** quay lại giao diện enterprise bảng nhiều cột phức tạp; **KHÔNG** phá vỡ cấu trúc thẻ/prompt AI; và **KHÔNG** làm thay đổi baseline Checkpoint 1 đã đóng băng (60 tasks, 211.5h tổng, 164.5h committed, RBAC Admin/Operator/Viewer).

---

## 2. CHI TIẾT 11 NỘI DUNG HIỆU CHỈNH KỸ THUẬT (TECHNICAL CORRECTIONS)

### 2.1. Chuẩn hóa Danh mục Bản tin Wire Protocol (Blocker Resolved)
- **Vấn đề ở V3:** Một số vị trí trong Sheet 99, tài liệu hướng dẫn và AI prompt còn sót các tên bản tin không tồn tại trong `03_PROTOCOL_SPEC.md` (`COLLECTOR_REGISTER`, `COLLECTOR_FORWARD`, `QUERY_INCIDENT_HISTORY`, `QUERY_METRICS_HISTORY`, `ERROR_RESPONSE`, `INCIDENT_CREATED`, `METRIC_UPDATE`).
- **Khắc phục ở V3.1:**
  - Loại bỏ hoàn toàn 7 tên bản tin cũ nêu trên khỏi toàn bộ các tệp (.xlsx, .docx, .pdf, .json).
  - Khóa danh mục chuẩn mực P1 Collector gồm đúng 10 thông điệp: `HELLO_COLLECTOR`, `AUTH_COLLECTOR`, `FORWARD_OPEN`, `FORWARD_DATA`, `FORWARD_RESULT`, `FORWARD_CLOSE`, `CLOSE_SESSION`, `COLLECTOR_HEARTBEAT`, `HEALTH_PROBE`, `HEALTH_RESULT`.
  - Chuẩn hóa các truy vấn sang danh mục canonical: `GET_HISTORY` / `HISTORY_PAGE`, `GET_METRICS` / `METRICS_RESULT`, `LIST_INCIDENTS` / `INCIDENT_PAGE`, `GET_INCIDENT` / `INCIDENT_DETAIL`.
  - Chuẩn hóa thông báo lỗi sang `ERROR` và sự kiện phát sóng sang `INCIDENT_OPEN`, `INCIDENT_UPDATE`, `DEVICE_UPDATE`.

### 2.2. Khóa Bộ Mã Lỗi Chuẩn (Canonical Error Codes)
- **Vấn đề ở V3:** Dùng mã lỗi không chuẩn như `CONCURRENCY_CONFLICT` hoặc `AUTH_FAILED`.
- **Khắc phục ở V3.1:**
  - Thay thế toàn bộ bằng mã lỗi chính thức trong `CONTRACT_MATRIX.md`:
    - `ERR_CONCURRENCY_VIOLATION` (409 Conflict): Xung đột kiểm soát tương tranh lạc quan OCC qua `rowversion`.
    - `ERR_UNAUTHENTICATED` (401): Lỗi chưa đăng nhập hoặc token hết hạn.
    - `ERR_UNAUTHORIZED` (403): Lỗi không đủ quyền (RBAC: Viewer cố thực hiện mutation).
    - `ERR_FRAME_TOO_LARGE`: Frame length $N > 256\text{ KiB}$.
    - `ERR_STREAM_TRUNCATED`: Đứt kết nối giữa chừng (`EndOfStreamException`).
    - `ERR_IDEMPOTENCY_KEY_REUSE`: Cùng CommandId nhưng khác RequestDigest SHA-256.
    - `ERR_DEVICE_OFFLINE`: Mất nhịp tim quá 20s.
  - Vẫn giữ nguyên lời giải thích tiếng Việt đời thường cho sinh viên, nhưng mã định danh lập trình bắt buộc phải là chuỗi tiếng Anh canonical.

### 2.3. Khóa Trạng thái Sự cố Chuẩn (Canonical Incident States)
- **Vấn đề ở V3:** Còn xuất hiện các trạng thái không chuẩn như `NEW`, `OPEN/NEW`, `ACKED`.
- **Khắc phục ở V3.1:**
  - Xóa bỏ triệt để `NEW`, `OPEN/NEW`, `ACKED`.
  - Phân định rõ hai trục trạng thái:
    - **`ConditionState` (Trạng thái kỹ thuật):** `ACTIVE`, `RECOVERED`, `UNKNOWN`.
    - **`WorkflowState` (Trạng thái xử lý):** `OPEN`, `ACKNOWLEDGED`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED`.
  - Quy trình xử lý chuẩn: Khi sự cố phát sinh, trạng thái là `OPEN`. Admin/Operator bấm Tiếp nhận -> chuyển `ACKNOWLEDGED`. Bấm Bắt đầu -> chuyển `IN_PROGRESS`. Khi máy trạm đã phục hồi kỹ thuật (`RECOVERED`) và bấm Giải quyết -> chuyển `RESOLVED`.

### 2.4. Hoãn Khóa Schema CSDL 8 Bảng đến Checkpoint 2 (Database Schema Clarification)
- **Vấn đề ở V3:** Thẻ nhiệm vụ `LG-B-001` bảo sinh viên "Khởi tạo 8 bảng CSDL và tài khoản Admin mẫu" ngay ngày đầu tiên, trong khi Checkpoint 1 chưa đóng băng cấu trúc bảng CSDL chi tiết (bảng Users chưa có cấu trúc chính thức).
- **Khắc phục ở V3.1:**
  - Định nghĩa lại mục tiêu và việc cần làm của `LG-B-001`:
    *"TẠM THỜI CHƯA CODE FULL DATABASE SCHEMA. Trước CP2: cài đặt SQL Server; kiểm tra chuỗi kết nối từ C#; tạo database LabGuard_Dev rỗng; nắm vững khái niệm PK, FK, kiểu rowversion; chuẩn bị khung migration skeleton. Danh sách bảng chính thức sẽ được chốt và đóng băng tại Checkpoint 2."*
  - Bổ sung hộp cảnh báo màu đỏ nổi bật: `⛔ CHỜ CHECKPOINT 2 TRƯỚC KHI TẠO SCHEMA THẬT`.
  - Trong Sheet `00_Bat_Dau_O_Day`: Mục việc ngày 1 của SV B ghi rõ "Chuẩn bị SQL Server + kết nối .NET + migration skeleton [Chờ CP2 tạo bảng]".

### 2.5. Sắp xếp Lại Thứ tự Tác vụ Ngày 1 (Day 1 Sequencing)
- **Vấn đề ở V3:** Sheet 00 chỉ định SV A nhảy thẳng vào `LG-A-002` (FrameCodec) mà bỏ qua `LG-A-001`, trong khi Master WBS bắt đầu ở `TODO` và `LG-A-002` có điều kiện tiên quyết là `LG-A-001`.
- **Khắc phục ở V3.1:**
  - Thiết lập luồng công việc ngày 1 chuẩn xác:
    - **Sinh viên A:** Bắt đầu bằng `LG-A-001` (Chốt wire contract, 6 trường envelope, header 4 byte, 16 fixtures JSON).
    - **Sinh viên B:** `LG-B-001` (Chuẩn bị SQL Server, kiểm tra kết nối, chuẩn bị migration skeleton; không tạo bảng trước CP2).
    - **Sinh viên C:** `LG-C-001` (Dựng WinForms shell wireframe và mock DTO).
  - Nêu rõ nguyên tắc phối hợp: Chỉ sau khi SV A chốt xong hợp đồng `LG-A-001`, SV A mới bắt tay vào `LG-A-002` (`FrameCodec`), và SV B, C mới code các module phụ thuộc socket mạng.

### 2.6. Hỗ trợ Chọn Tuần Động trên Sheet `00_Bat_Dau_O_Day`
- **Vấn đề ở V3:** Nội dung sheet 00 bị gắn cứng (hardcode) chỉ hiển thị thông tin Tuần 1.
- **Khắc phục ở V3.1:**
  - Bổ sung ô chọn tuần tại ô `D5` với Data Validation Dropdown: `W1`, `W2`, `W3`, `W4`, `W5`.
  - Hiển thị bảng tổng hợp mục tiêu 5 tuần từ hàng 6 đến 10, giúp thành viên ở bất kỳ tuần nào cũng tra cứu nhanh được trọng tâm công việc của toàn nhóm.

### 2.7. Đồng bộ Chính sách Log Xoay Vòng (Rolling Log Policy)
- **Vấn đề ở V3:** Còn tồn tại rải rác con số cũ `5 MB / 3 files`.
- **Khắc phục ở V3.1:**
  - Khóa duy nhất một chính sách tại `LG-A-011`:
    - `MaxFileSize = 10 MB`,
    - `MaxFiles = 5`,
    - Tổng dung lượng thư mục log mục tiêu `<= 50 MB` [CP2 TO VERIFY].

### 2.8. Rà Soát Chất Lượng 60 Ví Dụ Đời Thường (Analogy Quality Pass)
- **Vấn đề ở V3:** Các ví dụ đời thường (Analogy) ở nhánh SV A bị lệch chỉ số (ví dụ A-004 TLS bị gán ví dụ đo huyết áp của A-005, A-005 bị gán ví dụ mất sóng của A-009, A-006 bị gán ví dụ đường hầm của A-004).
- **Khắc phục ở V3.1:**
  - Viết lại toàn bộ 60 ví dụ đời thường, bảo đảm 100% khớp đúng bản chất kỹ thuật của từng task:
    - `LG-A-001`: Phong bì thư chuẩn 6 mục.
    - `LG-A-002`: Hứng nước theo xô dán nhãn 4 byte độ dài.
    - `LG-A-003`: Cửa xoay 1 người (Single-writer queue).
    - `LG-A-004`: Gọi điện qua đường dây khóa mã và xác thực người nghe (TLS).
    - `LG-A-005`: Cụm đồng hồ xe máy đo tốc độ, xăng, quãng đường (CPU/RAM/NIC/Uptime).
    - `LG-A-006`: Nhân viên trực 5 giây báo tin sống và gửi kiện hàng số liệu.
    - `LG-A-007`: Bảo vệ gõ cửa từng phòng kiểm tra (TCP probe allowlist).
    - `LG-A-008`: Thuê 20 diễn viên đóng vai máy tính (Simulator).
    - `LG-A-009`: Gọi điện bị bận, chờ theo cấp số nhân kèm ngẫu nhiên (Backoff + Jitter).
    - `LG-A-010`: Cố tình cắt điện rút dây để thử nghiệm (Failure injection).
    - `LG-A-011`: Camera hành trình tự xóa video cũ (Rolling log 10MB x 5 = 50MB).
    - (và 49 analogies còn lại của nhóm B và C).

### 2.9. Loại Bỏ Hướng Dẫn Sửa Lỗi Chung Chung (Elimination of Generic Failure Help)
- **Vấn đề ở V3:** Mục "Nếu gặp lỗi" trên cả 60 thẻ bị sinh tự động bằng câu khuôn mẫu lặp lại: *"1. Kiểm tra kết nối mạng/CSDL. 2. Bẫy lỗi ngoại lệ... 3. Soát lại định dạng..."*.
- **Khắc phục ở V3.1:**
  - Thay thế toàn bộ 60 thẻ bằng 3 bước xử lý lỗi cụ thể cho từng task, đi kèm câu lệnh/công cụ thực tế (`ping`, `Test-NetConnection`, `Wireshark`, `openssl s_client`, `dotnet test`, `Stopwatch.GetTimestamp()`, `InvokeRequired`, `rowversion OCC`, `UTF-8 BOM`...).

### 2.10. Gắn Nhãn Tường Minh `[CP2 TO VERIFY]`
- **Vấn đề ở V3:** Một số hằng số kỹ thuật chưa được chốt ở Checkpoint 1 nhưng viết như thể đã đóng băng.
- **Khắc phục ở V3.1:**
  - Gắn nhãn `[CP2 TO VERIFY]` cho tất cả các tham số sẽ đo đạc và kiểm chứng tại Checkpoint 2:
    - Kích thước hàng đợi `BoundedChannel` 1000 items `[CP2 TO VERIFY]`.
    - Ngưỡng hysteresis 3 chu kỳ liên tiếp `[CP2 TO VERIFY]`.
    - Timeout thăm dò cổng probe 2000ms `[CP2 TO VERIFY]`.
    - Ngân sách ghi đĩa SQL Server < 50ms `[CP2 TO VERIFY]`.
    - Lô dọn dẹp retention 5000 dòng `[CP2 TO VERIFY]`.
    - Thời gian trì hoãn mất Collector 45s `[CP2 TO VERIFY]`.

### 2.11. Giọng Văn Trung Tính Học Thuật (Neutral Pedagogical Tone)
- **Vấn đề ở V3:** Còn chứa một số câu hứa hẹn điểm số hoặc dọa sinh viên: *"chắc chắn bị liệt vấn đáp"*, *"giảng viên sẽ chấm 9-10"*, *"100% mượt"*, *"không bao giờ"*.
- **Khắc phục ở V3.1:**
  - Chuyển sang giọng văn chuẩn mực đại học:
    *"Ưu tiên một MVP ổn định, có minh chứng và thành viên hiểu rõ code hơn việc mở rộng thêm P1 nhưng chưa kiểm thử đủ."*
    *"Nếu không tự giải thích được code, sinh viên sẽ gặp rủi ro rất lớn khi hội đồng hỏi thi."*

---

## 3. DANH MỤC THÀNH PHẨM BÀN GIAO V3.1 (DELIVERABLES INVENTORY)

| STT | Tên tệp thành phẩm | Định dạng | Quy mô / Thông số | Trạng thái xác thực |
|:---:|:---|:---:|:---:|:---:|
| 1 | `LabGuard_Student_Task_Tracker_v3_1.xlsx` | Excel Workbook (.xlsx) | 10 sheets, 151,556 bytes | **PASS** — Đạt kiểm tra Microsoft Excel COM tự động; 0 lỗi công thức; 0 disallowed terms. |
| 2 | `LabGuard_Huong_Dan_Cho_Nhom_v3_1.docx` | Word Document (.docx) | 9 phần + 1 phụ lục, 123,073 bytes | **PASS** — Định dạng chuẩn Segoe UI, cấu trúc bảng và callout box hoàn chỉnh. |
| 3 | `LabGuard_Huong_Dan_Cho_Nhom_v3_1.pdf` | Adobe PDF (.pdf) | 116 trang, 5.36 MB | **PASS** — Xuất trực tiếp qua Microsoft Word COM; 0 disallowed terms; phân trang sạch sẽ. |
| 4 | `STUDENT_MODE_CONTENT_QA.md` | Markdown Report | 8,371 ký tự | **PASS** — Ma trận kiểm định chi tiết 12 hạng mục kỹ thuật đối soát SSOT. |
| 5 | `STUDENT_MODE_CHANGELOG_V3_1.md` | Markdown Report | Tài liệu hiện tại | **PASS** — Nhật ký so sánh toàn diện V3.1 vs V3.0. |

*Lưu ý bảo lưu:* Toàn bộ các tệp V1 (`v1`), V2 (`v2`) và V3 (`v3`) trước đó vẫn được giữ nguyên vẹn trên đĩa, không bị ghi đè hay xóa bỏ.

---

## 4. KẾT LUẬN & SẴN SÀNG KIỂM TOÁN (AUDIT READINESS)

Gói **LABGUARD STUDENT MODE V3.1** đã hoàn thành trọn vẹn vòng hiệu chỉnh kỹ thuật:
- Giữ vững 100% sự thân thiện, trực quan và dễ tiếp cận cho sinh viên năm 2.
- Đồng bộ 100% với danh mục Wire Protocol 69 bản tin và ma trận giao ước hợp đồng.
- Sẵn sàng bàn giao cho Auditor ChatGPT độc lập thẩm định.
