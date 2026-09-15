# NHẬT KÝ THAY ĐỔI STUDENT MODE V3.1.2 (STUDENT_MODE_CHANGELOG_V3_1_2.md)

**Dự án:** LABGUARD — Lightweight Network Monitoring & Incident Management System  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Phiên bản:** V3.1.2 (True Final Micro-Hotfix)  
**Ngày phát hành:** 14/09/2026  
**Vai trò thực hiện:** Technical Editor, Excel/Spreadsheet UX Designer, Beginner-first NT106 Mentor, Documentation Engineer  
**Đối tượng bàn giao:** Đội ngũ phát triển nhóm sinh viên & Đơn vị Kiểm toán độc lập (Independent Auditor)

---

## 1. TỔNG QUAN VÒNG CHỈNH SỬA V3.1.2 (EXECUTIVE SUMMARY)

Phiên bản **Student Mode V3.1.1** đã vượt qua hầu hết các tiêu chí khắt khe của đợt Kiểm toán độc lập (Independent Audit). Vòng **V3.1.2 True Final Micro-Hotfix** là bước tinh chỉnh vi mô cuối cùng nhằm xử lý dứt điểm 5 phát hiện còn lại trước khi đóng băng hoàn toàn gói tài liệu:
1. **Xóa triệt để mâu thuẫn nội tại tại LG-A-014:** Loại bỏ hoàn toàn wording `"Health Probe TCP chu kỳ 2s"` còn sót tại mục tiêu cụ thể; chuyển sang chuẩn tham số danh nghĩa `HealthProbeInterval [CP2 TO VERIFY]` (giá trị đề xuất 5s).
2. **Gắn nhãn `[CP2 TO VERIFY]` toàn diện từ gốc:** Bổ sung nhãn `[CP2 TO VERIFY]` ngay từ mục tiêu task, mô tả hợp đồng, 4 bước hành động và kết quả test mong đợi của các task chứa thông số dự kiến (`LG-A-003`, `LG-B-003`, `LG-A-009`, `LG-A-015`, `LG-B-006`).
3. **Chuẩn hóa ngữ nghĩa hạch toán giờ công (Workload Accounting):** Phân định rạch ròi giữa Giờ cơ bản (Base Hours), Giờ dự phòng (Buffer Hours) và Tổng giờ công (Total Hours). Không cộng lẫn lộn số liệu legacy `164.5h` với giờ cơ bản của P1.
4. **Sửa ngữ nghĩa đồng bộ trạng thái Excel:** Chuyển đổi toàn bộ cụm từ `"đồng bộ 2 chiều"` thành `"Đồng bộ trạng thái từ thẻ cá nhân về Master WBS"` (phản ánh chính xác cơ chế liên kết công thức 1 chiều).
5. **Loại bỏ các phát biểu suy đoán học thuật không có nguồn chính thức:** Thay thế các câu văn về "hoàn toàn có quyền sử dụng AI" và "Hội đồng đánh giá cao" bằng văn phong trung lập, chuẩn mực học thuật UIT.

**Cam kết bất biến:**
- Tuyệt đối **KHÔNG** tái thiết kế kiến trúc, không tạo phiên bản V4.
- Giữ nguyên 100% danh mục 60 Task ID, phân công Owner (A: 20, B: 20, C: 20) và ngân sách giờ công cơ bản/dự phòng/tổng.
- Bảo toàn nguyên vẹn các phiên bản trước (V1, V2, V3, V3.1, V3.1.1).
- Không viết mã ứng dụng LabGuard; không bắt đầu Checkpoint 2 trước khi hoàn tất kiểm toán.

---

## 2. CHI TIẾT 5 NỘI DUNG HIỆU CHỈNH VI MÔ (MICRO-HOTFIX DETAILS)

### 2.1. Blocker 1 — Xử lý Triệt để Wording Cố định 2s tại LG-A-014
- **Hiện trạng trước sửa:** Dù phần gỡ lỗi đã có nhãn CP2, nhưng trường `plain_goal` của task `LG-A-014` vẫn còn chứa chuỗi `"Health Probe TCP chu kỳ 2s (3 fail / 2 success)"`.
- **Khắc phục tại V3.1.2:**
  - Viết lại toàn diện mục tiêu của `LG-A-014`:
    *"Hoàn thành DuplexRelay.cs & HealthPoller.cs đáp ứng tiêu chuẩn Stream.CopyToAsync hai chiều, HealthProbeInterval [CP2 TO VERIFY] (giá trị đề xuất 5s / 3 fail / 2 recovery [CP2 TO VERIFY])"*.
  - Toàn bộ 4 bước hành động, prompt AI, test case và file Master WBS quy về 3 biến danh nghĩa:
    - `HealthProbeInterval [CP2 TO VERIFY]` (giá trị đề xuất 5s)
    - `HealthFailThreshold [CP2 TO VERIFY]` (giá trị đề xuất 3 chu kỳ)
    - `HealthRecoveryThreshold [CP2 TO VERIFY]` (giá trị đề xuất 2 chu kỳ)
  - Quét tự động trên toàn bộ kho tệp V3.1.2:
    - Tìm kiếm chính xác `"Health Probe TCP chu kỳ 2s"`: **0 kết quả**.
    - Tìm kiếm chuỗi `"chu kỳ 2s"`: **0 kết quả**.

### 2.2. Item 2 — Gắn Nhãn `[CP2 TO VERIFY]` Toàn diện Ngay từ Mục Tiêu và Hợp Đồng
- **Hiện trạng trước sửa:** Một số thông số giả định chưa đóng băng ở CP1 vẫn được viết như hằng số cố định trong mục tiêu và các bước hành động.
- **Khắc phục tại V3.1.2:**
  - `LG-A-003`: Kích thước hàng đợi được ghi rõ là `BoundedChannelOptions(100 [CP2 TO VERIFY])` ngay từ mục tiêu và bước 1.
  - `LG-B-003`: Hàng đợi máy chủ ghi rõ `BoundedChannelOptions(1000 [CP2 TO VERIFY])` trong mục tiêu, bước 3 và tiêu chuẩn nghiệm thu.
  - `LG-A-009`: Thuật toán backoff và bộ đệm replay ghi rõ: `Backoff lũy thừa: 1s, 2s, 4s, 8s, max 30s + jitter 10% [CP2 TO VERIFY]; Replay buffer tối đa 50 batches [CP2 TO VERIFY]`.
  - `LG-A-015`: Ngưỡng failover ghi rõ: `thời gian tái kết nối p95 <= 45s [CP2 TO VERIFY]` trong cả các bước hành động và kết quả test mong đợi.
  - `LG-B-006`: Ngưỡng đánh giá máy offline ghi rõ: `Missed 3 intervals (15s) + 5s grace = 20s [CP2 TO VERIFY] offline deadline`.
  - Tổng số nhãn `[CP2 TO VERIFY]` được phủ rộng khắp: **68 vị trí** trong Sổ tay PDF và **101 vị trí** trong Sổ theo dõi Excel.

### 2.3. Item 3 — Chuẩn hóa Toàn Diện Ngữ Nghĩa Hạch Toán Giờ Công (Workload Accounting)
- **Hiện trạng trước sửa:** Việc ghi `"211.5h = 164.5h committed + 47h P1"` gây mâu thuẫn số học vì 164.5h là số liệu legacy trong khi 47.0h là Base Hours của P1 (chưa cộng 4.5h buffer).
- **Khắc phục tại V3.1.2:**
  - Tính toán trực tiếp và minh bạch từ 60 dòng Master WBS SSOT:
    - **TỔNG GIỜ CÔNG DỰ ÁN (TOTAL WORKLOAD = BASE + BUFFER):**
      - Giai đoạn P0 (51 tasks): **160.0h total** (gồm 137.5h base + 22.5h buffer).
      - Giai đoạn P1 (9 tasks): **51.5h total** (gồm 47.0h base + 4.5h buffer).
      - TOÀN DỰ ÁN (60 tasks): **211.5h total** (gồm 184.5h base + 27.0h buffer).
    - **GIỜ CÔNG CƠ BẢN (BASE HOURS):**
      - P0 Base = 137.5h
      - P1 Base = 47.0h
      - Toàn dự án Base = 184.5h
    - **GIỜ DỰ PHÒNG RỦI RO (BUFFER / CONTINGENCY HOURS):**
      - P0 Buffer = 22.5h
      - P1 Buffer = 4.5h
      - Toàn dự án Buffer = 27.0h
    - **PHÂN BỔ THEO THÀNH VIÊN:**
      - Sinh viên A (20 tasks): 60.0h base + 9.0h buffer = 69.0h total.
      - Sinh viên B (20 tasks): 63.0h base + 9.0h buffer = 72.0h total.
      - Sinh viên C (20 tasks): 61.5h base + 9.0h buffer = 70.5h total.
  - Chú thích rõ ràng: Con số `164.5h` được ghi chú là số liệu ước tính trong tài liệu lập kế hoạch ban đầu (legacy planning figure), không dùng để cộng trực tiếp với Base Hours của P1.

### 2.4. Item 4 — Sửa Đổi Ngữ Nghĩa Đồng Bộ Trạng Thái Excel (One-Way Sync)
- **Hiện trạng trước sửa:** Gọi cơ chế cập nhật trạng thái là "đồng bộ 2 chiều", trong khi bản chất kỹ thuật là các ô trạng thái thẻ cá nhân (A, B, C) liên kết bằng công thức Excel dẫn về Sheet 99.
- **Khắc phục tại V3.1.2:**
  - Đổi toàn bộ wording thành: `"Đồng bộ trạng thái từ thẻ cá nhân về Master WBS"`.
  - Quét tự động trên toàn bộ tài liệu: Số lần xuất hiện cụm từ `"đồng bộ 2 chiều"` = **0**.

### 2.5. Item 5 — Loại Bỏ Hoàn Toàn Phát Biểu Suy Đoán Học Thuật
- **Hiện trạng trước sửa:** Chứa các câu văn mang tính suy đoán chính sách môn học ("hoàn toàn có quyền sử dụng AI") hoặc suy đoán thái độ hội đồng ("Hội đồng đánh giá cao").
- **Khắc phục tại V3.1.2:**
  - Thay câu về AI thành: *"Nếu quy định của môn học và giảng viên cho phép, AI có thể được dùng để hỗ trợ giải thích code, gỡ lỗi và học tập. Thành viên vẫn phải hiểu và tự bảo vệ phần mình thực hiện. Nếu chỉ copy-paste mà không hiểu bản chất, bạn sẽ không thể trả lời câu hỏi vấn đáp: 'Tại sao chỗ này dùng hàm này?' hoặc 'Bỏ dòng code này đi thì hệ thống xảy ra lỗi gì?'."*
  - Thay câu về hội đồng tại Cổng M4 thành: *"Chiến lược của nhóm là ưu tiên MVP ổn định, có bằng chứng kiểm thử và thành viên hiểu rõ phần mình thực hiện trước khi mở rộng P1. Một MVP cốt lõi chạy ổn định với bằng chứng thực nghiệm rõ ràng sẽ bảo đảm an toàn kỹ thuật hơn là cố mở rộng tính năng phân tán nâng cao khi thời gian và nhân lực không cho phép."*
  - Thay câu về hội đồng tại Kịch bản Demo 12 phút thành: *"Đây là phương án dự phòng bảo đảm nhóm luôn có minh chứng kỹ thuật hoạt động ngay cả khi môi trường phòng lab gặp sự cố!"*
  - Quét tự động trên toàn bộ tài liệu:
    - `"hoàn toàn có quyền sử dụng AI"`: **0 kết quả**.
    - `"Hội đồng đánh giá cao"`: **0 kết quả**.

---

## 3. BẢNG TỔNG HỢP KIỂM CHỨNG TỰ ĐỘNG (AUTOMATED VERIFICATION METRICS)

| Chỉ số Kiểm tra | Mục tiêu Yêu cầu | Kết quả Đo đạc V3.1.2 | Đánh giá |
| :--- | :---: | :---: | :---: |
| Số nhiệm vụ WBS & Phân bổ | 60 tasks (A:20, B:20, C:20) | 60 tasks (A:20, B:20, C:20) | **PASS** |
| Hạch toán Giờ công P0 | Base: 137.5h, Buffer: 22.5h, Total: 160.0h | 137.5h base, 22.5h buf, 160.0h tot | **PASS** |
| Hạch toán Giờ công P1 | Base: 47.0h, Buffer: 4.5h, Total: 51.5h | 47.0h base, 4.5h buf, 51.5h tot | **PASS** |
| Hạch toán Giờ công Toàn dự án | Base: 184.5h, Buffer: 27.0h, Total: 211.5h | 184.5h base, 27.0h buf, 211.5h tot | **PASS** |
| Bản tin giao thức legacy cấm | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Cụm từ "Health Probe TCP chu kỳ 2s" | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Cụm từ "chu kỳ 2s" trong health probe | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Cụm từ "đồng bộ 2 chiều" | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Cụm từ "hoàn toàn có quyền sử dụng AI" | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Cụm từ "Hội đồng đánh giá cao" | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Chuỗi "5 MB" trong context rolling log | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Chuỗi "3 file" trong context rolling log | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Nhãn thực nghiệm `[CP2 TO VERIFY]` | Hiện diện trên toàn bộ ngưỡng giả định | PDF: 68 lần \| XLSX: 101 lần | **PASS** |
| Dropdown trạng thái trên thẻ cá nhân | 60/60 thẻ | 60/60 thẻ (100%) | **PASS** |
| Công thức liên kết trạng thái Sheet 99 | 60 công thức liên kết | 60/60 công thức liên kết | **PASS** |
| Ô chọn tuần Sheet 00 & VLOOKUP động | D5 Dropdown + D6:D9 VLOOKUP | Đã kiểm chứng trên Excel COM | **PASS** |
| Lỗi công thức Excel (`#REF!`, `#NAME?`, `#N/A`) | 0 lỗi | **0 lỗi** | **PASS** |
| Số trang Sổ tay Hướng dẫn PDF | >= 100 trang | **115 trang** (5.37 MB) | **PASS** |

---

## 4. DANH MỤC TỆP BÀN GIAO V3.1.2

1. `LabGuard_Student_Task_Tracker_v3_1_2.xlsx` (154,881 bytes, 10 sheets)
2. `LabGuard_Huong_Dan_Cho_Nhom_v3_1_2.docx` (124,843 bytes)
3. `LabGuard_Huong_Dan_Cho_Nhom_v3_1_2.pdf` (5,633,891 bytes, 115 trang)
4. `STUDENT_MODE_CHANGELOG_V3_1_2.md` (Nhật ký chi tiết)
5. `STUDENT_MODE_TRUE_FINAL_QA.md` (Báo cáo thẩm định QA thực tế)

Toàn bộ các phiên bản trước (V1, V2, V3, V3.1, V3.1.1) được lưu giữ nguyên trạng trên hệ thống tệp.
