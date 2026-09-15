# LABGUARD — NHẬT KÝ NGHIÊN CỨU & THAY ĐỔI (CHANGELOG DEEP RESEARCH)
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (UIT)  

---

## [CHECKPOINT 1] — 13/09/2026

### 1. Phân tích hiện trạng & Đối soát chương trình (Current-State & Course Alignment)
- **Tạo mới `research/00_CURRENT_STATE_AUDIT.md`**:
  - Rà soát toàn bộ 5 file `sections/*.tex` và 3 file `appendices/*.tex`.
  - Nhận diện 20 điểm nghẽn thuật ngữ (Jargon Avalanche) gây khó hiểu cho người mới học NT106.
  - Phân tích rủi ro giữa mục tiêu thiết kế và kết quả đo đạc thực tế; tái khẳng định cam kết 135 giờ kỹ thuật cho nhóm 3 người.
- **Tạo mới `research/01_COURSE_ALIGNMENT.md`**:
  - Đối soát 12 thành phần kỹ thuật của LabGuard với 7 bài thực hành trong Giáo trình Thực hành LTM căn bản UIT (ThS. Đỗ Thị Hương Lan & ThS. Trần Hồng Nghi, NXB ĐHQG-HCM 2022).
  - Khẳng định tính chất "Đồ án Lập trình mạng thuần túy", phân biệt rõ ràng với phần mềm quản lý CRUD thông thường.
  - Phân tích 3 bước tiến kỹ thuật từ bài thực hành căn bản lên đồ án LabGuard:
    1. Từ Delimiter `\n` sang Length-Prefix Framing 4-byte.
    2. Từ `CheckForIllegalCrossThreadCalls = false` sang `Control.Invoke`.
    3. Từ `while (socket.Connected)` sang Application Heartbeat có Deadline.

### 2. Sổ tay nghiên cứu & Nguồn chuẩn mực
- **Tạo mới `research/RESEARCH_LEDGER.md`**: Ghi nhận 15 claim kỹ thuật then chốt với phân loại nguồn nghiêm ngặt (`[COURSE]`, `[STANDARD]`, `[SOURCE]`, `[REFERENCE]`, `[PROPOSAL]`).
- **Tạo mới `SOURCE_MAP.md`**: Phân tầng nguồn tài liệu theo Tier 1 (RFC, UIT, Microsoft, OWASP), Tier 2 (Master Plan), Tier 3 (Khóa trước) và Tier 4.
- **Tạo mới `DECISIONS.md`**: Ban hành 4 quyết định kiến trúc ban đầu (ADR-001 đến ADR-004) về Framing, Single Writer, Heartbeat và Khóa phạm vi MVP.
- **Tạo mới `research/OPEN_QUESTIONS.md`**: Nêu 5 câu hỏi mở về rubric, demo Internet/CGNAT, SQLite fallback và PBKDF2 iterations.

### 3. Nghiên cứu sâu Tầng Giao vận & TCP Socket Core
- **Tạo mới `research/02_NETWORKING_CORE.md`**:
  - Giải thích toàn diện 15 câu hỏi cốt lõi về TCP theo quy chuẩn 9 bước sư phạm: *Vấn đề thực tế $\rightarrow$ Trực giác $\rightarrow$ Ví dụ đơn giản $\rightarrow$ Thuật ngữ $\rightarrow$ Cơ chế $\rightarrow$ Cách LabGuard dùng $\rightarrow$ Hậu quả nếu sai $\rightarrow$ Cách test $\rightarrow$ Câu hỏi vấn đáp*.
  - Cung cấp mã nguồn chuẩn `FrameCodec.cs` với thuật toán `ReadExactlyBytesAsync` giải quyết triệt để lỗi Partial Read và Dính gói (Packet Coalescing).
  - Vẽ sơ đồ tuần tự (Mermaid Sequence Diagram) 4 giai đoạn kết nối giữa Agent và Central Server.
  - Xây dựng chi tiết 5 kịch bản kiểm thử giao thức (TC-NET-01 đến TC-NET-05) với cấu trúc SETUP - ACTION - EXPECTED - EVIDENCE.
  - Chuẩn bị 3 câu hỏi vấn đáp trọng tâm về TCP Framing, Partial Read và giới hạn `MaxFrameSize`.

---

## [CHECKPOINT 1 REVISION] — 13/09/2026 (Xử lý phản hồi Audit của ChatGPT Work)

### 1. Đồng bộ & Sửa đổi các khẳng định kỹ thuật (Claims Revision)
- **Chuẩn hóa Liveness & Clock:** Thay thế việc so sánh timestamp của client bằng đo thời gian bằng `Stopwatch` / monotonic clock tại Central Server cho ngưỡng offline $> 20$s. Bỏ khẳng định phi thực tế "phục hồi dưới 20s". Target failover p95 $\le 45$s được gắn nhãn `[DESIGN TARGET]`.
- **Chuẩn hóa Rule Engine:** CPU $\ge 90\%$ trong 3 sample liên tiếp (chu kỳ 5s) kích hoạt `HIGH_CPU`; phục hồi khi CPU $\le 75\%$ trong 3 sample liên tiếp. Đảm bảo dải hysteresis 15% chống hiện tượng rung giật trạng thái.
- **Chuẩn hóa Framing & Protocol:** 4-byte Big-Endian length $N$ biểu diễn độ dài payload JSON UTF-8 ($N$ không bao gồm 4 byte header, giới hạn tối đa 256 KiB). Chuẩn hóa envelope: `version`, `messageType`, `requestId`, `deviceId`, `sentAtUtc`, `isReplay`, `payload`. Sửa claim "loại bỏ 100% JsonException" thành framing giải quyết ranh giới byte stream TCP; JsonException do payload hỏng vẫn được xử lý qua try-catch an toàn.
- **Chuẩn hóa Concurrency:** Central Server sử dụng async I/O trên ThreadPool (`ReadAsync`). Mỗi socket có 1 Task đọc và 1 Task Single Writer tiêu thụ từ `Channel<byte[]>` có giới hạn, tránh đồng thời ghi socket mà không cần lock đồng bộ.
- **Chuẩn hóa UI Threading:** Network callback cập nhật WinForms bắt buộc dùng `Control.Invoke` / `BeginInvoke` (hoặc `InvokeAsync` trên .NET 9+ WinForms).
- **Khóa chặt CSDL (ADR-005):** Hủy đề xuất SQLite hot-swap, giữ SQL Server 2022 Express làm baseline duy nhất. Làm rõ `rowversion` là kiểu nhị phân 8-byte tự tăng của SQL Server, không phải kiểu ngày giờ (`datetime`).
- **Tháo gỡ phụ thuộc P1 (ADR-006):** Quy tắc tách rời DAG (DAG Decoupling) cho phép bỏ nhánh P1 sau gate M4 mà không làm đình trệ các task kiểm thử và đóng gói MVP ở Tuần 4--5.

### 2. Bổ sung Test Harness & Thực thi kiểm chứng (Execution & Evidence)
- **Tạo và chạy test harness C# .NET 9:** `scratch/test_frame_codec/Program.cs` biên dịch và thực thi bằng `dotnet run` $\rightarrow$ **5/5 tests PASSED** (TC-NET-01 đến TC-NET-05).
- **Biên dịch Đề cương LaTeX:**
  - Sửa lỗi rò rỉ font `\scriptsize` tại `appendices/B-TaskMap.tex` bằng cách khôi phục `\normalsize`.
  - Cập nhật sơ đồ TikZ Hình 2 với nhãn `relapse (tái phát)` và caption làm rõ phân tách 2 lớp trạng thái.
  - Bổ sung trích dẫn chuẩn RFC 9293, RFC 8259, RFC 8446, Giáo trình UIT và Microsoft Learn vào `bibliography.bib`.
  - Thêm `\usepackage{float}` và thông tin `Biên soạn: Võ Trọng Phúc` vào `main.tex`.
  - Chạy chuỗi biên dịch `pdflatex` + `biber` + `pdflatex` + `pdflatex` xuất file `main.pdf` 30 trang hoàn chỉnh, không lỗi.

### 3. Xuất hồ sơ bàn giao tái đối soát (Re-Audit Handoff)
- **Tạo mới `AUDIT_RESPONSE_CHECKPOINT_1.md`:** Ma trận phản hồi chi tiết 10 finding của Auditor.
- **Cập nhật `AUDIT_HANDOFF.md`:** Bàn giao bộ tài liệu đã đồng bộ và bằng chứng kiểm chứng.
- **Cập nhật `PROJECT_STATE.md` và `TODO.md`:** Đánh dấu hoàn thành toàn bộ công việc Checkpoint 1 Revision.
- **Dừng lại theo quy luật:** Dừng tại Checkpoint 1 và đợi kết quả đối soát của ChatGPT Work trước khi tiến hành Checkpoint 2.
