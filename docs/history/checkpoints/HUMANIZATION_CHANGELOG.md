# LABGUARD NT106 — HUMANIZATION / DE-AI POLISH CHANGELOG

## 1. Mục đích đợt hiệu chỉnh
Đợt hiệu chỉnh này nhằm tinh gọn toàn bộ hệ thống tài liệu và công cụ quản lý đồ án NT106, chuyển đổi từ phong cách "AI consultant / enterprise framework" sang văn phong tự nhiên, thực tế và trong sáng của một nhóm sinh viên kỹ thuật UIT thực hiện đồ án môn học.

Toàn bộ nền tảng kỹ thuật, phân công công việc, giờ công, giao thức truyền thông và các nhãn kiểm soát phạm vi đã được đóng băng ở Checkpoint 1 (V3.1.2a) được **giữ nguyên 100% không đổi**.

---

## 2. Danh mục tài liệu chuyển giao chính thức (Clean Deliverables)

| Tên tệp | Định dạng | Mục đích sử dụng | Trạng thái kỹ thuật |
| :--- | :--- | :--- | :--- |
| `LabGuard_Task_Tracker.xlsx` | Excel Workbook | Bảng điều phối tiến độ 5 tuần, thẻ 60 nhiệm vụ, theo dõi test và ôn tập vấn đáp | 100% Khớp WBS SSOT, 0 lỗi công thức |
| `LabGuard_Huong_Dan_Cho_Nhom.docx` | MS Word | Sổ tay hướng dẫn chi tiết kiến trúc, từng bước làm việc, hướng dẫn dùng AI và kịch bản demo | 100% Khớp Protocol Spec |
| `LabGuard_Huong_Dan_Cho_Nhom.pdf` | PDF Document | Bản in chuẩn hóa (112 trang) lưu trữ và nộp báo cáo | Biên dịch trực tiếp qua MS Word COM |

*Ghi chú: Các phiên bản trung gian (V3, V3.1, V3.1.1, V3.1.2, V3.1.2a) được lưu trữ làm hồ sơ kiểm toán lịch sử.*

---

## 3. Các thay đổi chính về giao diện & cách trình bày

### 3.1. Tinh giản tiêu đề và xóa bỏ các khẩu hiệu AI
- **Bìa tài liệu Word / PDF:**
  - *Trước:* `SỔ TAY HƯỚNG DẪN THỰC THI CHO NHÓM (STUDENT MODE V3.1.2 — TRUE FINAL TECHNICAL BASELINE)`
  - *Sau:* `SỔ TAY HƯỚNG DẪN THỰC HIỆN ĐỒ ÁN LABGUARD`
  - Đưa thông tin Trường, Khoa và Môn học về đúng quy chuẩn học thuật nghiêm túc.
- **Tiêu đề bảng tính Excel (Sheet 00):**
  - *Trước:* `LABGUARD NT106 — BẮT ĐẦU Ở ĐÂY (STUDENT MODE V3.1.2)` kèm phụ đề tiếp thị: *"Mở file lên 30 giây là biết hôm nay phải làm gì..."*
  - *Sau:* `LABGUARD NT106 — BẮT ĐẦU Ở ĐÂY` kèm hướng dẫn thao tác tự nhiên: *"Chọn tuần hiện tại để xem việc của từng thành viên và mục tiêu cần hoàn thành."*
- **Tiêu đề Master WBS (Sheet 99):**
  - *Trước:* `⚠️ SHEET NÀY DÙNG ĐỂ TRA CỨU SÂU CHO KỸ THUẬT VÀ HỘI ĐỒNG CHẤM THI (MASTER WBS SSOT)`
  - *Sau:* `LABGUARD NT106 — DANH SÁCH NHIỆM VỤ` | Phụ đề: `60 nhiệm vụ — 20 nhiệm vụ / người. Trạng thái tự động đồng bộ từ thẻ công việc của từng thành viên.`

### 3.2. Chuẩn hóa tiêu đề mục và giảm thiểu biểu tượng cảm xúc (Emoji)
- Giảm 60–70% số lượng biểu tượng cảm xúc (📌, 📋, 🧠, 👉, ⛔) để giao diện nhìn trang nhã, không mang cảm giác tài liệu tự động sinh:
  - `PHẦN BẠN PHẢI TỰ HIỂU (BẮT BUỘC ĐỂ THI VẤN ĐÁP)` $\rightarrow$ `Cần hiểu trước khi vấn đáp`
  - `PROMPT COPY NHANH CHO AI` $\rightarrow$ `Prompt tham khảo`
  - `NẾU GẶP LỖI (3 BƯỚC KIỂM TRA CỤ THỂ)` $\rightarrow$ `Nếu gặp lỗi`
  - `MINH CHỨNG & VẤN ĐÁP` $\rightarrow$ `Minh chứng và câu hỏi vấn đáp`
  - `HẠCH TOÁN NGÂN SÁCH GIỜ CÔNG MINH BẠCH (WORKLOAD ACCOUNTING)` $\rightarrow$ `Khối lượng công việc dự kiến`
  - `CỔNG M4 SINH TỬ` $\rightarrow$ `Mốc đánh giá M4 (cuối tuần 3)`

### 3.3. Tinh giản Prompt AI
- Loại bỏ đoạn mở đầu rập khuôn lặp đi lặp lại ở cả 60 nhiệm vụ (*"Tôi là sinh viên năm 2 đang làm đồ án NT106..."*).
- Chuyển thành mẫu prompt ngắn gọn, thực dụng:
  - Nêu rõ mã nhiệm vụ (`Task ID`) và mục tiêu cụ thể.
  - Yêu cầu giải thích ngắn gọn bản chất kỹ thuật.
  - Chỉ rõ file cần tạo / sửa.
  - Hướng dẫn từng bước với C# .NET 9 kèm cách chạy test kiểm chứng.
  - Đặt 3 câu hỏi vấn đáp trọng tâm để tự ôn tập.

### 3.4. Bảo toàn nguyên vẹn các bất biến kỹ thuật
1. **60 Nhiệm vụ:** Phân bổ đều 20 (SV A) - 20 (SV B) - 20 (SV C).
2. **Khối lượng giờ công:**
   - P0 (51 nhiệm vụ): 137.5h cơ bản + 22.5h dự phòng = 160.0h.
   - P1 (9 nhiệm vụ): 47.0h cơ bản + 4.5h dự phòng = 51.5h.
   - Toàn bộ đồ án: 184.5h cơ bản + 27.0h dự phòng = 211.5h.
3. **Công thức Excel:**
   - 4 công thức `=VLOOKUP` động tại Sheet `00_Bat_Dau_O_Day` (D6:D9).
   - 60 công thức đồng bộ trạng thái từ thẻ cá nhân về bảng chi tiết tại Sheet `99_Chi_Tiet_Ky_Thuat` (cột 30).
   - 0 lỗi công thức (`#REF!`, `#NAME?`, `#VALUE!`, `#N/A`).
4. **Nhãn `[CP2 TO VERIFY]`:** Bảo tồn nguyên vẹn tại tất cả các vị trí kỹ thuật, bao gồm 4 câu hỏi vấn đáp then chốt đã hiệu chỉnh ở bản V3.1.2a (`LG-A-003`, `LG-A-009`, `LG-B-003`, `LG-B-006`).
