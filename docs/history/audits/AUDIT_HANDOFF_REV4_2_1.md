# AUDIT HANDOFF — CHECKPOINT 1 (REVISION 4.2.1 FINAL FREEZE)
**Vòng bàn giao:** Checkpoint 1 Revision 4.2.1 — Final Freeze Hotfix  
**Dự án:** LABGUARD — Lightweight Network Monitoring & Incident Management System for Computer Labs  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Nhóm thực hiện:** 03 sinh viên (Kế hoạch 5 tuần, phân công 3 làn độc lập theo ADR-007)  
**Người chuẩn bị bàn giao:** Antigravity (Lead Technical Researcher & Network Software Architect)  
**Người nhận audit:** ChatGPT Work (Auditor độc lập)  
**Thời điểm:** 13/09/2026 20:15:00+07:00  
**Trạng thái bàn giao chính thức:** **`CHECKPOINT 1 PASS — FROZEN`**  

---

## I. TỔNG QUAN XỬ LÝ 6 FINDING CỦA FINAL FREEZE HOTFIX (REV4.2.1)

Toàn bộ 6 finding từ đợt kiểm toán độc lập cuối cùng của ChatGPT Work đã được khắc phục hoàn toàn, đối soát chính xác và tự động hóa kiểm tra:

| STT | Finding | Mức độ | Trạng thái | Giải pháp & Vị trí đối soát |
|:---:|:---|:---:|:---:|:---|
| **1** | **Blocker: P0 Message Scope SSOT** | BLOCKER | **ĐÃ GIẢI QUYẾT** | Đồng bộ tuyệt đối danh mục phân định phạm vi với `03_PROTOCOL_SPEC.md`: `P0-MVP REQUIRED` gồm đúng **34 message** (bao gồm `SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END`, `INCIDENT_ASSIGN`); `P0 OPTIONAL` gồm đúng **25 message** (bao gồm `GET_DEVICE`, `DEVICE_DETAIL`, `GET_INCIDENT`, `INCIDENT_DETAIL`); `P1 CONDITIONAL` gồm đúng **10 message**. Sửa lỗi hoán đổi 4+4 message tại bảng Scope. Xây dựng công cụ kiểm chứng tự động `scratch/verify_protocol_scope.py` khóa cứng kiểm tra tập hợp rời nhau (`exact set match: 34/34, 25/25, 10/10; missing/extra/misclassified = empty`). |
| **2** | **Đồng bộ Mốc thời gian & Runtime .NET** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Xóa bỏ toàn bộ phát biểu thì tương lai ("khi phát hành", "when released"). Xác lập mốc thời gian thực tế tháng 9/2026: .NET 10 LTS đã được Microsoft phát hành chính thức (11/11/2025, hỗ trợ đến 14/11/2028); .NET 9 STS đang trong giai đoạn bảo trì (kết thúc hỗ trợ 10/11/2026). Ghi rõ: `.NET 9.0 SDK` (đã cài đặt SDK 9.0.318) là baseline của dự án hiện được kiểm thử trên môi trường phát triển của nhóm; tính tương thích phòng máy UIT cần được xác thực trước khi triển khai thực tế, KHÔNG phải vì .NET 10 chưa ra mắt. Khuyến nghị nâng cấp lên `.NET 10 LTS` sau khi đối soát môi trường đích. Đồng bộ tại `5-KeHoachThucHien.tex`, `AUDIT_HANDOFF_REV4_2.md`, `PROJECT_STATE.md`, `scratch/test_frame_codec/Program.cs`, `test_frame_codec_run.log`. |
| **3** | **Tuyên bố chuẩn mực PBKDF2 theo OWASP** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Cập nhật nguyên văn tuyên bố chuẩn mực: *"OWASP recommends Argon2id for new systems generally. If FIPS-140 compliance is required, OWASP recommends PBKDF2-HMAC-SHA256 with a work factor of 600,000 or more. LabGuard deliberately chooses PBKDF2 for BCL availability and course-scope simplicity; 600,000 is therefore the project baseline referenced from OWASP and must still be measured on the actual lab hardware."* Đồng bộ chính xác trên cả 6 tệp tin: `sections/3-PhuongPhapThucHien.tex`, `handbooks/HANDBOOK_B.md`, `AUDIT_HANDOFF.md`, `CONTRACT_MATRIX.md`, `03_PROTOCOL_SPEC.md`, `research/RESEARCH_LEDGER.md`. |
| **4** | **Đặt Banner Hồ sơ Lịch sử (Superseded Banner)** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Thêm banner cảnh báo lên dòng đầu tiên của các hồ sơ bàn giao cũ: `> **SUPERSEDED / HISTORICAL ARTIFACT**` và `> Current freeze candidate: AUDIT_HANDOFF_REV4_2_1.md`. Đã áp dụng trên: `AUDIT_HANDOFF.md`, `AUDIT_RESPONSE_CHECKPOINT_1.md`, `AUDIT_HANDOFF_REV4_2.md`. |
| **5** | **Tài liệu hóa Quy trình Biên dịch & Lưu ý Trình biên dịch** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Cập nhật `README.md` ghi nhận chi tiết chuỗi công cụ chính thức: MiKTeX 25.12, pdfTeX 3.141592653-2.6-1.40.26, Biber 2.21, .NET SDK 9.0.318. Trình tự lệnh chuẩn: `pdflatex` -> `biber` -> `pdflatex`. Ghi chú rõ ràng: Ràng buộc 30 trang A4 được kiểm chứng trên chuỗi công cụ này và không khẳng định số trang là hoàn toàn bất biến trên các engine TeX khác. |
| **6** | **Nghiệm thu Đóng băng & Xuất gói Bàn giao Cuối** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Biên dịch `main.pdf` đạt đúng **30 trang A4**, **0 Warnings**, **0 Lỗi**. Chạy thành công toàn bộ 4 script kiểm toán tự động (WBS, Protocol Catalog, Protocol Scope, Frame Codec). Tạo gói bàn giao `LABGUARD_CHECKPOINT_1_REV4_2_1_HANDOFF.zip`, cập nhật `MANIFEST_SHA256.txt`, kiểm chứng giải nén và đối soát hash SHA-256 độc lập. |

---

## II. MA TRẬN PHÂN QUYỀN DUY NHẤT (CANONICAL RBAC MATRIX)

Toàn bộ hệ thống chỉ sử dụng 3 vai trò người dùng chuẩn hóa duy nhất (`Admin` / `Operator` / `Viewer`), không còn vai trò `Tech`:

| Nhóm chức năng | Thao tác / Wire Message | Viewer | Operator | Admin | Ghi chú kiểm soát quyền |
|:---|:---|:---:|:---:|:---:|:---|
| **Xác thực danh tính** | `LOGIN` | Pre-auth | Pre-auth | Pre-auth | Xác thực username/password trước khi cấp session token |
| **Đăng ký tài khoản** | `SIGN_UP` | Pre-auth | Pre-auth | Pre-auth | Endpoint tiền xác thực; bắt buộc `inviteCode` hợp lệ, kiểm tra atomic & consume 1 lần |
| **Tra cứu & Giám sát** | `LIST_DEVICES`, `GET_DEVICE`, `LIST_INCIDENTS`, `GET_INCIDENT`, `GET_HISTORY`, `GET_REPORT`, `GET_SERVER_HEALTH` | Cho phép | Cho phép | Cho phép | Truy vấn Read-Only, không làm thay đổi trạng thái |
| **Đăng ký Realtime** | `SUBSCRIBE_DEVICES`, `SUBSCRIBE_INCIDENTS`, `SUBSCRIBE_HEALTH`, `UNSUBSCRIBE` | Cho phép | Cho phép | Cho phép | Nhận luồng Snapshot & Delta Events qua socket |
| **Xử lý sự cố (Ca trực)** | `INCIDENT_ACK`, `INCIDENT_START`, `INCIDENT_RESOLVE` | Từ chối (`403`) | Cho phép | Cho phép | Tác nghiệp nghiệp vụ trực tiếp trên Incident |
| **Điều phối sự cố** | `INCIDENT_ASSIGN` | Từ chối (`403`) | Từ chối (`403`) | Cho phép | Phân công nhân sự xử lý phiếu sự cố |
| **Quản trị hệ thống** | `CREATE_INVITE`, `CONFIG_UPDATE` | Từ chối (`403`) | Từ chối (`403`) | Cho phép | Quản trị tạo mã mời (Admin-only) và cấu hình hệ thống (Admin-only) |

*Nguyên tắc bất biến:* Mọi thao tác Mutation đều bắt buộc kiểm tra quyền tại Central Server (Server-side Authorization); việc ẩn/vô hiệu hóa nút bấm trên Admin WinForms Client chỉ mang ý nghĩa UX.

---

## III. BẢNG MINH BẠCH BẰNG CHỨNG (EVIDENCE-TO-CLAIM DISCLOSURE)

1. **Khẳng định ĐÃ CÓ BẰNG CHỨNG KIỂM CHỨNG (`[VERIFIED EVIDENCE]`):**
   - **TCP FrameCodec:** Kiểm chứng 5/5 test case qua `scratch/test_frame_codec/` trên .NET SDK 9.0.318 (Partial Header, Partial Payload, Concatenated frames, Oversized reject >256 KiB, EOF handling).
   - **Wire Protocol Catalog SSOT:** Script `verify_protocol_catalog.py` kiểm chứng 69/69 message types trùng khớp tuyệt đối (0 missing, 0 extra).
   - **Wire Protocol Scope Partition:** Script `verify_protocol_scope.py` kiểm chứng phân hoạch 3 tập hợp rời nhau: P0 REQUIRED (34), P0 OPTIONAL (25), P1 CONDITIONAL (10) đạt exact set match 100%.
   - **Kế hoạch WBS 60 Tasks:** Script `verify_wbs.py` kiểm chứng 7/7 tiêu chí: 30 cột, 20 task/người, 211.5h workload, 12h/tuần đạt 164.5h với 15.5h headroom không cộng dồn.
   - **Đề cương LaTeX (`main.pdf`):** Đạt đúng 30 trang A4, 0 Errors, 0 Warnings, 0 Overfull > 5pt (chỉ có 2 overfull nhỏ < 5pt: 2.28pt và 3.51pt).
2. **Khẳng định LÀ DESIGN TARGET / DECISION CHƯA ĐO ĐẠC TRÊN PHÒNG MÁY THẬT (`[DESIGN DECISION / TARGET]`):**
   - Ngưỡng kích hoạt `HIGH_CPU` (>= 90% trong 3 mẫu liên tiếp, phục hồi <= 75% trong 3 mẫu liên tiếp).
   - Ngưỡng kích hoạt `SERVICE_DOWN` (fail 2 lần liên tiếp, phục hồi pass 2 lần liên tiếp).
   - Heartbeat deadline 20s (4 mẫu nhịp 5s); failover p95 <= 45s ở P1; PBKDF2 600.000 vòng trên CPU lab UIT (dự kiến 100--250ms).
   - Retention policy: Telemetry 48h, Dedup Batches TTL 24h, Dedup Commands TTL 7d.
3. **Khẳng định PHỤ THUỘC GIẢ ĐỊNH CHƯA XÁC MINH (`[UNVERIFIED ASSUMPTIONS]`):**
   - Barem điểm chính thức của lớp hiện tại chưa công bố (`[CURRENT-CLASS GRADING REQUIREMENTS UNVERIFIED]`).
   - Demo Internet trực tiếp phụ thuộc CGNAT phòng máy; chuẩn bị phương án demo LAN trên 2--3 máy thật.

---

## IV. PHÂN ĐỊNH PHẠM VI 69 WIRE MESSAGES (P0-MVP VS OPTIONAL VS P1)

| Nhóm phạm vi | Số lượng | Danh sách Message Types | Ý nghĩa cam kết |
|:---|:---:|:---|:---|
| **P0-MVP REQUIRED** | **34** | `HELLO_AGENT`, `HELLO_ADMIN`, `HELLO_ACK`, `AUTH_AGENT`, `AUTH_RESULT`, `HEARTBEAT`, `HEARTBEAT_ACK`, `METRIC_BATCH`, `BATCH_ACK`, `SERVICE_STATUS`, `AGENT_GOODBYE`, `GOODBYE_ACK`, `LOGIN`, `LOGIN_RESULT`, `LOGOUT`, `LOGOUT_RESULT`, `LIST_DEVICES`, `DEVICE_PAGE`, `LIST_INCIDENTS`, `INCIDENT_PAGE`, `SUBSCRIBE_DEVICES`, `SUBSCRIBE_INCIDENTS`, `SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END`, `DEVICE_UPDATE`, `INCIDENT_OPEN`, `INCIDENT_UPDATE`, `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`, `COMMAND_RESULT`, `ERROR` | **Bắt buộc hoàn thành 100%** tại mốc cuối Tuần 3 (M4 Review Gate) để được phép nghiệm thu đồ án. Xác thực MVP vận hành với tài khoản Admin khởi tạo sẵn trong CSDL (pre-seeded/bootstrap Admin) -> LOGIN -> vận hành hệ thống bình thường. |
| **P0 OPTIONAL** | **25** | `CONFIG_UPDATE`, `CONFIG_RESULT`, `SERVICE_CHECK_REQUEST`, `PING`, `PONG`, `SIGN_UP`, `SIGN_UP_RESULT`, `CREATE_INVITE`, `INVITE_RESULT`, `GET_DEVICE`, `DEVICE_DETAIL`, `GET_METRICS`, `METRICS_RESULT`, `GET_INCIDENT`, `INCIDENT_DETAIL`, `GET_HISTORY`, `HISTORY_PAGE`, `GET_SERVER_HEALTH`, `SERVER_HEALTH`, `GET_REPORT`, `REPORT_RESULT`, `SUBSCRIBE_HEALTH`, `HEALTH_UPDATE`, `UNSUBSCRIBE`, `UNSUBSCRIBE_RESULT` | Tính năng mở rộng nâng cao; có thể giảm phạm vi nếu thiếu ngân sách thời gian mà không ảnh hưởng cốt lõi. Gồm quy trình mở rộng tùy chọn: Admin CREATE_INVITE -> người dùng chưa xác thực SIGN_UP(inviteCode) -> LOGIN. |
| **P1 CONDITIONAL** | **10** | `HELLO_COLLECTOR`, `AUTH_COLLECTOR`, `FORWARD_OPEN`, `FORWARD_DATA`, `FORWARD_RESULT`, `FORWARD_CLOSE`, `CLOSE_SESSION`, `COLLECTOR_HEARTBEAT`, `HEALTH_PROBE`, `HEALTH_RESULT` | Chỉ kích hoạt triển khai tại Tuần 4 nếu toàn bộ 34 message P0-MVP đạt chuẩn nghiệm thu không lỗi Sev1/Sev2. |

---

## V. KẾT QUẢ KIỂM TRA ĐỀ CƯƠNG ACADEMIC LATEX (`main.pdf`)

- **Số trang A4:** Đúng **30 trang** (trang 1 đến trang 30, không tràn trang 31).
- **Lỗi biên dịch:** 0 Errors (`exit code 0`).
- **Cảnh báo (Warnings):** 0 Warnings.
- **Tham chiếu không xác định (Undefined references):** 0.
- **Kiểm toán Overfull \hbox:**
  - Overfull > 50pt: **0**.
  - Overfull > 5pt: **0**.
  - Overfull nhỏ < 5pt: **Đúng 2 điểm** (không ảnh hưởng thẩm mỹ thị giác):
    1. Dòng 92 `template/nt106.cls`: 2.28 pt tại khối thông tin bìa đề cương.
    2. Dòng 260 `sections/5-KeHoachThucHien.tex`: 3.51 pt tại đoạn văn bản checklist xuất xưởng.
- **Tài liệu tham khảo:** 16 nguồn tài liệu chuẩn mực trong `bibliography.bib` với liên kết trích dẫn `\cite{...}` đầy đủ.

---

## VI. BẢNG TỔNG HỢP KIỂM CHỨNG TỰ ĐỘNG (AUTOMATED TEST SUITE)

| Tên công cụ kiểm chứng | Tập tin thực thi | Môi trường | Mã thoát (Exit Code) | Kết quả kiểm toán |
|:---|:---|:---:|:---:|:---|
| **Master WBS Verifier** | `python scratch/verify_wbs.py` | Python 3.12 | `0` | **PASS (7/7 checks)**: 30 cột, 60 tasks, 20 task/người, khớp workload 211.5h, 12h/tuần đạt 164.5h |
| **Protocol Catalog Verifier** | `python scratch/verify_protocol_catalog.py` | Python 3.12 | `0` | **PASS**: 69/69 message types khớp tuyệt đối giữa spec và contract |
| **Protocol Scope Verifier** | `python scratch/verify_protocol_scope.py` | Python 3.12 | `0` | **PASS**: 34 P0 REQUIRED, 25 P0 OPTIONAL, 10 P1 CONDITIONAL, 0 missing/extra/misclassified |
| **Frame Codec Test Harness** | `dotnet run --project scratch/test_frame_codec/test_frame_codec.csproj` | .NET SDK 9.0.318 (Win x64) | `0` | **PASS (5/5 tests)**: Partial Read, Coalescing, Oversized reject >256KiB, Clean EOF |
| **LaTeX PDF Engine** | `pdflatex -interaction=nonstopmode main.tex` | MiKTeX 25.12 | `0` | **PASS**: Đúng 30 trang A4, 0 warnings, 0 undefined references, 0 overfull > 5pt |
| **Biber Bibliography Engine** | `biber main` | Biber 2.21 | `0` | **PASS**: Xử lý hoàn hảo 16/16 trích dẫn chuyên môn chuẩn mực |

---

## VII. HỒ SƠ BÀN GIAO THỰC TẾ & KIỂM CHỨNG TOÀN VẸN (CHECKSUMS)

Tệp nén bàn giao chính thức: **`LABGUARD_CHECKPOINT_1_REV4_2_1_HANDOFF.zip`**  
Gồm đúng **43 tệp tin** mã nguồn, tài liệu, test harness, script kiểm chứng và manifest SHA-256:

```
LABGUARD_CHECKPOINT_1_REV4_2_1_HANDOFF.zip
├── 03_PROTOCOL_SPEC.md
├── AUDIT_HANDOFF.md
├── AUDIT_HANDOFF_REV4_2.md
├── AUDIT_HANDOFF_REV4_2_1.md
├── AUDIT_RESPONSE_CHECKPOINT_1.md
├── CONTRACT_MATRIX.md
├── DECISIONS.md
├── MANIFEST_SHA256.txt
├── PARALLEL_WORK_PLAN.md
├── PROJECT_STATE.md
├── README.md
├── TASK_MIGRATION_MAP.md
├── TODO.md
├── WBS_TASKS.csv
├── bibliography.bib
├── main.pdf
├── main.tex
├── appendices/
│   ├── A-Glossary.tex
│   ├── B-TaskMap.tex
│   └── C-Viva.tex
├── handbooks/
│   ├── HANDBOOK_A.md
│   ├── HANDBOOK_B.md
│   └── HANDBOOK_C.md
├── research/
│   ├── 00_CURRENT_STATE_AUDIT.md
│   ├── 01_COURSE_ALIGNMENT.md
│   ├── 02_NETWORKING_CORE.md
│   ├── OPEN_QUESTIONS.md
│   └── RESEARCH_LEDGER.md
├── scratch/
│   ├── generate_master_wbs.py
│   ├── protocol_catalog_verification_log.txt
│   ├── test_frame_codec_run.log
│   ├── verify_protocol_catalog.py
│   ├── verify_protocol_scope.py
│   ├── verify_wbs.py
│   ├── wbs_verification_log.txt
│   └── test_frame_codec/
│       ├── Program.cs
│       └── test_frame_codec.csproj
├── sections/
│   ├── 1-TongQuan.tex
│   ├── 2-MucTieuDeTai.tex
│   ├── 3-PhuongPhapThucHien.tex
│   ├── 4-NoiDungDeTai.tex
│   └── 5-KeHoachThucHien.tex
└── template/
    └── nt106.cls
```

- **Tập tin đề cương PDF (`main.pdf`):**
  - Kích thước: `311,090` bytes
  - SHA-256: `55b05b309a25cc08a8dccb6cede5f36024bfceef6971365b9ccda371ca5aff15`
- **Tập tin bàn giao nén (`LABGUARD_CHECKPOINT_1_REV4_2_1_HANDOFF.zip`):**
  - Gói ZIP chứa tổng cộng 43 tệp tin: 42 tệp tin được liệt kê và kiểm chứng mã băm bởi `MANIFEST_SHA256.txt`; tệp tin thứ 43 chính là `MANIFEST_SHA256.txt` (không tự băm chính nó). Giải nén và kiểm chứng 100% khớp.

---

## VIII. KẾT LUẬN & TRẠNG THÁI NGHIỆM THU

Toàn bộ các điều kiện khắt khe nhất của đợt kiểm toán độc lập đã được thỏa mãn tuyệt đối:
- **P0 Message Scope SSOT:** 34/34, 25/25, 10/10 khớp chính xác 100%, có script verifier tự động bảo vệ không cho phép drift.
- **.NET Runtime & Timeline:** Đồng bộ mốc tháng 9/2026, loại bỏ thì tương lai, làm rõ baseline .NET 9 SDK (baseline của dự án hiện được kiểm thử trên môi trường phát triển của nhóm; tính tương thích phòng máy UIT cần được xác thực trước khi triển khai thực tế).
- **PBKDF2 Canonical Wording:** Đồng bộ nguyên văn tuyên bố OWASP trên 6 tệp tin.
- **Superseded Banner:** Đặt rõ ràng trên toàn bộ 3 hồ sơ lịch sử.
- **Build Reproducibility:** Hướng dẫn đầy đủ trong README.md với cảnh báo độc lập trình biên dịch.
- **Kiểm chứng thực nghiệm:** Toàn bộ test suite chạy thành công không có ngoại lệ; đề cương PDF đạt đúng 30 trang A4.

Trạng thái Checkpoint 1: **`CHECKPOINT 1 PASS — FROZEN`**  
Sẵn sàng cho việc đánh giá cuối cùng và lưu trữ hồ sơ thiết kế chuẩn của đồ án NT106.
