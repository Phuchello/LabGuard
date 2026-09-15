# LABGUARD — BÁO CÁO AUDIT HIỆN TRẠNG TÀI LIỆU & CODEBASE
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, Trường ĐH Công nghệ Thông tin — ĐHQG-HCM)  
**Tác giả audit:** Lead Technical Researcher & Network Software Architect (Antigravity)  
**Đối tượng bàn giao:** ChatGPT Work (Auditor độc lập) & Nhóm sinh viên NT106  
**Trạng thái:** Đã cập nhật sau vòng Audit của ChatGPT Work — Checkpoint 1  
**Ngày thực hiện:** 13/09/2026  

---

## 1. TỔNG QUAN HIỆN TRẠNG DỰ ÁN

### 1.1. Các tài nguyên hiện có trong workspace
1. **Bộ LaTeX đề cương v3 (`LABGUARD_DeCuong_NT106_LaTeX_v3`)**:
   - `main.tex`: Metadata, package, bố cục 5 phần và 3 phụ lục.
   - `template/nt106.cls`: Class đề cương chuẩn của UIT, đã chuyển đổi từ format KLTN sang Đồ án môn học NT106.
   - `sections/1-TongQuan.tex`: Bối cảnh phòng máy, vấn đề theo dõi đơn giản, kiến trúc MVP, personas, use cases, điểm khác biệt.
   - `sections/2-MucTieuDeTai.tex`: Mục tiêu tổng quát và 10 mục tiêu cụ thể.
   - `sections/3-PhuongPhapThucHien.tex`: 10 bước thực hiện kỹ thuật kèm các `beginnerbox` và `examplebox`.
   - `sections/4-NoiDungDeTai.tex`: 10 nội dung chính, phạm vi P0/P1/P2/P3, giới hạn đề tài, bảng truy vết yêu cầu (FR01–FR12).
   - `sections/5-KeHoachThucHien.tex`: Phân công 3 người, kế hoạch 5 tuần (W1–W5), biểu đồ Gantt (pgfgantt), bảng Milestones (M0–M7), Đồ thị phụ thuộc (DAG), quy tắc co giãn lịch, ma trận kiểm thử (33 test scenarios), kịch bản demo 7m45s, Git workflow và DoD.
   - `appendices/A-Glossary.tex`: Bảng tra cứu 77 thuật ngữ kỹ thuật.
   - `appendices/B-TaskMap.tex`: Bảng ánh xạ 60 task (LG-A-001 đến LG-C-020) tách từ Sprint plan.
   - `appendices/C-Viva.tex`: Khung vấn đáp cá nhân 3 thành viên và mô hình trả lời 6 bước (WHAT - WHY - FLOW - FAILURE - VERIFY - TRADE-OFF).
   - `bibliography.bib`: Tài liệu tham khảo.

2. **Tài liệu nguồn thiết kế**:
   - `c:\Users\lyle3\Downloads\LABGUARD_MASTER_PLAN.pdf` (13 trang, Phiên bản 1.0 ngày 13/09/2026): Bản blueprint tổng thể gồm 26 mục từ A đến Z, 60 task, 3 owner A/B/C, 135 giờ kỹ thuật, phân cấp MVP (P0) và nâng cao (P1).
   - *Lưu ý về các file chi tiết:* Các file đặc tả lẻ (`03_PROTOCOL_SPEC.md`, `04_DATABASE_DESIGN.md`, `11_WBS_TASKS.csv`, v.v.) hiện chưa có file zip vật lý độc lập trong workspace; mọi thông số kỹ thuật được kế thừa nguyên vẹn từ `LABGUARD_MASTER_PLAN.pdf` và các file source LaTeX hiện hữu.

3. **Tài liệu học tập môn NT106 tại UIT**:
   - `[COURSE]` Giáo trình chính thức: *Tài liệu hướng dẫn Thực hành Lập trình Mạng căn bản* — ThS. Đỗ Thị Hương Lan & ThS. Trần Hồng Nghi, NXB ĐHQG-HCM, 2022 (7 bài thực hành).
   - `[COURSE]` Slide bài giảng lý thuyết: TL1 đến TL8 (Tổng quan, Stream I/O, Multi-threading, Socket TCP/UDP, Web, Email, Security).
   - `[REFERENCE]` Bộ đề cương đáp án vấn đáp môn Lập trình mạng UIT trên mạng (`van-dap-mon-lap-trinh-mang-1.pdf`): 24 câu hỏi - đáp tham khảo (gắn nhãn `[REFERENCE]`, không tự gắn `[COURSE]`).
   - `[REFERENCE]` Các bài thực hành mẫu của sinh viên UIT các khóa trước (Lab 1 đến Lab 6, Assignment 1 đến 4, Báo cáo tiến độ đồ án).

---

## 2. ĐÁNH GIÁ SỰ PHÙ HỢP VỚI ĐỐI TƯỢNG BẮT ĐẦU (BEGINNER-READABILITY AUDIT)

### 2.1. Ưu điểm của bản đề cương hiện tại
- Đã tách bạch rõ ràng giữa **Đồ án môn học NT106** và **Khóa luận tốt nghiệp / NCKH**. Không lạm xưng đề tài nghiên cứu hàn lâm.
- Đã có các khung thông tin sư phạm trực quan: `beginnerbox` (giải thích từ khóa), `examplebox` (ví dụ kịch bản cụ thể), `warningbox` (lưu ý bẫy kỹ thuật), `vivabox` (câu hỏi bảo vệ cá nhân).
- Cấu trúc chặt chẽ, truy vết được từ Mục tiêu $\rightarrow$ Phân rã chức năng $\rightarrow$ Kế hoạch Sprint $\rightarrow$ WBS Task ID.
- Giữ vững quan điểm: **Networking là cốt lõi**, UI chỉ là công cụ hiển thị, kiên quyết loại bỏ các tính năng lệch hướng (AI/ML, Remote Shell, Kubernetes, Kafka, Cloud microservices).

### 2.2. Các điểm nghẽn nhận thức & Đính chính sau Audit Work
1. **Khái niệm "Length-prefix Framing" và quan hệ với Newline**:
   - *Đính chính sau Audit:* Không lập luận rằng "JSON chứa xuống dòng khiến mọi giao thức phân tách bằng newline đều sai". Theo **RFC 8259 §7**, ký tự xuống dòng trong chuỗi JSON bắt buộc phải được escape thành `\n`, do đó JSON đơn dòng vẫn có thể dùng delimiter đúng cách.
   - *Lý do LabGuard chọn Length-prefix:* Nhờ có contract nhị phân tường minh (biết trước độ dài $N$ để cấp phát bộ đệm một lần), kiểm soát parser sạch sẽ không cần quét từng byte trong RAM, và chống DoS chủ động qua `MaxFrameSize = 256 KiB`.
2. **Khẳng định về `FrameCodec`**:
   - Bỏ khẳng định "loại bỏ 100% JsonException".
   - Diễn giải chính xác: `FrameCodec` giải quyết triệt để bài toán phân mảnh byte stream (Partial Read & Dính gói). Lỗi JSON sai cú pháp, EOF giữa chừng và độ dài vượt ngưỡng vẫn phải được bắt và xử lý bằng exception handler riêng.
3. **Bản chất cờ `Socket.Connected` và phát hiện Offline**:
   - `Socket.Connected` chỉ phản ánh trạng thái quan sát từ thao tác I/O trước đó, không phải bằng chứng liveness hiện tại.
   - Bỏ cụm từ "phục hồi dưới 20 giây". Thay bằng: "Đánh dấu Offline khi quá **20 giây** kể từ heartbeat hợp lệ gần nhất theo đồng hồ đơn điệu phía server". Scheduler tick $\leq 1$s là `[DESIGN TARGET]`.
4. **Quy chuẩn ngưỡng CPU (`HIGH_CPU`)**:
   - Đồng bộ chuẩn xác theo Master Plan: Kích hoạt khi CPU $\geq 90\%$ trong **3 mẫu liên tiếp**; phục hồi khi CPU $\leq 75\%$ trong **3 mẫu liên tiếp**; chu kỳ lấy mẫu **5 giây**. Vùng trễ 15% ngăn ngừa hiện tượng Alert Flapping.
5. **Kiểu dữ liệu `rowversion` trong SQL Server**:
   - Có kích thước cố định **8 byte**, không liên quan đến ngày giờ. Tự động sinh giá trị nhị phân tự tăng duy nhất mới cho dòng đó khi insert hoặc update, kể cả khi update cùng giá trị. Dùng làm token cho Optimistic Concurrency Control.
6. **WinForms UI Marshaling**:
   - Cập nhật UI từ background task bắt buộc phải dispatch về UI thread thông qua `Control.Invoke`, `Control.BeginInvoke` hoặc `.NET 9+ Control.InvokeAsync`.
   - `CheckForIllegalCrossThreadCalls = false` chỉ tắt cờ kiểm tra của runtime, hoàn toàn không làm code thread-safe.

---

## 3. KHÓA PHẠM VI & TÍNH KHẢ THI (SCOPE LOCKS)

1. **MVP (P0) — BẮT BUỘC HOÀN THIỆN ĐẦY ĐỦ**:
   - Windows Agent: TCP/TLS, Host Telemetry (CPU/RAM/NIC/Uptime), Service Port Probe, Heartbeat, Auto-reconnect backoff.
   - Central Server: TCP/TLS Ingress, Framing Parser, Session Registry, Deduplication, Rule Engine (4 rules), Incident Engine, SQL Server 2022 Express.
   - Admin Client: WinForms, Secure Login (PBKDF2/Salt), Dashboard, Lab View, Device Detail, Incident Management, Realtime Updates (Snapshot + Delta).
   - Kiểm thử & Triển khai: Agent Simulator (10/20/50 agents), Mạng LAN trên 2–3 laptop thật.
2. **P1 (NÂNG CAO) — CÓ ĐIỀU KIỆN (CONDITIONAL)**:
   - Gateway định tuyến kết nối theo Least-Connections + 2 Collector Nodes trung gian + Failover có gián đoạn cho Agent (Target failover p95 $\leq 45$s là `[DESIGN TARGET]`).
   - **Quy tắc Gate M4:** Chỉ kích hoạt P1 nếu cuối Tuần 3 đạt tiêu chuẩn 0 lỗi Sev 1/2 đi kèm các test MVP bắt buộc đã chạy thật. Nếu hoãn (DEFER P1), cập nhật DAG: A016 $\rightarrow$ A012, B016 $\rightarrow$ B012, C016 $\rightarrow$ C011; ghi nhận test P1 là `SKIPPED`, M5 là `DEFERRED`.
3. **TUYỆT ĐỐI LOẠI BỎ (OUT OF SCOPE)**:
   - Không AI/Machine Learning, không Remote Shell/PowerShell, không Remote Desktop, không Kubernetes, không Kafka/Redis.
