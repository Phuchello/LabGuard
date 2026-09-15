# NHẬT KÝ THAY ĐỔI STUDENT MODE V3.1.1 (STUDENT_MODE_CHANGELOG_V3_1_1.md)

**Dự án:** LABGUARD — Lightweight Network Monitoring & Incident Management System  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Phiên bản:** V3.1.1 (Final Audit Hotfix Only)  
**Ngày phát hành:** 13/09/2026  
**Vai trò thực hiện:** Technical Editor, Excel/Spreadsheet UX Designer, Beginner-first NT106 Mentor, Documentation Engineer  
**Đối tượng bàn giao:** Đội ngũ phát triển nhóm sinh viên & Đơn vị Kiểm toán độc lập (Independent Auditor)

---

## 1. TỔNG QUAN VÒNG CHỈNH SỬA V3.1.1 (EXECUTIVE SUMMARY)

Phiên bản **Student Mode V3.1** đã hoàn thành xuất sắc mục tiêu sư phạm và khắc phục toàn diện kiến trúc cơ bản. Đợt kiểm toán độc lập cuối cùng (Independent Audit) phát hiện một số điểm sai lệch cục bộ cần vá lỗi nóng (Hotfix):
- Còn sót thông số log xoay vòng cũ `5 MB / 3 files` ở một số vị trí mô tả chi tiết.
- Cột mô tả kỹ thuật `LG-B-001` trong Master WBS (Sheet 99) chưa đồng bộ với hướng dẫn hoãn tạo schema CSDL 8 bảng sang Checkpoint 2.
- Giá trị timeout `ACK_TIMEOUT` và các ngưỡng `HealthProbe` cần được quy về biến danh nghĩa kèm nhãn `[CP2 TO VERIFY]`.
- Vòng đời sự cố cần chốt nhất quán 5 trạng thái chuẩn: `OPEN -> ACKNOWLEDGED -> IN_PROGRESS -> RECOVERED -> RESOLVED`.
- Cần bổ sung Data Validation Dropdown trạng thái cho 60/60 thẻ cá nhân và liên kết công thức tự động sang Sheet 99.
- Ô chọn tuần `D5` trên Sheet 00 cần công thức `=VLOOKUP` động thay đổi nội dung mục tiêu và nhiệm vụ trọng tâm theo tuần được chọn.

**Cam kết phạm vi:**
- Tuyệt đối **KHÔNG** tái thiết kế kiến trúc, không tạo phiên bản V4.
- Giữ nguyên 100% danh mục 60 Task ID, phân công Owner (A: 20, B: 20, C: 20) và dự toán ngân sách giờ công (Tổng: 211.5h, Committed: 164.5h, Contingency P1: 47h).
- Không code ứng dụng LabGuard, không bắt đầu Checkpoint 2 trước khi kết thúc đợt kiểm toán.

---

## 2. CHI TIẾT 9 NỘI DUNG SỬA LỖI (AUDIT FINDINGS RESOLUTION)

### 2.1. Blocker 1 — Đồng bộ Triệt để Chính sách Log Xoay Vòng (Rolling Log Policy)
- **Hiện trạng trước sửa:** Một số vị trí trong mô tả 4 bước và prompt AI của `LG-A-011` vẫn còn sót dòng chữ `"5 MB mỗi file / 3 files"`.
- **Khắc phục tại V3.1.1:**
  - Đồng bộ toàn bộ tài liệu Word, PDF, Excel (Sheet 01, Sheet 99, Sheet Test, Sheet Viva, AI Prompt) sang chính sách chuẩn duy nhất:
    - `MaxFileSize = 10 MB [CP2 TO VERIFY]`
    - `MaxFiles = 5 [CP2 TO VERIFY]`
    - Tổng dung lượng thư mục log mục tiêu `<= 50 MB [CP2 TO VERIFY]`
  - Kết quả kiểm tra chuỗi tự động:
    - Tìm kiếm chính xác `"5 MB"` trong ngữ cảnh rolling log: **0 kết quả**.
    - Tìm kiếm chính xác `"3 file"` trong ngữ cảnh rolling log: **0 kết quả**.

### 2.2. Blocker 2 — Xử lý Xung đột Schema CSDL tại LG-B-001 / Master WBS
- **Hiện trạng trước sửa:** Sổ tay hướng dẫn V3.1 đã ghi đúng việc hoãn tạo bảng sang Checkpoint 2, nhưng cột Mô tả chi tiết của `LG-B-001` trong Master WBS (Sheet 99) vẫn còn câu: *"Tạo cấu trúc 8 bảng CSDL và tài khoản Admin mẫu..."*.
- **Khắc phục tại V3.1.1:**
  - Cập nhật ô mô tả `LG-B-001` trong Sheet `99_Chi_Tiet_Ky_Thuat` khớp 100% với Sổ tay hướng dẫn:
    *"Trước CP2: cài SQL Server, kiểm tra connection từ C#, tạo LabGuard_Dev rỗng, học PK/FK/rowversion, chuẩn bị migration skeleton. Schema chính thức chốt tại Checkpoint 2."*
  - Khẳng định rõ: Không định nghĩa danh sách bảng cụ thể trước khi Checkpoint 2 chính thức đóng băng cấu trúc dữ liệu.

### 2.3. Item 3 — Thống nhất Biến danh nghĩa ACK_TIMEOUT tại LG-A-006
- **Hiện trạng trước sửa:** Các tài liệu còn dùng xen kẽ giữa giá trị cố định 5s và các mô tả thời gian chờ không nhất quán.
- **Khắc phục tại V3.1.1:**
  - Quy ước đồng nhất sử dụng tên tham số danh nghĩa: `ACK_TIMEOUT [CP2 TO VERIFY] (giá trị đề xuất 5s)`.
  - Giúp sinh viên nắm rõ 5s là giá trị cấu hình ban đầu đề xuất, việc tinh chỉnh thích ứng mạng sẽ được thực nghiệm tại Checkpoint 2.

### 2.4. Item 4 — Chuẩn hóa Ngưỡng Health Probe và Recovery tại LG-A-014
- **Hiện trạng trước sửa:** Còn lẫn lộn giữa chu kỳ lấy mẫu giám sát và ngưỡng phát hiện lỗi / phục hồi của máy trạm.
- **Khắc phục tại V3.1.1:**
  - Chuẩn hóa sang 3 tham số kỹ thuật danh nghĩa kèm nhãn thực nghiệm:
    - `HealthProbeInterval [CP2 TO VERIFY]` (chu kỳ thăm dò: 5s).
    - `HealthFailThreshold [CP2 TO VERIFY]` (ngưỡng đánh giá mất liên lạc: 3 chu kỳ liên tiếp không có phản hồi hợp lệ).
    - `HealthRecoveryThreshold [CP2 TO VERIFY]` (ngưỡng công nhận hồi phục: 2 chu kỳ liên tiếp bình thường).

### 2.5. Item 5 — Gắn Nhãn [CP2 TO VERIFY] cho Toàn bộ Ngưỡng Chưa Thực Nghiệm
- **Khắc phục tại V3.1.1:**
  - Rà soát toàn bộ thẻ nhiệm vụ, bảng theo dõi Excel và tài liệu hướng dẫn.
  - Gắn nhãn `[CP2 TO VERIFY]` tại tất cả các ngưỡng giả định chờ đo đạc tải thực tế tại phòng lab:
    - Kích thước hàng đợi `BoundedChannel(1000 items [CP2 TO VERIFY])`.
    - Thời gian liveness `20s [CP2 TO VERIFY]`.
    - Hysteresis chống chớp nháy `3 chu kỳ lấy mẫu [CP2 TO VERIFY]`.
    - Dung lượng log xoay vòng `10 MB / 5 files / <= 50 MB [CP2 TO VERIFY]`.
  - Tổng số nhãn xuất hiện: **42 lần** trong Sổ tay PDF và **63 lần** trong Sổ theo dõi Excel.

### 2.6. Item 6 — Khóa Nhất quán Vòng đời Sự cố 5 Trạng thái (Canonical Incident Lifecycle)
- **Khắc phục tại V3.1.1:**
  - Khóa chặt quy trình xử lý sự cố chuẩn gồm 5 trạng thái tuần tự:
    $$\text{OPEN} \longrightarrow \text{ACKNOWLEDGED} \longrightarrow \text{IN\_PROGRESS} \longrightarrow \text{RECOVERED} \longrightarrow \text{RESOLVED}$$
  - Làm rõ tính chất của `RECOVERED`: Đây là trạng thái ghi nhận thiết bị đã hồi phục kỹ thuật (máy trạm hạ tải hoặc có nhịp tim trở lại), đóng vai trò điều kiện cần tiên quyết để Admin/Operator bấm nút `RESOLVE` đóng sự cố chính thức.

### 2.7. Item 7 — Bổ sung Dropdown Trạng thái trên Thẻ Cá nhân & Đồng bộ 2 Chiều
- **Khắc phục tại V3.1.1:**
  - Tạo vùng tham chiếu nguồn tại Sheet `Lists` (`A2:A5` chứa 4 trạng thái: `Chưa làm`, `Đang làm`, `Kẹt`, `Xong`).
  - Thiết lập Data Validation Dropdown tại ô trạng thái của **toàn bộ 60/60 thẻ nhiệm vụ cá nhân** trong các sheet `01_Viec_Cua_A`, `02_Viec_Cua_B`, `03_Viec_Cua_C`.
  - Thiết lập công thức Excel liên kết tại Cột 30 (Trạng thái) của Sheet `99_Chi_Tiet_Ky_Thuat` trỏ về ô trạng thái tương ứng trên thẻ cá nhân (ví dụ `='01_Viec_Cua_A'!D{row}`). Khi thành viên cập nhật trên thẻ của mình, bảng Master WBS tự động cập nhật ngay lập tức.

### 2.8. Item 8 — Tương tác Động Chọn Tuần trên Sheet 00 (Dynamic Week Selector)
- **Khắc phục tại V3.1.1:**
  - Thiết lập Data Validation Dropdown tại ô `D5` của Sheet `00_Bat_Dau_O_Day` chứa danh sách các tuần: `W1`, `W2`, `W3`, `W4`, `W5`.
  - Thiết lập công thức tra cứu động Excel gốc tại các hàng 6 đến 9:
    - Ô `D6` (Mục tiêu tuần): `=VLOOKUP(D5, Lists!$D$2:$H$6, 2, FALSE)`
    - Ô `D7` (Trọng tâm việc A): `=VLOOKUP(D5, Lists!$D$2:$H$6, 3, FALSE)`
    - Ô `D8` (Trọng tâm việc B): `=VLOOKUP(D5, Lists!$D$2:$H$6, 4, FALSE)`
    - Ô `D9` (Trọng tâm việc C): `=VLOOKUP(D5, Lists!$D$2:$H$6, 5, FALSE)`
  - Khi sinh viên chọn tuần mới (ví dụ từ `W1` sang `W2` hoặc `W3`), toàn bộ khối thông tin mục tiêu và phân công trọng tâm thay đổi tức thì, không bị hardcode.

### 2.9. Item 9 — Rà soát Giọng văn Sư phạm & Loại bỏ Triệt để Bản tin Legacy
- **Khắc phục tại V3.1.1:**
  - Điều chỉnh giọng văn: Loại bỏ cảm giác hoảng loạn, giữ vững phong thái định hướng sư phạm, khích lệ sinh viên năm 2 kiên trì thực hiện từng bước.
  - Quét sạch toàn bộ các tên bản tin giao thức cũ (`COLLECTOR_REGISTER`, `COLLECTOR_FORWARD`, `QUERY_INCIDENT_HISTORY`, `QUERY_METRICS_HISTORY`, `GET_DEVICES`, `INCIDENT_UPDATE`, `INCIDENT_RESOLVE_CONFIRM`, `CONFIG_SET`, `AGENT_COMMAND`). Số lượng phát hiện: **0**.

---

## 3. BẢNG TỔNG HỢP KIỂM CHỨNG TỰ ĐỘNG (AUTOMATED VERIFICATION METRICS)

| Tiêu chí Kiểm tra | Chỉ số Yêu cầu | Kết quả Đo đạc V3.1.1 | Trạng thái |
| :--- | :---: | :---: | :---: |
| Số nhiệm vụ WBS & Phân bổ | 60 tasks (A:20, B:20, C:20) | 60 tasks (A:20, B:20, C:20) | **PASS** |
| Tổng ngân sách giờ công | 211.5h (164.5h committed + 47h P1) | 211.5h (164.5h committed + 47h P1) | **PASS** |
| Bản tin giao thức cũ (Legacy Disallowed) | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Chuỗi cũ "5 MB" trong context rolling log | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Chuỗi cũ "3 file" trong context rolling log | 0 từ | **0 từ** (PDF: 0, XLSX: 0) | **PASS** |
| Chính sách Log 10 MB / 5 files / <= 50 MB | Đồng bộ toàn bộ | 100% đồng bộ | **PASS** |
| Hoãn schema 8 bảng CSDL sang CP2 | Đồng bộ Sheet 99 & Sổ tay | 100% đồng bộ | **PASS** |
| Nhãn thực nghiệm `[CP2 TO VERIFY]` | Hiện diện tại các ngưỡng đo | PDF: 42 lần \| XLSX: 63 lần | **PASS** |
| Vòng đời sự cố (Incident 5-state lifecycle) | OPEN->ACK->IN_PROG->RECOV->RESOLV | 100% đồng bộ | **PASS** |
| Dropdown trạng thái trên thẻ cá nhân | 60/60 thẻ | 60/60 thẻ (100%) | **PASS** |
| Liên kết trạng thái Sheet 99 về thẻ cá nhân | Công thức động | 60 công thức liên kết | **PASS** |
| Ô chọn tuần Sheet 00 & VLOOKUP động | D5 Dropdown + D6:D9 VLOOKUP | Đã kiểm chứng trên Excel COM | **PASS** |
| Lỗi công thức Excel (`#REF!`, `#NAME?`, `#N/A`) | 0 lỗi | **0 lỗi** | **PASS** |
| Số trang Sổ tay Hướng dẫn PDF | >= 100 trang | **116 trang** (5.38 MB) | **PASS** |

---

## 4. KẾT LUẬN & CHUẨN BỊ BÀN GIAO

Toàn bộ các tệp bàn giao của gói **Student Mode V3.1.1** bao gồm:
1. `LabGuard_Student_Task_Tracker_v3_1_1.xlsx` (154,187 bytes, 10 sheets)
2. `LabGuard_Huong_Dan_Cho_Nhom_v3_1_1.docx` (123,719 bytes)
3. `LabGuard_Huong_Dan_Cho_Nhom_v3_1_1.pdf` (5,640,000 bytes, 116 trang)
4. `STUDENT_MODE_CHANGELOG_V3_1_1.md`
5. `STUDENT_MODE_FINAL_AUDIT_QA.md`

Các tệp phiên bản trước (V1, V2, V3, V3.1) được giữ nguyên vẹn để phục vụ đối chiếu lịch sử. Hệ thống tài liệu đã sẵn sàng cho đợt kiểm toán độc lập cuối cùng.
