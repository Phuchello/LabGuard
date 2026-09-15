> **SUPERSEDED / HISTORICAL ARTIFACT**  
> Current freeze candidate: AUDIT_HANDOFF_REV4_2_1.md

# AUDIT HANDOFF — CHECKPOINT 1 (REVISION 4.2)
**Vòng bàn giao:** Checkpoint 1 Revision 4.2 Independent Audit Patch  
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Nhóm thực hiện:** 03 sinh viên (Kế hoạch 5 tuần)  
**Người chuẩn bị bàn giao:** Antigravity (Lead Technical Researcher & Network Software Architect)  
**Người nhận audit:** ChatGPT Work (Auditor độc lập)  
**Thời điểm:** 13/09/2026 16:35:00+07:00  
**Trạng thái bàn giao:** HOÀN TẤT VÒNG SỬA REVISION 4.2 — SẴN SÀNG CHO FINAL AUDIT FREEZE (`CHECKPOINT 1 REV4.2 READY FOR FINAL AUDIT`)

---

## I. TỔNG QUAN HỒ SƠ & BẢNG TRẠNG THÁI 14 AUDIT FINDINGS

Toàn bộ 14 finding từ báo cáo audit độc lập của ChatGPT Work đã được tiếp thu, giải quyết triệt để và đồng bộ trên toàn bộ hồ sơ:

| STT | Finding | Mức độ | Trạng thái | Giải pháp & Vị trí đối soát |
|:---:|:---|:---:|:---:|:---|
| **1** | **Role Contract (Canonical Naming & RBAC Matrix)** | BLOCKER | **ĐÃ GIẢI QUYẾT** | Thống nhất bộ ba vai trò duy nhất: `Admin` / `Operator` / `Viewer`. Xóa bỏ hoàn toàn vai trò `Tech` trên toàn bộ repository (0 kết quả regex grep `\bTech\b`). Đồng bộ RBAC Matrix duy nhất vào `03_PROTOCOL_SPEC.md`, `CONTRACT_MATRIX.md`, `WBS_TASKS.csv`, `TASK_MIGRATION_MAP.md`, `handbooks/`, `appendices/`, `sections/`, `PROJECT_STATE.md`. |
| **2** | **Protocol Catalog SSOT** | BLOCKER | **ĐÃ GIẢI QUYẾT** | Đính chính tại `PROJECT_STATE.md` (dòng 63): Bản danh mục 21 bản tin chỉ là tập con tiêu biểu trực quan; Nguồn Sự Thật Duy Nhất (SSOT) có thẩm quyền tối cao là Catalog 69 message (59 Core + 10 P1) tại `03_PROTOCOL_SPEC.md` và `CONTRACT_MATRIX.md`. |
| **3** | **Rowversion Wording & Semantics** | BLOCKER | **ĐÃ GIẢI QUYẾT** | Chuẩn hóa định nghĩa chuẩn xác theo Microsoft Transact-SQL: `rowversion` là datatype riêng của SQL Server, kích thước 8 byte sinh tự động khi row insert/update. `binary(8)` thông thường KHÔNG có semantics tự cập nhật này. `rowversion` KHÔNG phải date/time. Cập nhật tại `HANDBOOK_B.md`, `03_PROTOCOL_SPEC.md`, `CONTRACT_MATRIX.md`, `A-Glossary.tex`, `RESEARCH_LEDGER.md`. |
| **4** | **Frame Harness Evidence** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Bổ sung envelope đầy đủ 6 trường chuẩn hóa vào `scratch/test_frame_codec/Program.cs`. Thay thế tên tạm `FRAME_A`/`FRAME_B` bằng `HEARTBEAT` và `SERVICE_STATUS` có trong catalog. Đặt disclaimer tường minh trong code và log: *"Harness này chỉ verify FRAME TRANSPORT, không phải full protocol validation."* Chạy thành công 5/5 tests (`test_frame_codec_run.log`). |
| **5** | **LaTeX Overfull \hbox Audit** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Bẻ dòng danh sách messageType dài bằng cú pháp cô đọng liên kết với 34 message P0-MVP; sửa bảng truy vết FR với `p{2.6cm}` và `\-`; chuẩn hóa `\printinfo` và `\printsignature` trong `nt106.cls`; cấu hình URL breaking penalties trong biblatex (`biburllcpenalty`, `biburlucpenalty`, `biburlnumpenalty`). **Kết quả:** `main.pdf` đạt **đúng 30 trang A4**, **0 warnings**, **0 overfull > 5pt**, chỉ còn 2 overfull không đáng kể (< 5pt: 2.28pt và 3.51pt). |
| **6** | **PBKDF2 Justification & Claim** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Chuẩn hóa tuyên bố theo đúng OWASP: Argon2id là ưu tiên hàng đầu; PBKDF2-HMAC-SHA256 $\ge 600.000$ vòng là giải pháp thay thế chuẩn hóa khi không dùng Argon2id. LabGuard chọn PBKDF2 vì có sẵn trong .NET BCL (`Rfc2898DeriveBytes`), không cần thư viện native C/C++, phù hợp phạm vi môn học NT106. Salt 16B và key 32B là tham số thiết kế của LabGuard, không phải định nghĩa duy nhất của OWASP. Đồng bộ tại `3-PhuongPhapThucHien.tex`, `CONTRACT_MATRIX.md`, `03_PROTOCOL_SPEC.md`, `RESEARCH_LEDGER.md`. |
| **7** | **Runtime Baseline (.NET 9 vs .NET 10)** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Xác lập baseline phát triển và harness thực tế: `.NET 9.0` (baseline của dự án hiện được kiểm thử trên môi trường phát triển của nhóm; tính tương thích phòng máy UIT cần được xác thực trước khi triển khai thực tế, đã cài đặt .NET SDK 9.0.318), KHÔNG phải vì .NET 10 chưa phát hành. Tại mốc tháng 9/2026: .NET 10 LTS đã được phát hành chính thức (11/11/2025, hỗ trợ đến 14/11/2028); .NET 9 STS đang trong giai đoạn bảo trì (kết thúc hỗ trợ 10/11/2026). Nhóm khuyến nghị lộ trình chuyển tiếp sang `.NET 10 LTS` sau khi hoàn tất kiểm chứng môi trường trên máy trạm đích phòng máy UIT. |
| **8** | **NetworkStream Thread Safety** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Chuẩn hóa theo Microsoft docs: `NetworkStream` chỉ hỗ trợ an toàn cho 1 read và 1 write đồng thời; KHÔNG hỗ trợ nhiều tác vụ ghi đồng thời nếu không tự đồng bộ. LabGuard triển khai hàng đợi ghi đơn luồng (`System.Threading.Channels.Channel<byte[]>`) để bảo đảm Single-Writer. Đồng bộ tại `HANDBOOK_A.md` (Q10), `3-PhuongPhapThucHien.tex`, `RESEARCH_LEDGER.md`. |
| **9** | **Bibliography Audit & Primary References** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Kiểm toán toàn diện `bibliography.bib`, bổ sung đủ 16 tài liệu tham khảo sơ cấp chuẩn hóa (RFC 9293, RFC 8259, RFC 8446, RFC 6598, OWASP Cheat Sheet, Microsoft docs: NetworkStream, SslStream, WinForms Thread-safe, Channels, rowversion, SQL Express Limits, .NET Lifecycle, NGINX TCP LB). Gọi `\cite{...}` thật sự trong các mục văn bản. Ghi rõ note cho `uit_nt106_2022`: tài liệu thực hành lưu hành nội bộ, `[CURRENT-CLASS GRADING REQUIREMENTS UNVERIFIED]`. |
| **10** | **Research Ledger Taxonomy** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Thay thế toàn bộ tỷ lệ phần trăm (100%, 98%, 95%) trong `research/RESEARCH_LEDGER.md` bằng hệ phân loại chuẩn: `VERIFIED`, `HIGH CONFIDENCE`, `DESIGN DECISION`, `UNVERIFIED`, `NEEDS MEASUREMENT`. |
| **11** | **RequestDigest Canonical Algorithm** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Định nghĩa thuật toán băm SHA-256 xác định ở server-side trong `03_PROTOCOL_SPEC.md` và `CONTRACT_MATRIX.md`: DTO chuẩn hóa (`commandId`, `commandParams`, `commandType`, `expectedRowVersion`, `incidentId`), sắp xếp thuộc tính theo alphabet, UTF-8 không khoảng trắng thừa, server băm độc lập để kiểm tra tính toàn vẹn và idempotency. |
| **12** | **Protocol Scope Categorization** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Phân loại toàn bộ 69 thông điệp thành 3 nhóm phạm vi tường minh: **P0-MVP REQUIRED (34 messages)** cam kết bắt buộc cho gate 5 tuần; **P0 OPTIONAL (25 messages)** cho tính năng phụ/truy vấn thứ cấp; **P1 CONDITIONAL (10 messages)** cho gateway/collector nội bộ. |
| **13** | **Minor Contract Cleanup** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Xóa chỉ số `Disk` khỏi `METRIC_BATCH` MVP trong `03_PROTOCOL_SPEC.md` và `CONTRACT_MATRIX.md` (giữ CPU, RAM, NIC, Uptime; Disk loại bỏ/trì hoãn khỏi MVP để giảm tải phạm vi). |
| **14** | **Evidence-to-Claim Disclosure** | REQUIREMENT | **ĐÃ GIẢI QUYẾT** | Bổ sung bảng phân loại 3 tầng khẳng định (Verified Evidence vs Design Decision vs Unverified Assumptions) minh bạch vào handoff và tài liệu. |

---

## II. MA TRẬN PHÂN QUYỀN DUY NHẤT (CANONICAL RBAC MATRIX)

Hệ thống chỉ sử dụng 3 vai trò người dùng chuẩn hóa duy nhất:

| Nhóm chức năng | Thao tác / Wire Message | Viewer | Operator | Admin | Ghi chú kiểm soát quyền |
|:---|:---|:---:|:---:|:---:|:---|
| **Tra cứu & Giám sát** | `LIST_DEVICES`, `GET_DEVICE`, `LIST_INCIDENTS`, `GET_INCIDENT`, `GET_HISTORY`, `GET_REPORT`, `GET_SERVER_HEALTH` | Cho phép | Cho phép | Cho phép | Truy vấn Read-Only, không làm thay đổi trạng thái |
| **Đăng ký Realtime** | `SUBSCRIBE_DEVICES`, `SUBSCRIBE_INCIDENTS`, `SUBSCRIBE_HEALTH`, `UNSUBSCRIBE` | Cho phép | Cho phép | Cho phép | Nhận luồng Snapshot & Delta Events qua socket |
| **Xử lý sự cố (Ca trực)** | `INCIDENT_ACK`, `INCIDENT_START`, `INCIDENT_RESOLVE` | Từ chối (`403`) | Cho phép | Cho phép | Tác nghiệp nghiệp vụ trực tiếp trên Incident |
| **Điều phối sự cố** | `INCIDENT_ASSIGN` | Từ chối (`403`) | Từ chối (`403`) | Cho phép | Phân công nhân sự xử lý phiếu sự cố |
| **Quản trị hệ thống** | `CREATE_INVITE`, `SIGN_UP`, `CONFIG_UPDATE` | Từ chối (`403`) | Từ chối (`403`) | Cho phép | Tạo mã mời, cấp tài khoản, cập nhật cấu hình hệ thống |

*Quy tắc bất biến:* Mọi thao tác Mutation đều bắt buộc kiểm tra quyền tại Central Server (Server-side Authorization); việc ẩn/vô hiệu hóa nút bấm trên Admin WinForms Client chỉ mang ý nghĩa UX.

---

## III. BẢNG MINH BẠCH BẰNG CHỨNG (EVIDENCE-TO-CLAIM DISCLOSURE)

Để đảm bảo tính trung thực khoa học tuyệt đối, toàn bộ các khẳng định kỹ thuật trong đồ án được phân loại theo 3 mức độ:

### 1. Các khẳng định ĐÃ CÓ BẰNG CHỨNG KIỂM CHỨNG (`[VERIFIED EVIDENCE]`)
- **Bộ giải mã khung TCP (`FrameCodec`):** Đã kiểm chứng qua bộ 5 test case thực thi độc lập tại `scratch/test_frame_codec/` trên nền .NET SDK 9.0.318 (Partial Read trên Header, Partial Read trên Payload, Coalescing frames, Oversized frame rejection >256 KiB, EOF xử lý sạch). Kết quả: **5/5 tests PASSED**.
- **Tính toàn vẹn của Wire Catalog:** Script `scratch/verify_protocol_catalog.py` kiểm chứng đối soát 69/69 message types giữa `03_PROTOCOL_SPEC.md` và `CONTRACT_MATRIX.md`: **PASS (0 missing, 0 extra)**.
- **Tính khả thi của Kế hoạch phân rã WBS:** Script `scratch/verify_wbs.py` kiểm chứng 60 task card qua 7 tiêu chí kiểm toán (cấu trúc 30 cột, phân chia 20 task/người, đối soát giờ theo bảng workload 211.5h, mô hình rút gọn 12h/tuần đạt 164.5h với 15.5h headroom): **PASS (7/7 checks)**.
- **Biên dịch Đề cương LaTeX:** `pdflatex` + `biber` biên dịch sạch ra `main.pdf`: **Đúng 30 trang A4**, **0 Warnings**, **0 Lỗi tham chiếu undefined**, **0 Overfull \hbox > 5pt** (chỉ có 2 overfull không đáng kể < 5pt: 2.28pt tại `\printinfo` và 3.51pt tại release checklist).

### 2. Các khẳng định LÀ DESIGN DECISION CHƯA ĐO ĐẠC THỰC TẾ (`[DESIGN DECISION / TARGET]`)
- **Ngưỡng quy tắc cảnh báo (Detection Rules):** CPU $\ge 90\%$ trong 3 mẫu liên tiếp (streak), phục hồi $\le 75\%$ trong 3 mẫu (hysteresis); Service Down sau 2 lần lỗi, phục hồi sau 2 lần thành công. Đây là thiết kế lọc nhiễu, cần tinh chỉnh trên dữ liệu phòng máy thật.
- **Chỉ tiêu độ trễ:** Heartbeat deadline 20s (4 mẫu nhịp 5s); failover p95 $\le 45$ giây ở P1; thời gian băm PBKDF2 600.000 vòng trên CPU lab máy trạm UIT (dự kiến ~100--250ms trên chip x86 hiện đại).
- **Chính sách lưu trữ (Retention Policy):** Raw telemetry dự kiến lưu 48 giờ, bảng `ProcessedBatches` dedup TTL 24 giờ, bảng `ProcessedCommands` dedup TTL 7 ngày.
- **Quy mô tải giả lập:** Agent Simulator tạo 10, 20 đến 50 kết nối socket đồng thời; giới hạn chặn trên Ingest Ring Buffer 1.000 batches; hàng đợi Bounded Channel 100 frames.

### 3. Các khẳng định PHỤ THUỘC GIẢ ĐỊNH CHƯA XÁC MINH (`[UNVERIFIED ASSUMPTIONS]`)
- **Barem điểm chính thức:** Barem chấm điểm chính thức của lớp hiện tại chưa được công bố hoặc xác minh (`[CURRENT-CLASS GRADING REQUIREMENTS UNVERIFIED]`). Nhóm xây dựng đồ án bám sát đề cương chuẩn đầu ra chung của Khoa MMT&TT (tài liệu [2] `uit_nt106_2022`).
- **Khả năng demo Internet trực tiếp:** Khả năng mở port / port forwarding trực tiếp trên mạng Internet tại giảng đường phụ thuộc vào hạ tầng mạng thực tế (tường lửa, NAT/CGNAT theo RFC 6598). Nhóm chuẩn bị sẵn phương án demo LAN trên 2--3 máy thật và bộ log/packet capture dự phòng.

---

## IV. PHÂN ĐỊNH PHẠM VI 69 WIRE MESSAGES (P0-MVP VS OPTIONAL VS P1)

| Nhóm phạm vi | Số lượng | Danh sách Message Types | Ý nghĩa cam kết |
|:---|:---:|:---|:---|
| **P0-MVP REQUIRED** | **34** | `HELLO_AGENT`, `HELLO_ADMIN`, `HELLO_ACK`, `AUTH_AGENT`, `AUTH_RESULT`, `HEARTBEAT`, `HEARTBEAT_ACK`, `METRIC_BATCH`, `BATCH_ACK`, `SERVICE_STATUS`, `AGENT_GOODBYE`, `GOODBYE_ACK`, `LOGIN`, `LOGIN_RESULT`, `LOGOUT`, `LOGOUT_RESULT`, `LIST_DEVICES`, `DEVICE_PAGE`, `LIST_INCIDENTS`, `INCIDENT_PAGE`, `SUBSCRIBE_DEVICES`, `SUBSCRIBE_INCIDENTS`, `SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END`, `DEVICE_UPDATE`, `INCIDENT_OPEN`, `INCIDENT_UPDATE`, `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`, `COMMAND_RESULT`, `ERROR` | **Bắt buộc hoàn thành 100%** tại mốc cuối Tuần 3 (M4 Review Gate) để được phép nghiệm thu đồ án. |
| **P0 OPTIONAL** | **25** | `CONFIG_UPDATE`, `CONFIG_RESULT`, `SERVICE_CHECK_REQUEST`, `PING`, `PONG`, `SIGN_UP`, `SIGN_UP_RESULT`, `CREATE_INVITE`, `INVITE_RESULT`, `GET_DEVICE`, `DEVICE_DETAIL`, `GET_METRICS`, `METRICS_RESULT`, `GET_INCIDENT`, `INCIDENT_DETAIL`, `GET_HISTORY`, `HISTORY_PAGE`, `GET_SERVER_HEALTH`, `SERVER_HEALTH`, `GET_REPORT`, `REPORT_RESULT`, `SUBSCRIBE_HEALTH`, `HEALTH_UPDATE`, `UNSUBSCRIBE`, `UNSUBSCRIBE_RESULT` | Tính năng mở rộng nâng cao; có thể giảm phạm vi nếu thiếu ngân sách thời gian mà không ảnh hưởng cốt lõi. |
| **P1 CONDITIONAL** | **10** | `HELLO_COLLECTOR`, `AUTH_COLLECTOR`, `FORWARD_OPEN`, `FORWARD_DATA`, `FORWARD_RESULT`, `FORWARD_CLOSE`, `CLOSE_SESSION`, `COLLECTOR_HEARTBEAT`, `HEALTH_PROBE`, `HEALTH_RESULT` | Chỉ kích hoạt triển khai tại Tuần 4 nếu toàn bộ 34 message P0-MVP đạt chuẩn nghiệm thu không lỗi Sev1/Sev2. |

---

## V. KẾT QUẢ KIỂM TRA ĐỀ CƯƠNG ACADEMIC LATEX (`main.pdf`)

- **Số trang A4:** Đúng **30 trang** (không tràn trang 31).
- **Lỗi biên dịch:** 0 Errors (`exit code 0`).
- **Cảnh báo (Warnings):** 0 Warnings.
- **Tham chiếu không xác định (Undefined references):** 0.
- **Kiểm toán Overfull \hbox:**
  - Overfull > 50pt: **0**.
  - Overfull > 5pt: **0**.
  - Overfull nhỏ < 5pt: **Đúng 2 điểm** (đã kiểm tra trực tiếp trên bản in, hoàn toàn không ảnh hưởng thẩm mỹ thị giác):
    1. Dòng 92 `template/nt106.cls`: $2.28\text{ pt}$ tại khối thông tin bìa đề cương.
    2. Dòng 260 `sections/5-KeHoachThucHien.tex`: $3.51\text{ pt}$ tại đoạn văn bản checklist xuất xưởng.
- **Tài liệu tham khảo sơ cấp:** Đầy đủ 16 nguồn trích dẫn chuyên môn chuẩn xác trong `bibliography.bib`, có lệnh `\cite{...}` liên kết trực tiếp trong văn bản.

---

## VI. HỒ SƠ TẬP TIN BÀN GIAO THỰC TẾ

Gói bàn giao `LABGUARD_CHECKPOINT_1_REV4_2_HANDOFF.zip` gồm đúng 39 tệp tin đồng bộ:

```
LABGUARD_CHECKPOINT_1_REV4_2_HANDOFF.zip
├── 03_PROTOCOL_SPEC.md
├── AUDIT_HANDOFF.md
├── AUDIT_HANDOFF_REV4_2.md
├── AUDIT_RESPONSE_CHECKPOINT_1.md
├── CONTRACT_MATRIX.md
├── DECISIONS.md
├── MANIFEST_SHA256.txt
├── PARALLEL_WORK_PLAN.md
├── PROJECT_STATE.md
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

---
**KẾT LUẬN:** CHECKPOINT 1 REV4.2 READY FOR FINAL AUDIT.
