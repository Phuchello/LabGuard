# LABGUARD NT106 — NHẬT KÝ THAY ĐỔI & NÂNG CẤP TEAM PACKAGE V2 (CHANGELOG)
**Dự án:** LABGUARD — Hệ thống giám sát mạng & quản lý sự cố phòng máy tập trung  
**Môn học:** NT106 — Lập trình mạng căn bản (UIT, ĐHQG-HCM)  
**Thời điểm phát hành:** 2026-09-13  
**Phiên bản gốc đối chiếu:** Team Package V1  
**Phiên bản hiện tại:** Team Package V2 (Bản hoàn thiện sư phạm, bản địa hóa Tiếng Việt)  

---

## 1. TỔNG QUAN MỤC TIÊU NÂNG CẤP LÊN PHIÊN BẢN V2

Phiên bản V1 được lập ra như một bản phác thảo ban đầu nhưng bộc lộ nhiều hạn chế đối với sinh viên năm 2:
- Tiêu đề và nội dung còn pha trộn Tiếng Anh - Tiếng Việt thiếu nhất quán.
- Chưa có giao diện "tác chiến hàng ngày" (daily control center) khiến sinh viên mở file bị ngợp bởi 60 nhiệm vụ.
- Thiếu giải thích trực giác bản chất kỹ thuật (tại sao phải làm việc này, rủi ro nếu làm sai là gì).
- Bảng checklist và rủi ro còn sơ sài, chưa phản ánh đúng 11 nhóm kiểm soát chất lượng và 14 rủi ro thực tế.
- Chưa có hồ sơ 60 bài kiểm thử ánh xạ 1-1 với 60 nhiệm vụ WBS.

**Phiên bản V2 giải quyết triệt để các vấn đề trên** mà vẫn bảo lưu 100% tính toàn vẹn của Checkpoint 1:
- Giữ nguyên 60 nhiệm vụ WBS, 20 task/người, 211.5h tổng tải, 164.5h cam kết MVP, 15.5h dự trữ dung lượng.
- Chuẩn hóa Tiếng Việt toàn diện với phong cách sư phạm, dễ hiểu cho sinh viên năm 2.
- Xuất bản bộ đôi công cụ hoàn chỉnh: Bảng tính điều phối `LabGuard_Team_Task_Tracker_v2.xlsx` và Sổ tay thực thi 138 trang `LabGuard_Team_Execution_Guide_v2.docx` / `.pdf`.

---

## 2. CHI TIẾT NÂNG CẤP BẢNG TÍNH TASK TRACKER V2

### 2.1. Cấu trúc 14 Sheet trong `LabGuard_Team_Task_Tracker_v2.xlsx`

| STT | Tên Sheet | Màu Tab | Chức năng & Nâng cấp nổi bật so với V1 |
| :---: | :--- | :---: | :--- |
| **01** | `00_Hom_Nay_Lam_Gi` | `#0070C0` (Blue) | **MỚI HOÀN TOÀN:** Đặt làm tab mở đầu tiên. Chứa 3 thẻ tác chiến cá nhân (A, B, C) với nhiệm vụ active, mục tiêu trước 23:00, blocker và 4 khung cảnh báo đỏ toàn dự án. |
| **02** | `01_Dashboard` | `#1F4E78` (Navy) | Thiết kế lại theo chuẩn Soft Enterprise Navy: 6 thẻ KPI tổng quan, bảng phân bổ công việc theo 3 người, bảng tiến độ 5 tuần và 7 cổng Gate M1-M7. |
| **03** | `02_Master_Tasks` | `#2F5597` (Dark Blue) | Việt hóa toàn bộ 30 cột tiêu đề chuẩn WBS. Tích hợp Data Validation dropdown (Owner, Priority, Status), AutoFilter, Freeze Panes tại hàng 5. |
| **04** | `03_Person_A` | `#5B9BD5` (Blue) | Tóm tắt KPI cá nhân + 20 Thẻ nhiệm vụ chi tiết của SV A (18 trường thông tin/thẻ, bảng checklist DoD 11 mục có checkbox). |
| **05** | `04_Person_B` | `#41719C` (Blue-Gray) | Tóm tắt KPI cá nhân + 20 Thẻ nhiệm vụ chi tiết của SV B (Server & CSDL) với đầy đủ kịch bản kiểm thử, tình huống lỗi và câu hỏi viva. |
| **06** | `05_Person_C` | `#1F4E78` (Navy) | Tóm tắt KPI cá nhân + 20 Thẻ nhiệm vụ chi tiết của SV C (Dashboard WinForms & Collector) với bẫy lỗi luồng UI Invoke và OCC. |
| **07** | `06_Weekly_Plan` | `#70AD47` (Green) | Lộ trình 5 tuần chi tiết, 7 mốc Gate M1-M7, cơ chế cắt giảm phạm vi DEFER P1 (-47.0h) tại cổng M4 và bảng cân đối công suất 12h/tuần toán học. |
| **08** | `07_Checklists` | `#FFC000` (Gold) | **NÂNG CẤP LỚN:** Tái cấu trúc từ 7 nhóm cũ thành 11 nhóm chuẩn (A đến K) gồm 44 tiêu chí từ môi trường, Git, code, test LAN đến demo và sao lưu. |
| **09** | `08_Test_Evidence` | `#ED7D31` (Orange) | **MỚI HOÀN TOÀN:** 60 kịch bản kiểm thử ánh xạ 1-1 với 60 tasks: Phương pháp, tiêu chuẩn nghiệm thu, bẫy lỗi và đường dẫn tệp minh chứng. |
| **10** | `09_Viva_Tracker` | `#7030A0` (Purple) | 90 câu hỏi vấn đáp chuyên sâu (30 câu/người). Tích hợp thang điểm tự chấm 0-10, công thức xếp loại tự động và Định dạng có điều kiện 4 dải màu. |
| **11** | `10_Risks_Blockers` | `#C00000` (Red) | **NÂNG CẤP LỚN:** Mở rộng từ 12 lên 14 rủi ro kỹ thuật và vận hành thực tế (bổ sung RSK-13 High-DPI và RSK-14 Tràn nhật ký CSDL LDF). |
| **12** | `11_Daily_Standup` | `#00B050` (Green) | **MỚI HOÀN TOÀN:** Nhật ký họp nhanh 10 phút hàng ngày theo dõi tiến độ thực tế giữa 3 thành viên, phát hiện sớm blocker. |
| **13** | `12_How_To_Use` | `#808080` (Gray) | Cẩm nang hướng dẫn sử dụng chi tiết bằng Tiếng Việt: 4 bước vận hành hàng ngày, quy ước màu sắc, kỷ luật thu thập minh chứng. |
| **14** | `Lists` | `#A6A6A6` (Light Gray) | Bảng dữ liệu nguồn phục vụ tính năng Data Validation dropdown cho toàn bộ workbook. |

### 2.2. Kiểm định công thức và kỹ thuật trên Excel V2
- **Tự động hóa hoàn toàn qua openpyxl:** Không sử dụng macro VBA để đảm bảo tính an toàn và khả năng tương thích tối đa trên mọi hệ điều hành.
- **Kiểm định qua Microsoft Excel COM thật:**
  - Lệnh kiểm tra: `excel.CalculateFull()` trên tiến trình `Excel.Application` độc lập.
  - Quét toàn bộ `UsedRange` của 14 sheets.
  - Kết quả: **Chính xác 0 ô lỗi** (`#REF!`, `#NAME?`, `#VALUE!`, `#DIV/0!`, `#N/A`).
  - Xác nhận sheet mở mặc định: `00_Hom_Nay_Lam_Gi`.

---

## 3. CHI TIẾT NÂNG CẤP SỔ TAY THỰC THI (EXECUTION GUIDE V2)

### 3.1. So sánh quy mô tài liệu V1 vs V2

| Đặc điểm | Phiên bản V1 | Phiên bản V2 |
| :--- | :--- | :--- |
| **Số phần nội dung** | 9 phần khái quát | **14 phần chi tiết** (Phần 0 đến Phần 13 + Phụ lục) |
| **Độ dài tệp PDF** | ~35 trang tóm tắt | **138 trang hoàn chỉnh**, bố cục chuyên nghiệp |
| **Dung lượng tệp PDF** | ~1.2 MB | **5.74 MB** (chuyển đổi chuẩn xác qua Word 16.0 COM) |
| **Ngôn ngữ & Sư phạm** | Pha trộn Tiếng Anh, nặng về lý thuyết | **Tiếng Việt chuẩn mực**, giải thích trực giác trước khi code |
| **Độ chi tiết thẻ nhiệm vụ** | Bảng tóm tắt ngắn | **18 trường chi tiết / nhiệm vụ**, đủ 60 tasks |
| **Tiêu chuẩn hoàn thành** | Checklist chung chung | **Checklist DoD 11 mục** riêng cho từng nhiệm vụ |
| **Kịch bản Demo** | Ý tưởng tổng quát | **Kịch bản 6 bước bấm giờ chi tiết 12 phút** |

### 3.2. Cấu trúc 14 Phần trong Execution Guide V2
1. **Phần 0: Bản đồ định hướng cho sinh viên năm 2:** Vị trí môn NT106, sự khác biệt giữa hệ phân tán socket và ứng dụng web CRUD, phân vai A/B/C và 3 nguyên tắc vàng.
2. **Phần 1: Kiến trúc hệ thống nhìn qua luồng dữ liệu:** Luồng dữ liệu 7 bước từ Agent -> Socket -> FrameCodec -> BoundedChannel -> Ingest -> Rule -> UI. Bản chất TCP Byte Stream, Partial Read, Frame Coalescing và giải pháp Big-Endian Framing.
3. **Phần 2: Hợp đồng dữ liệu & Quy ước Wire Protocol:** Cấu trúc Outer Envelope 6 trường bất biến. Phân loại chuẩn 69 bản tin (34 P0-MVP Req, 25 P0-Opt, 10 P1). Cơ chế Snapshot-First + Incremental Events. Khóa lạc quan (OCC) với `expectedRowVersion`.
4. **Phần 3: Ma trận trách nhiệm & Quy trình cộng tác Git:** Ma trận RACI, quy trình Git Flow (main được bảo vệ, tạo nhánh theo Task ID, PR bắt buộc Reviewer duyệt, commit message chuẩn).
5. **Phần 4: Kế hoạch 5 tuần, các mốc kiểm soát (Gate M1-M7) & Kỷ luật cắt phạm vi:** 7 cổng chất lượng kỹ thuật. Cổng M4 sinh tử cuối Tuần 3 với quy tắc 3 điều kiện làm P1. Kịch bản kích hoạt `DEFER P1` (-47.0h), tái phân bổ tuần 4 sang Hardening và bảng cân đối 12h/tuần toán học.
6. **Phần 5: Sổ tay chi tiết 20 nhiệm vụ của Sinh viên A (Agent & Mạng):** Hướng dẫn chuyên sâu từ LG-A-001 đến LG-A-020 (Envelope, FrameCodec, Single Writer Channel, Hardware Telemetry, Heartbeat, Exponential Backoff Jitter, Wireshark verification).
7. **Phần 6: Sổ tay chi tiết 20 nhiệm vụ của Sinh viên B (Server & Cơ sở dữ liệu):** Hướng dẫn chuyên sâu từ LG-B-001 đến LG-B-020 (8 bảng SQL, Filtered Unique Index, Atomic Transaction, Bounded Ingress Channel, PBKDF2 HMAC-SHA256, FixedTimeEquals, Idempotent Ingestion, Rule Hysteresis, OCC rowversion, DB Restore Proof).
8. **Phần 7: Sổ tay chi tiết 20 nhiệm vụ của Sinh viên C (Dashboard & UI):** Hướng dẫn chuyên sâu từ LG-C-001 đến LG-C-020 (WinForms Shell Wireframe, High-DPI PerMonitorV2, SslStream RPC Client, Request/Response Correlation qua TaskCompletionSource, Snapshot Receiver, UI Thread Invoke Marshal, Lab View Grid, OCC action buttons, Collector Multiplexing & Reply Routing).
9. **Phần 8: Ma trận tích hợp & Kịch bản kiểm thử chéo:** Ma trận giao thoa 3 bên và 5 kịch bản tích hợp chéo end-to-end quan trọng nhất.
10. **Phần 9: Kịch bản kiểm thử mạng LAN thực tế & Hồ sơ minh chứng:** Bố trí 3 máy tính vật lý trong mạng LAN phòng lab, cấu hình IP/Firewall, bắt gói tin Wireshark chứng minh TLS, bẫy lỗi xung đột OCC và thu thập hồ sơ minh chứng.
11. **Phần 10: 90 câu hỏi vấn đáp (Viva) & Chiến lược trả lời điểm cao:** Công thức 3 bước vàng UIT ("Bản chất -> Quyết định thiết kế -> Dẫn chứng mã nguồn"). Tuyển tập trọn vẹn 90 câu hỏi (30 A, 30 B, 30 C) kèm câu trả lời chuẩn và vị trí code minh chứng.
12. **Phần 11: Sổ tay xử lý sự cố & 14 rủi ro thực tế:** Danh mục 14 rủi ro thực tế (RSK-01..14) kèm cảnh báo sớm, mitigation, fallback. Cẩm nang tra cứu & khắc phục 5 lỗi lập trình kinh điển (`Cross-thread operation`, `EndOfStreamException`, `ConcurrencyException`, `AuthenticationException`, `SqlException`).
13. **Phần 12: Kịch bản diễn tập Demo 12 phút trước hội đồng:** Kịch bản bấm giờ từng giây gồm 6 bước nghiệp vụ xuyên suốt. Phân vai thuyết minh giữa 3 sinh viên và kế hoạch dự phòng rủi ro sân khấu (video DemoBackup.mp4, router riêng).
14. **Phần 13: Hướng dẫn sử dụng file Task Tracker V2:** Quy trình 4 bước vận hành bảng tính hàng ngày cho sinh viên.
15. **Phụ lục: Bảng tra cứu nhanh mã lỗi, mẫu gói tin & lệnh thông dụng:** Danh mục mã lỗi hệ thống (`CONCURRENCY_CONFLICT`, `AUTH_FAILED`, `RATE_LIMIT_EXCEEDED`...), mẫu JSON và các lệnh dòng lệnh (.NET, SQL Server, Git).

---

## 4. BẢNG TỔNG KẾT DANH MỤC BÀN GIAO V2

| STT | Tên tệp tin | Định dạng | Trạng thái trên đĩa | Dung lượng |
| :---: | :--- | :---: | :---: | :---: |
| 1 | `LabGuard_Team_Task_Tracker_v2.xlsx` | Excel Workbook | **ĐÃ TẠO MỚI** (Không đè V1) | ~85 KB |
| 2 | `LabGuard_Team_Execution_Guide_v2.docx` | Word Document | **ĐÃ TẠO MỚI** (Không đè V1) | ~140 KB |
| 3 | `LabGuard_Team_Execution_Guide_v2.pdf` | Adobe PDF (138 trang) | **ĐÃ TẠO MỚI** (Word COM) | 5.74 MB |
| 4 | `TEAM_PACKAGE_AUDIT_NOTES.md` | Markdown Report | **ĐÃ TẠO MỚI** | ~6.3 KB |
| 5 | `TEAM_PACKAGE_CHANGELOG_V2.md` | Markdown Report | **ĐÃ TẠO MỚI** | ~10 KB |

---
*Nhật ký được lập và xác thực tự động bởi hệ thống Antigravity. Sẵn sàng cho quy trình kiểm toán độc lập.*
