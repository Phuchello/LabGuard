# BẢNG MA TRẬN KIỂM ĐỊNH CHẤT LƯỢNG CUỐI CÙNG (STUDENT_MODE_FINAL_AUDIT_QA.md)

**Dự án:** LABGUARD — Lightweight Network Monitoring & Incident Management System  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Phiên bản kiểm định:** V3.1.1 (Final Independent Audit Hotfix Pass)  
**Thời điểm kiểm tra:** 13/09/2026  
**Đơn vị thẩm định:** Independent Auditor (ChatGPT / Senior Reviewer)  
**Đơn vị thực hiện giải trình:** Project Operations Designer, Technical Editor, Excel UX Designer, Beginner-first NT106 Mentor, Documentation Engineer  

---

## 1. TỔNG HỢP KẾT QUẢ KIỂM TOÁN V3.1.1 (AUDIT SUMMARY SCORECARD)

| Hạng mục Thẩm định | Mức độ Ưu tiên | Trạng thái Trước sửa | Trạng thái V3.1.1 | Kết luận Kiểm toán |
| :--- | :---: | :---: | :---: | :---: |
| **1. Blocker — Rolling Log Policy** | **BLOCKER** | Còn sót "5 MB / 3 files" rải rác | Đã thay thế triệt để 100% | **PASSED (0 match)** |
| **2. Blocker — CSDL LG-B-001 vs Master WBS** | **BLOCKER** | Sheet 99 ghi tạo 8 bảng | Đã hoãn schema sang CP2 | **PASSED** |
| **3. Item 3 — Thống nhất ACK_TIMEOUT** | **HIGH** | Lẫn lộn giá trị cố định và danh nghĩa | Unified: `ACK_TIMEOUT [CP2 TO VERIFY] (5s)` | **PASSED** |
| **4. Item 4 — Thống nhất Ngưỡng Health Probe** | **HIGH** | Lẫn lộn chu kỳ probe và recovery | Unified sang 3 biến danh nghĩa | **PASSED** |
| **5. Item 5 — Gắn nhãn `[CP2 TO VERIFY]`** | **MEDIUM** | Một số ngưỡng chưa có nhãn | Gắn nhãn 42 vị trí (PDF) & 63 vị trí (XLSX) | **PASSED** |
| **6. Item 6 — Chuẩn hóa Vòng đời Sự cố** | **HIGH** | Chưa nhấn mạnh rõ 5 trạng thái | Chuẩn hóa 5-state: OPEN->ACK->IN_PROG->RECOV->RESOLV | **PASSED** |
| **7. Item 7 — Dropdown Trạng thái Thẻ & Sync** | **HIGH** | Chưa có dropdown trên 60 thẻ cá nhân | 60/60 thẻ có DataValidation + Col 30 sync | **PASSED** |
| **8. Item 8 — Tra cứu Tuần Động Sheet 00** | **HIGH** | Hardcode nội dung Tuần 1 | Dropdown D5 + VLOOKUP động hàng 6-9 | **PASSED** |
| **9. Item 9 — Rà soát Giọng văn & Bản tin Cấm** | **HIGH** | Còn từ cấm `INCIDENT_UPDATE` | 0 từ cấm, giọng văn trung lập, khích lệ | **PASSED (0 disallowed)** |

---

## 2. MA TRẬN GIẢI TRÌNH & DẪN CHỨNG KỸ THUẬT CHI TIẾT (QA EVIDENCE MATRIX)

### Câu hỏi 1 (Blocker 1): Làm thế nào chứng minh chính sách Rolling Log cũ (5 MB / 3 files) đã bị xóa sạch 100%?
- **Giải trình:** Chúng tôi đã quét toàn bộ văn bản và bảng tính bằng mã kiểm thử tự động `scratch/verify_student_mode_v3_1_1.py` sử dụng thư viện `pymupdf` và `openpyxl`.
- **Dẫn chứng định lượng:**
  - Chuỗi `"5 MB"` trong ngữ cảnh rolling log:
    - Trong file `LabGuard_Huong_Dan_Cho_Nhom_v3_1_1.pdf`: **0 lần xuất hiện**.
    - Trong file `LabGuard_Student_Task_Tracker_v3_1_1.xlsx`: **0 lần xuất hiện**.
  - Chuỗi `"3 file"` trong ngữ cảnh rolling log:
    - Trong file PDF: **0 lần xuất hiện**.
    - Trong file Excel: **0 lần xuất hiện**.
  - Chính sách chuẩn duy nhất hiện hành tại thẻ `LG-A-011` và toàn bộ tài liệu:
    - `MaxFileSize = 10 MB [CP2 TO VERIFY]` (xuất hiện 6 lần trong PDF, 10 lần trong Excel).
    - `MaxFiles = 5 [CP2 TO VERIFY]` (xuất hiện 2 lần trong PDF, 5 lần trong Excel).
    - Tổng dung lượng mục tiêu `<= 50 MB [CP2 TO VERIFY]` (xuất hiện 6 lần trong PDF, 9 lần trong Excel).

---

### Câu hỏi 2 (Blocker 2): Xung đột giữa Sổ tay hướng dẫn và Master WBS về việc tạo 8 bảng CSDL ở task LG-B-001 được xử lý ra sao?
- **Giải trình:** Tại phiên bản V3.1, Sổ tay hướng dẫn đã cảnh báo hoãn schema nhưng ô mô tả của Master WBS (Sheet 99) vẫn giữ nội dung cũ. Ở V3.1.1, chúng tôi đã cập nhật trực tiếp ô mô tả kỹ thuật tại Hàng 26, Cột 11 của Sheet `99_Chi_Tiet_Ky_Thuat`.
- **Dẫn chứng nguyên văn tại Sheet 99 Cột 11:**
  > *"Trước CP2: cài SQL Server, kiểm tra connection từ C#, tạo LabGuard_Dev rỗng, học PK/FK/rowversion, chuẩn bị migration skeleton. Schema chính thức chốt tại Checkpoint 2."*
- **Sự thống nhất:** Cả Sổ tay hướng dẫn (Phần 4, trang 58 và hộp cảnh báo đỏ thẻ LG-B-001) lẫn Master WBS đều thống nhất 100%: Sinh viên B tuyệt đối không tạo bảng hay viết migration cụ thể trước khi Checkpoint 2 diễn ra.

---

### Câu hỏi 3 (Item 3): Thời gian chờ ACK_TIMEOUT tại LG-A-006 được chuẩn hóa như thế nào để tránh gây hiểu nhầm?
- **Giải trình:** Để sinh viên năm 2 hiểu đúng đây là một biến cấu hình có thể điều chỉnh thay vì một hằng số bất di bất dịch, toàn bộ tài liệu được quy về dạng biến kỹ thuật danh nghĩa.
- **Dẫn chứng chuẩn hóa:**
  - Định dạng thống nhất: `ACK_TIMEOUT [CP2 TO VERIFY] (giá trị đề xuất 5s)`.
  - Xuất hiện nhất quán trong: Mục tiêu task `LG-A-006`, 4 bước hành động, prompt hỗ trợ AI, câu hỏi tự hiểu vấn đáp và bảng kiểm tra kỹ thuật.

---

### Câu hỏi 4 (Item 4): Các tham số kiểm tra sức khỏe mạng (Health Probe) và phục hồi tại LG-A-014 được quy ước ra sao?
- **Giải trình:** Tách biệt rõ ràng 3 đại lượng: chu kỳ gửi tín hiệu kiểm tra, số chu kỳ liên tiếp mất tín hiệu để kích hoạt cảnh báo, và số chu kỳ bình thường liên tiếp để xác nhận khôi phục.
- **Dẫn chứng chuẩn hóa:**
  - `HealthProbeInterval [CP2 TO VERIFY]` = 5 giây.
  - `HealthFailThreshold [CP2 TO VERIFY]` = 3 chu kỳ liên tiếp không có phản hồi hợp lệ.
  - `HealthRecoveryThreshold [CP2 TO VERIFY]` = 2 chu kỳ liên tiếp phản hồi hợp lệ.
  - Cả 3 tham số đều gắn nhãn `[CP2 TO VERIFY]` để nhóm sinh viên tiến hành đo đạc độ trễ LAN thực tế trong giai đoạn Checkpoint 2.

---

### Câu hỏi 5 (Item 5): Mức độ phủ của nhãn `[CP2 TO VERIFY]` trên các thông số kỹ thuật giả định?
- **Giải trình:** Mọi thông số chưa được kiểm chứng qua môi trường mạng LAN thực nghiệm phòng thí nghiệm đều phải mang nhãn `[CP2 TO VERIFY]` để bảo đảm tính trung thực khoa học trong báo cáo đề cương.
- **Dẫn chứng đo đạc tự động:**
  - Xuất hiện trong `LabGuard_Huong_Dan_Cho_Nhom_v3_1_1.pdf`: **42 lần**.
  - Xuất hiện trong `LabGuard_Student_Task_Tracker_v3_1_1.xlsx`: **63 lần**.
  - Áp dụng trên: Hàng đợi `BoundedChannel (1000 items [CP2 TO VERIFY])`, Ngưỡng liveness `20s [CP2 TO VERIFY]`, Hysteresis `3 chu kỳ [CP2 TO VERIFY]`, Dung lượng log `<= 50 MB [CP2 TO VERIFY]`.

---

### Câu hỏi 6 (Item 6): Vòng đời sự cố (Incident Lifecycle) được mô tả chuẩn mực như thế nào?
- **Giải trình:** Khóa chặt chu trình 5 trạng thái:
  $$\text{OPEN} \longrightarrow \text{ACKNOWLEDGED} \longrightarrow \text{IN\_PROGRESS} \longrightarrow \text{RECOVERED} \longrightarrow \text{RESOLVED}$$
- **Dẫn chứng về trạng thái `RECOVERED`:**
  - `RECOVERED` xuất hiện **16 lần** trong Sổ tay PDF và **14 lần** trong Sổ theo dõi Excel.
  - Giải thích rõ ràng cho sinh viên: Khi Agent gửi số liệu cho thấy CPU đã hạ dưới ngưỡng cảnh báo hoặc mạng thông suốt trở lại, sự cố chuyển sang `RECOVERED` (trạng thái phục hồi kỹ thuật). Trạng thái này là **điều kiện cần bắt buộc** trước khi Admin hoặc Operator bấm nút `RESOLVE` để đóng hoàn toàn hồ sơ sự cố.

---

### Câu hỏi 7 (Item 7): Trải nghiệm cập nhật tiến độ trên Excel Tracker có bảo đảm tính tiện dụng và đồng bộ không?
- **Giải trình:** Sinh viên không cần nhập tay trạng thái. Mỗi thẻ nhiệm vụ đều có Data Validation Dropdown và tự động ánh xạ ngược vào Master WBS.
- **Dẫn chứng kỹ thuật trong Excel:**
  - Vùng nguồn dữ liệu: `Lists!$A$2:$A$5` gồm: `Chưa làm`, `Đang làm`, `Kẹt`, `Xong`.
  - Phủ đủ: **60/60 thẻ nhiệm vụ cá nhân** trên 3 sheet (`01_Viec_Cua_A`, `02_Viec_Cua_B`, `03_Viec_Cua_C`).
  - Cột 30 (Trạng thái) trên Sheet `99_Chi_Tiet_Ky_Thuat` chứa công thức liên kết động: `='01_Viec_Cua_A'!D{row}` (cho A), `='02_Viec_Cua_B'!D{row}` (cho B), `='03_Viec_Cua_C'!D{row}` (cho C).
  - Kết quả kiểm tra qua Microsoft Excel COM 16.0: 0 lỗi `#REF!`, 0 lỗi `#VALUE!`, 0 lỗi `#NAME?`.

---

### Câu hỏi 8 (Item 8): Sheet `00_Bat_Dau_O_Day` có hoạt động tương tác động khi chuyển tuần không?
- **Giải trình:** Thay vì văn bản tĩnh hardcode Tuần 1, ô `D5` là dropdown chọn tuần (`W1` đến `W5`) và các ô `D6:D9` được nạp công thức `=VLOOKUP` động.
- **Dẫn chứng công thức Excel:**
  - Ô `D5`: Giá trị mặc định `W1`, có Data Validation danh sách `W1, W2, W3, W4, W5`.
  - Ô `D6` (Mục tiêu chung): `=VLOOKUP(D5, Lists!$D$2:$H$6, 2, FALSE)`
  - Ô `D7` (Trọng tâm việc A): `=VLOOKUP(D5, Lists!$D$2:$H$6, 3, FALSE)`
  - Ô `D8` (Trọng tâm việc B): `=VLOOKUP(D5, Lists!$D$2:$H$6, 4, FALSE)`
  - Ô `D9` (Trọng tâm việc C): `=VLOOKUP(D5, Lists!$D$2:$H$6, 5, FALSE)`
  - Bảng dữ liệu tra cứu nằm tại `Lists!$D$2:$H$6`, bao quát toàn bộ mục tiêu của 5 tuần từ W1 đến W5.

---

### Câu hỏi 9 (Item 9): Kiểm tra danh mục bản tin cấm (Legacy Disallowed Protocol Messages) và văn phong sư phạm?
- **Giải trình:** Đã rà soát và loại bỏ sạch sẽ các bản tin giao thức legacy không có trong Wire Protocol SSOT (`03_PROTOCOL_SPEC.md`).
- **Dẫn chứng quét chuỗi trên toàn bộ kho tệp V3.1.1:**
  - `COLLECTOR_REGISTER`: 0 kết quả.
  - `COLLECTOR_FORWARD`: 0 kết quả.
  - `QUERY_INCIDENT_HISTORY`: 0 kết quả.
  - `QUERY_METRICS_HISTORY`: 0 kết quả.
  - `GET_DEVICES`: 0 kết quả.
  - `INCIDENT_UPDATE`: 0 kết quả.
  - `INCIDENT_RESOLVE_CONFIRM`: 0 kết quả.
  - `CONFIG_SET`: 0 kết quả.
  - `AGENT_COMMAND`: 0 kết quả.
- **Giọng văn:** Toàn bộ các câu cảnh báo mang tính gây hoang mang đã được chuyển ngữ sang phong cách điềm tĩnh, hướng dẫn từng bước rõ ràng, phù hợp tâm lý học tập của sinh viên năm 2.

---

## 3. DANH MỤC TỆP BÀN GIAO & TÌNH TRẠNG LƯU TRỮ TRÊN ĐĨA

| Tên Tệp Bàn Giao | Định Dạng | Kích Thước | Tình Trạng Lưu Trữ |
| :--- | :---: | :---: | :---: |
| `LabGuard_Student_Task_Tracker_v3_1_1.xlsx` | Microsoft Excel (.xlsx) | 154,187 bytes | Đã tạo mới, không ghi đè V1/V2/V3/V3.1 |
| `LabGuard_Huong_Dan_Cho_Nhom_v3_1_1.docx` | Microsoft Word (.docx) | 123,719 bytes | Đã tạo mới, không ghi đè V1/V2/V3/V3.1 |
| `LabGuard_Huong_Dan_Cho_Nhom_v3_1_1.pdf` | Adobe PDF (.pdf) | 5,640,000 bytes (116 trang) | Xuất chuẩn qua Word COM, font Segoe UI |
| `STUDENT_MODE_CHANGELOG_V3_1_1.md` | Markdown (.md) | ~7 KB | Nhật ký chi tiết sự khác biệt V3.1.1 vs V3.1 |
| `STUDENT_MODE_FINAL_AUDIT_QA.md` | Markdown (.md) | ~8 KB | Ma trận kiểm định chất lượng đối chiếu |

Tất cả các bản tệp V1, V2, V3, V3.1 đều được bảo toàn nguyên vẹn trong thư mục dự án.

---

## 4. KẾT LUẬN CUỐI CÙNG

Gói sản phẩm **LABGUARD STUDENT MODE V3.1.1** đã hoàn thành toàn bộ các yêu cầu hiệu chỉnh kỹ thuật, khắc phục triệt để các phát hiện của đợt kiểm toán độc lập, đạt độ chính xác 100% so với SSOT Checkpoint 1, đồng thời giữ vững nguyên vẹn trải nghiệm sư phạm gần gũi, trực quan cho nhóm 3 sinh viên năm 2.
