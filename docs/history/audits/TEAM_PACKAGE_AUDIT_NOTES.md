# LABGUARD NT106 — TEAM PACKAGE V2 AUDIT NOTES
**Dành cho Independent Auditor: ChatGPT Work**  
**Thời điểm phát hành:** 2026-09-13  
**Bộ sản phẩm kiểm toán:**
1. `LabGuard_Team_Task_Tracker_v2.xlsx`
2. `LabGuard_Team_Execution_Guide_v2.docx`
3. `LabGuard_Team_Execution_Guide_v2.pdf` (138 trang)
4. `TEAM_PACKAGE_CHANGELOG_V2.md`
5. `TEAM_PACKAGE_AUDIT_NOTES.md`

---

## 1. TỔNG QUAN TUÂN THỦ NGUYÊN TẮC ĐÓNG BĂNG (FREEZE COMPLIANCE)

Bộ sản phẩm **Team Package V2** được xây dựng nhằm mục tiêu chuyển giao toàn diện kế hoạch tác chiến cho 3 sinh viên năm 2 thực hiện đồ án môn học NT106 (Lập trình mạng căn bản, UIT). 

Chúng tôi cam kết tuân thủ tuyệt đối các nguyên tắc sau:
1. **Bảo lưu 100% kết quả Checkpoint 1:** Không thay đổi bất kỳ thành phần kiến trúc cốt lõi, wire protocol, mô hình phân quyền (RBAC) hay phân chia WBS đã được audit và freeze ở Checkpoint 1 Revision 4.2.1.
2. **Không ghi đè tệp V1:** Giữ nguyên vẹn các tệp gốc `LabGuard_Team_Task_Tracker_v1.xlsx`, `LabGuard_Team_Execution_Guide_v1.docx`, `LabGuard_Team_Execution_Guide_v1.pdf`. Toàn bộ sản phẩm cải tiến được lưu sang định dạng `_v2`.
3. **Không tự ý code triển khai trước:** Giữ đúng trạng thái thiết kế, tập trung chuẩn bị công cụ điều phối, tài liệu hướng dẫn sư phạm và hồ sơ kiểm soát.
4. **Không đóng vai auditor cuối cùng:** Bàn giao gói hoàn chỉnh để ChatGPT thực hiện kiểm toán độc lập.

---

## 2. ĐỐI SOÁT HỢP ĐỒNG KỸ THUẬT & KHỐI LƯỢNG (SSOT RECONCILIATION)

| Thành phần kiểm toán | Tiêu chuẩn Checkpoint 1 (SSOT) | Hiện trạng trong Team Package V2 | Kết luận kiểm toán |
| :--- | :--- | :--- | :--- |
| **Tổng số nhiệm vụ WBS** | Chính xác 60 tasks (20 task/sinh viên A, B, C) | Đúng 60 tasks (`LG-A-001..020`, `LG-B-001..020`, `LG-C-001..020`) | **PASS (100% Khớp)** |
| **Tổng tải toàn hệ thống** | 211.5 giờ (bao gồm P0, P1, Buffer) | Đúng 211.5 giờ (SV A: 69.0h, SV B: 72.0h, SV C: 70.5h) | **PASS (100% Khớp)** |
| **Tải cam kết MVP** | 164.5 giờ (Cắt giảm 47.0h P1) | Đúng 164.5 giờ (SV A: 54.0h, SV B: 56.0h, SV C: 54.5h) | **PASS (100% Khớp)** |
| **Quỹ đệm rủi ro (Buffer)** | 27.0 giờ (9.0h / sinh viên) | Đúng 27.0 giờ tích hợp trong các task W5 | **PASS (100% Khớp)** |
| **Dung lượng dự trữ (Headroom)** | 15.5 giờ (180h trần - 164.5h cam kết) | Đúng 15.5 giờ, không tính trùng với Buffer | **PASS (100% Khớp)** |
| **Trần công suất sinh viên** | 12.0 giờ/người/tuần | Trung bình 10.9 giờ/người/tuần (dưới trần 12h) | **PASS (100% Khớp)** |
| **Mô hình phân quyền (RBAC)** | `Admin`, `Operator`, `Viewer` (Triệt tiêu 'Tech') | Duy nhất 3 vai trò: `Admin`, `Operator`, `Viewer` | **PASS (100% Khớp)** |
| **Danh mục Wire Protocol** | 69 bản tin (34 P0-MVP Req, 25 P0-Opt, 10 P1) | Chuẩn hóa danh mục 69 bản tin từ `03_PROTOCOL_SPEC.md` | **PASS (100% Khớp)** |
| **Hợp đồng SIGN_UP / LOGIN** | Pre-auth SIGN_UP cần inviteCode; Seed Admin | Chuẩn hóa trong tài liệu hướng dẫn và task LG-B-004, LG-C-003 | **PASS (100% Khớp)** |
| **Cơ chế khóa OCC sự cố** | Cột `rowversion` SQL Server + `expectedRowVersion` | Chi tiết hóa trong LG-B-009, LG-C-008 và bài kiểm tra LAN | **PASS (100% Khớp)** |
| **Khử trùng lặp nạp số liệu** | `ProcessedBatches (DeviceId, BootId, BatchId)` | Hiện thực trong LG-B-005 và quy trình gửi BATCH_ACK | **PASS (100% Khớp)** |

---

## 3. CÁC QUYẾT ĐỊNH THIẾT KẾ ĐẶC THÙ TRONG V2

### 3.1. Thiết kế bảng tính `LabGuard_Team_Task_Tracker_v2.xlsx`
1. **Sheet mở đầu bắt buộc:** Sheet `00_Hom_Nay_Lam_Gi` được đặt làm sheet đầu tiên và thiết lập `wb.active` khi mở tệp. Thiết kế này giúp sinh viên không bị choáng ngợp bởi 60 nhiệm vụ mà tập trung ngay vào: Nhiệm vụ active hôm nay, mục tiêu trước 23:00, blocker cần gỡ và 4 cảnh báo đỏ.
2. **Kiểm tra công thức bằng Excel COM thật:** Toàn bộ công thức (`COUNTA`, `COUNTIF`, `SUM`, `AVERAGE`, `IF`) đã được mở bằng Microsoft Excel 16.0 COM, thực thi `CalculateFull()` và quét toàn bộ các ô sử dụng. Kết quả xác nhận **0 lỗi** (`#REF!`, `#NAME?`, `#VALUE!`, `#DIV/0!`, `#N/A`).
3. **Bảng màu Soft Enterprise Navy:** Sử dụng bảng mã màu chuẩn doanh nghiệp (`#1F4E78`, `#2F5597`, `#D9E1F2`, `#E2EFDA`, `#FFF2CC`, `#FCE4D6`) với font Segoe UI đồng bộ, phân cấp trực quan rõ ràng.

### 3.2. Thiết kế sổ tay `LabGuard_Team_Execution_Guide_v2.docx` & `.pdf`
1. **Phương pháp tiếp cận Beginner-First:** Thay vì đưa ngay mã nguồn khô khan, mỗi nhiệm vụ và phần hướng dẫn đều mở đầu bằng câu hỏi trực giác *"Tại sao nhiệm vụ này tồn tại?"* (Why Exists) và giải thích bản chất phân tán (TCP Byte Stream, Partial Read, Thread Marshaling) bằng ví dụ đời sống.
2. **Quy tắc bản địa hóa Tiếng Việt:** Ưu tiên thuật ngữ Tiếng Việt chuẩn xác kèm thuật ngữ Tiếng Anh trong ngoặc đơn ở lần xuất hiện đầu tiên (ví dụ: *"Nhịp kiểm tra sống (heartbeat)"*, *"Khóa lạc quan (Optimistic Concurrency Control - OCC)"*). Giữ nguyên vẹn tên mã định danh kỹ thuật (`METRIC_BATCH`, `rowversion`, `Control.InvokeRequired`, `TcpClient`, `SslStream`).
3. **Bao phủ 100% nội dung:** Tài liệu gồm 14 phần đầy đủ (Phần 0 đến Phần 13 + Phụ lục), biên dịch ra 138 trang PDF sắc nét thông qua Microsoft Word COM.

---

## 4. GHI CHÚ KIỂM TOÁN DÀNH CHO CHATGPT AUDITOR

1. **Về 4 bản tin hoán đổi phạm vi trong REV4.2:** 
   Bộ sản phẩm V2 tuân thủ nghiêm ngặt theo erratum Checkpoint 1: 4 bản tin `SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END`, `INCIDENT_ASSIGN` thuộc phạm vi **P0-MVP REQUIRED** (vì cần thiết cho khởi tạo cache và phân công sự cố); trong khi 4 bản tin `GET_DEVICE`, `DEVICE_DETAIL`, `GET_INCIDENT`, `INCIDENT_DETAIL` thuộc phạm vi **P0 OPTIONAL**.
2. **Về cơ chế Scope Cut tại Cổng M4:**
   Quy tắc kích hoạt `DEFER P1` được văn bản hóa rõ ràng: Nếu trượt cổng M4 ở Tuần 3, nhóm hoãn ngay 9 tasks P1 (-47.0h), chuyển sang Hardening và chuẩn bị minh chứng. Toán học 12h/tuần được chứng minh tường minh với 15.5h dung lượng dự trữ độc lập, không cộng dồn sai lệch vào buffer.
3. **Về dữ liệu kiểm thử và vấn đáp:**
   - Đủ 60 test cases tương ứng 60 tasks trong `08_Test_Evidence`.
   - Đủ 90 câu hỏi vấn đáp (30 câu/người) trong `09_Viva_Tracker` và Phần 10 của Guide.
   - Đủ 14 rủi ro thực tế (RSK-01 đến RSK-14) trong `10_Risks_Blockers` và Phần 11 của Guide.
   - Đủ 44 tiêu chí checklist chia thành 11 nhóm (A đến K) trong `07_Checklists`.

---
*Báo cáo được lập bởi Assistant và sẵn sàng cho vòng kiểm toán độc lập của ChatGPT Work.*
