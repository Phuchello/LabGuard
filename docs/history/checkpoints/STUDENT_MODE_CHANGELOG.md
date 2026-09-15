# LABGUARD NT106 — NHẬT KÝ NÂNG CẤP STUDENT MODE V3 (CHANGELOG)
**Dự án:** LABGUARD — Hệ thống giám sát mạng & Quản lý sự cố phòng máy tập trung  
**Môn học:** NT106 — Lập trình mạng căn bản (UIT, ĐHQG-HCM)  
**Đối tượng sử dụng:** 03 Sinh viên năm 2 thực hiện đồ án  
**Thời điểm phát hành:** 2026-09-13  
**Phiên bản:** Team Package V3 (Student Mode — Bản tinh gọn sư phạm & Trợ lực AI)  

---

## 1. BỐI CẢNH VÀ LÝ DO RA ĐỜI PHIÊN BẢN V3

Sau khi kiểm định toàn diện Team Package V2, chúng tôi nhận thấy phiên bản V2 đạt chuẩn doanh nghiệp (Enterprise Grade) rất cao, đáp ứng đầy đủ tiêu chí kỹ thuật và quy trình quản trị dự án. Tuy nhiên, đối với nhóm sinh viên năm 2 đang trong quá trình học môn Lập trình mạng, bộ tài liệu V2 tồn tại một số rào cản thực tế:
- **Quá tải thông tin (Cognitive Overload):** Màn hình bảng tính quá nhiều cột (30 cột), nhiều thuật ngữ học thuật và quy trình Jira/Scrum/GitFlow phức tạp khiến sinh viên mở file bị ngợp.
- **Khó xác định hành động tức thời:** Sinh viên mất nhiều thời gian đọc quy trình thay vì biết ngay hôm nay mình cần sửa file nào, code hàm nào.
- **Chưa tối ưu hóa thói quen dùng AI:** Sinh viên năm 2 chủ yếu dùng AI (ChatGPT, Claude, Copilot) để hỗ trợ viết code, giải thích code và gỡ lỗi nhưng chưa có câu lệnh gợi ý (prompt) chuẩn may đo cho từng task, dễ dẫn đến việc copy code tràn lan mà không hiểu bản chất, dẫn đến nguy cơ bị điểm liệt khi thi vấn đáp.

**Phiên bản V3 (Student Mode)** được thiết kế lại hoàn toàn dựa trên triết lý:
> **"Mở file lên 30 giây là biết hôm nay mình phải làm gì."**  
> **Ưu tiên: DỄ HIỂU > DỄ LÀM > DỄ CHECK > ĐẸP > QUY TRÌNH ENTERPRISE.**

Phiên bản V3 **KHÔNG phá vỡ và KHÔNG ghi đè** bất kỳ tệp tin nào của V1 và V2. Toàn bộ 60 nhiệm vụ WBS, 211.5h khối lượng, hợp đồng giao thức 69 bản tin và mô hình phân quyền Checkpoint 1 vẫn được bảo lưu 100% làm chân lý duy nhất (Single Source of Truth) ở tầng kỹ thuật.

---

## 2. NÂNG CẤP BẢNG TÍNH ĐIỀU PHỐI `LabGuard_Student_Task_Tracker_v3.xlsx`

### 2.1. Cấu trúc 10 Sheet thân thiện với sinh viên

| STT | Tên Sheet | Màu Tab | Mục đích & Trải nghiệm sinh viên |
| :---: | :--- | :---: | :--- |
| **01** | `00_Bat_Dau_O_Day` | `#0070C0` (Blue) | **MÀN HÌNH QUAN TRỌNG NHẤT:** Mở file là vào sheet này đầu tiên. Trả lời trực diện 5 câu hỏi: *(1) Tuần này nhóm cần đạt gì? (2) A đang làm gì? (3) B đang làm gì? (4) C đang làm gì? (5) Cuối ngày cần chứng minh được gì?*. Kèm 7 checkbox hành động thực tế và tiêu chí mắt thường khi nào được bấm DONE. |
| **02** | `01_Viec_Cua_A` | `#2B579A` (Blue/Cyan) | **THẺ NHIỆM VỤ CỦA A (AGENT & MẠNG):** 20 nhiệm vụ chia theo 5 tuần. Mỗi nhiệm vụ là 1 CARD độc lập gồm mục tiêu 1 câu, trực giác đời thường, 4 bước làm, **Prompt copy nhanh cho AI**, hộp *"PHẦN BẠN PHẢI TỰ HIỂU"* để thi viva, cách test và cách sửa lỗi. |
| **03** | `02_Viec_Cua_B` | `#4A2574` (Purple/Navy) | **THẺ NHIỆM VỤ CỦA B (SERVER & CSDL):** 20 thẻ nhiệm vụ cho phân hệ máy chủ, CSDL 8 bảng, khóa lạc quan OCC và bảo mật PBKDF2 với phong cách thẻ sinh viên trực quan. |
| **04** | `03_Viec_Cua_C` | `#107C41` (Teal/Emerald) | **THẺ NHIỆM VỤ CỦA C (DASHBOARD & UI):** 20 thẻ nhiệm vụ cho giao diện WinForms, điều phối luồng UI Invoke an toàn, xử lý xung đột 2 Admin và kịch bản demo 12 phút. |
| **05** | `04_Tu_Dien_De_Hieu` | `#ED7D31` (Orange) | **TỪ ĐIỂN 'NÓI TIẾNG NGƯỜI':** Bảng tra cứu 15 khái niệm khó (Byte Stream, Framing, Partial Read, Heartbeat, Backpressure, OCC, rowversion, Invoke...) được giải thích bằng ví dụ đời thường thay vì định nghĩa sách giáo khoa. |
| **06** | `05_Ke_Hoach_5_Tuan` | `#70AD47` (Green) | **KẾ HOẠCH 5 TUẦN SIÊU ĐƠN GIẢN:** Mỗi tuần chỉ có 1 câu mục tiêu duy nhất, việc cụ thể của A/B/C, bài test chung và hướng dẫn kích hoạt `DEFER P1` (-47h) nếu trượt cổng M4 ở Tuần 3. |
| **07** | `06_Test_Da_Chay` | `#ED7D31` (Orange) | **BẢNG THEO DÕI KIỂM THỬ:** Tinh gọn, sinh viên chỉ cần ghi mã task, kết quả mong đợi, chọn PASS/FAIL và gắn đường dẫn ảnh/log minh chứng. |
| **08** | `07_On_Van_Dap` | `#7030A0` (Purple) | **BẢNG ÔN VẤN ĐÁP CÁ NHÂN:** 90 câu hỏi chia theo A, B, C với 2 câu tự đánh giá: *"Tôi đã hiểu bản chất chưa?"*, *"Tôi tự giải thích được bằng lời?"* và điểm tự tin 0-10. |
| **09** | `99_Chi_Tiet_Ky_Thuat` | `#595959` (Gray) | **MASTER WBS DÀNH CHO TRA CỨU SÂU:** Lưu trữ đầy đủ 60 nhiệm vụ với 30 cột kỹ thuật từ Checkpoint 1/V2, có biển cảnh báo màu vàng: *"Sheet này dùng để tra cứu sâu cho kỹ thuật và hội đồng chấm thi. Sinh viên không cần đọc toàn bộ trước khi bắt đầu."* |
| **10** | `Lists` | `#A6A6A6` (Light Gray) | Danh mục dữ liệu phục vụ Data Validation dropdowns (Chưa làm, Đang làm, Kẹt, Xong; PASS, FAIL...). |

### 2.2. Kiểm định công thức bằng Excel COM
- Đã chạy kiểm tra tự động trên Microsoft Excel 16.0 COM (`excel.CalculateFull()`).
- Kết quả kiểm định: **Chính xác 0 ô lỗi** (`#REF!`, `#NAME?`, `#VALUE!`, `#DIV/0!`, `#N/A`).
- Tệp mở mặc định vào sheet `00_Bat_Dau_O_Day`.

---

## 3. NÂNG CẤP SỔ TAY HƯỚNG DẪN `LabGuard_Huong_Dan_Cho_Nhom_v3` (.docx & .pdf)

Sổ tay V3 được tái cấu trúc toàn diện từ bản 138 trang của V2 thành một cẩm nang học tập thực chiến cô đọng **110 trang PDF** (5.32 MB), loại bỏ triệt để sự lặp lại của 60 bảng DoD 11 mục, tập trung vào phương pháp sư phạm:

1. **Chương mở đầu bắt buộc: "NẾU BẠN NHỜ AI VIẾT CODE — HÃY LÀM ĐÚNG 5 BƯỚC NÀY":**
   - Hướng dẫn sinh viên cách làm chủ code do AI sinh ra.
   - Bắt buộc trả lời 3 câu hỏi: Input là gì? Output là gì? Tại sao cần đoạn code này?
   - Cố tình làm lỗi code để hiểu ngoại lệ.
   - Tự giải thích lại bằng lời mà không nhìn màn hình AI trước khi đánh dấu hoàn thành.
2. **Kiến trúc LabGuard bằng ví dụ đời thường:**
   - Dùng hình tượng Trạm kiểm soát y tế trường học (Học sinh đo nhiệt độ = Agent; Bác sĩ trực = Central Server; Bảng điện tử cổng trường = Dashboard WinForms) giúp sinh viên nắm bắt hệ thống trong 5 phút.
   - Giải thích bản chất TCP Byte Stream (ống nước chảy liên tục) và lý do bắt buộc phải có FrameCodec 4 byte Big-Endian.
3. **60 Thẻ nhiệm vụ tinh giản:**
   - Mỗi task chỉ gồm 5 khối thông tin cốt lõi: *Mục tiêu 1 câu -> Ví dụ đời thường -> 4 việc cần làm -> Prompt copy cho AI -> Phần phải tự hiểu để thi viva*.
4. **Kịch bản test mạng LAN thật 3 máy:**
   - Hướng dẫn cắm dây switch, lấy IP, mở cổng tường lửa và 3 bài test thực địa (Bắt Wireshark thấy TLS, rút dây mạng liveness 20s, bẫy lỗi xung đột 2 Admin OCC).
5. **Bộ 90 câu hỏi Viva & Công thức 3 bước vàng UIT:**
   - *Bước 1: Nêu bản chất lý thuyết ngắn gọn -> Bước 2: Nêu quyết định trong đồ án -> Bước 3: Mở code chỉ đúng dòng lệnh minh họa*.
6. **Kịch bản Demo 12 phút trước hội đồng:**
   - Bấm giờ từng phút cho 6 bước demo.
   - Chuẩn bị kế hoạch cứu nguy (video `DemoBackup.mp4` quay sẵn) nếu phòng thi gặp sự cố mất điện/rớt mạng.
7. **Từ điển 'Nói tiếng người' (Phụ lục):**
   - 15 thuật ngữ mạng cốt lõi được giải thích bình dị, dễ hiểu.

---

## 4. TỔNG KẾT DANH MỤC TỆP PHÁT HÀNH V3

| STT | Tên tệp tin | Định dạng | Tình trạng | Dung lượng |
| :---: | :--- | :---: | :---: | :---: |
| 1 | `LabGuard_Student_Task_Tracker_v3.xlsx` | Excel Workbook | **MỚI HOÀN TOÀN** | 141 KB |
| 2 | `LabGuard_Huong_Dan_Cho_Nhom_v3.docx` | Word Document | **MỚI HOÀN TOÀN** | 113 KB |
| 3 | `LabGuard_Huong_Dan_Cho_Nhom_v3.pdf` | Adobe PDF (110 trang) | **MỚI HOÀN TOÀN** | 5.32 MB |
| 4 | `STUDENT_MODE_CHANGELOG.md` | Markdown Report | **MỚI HOÀN TOÀN** | ~8 KB |

*(Toàn bộ các tệp V1 và V2 vẫn được lưu trữ nguyên vẹn trên đĩa, không bị thay đổi).*

---
*Gói sản phẩm Student Mode V3 đã hoàn tất kiểm tra và sẵn sàng đưa vào vận hành thực tế cho nhóm sinh viên.*
