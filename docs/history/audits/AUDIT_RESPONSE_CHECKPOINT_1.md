> **SUPERSEDED / HISTORICAL ARTIFACT**  
> Current freeze candidate: AUDIT_HANDOFF_REV4_2_1.md

# BÁO CÁO PHẢN HỒI AUDIT CHECKPOINT 1 — REVISION 4.1
**Vòng thẩm định:** R4.1 Contract & Evidence Closeout (Final Delivery)  
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Tác giả kỹ thuật:** Antigravity (Lead Technical Researcher & Network Software Architect)  
**Auditor độc lập:** ChatGPT Work  
**Mốc thời gian:** 13/09/2026  
**Trạng thái vòng sửa:** HOÀN TẤT BÀN GIAO THỰC TẾ (SẴN SÀNG CHO WORK GIẢI NÉN VÀ TÁI ĐỐI SOÁT ĐỘC LẬP)

---

## I. QUY ƯỚC 5 TRẠNG THÁI THẨM ĐỊNH CHUẨN

1. **FIXED**: Đã sửa đổi mã tài liệu, sơ đồ, code test, biên dịch sạch và đồng bộ toàn bộ tài liệu liên quan.
2. **UNVERIFIED**: Nội dung giả định/thiết kế ngoài luồng, chưa có nguồn kiểm chứng độc lập tại UIT (ví dụ tài liệu tham khảo giáo trình UIT cần đối soát thực địa).
3. **AUTHOR-VERIFIED**: Đã được tác giả lập trình và chạy kiểm chứng thực nghiệm nội bộ trên môi trường thực tế, cung cấp mã nguồn và nhật ký thực thi đầy đủ.
4. **AUDITOR NOT RUN**: Kiểm thử đã có mã nguồn nhưng chưa được Auditor độc lập tái lập chạy thử trên môi trường của Auditor.
5. **DELIVERED**: Gói file bàn giao thực tế đã được đóng gói, đo lường checksum và xuất xưởng sẵn sàng trên ổ đĩa.

---

## II. MA TRẬN PHẢN HỒI 10 FINDING AUDIT (R4-01 ĐẾN R4-10)

| Finding ID | Nội dung Audit từ ChatGPT Work | Giải pháp kỹ thuật & Chuẩn hóa | Vị trí cập nhật trong tài liệu | Bằng chứng đối soát | Trạng thái |
| :--- | :--- | :--- | :--- | :--- | :---: |
| **R4-01** | Workload bị lệch giữa các tài liệu (100.5h, 27h P1, 23h buffer); thiếu nguồn sự thật duy nhất (Single Source of Truth). | Đồng bộ toàn diện về bảng `tab:workload` (hiện tại ở trang 15 của `main.pdf`): P0 Core: 76.5h (A: 24.0, B: 27.0, C: 25.5), Harness: 28.0h (A: 10.0, B: 9.0, C: 9.0), Tích hợp: 18.0h (A: 6.0, B: 6.0, C: 6.0), Viva: 15.0h (A: 5.0, B: 5.0, C: 5.0), P1: 47.0h (A: 15.0, B: 16.0, C: 16.0), Buffer: 27.0h (A: 9.0, B: 9.0, C: 9.0). Tổng: 211.5h (A: 69.0, B: 72.0, C: 70.5). Đã loại bỏ khỏi baseline các con số cũ (100.5h, 27h). | `sections/5-KeHoachThucHien.tex`, `PARALLEL_WORK_PLAN.md`, `TASK_MIGRATION_MAP.md`, `PROJECT_STATE.md`, `TODO.md` | Bảng `tab:workload` và script `verify_wbs.py` khớp theo script đối chiếu | **FIXED** |
| **R4-02** | Logic toán học phương án 12h/tuần bị cộng dồn buffer sai lệch thành 60h/người hoặc đội buffer lên 42.5h. | Chuẩn hóa toán học: Tổng ngân sách 211.5h (~14.1h/w). Năng lực trần 12h/w là 180.0h (3 \times 12 \times 5). DEFER P1 (-47.0h) cho khối lượng cam kết thực tế 164.5h (A: 54.0h, B: 56.0h, C: 54.5h). Khối lượng 164.5h này ĐÃ BAO GỒM ĐỦ 27.0h buffer. Chênh lệch 180.0 - 164.5 = 15.5h là dung lượng năng lực dự phòng (capacity headroom), không cộng dồn vào workload và không tính trùng buffer. | `sections/5-KeHoachThucHien.tex` (Mục 5.1), `PARALLEL_WORK_PLAN.md` (Mục II.3), `PROJECT_STATE.md` | Script `verify_wbs.py` in bảng phân tích toán học 164.5h + 15.5h = 180.0h | **FIXED** |
| **R4-03** | Master WBS CSV bị rút gọn còn 15 cột thiếu nghiệp vụ; số giờ chưa cộng dồn bottom-up khớp bảng tải; thiếu script kiểm tra tự động. | Khôi phục Master WBS đầy đủ 30 cột (>= 27 cột): 60 tasks có trạng thái `TODO vì đây là checkpoint thiết kế; việc triển khai MVP bắt đầu theo kế hoạch sau khi contract được freeze.` Script `verify_wbs.py` kiểm tra tự động 7/7 test passed. Cung cấp file CSV và log thật trong ZIP. | `WBS_TASKS.csv`, `scratch/generate_master_wbs.py`, `scratch/verify_wbs.py`, `appendices/B-TaskMap.tex` | File `scratch/wbs_verification_log.txt` ghi nhận PASS toàn bộ | **AUTHOR-VERIFIED / DELIVERED / AUDITOR NOT RUN** |
| **R4-04** | Wire message catalog trong CONTRACT_MATRIX.md chứa nhiều tên tự đặt không có trong `03_PROTOCOL_SPEC.md` (Blocker C-01). | Đã loại bỏ khỏi baseline toàn bộ tên sai (`ERROR_RESPONSE`, `RATE_LIMIT_WARNING`, `SESSION_EXPIRED`, `CONFIG_PUSH`, `CONFIG_ACK`, `AGENT_RESTART`, `COMMAND_CANCEL`, `GATEWAY_HELLO`, `GATEWAY_ACK`, `ROUTE_REGISTER`, `ROUTE_UNREGISTER`, `GATEWAY_HEARTBEAT`, `COLLECTOR_HANDSHAKE`, `COLLECTOR_ACK`, `AGGREGATE_BATCH`, `BATCH_PERSISTED`). Khôi phục chính xác tập hợp 69 message types từ `03_PROTOCOL_SPEC.md` (59 Core + 10 Internal P1). Catalog được đối chiếu bằng phép so sánh tập hợp tên message với `03_PROTOCOL_SPEC.md`. Script `verify_protocol_catalog.py` tự động so khớp tập hợp: missing = ∅, extra = ∅. | `03_PROTOCOL_SPEC.md`, `CONTRACT_MATRIX.md` (Mục III), `scratch/verify_protocol_catalog.py`, `scratch/protocol_catalog_verification_log.txt` | Script `verify_protocol_catalog.py` kiểm chứng 69/69 message types trùng khớp | **AUTHOR-VERIFIED / DELIVERED / AUDITOR NOT RUN** |
| **R4-05** | PBKDF2 bị hạ xuống 100.000 iterations ở một số file, sai lệch khuyến nghị OWASP và blueprint gốc. | Khôi phục chuẩn: `PBKDF2-HMAC-SHA256`, 600,000 iterations (OWASP), random salt 16 bytes (128-bit), output 32 bytes (256-bit), lưu trữ work factor và salt trong DB, constant-time compare qua `CryptographicOperations.FixedTimeEquals`. Rà soát và thay thế toàn bộ số 100,000 cũ. | `CONTRACT_MATRIX.md`, `sections/3-PhuongPhapThucHien.tex`, `sections/4-NoiDungDeTai.tex`, `DECISIONS.md`, `handbooks/HANDBOOK_B.md` | Không còn bất kỳ file nào chứa "100.000" iterations | **FIXED** |
| **R4-06** | Diễn đạt kỹ thuật dễ gây hiểu nhầm về `rowversion`, UI marshaling, RBAC authorization, và RequestDigest. | Nêu rõ `rowversion` là kiểu dữ liệu đặc thù SQL Server, là token nhị phân opaque 8 byte, được SQL Server tự động tạo giá trị mới khi một dòng chứa cột `rowversion` được cập nhật (không gọi một cột `binary(8)` thông thường là `rowversion`); WinForms điều phối luồng đa dạng qua `Control.Invoke`, `BeginInvoke`, hoặc `InvokeAsync`; kiểm tra quyền RBAC bắt buộc tại Central Server (UI chỉ là UX optimization); `RequestDigest` băm SHA-256 từ chuỗi canonical UTF-8 chuẩn hóa của dedicated command DTO. | `sections/3-PhuongPhapThucHien.tex`, `appendices/A-Glossary.tex`, `CONTRACT_MATRIX.md`, `handbooks/HANDBOOK_B.md`, `handbooks/HANDBOOK_C.md` | Đề cương PDF và handbook định nghĩa chuẩn xác các khái niệm | **FIXED** |
| **R4-07** | Tồn dư cụm từ "Collector 02" / "mỗi người sở hữu đúng một executable" trong đề cương PDF. | Thay thế "Collector 02" thành "Collector App" (một binary `LabGuard.Collector.exe` duy nhất dùng chung cho cả 2 instance do SV C phát triển, SV A triển khai). Mô tả lane rõ ràng: A (Agent + Sim + Gateway/deploy P1), B (Central + DB + Auth/RBAC), C (Admin + Collector App P1); mỗi người có một lane P0 độc lập, P1 bổ sung extension theo đúng ownership. Bảng workload mô tả: "P0 gồm 3 ứng dụng chính và Shared.Protocol; P1 bổ sung Gateway và Collector App". Tách phụ thuộc: "dependency graph tách P0 khỏi P1 để việc trì hoãn nhánh P1 không chặn đường hoàn thành MVP." | `appendices/C-Viva.tex`, `sections/5-KeHoachThucHien.tex`, `PARALLEL_WORK_PLAN.md` | Không còn cụm từ "Collector 02" như một ứng dụng riêng biệt | **FIXED** |
| **R4-08** | Kỷ luật văn phong kỹ thuật (Tone discipline): còn từ ngữ tuyệt đối hóa, tag giáo trình chưa chuẩn. | Thay thế các cụm từ khẳng định tuyệt đối bằng văn phong kỹ thuật khách quan ("Không triển khai code sản phẩm tại Checkpoint 1", "theo invariant được kiểm thử", "khớp theo script đối chiếu", "PDF hiện có 30 trang", "đã loại bỏ khỏi baseline"). Gắn nhãn chuẩn: `[COURSE-ARTIFACT; CURRENT-CLASS UNVERIFIED]` cho tài liệu thực hành UIT trong `bibliography.bib`. Dùng đúng 5 trạng thái audit. | `bibliography.bib`, `research/RESEARCH_LEDGER.md`, `AUDIT_RESPONSE_CHECKPOINT_1.md` | Tài liệu tham khảo và báo cáo tuân thủ nghiêm ngặt tone kỹ thuật | **FIXED** |
| **R4-09** | Đóng gói bàn giao thật và minh chứng kiểm thử: thiếu file ZIP đính kèm thật để auditor tải và chạy (Blocker E-01). | Đã tạo file ZIP thật `LABGUARD_CHECKPOINT_1_REV4_1_HANDOFF.zip` (39 files). Bảng băm `MANIFEST_SHA256.txt` chứng thực 38 file thành phần bên trong. Giải nén kiểm thử độc lập trong thư mục tạm đạt 100% tests PASSED. | `LABGUARD_CHECKPOINT_1_REV4_1_HANDOFF.zip`, `scratch/test_frame_codec_run.log`, `scratch/wbs_verification_log.txt`, `scratch/protocol_catalog_verification_log.txt`, `MANIFEST_SHA256.txt` | File ZIP thực tế kèm checksum và nhật ký thực thi | **DELIVERED / AUDITOR NOT RUN** |
| **R4-10** | Cần biên dịch lại main.pdf đảm bảo đúng 30 trang A4, set pdftitle metadata, sửa pdfstartview=Fit. | `main.pdf` biên dịch thành công đúng 30 trang A4 bằng pdflatex + biber (Exit code 0), không tràn lề, đã thêm `\hypersetup{pdftitle={...}}`, sửa `pdfstartview=Fit`, câu tách DAG được cập nhật. SHA-256: `d09e7e0ff8e2ba0b77ac23b2fda6e2a6c856887d14a644e5fa7298ee1e64afcc`. | `template/nt106.cls`, `main.tex`, `main.pdf` | `main.pdf` hiện có 30 trang A4, 0 errors, pdftitle metadata set | **FIXED** |

---

## III. BẢNG PHÂN CÔNG OWNERSHIP VÀ CÂN ĐỐI TẢI (SINGLE SOURCE OF TRUTH)

```
+---------------------------------------------------------------------------------------+
|                                    SHARED.PROTOCOL                                    |
|             (Envelope, Length-Prefix Codec, DTOs, Error Codes, Wire Enums)            |
|                            Lead: Sinh viên A | Review: B, C                           |
+---------------------------------------------------------------------------------------+
           |                                   |                                   |
           v                                   v                                   v
+-----------------------+           +-----------------------+           +-----------------------+
|    SINH VIÊN A        |           |    SINH VIÊN B        |           |    SINH VIÊN C        |
|  Agent Windows Core   |           |  Central Server Core  |           |  Admin WinForms Client|
|  Simulator 20+ nodes  |           |  SQL Server Ingest/DB |           |  Realtime Dash & Grid |
|  Network Load Harness |  ===TCP==>|  Rule Engine & Eval   |  ===TCP==>|  Client RPC & Cache   |
|  Gateway L4 (P1)      |   TLS     |  Auth Server & RBAC   |   TLS     |  Multi-Admin Conflict |
|  Deploy 2 Collectors  |           |  Snapshot & Fanout    |           |  Shared Collector App |
|                       |           |                       |           |                       |
| Self-test Harness:    |           | Self-test Harness:    |           | Self-test Harness:    |
| Mini Test Server      |           | Simulated Ingest Gen  |           | Mock Central Server   |
+-----------------------+           +-----------------------+           +-----------------------+
```

### Bảng đối soát tải kỹ thuật chuẩn (Nguồn sự thật: `tab:workload`, trang 15 `main.pdf`)

| Thành viên | Trách nhiệm chính | P0 Core | Harness | Integ. | Viva | P1 | Buffer | Tổng cộng | Định mức tuần |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Sinh viên A** | Agent Windows, Simulator, Network Load, Gateway (P1), Deploy | 24.0h | 10.0h | 6.0h | 5.0h | 15.0h | 9.0h | **69.0h** | 13.8h/w |
| **Sinh viên B** | Central Server Core, SQL Server, Rule Engine, Auth Server, Fanout | 27.0h | 9.0h | 6.0h | 5.0h | 16.0h | 9.0h | **72.0h** | 14.4h/w |
| **Sinh viên C** | Admin WinForms, Ingestion, Client RPC, Multi-Admin, Collector App (P1) | 25.5h | 9.0h | 6.0h | 5.0h | 16.0h | 9.0h | **70.5h** | 14.1h/w |
| **TOÀN ĐỘI** | **Hệ thống LABGUARD (P0: 3 App + Shared.Protocol; P1: Gateway + Collector App)** | **76.5h** | **28.0h** | **18.0h** | **15.0h** | **47.0h** | **27.0h** | **211.5h** | **42.3h/w** |

---

## IV. BẰNG CHỨNG KIỂM CHỨNG & DANH MỤC HỒ SƠ CHUYỂN GIAO (R4.1)

1. **Test Harness Frame Codec (`scratch/test_frame_codec/`):**
   - Môi trường: .NET 9.0 (Windows x64) | Lệnh chạy: `dotnet run --project scratch/test_frame_codec/test_frame_codec.csproj`
   - Kết quả: **5/5 tests PASSED** (Nhật ký thực thi ghi tại `scratch/test_frame_codec_run.log`).
2. **Kiểm tra tự động Master WBS (`scratch/verify_wbs.py`):**
   - Môi trường: Python 3.12.10 (Windows x64) | Lệnh chạy: `python scratch/verify_wbs.py`
   - Kết quả: **7/7 checks PASSED** (Nhật ký thực thi ghi tại `scratch/wbs_verification_log.txt`).
3. **Kiểm tra tự động Protocol Catalog (`scratch/verify_protocol_catalog.py`):**
   - Môi trường: Python 3.12.10 (Windows x64) | Lệnh chạy: `python scratch/verify_protocol_catalog.py`
   - Kết quả: **PASS (69/69 messages matched, 0 missing, 0 extra)** (Nhật ký thực thi ghi tại `scratch/protocol_catalog_verification_log.txt`).
4. **Biên dịch Đề cương `main.pdf`:**
   - Môi trường: MiKTeX 25.12 (`pdflatex` + `biber`) | Exit code: 0
   - Quy cách: **PDF hiện có 30 trang A4**, metadata `pdftitle` và `pdfstartview=Fit` hoàn chỉnh, SHA-256: `d09e7e0ff8e2ba0b77ac23b2fda6e2a6c856887d14a644e5fa7298ee1e64afcc`.
5. **Gói hồ sơ bàn giao đính kèm:**
   - File nén thực tế: `LABGUARD_CHECKPOINT_1_REV4_1_HANDOFF.zip` (466,567 bytes, SHA-256: `86cdd8bb0a19dedd5769bcb8ecd2ee9a228c0498fdbbe67e3d35bca6ce645a38`, kèm bảng mã băm `MANIFEST_SHA256.txt`).
