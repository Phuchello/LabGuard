# LABGUARD — BẢNG THEO DÕI CÔNG VIỆC (TODO LIST)
**Dự án:** LABGUARD — Lightweight Network Monitoring & Incident Management System for Computer Labs  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Quy mô:** Đúng 3 sinh viên | 5 tuần  
**Trạng thái:** Checkpoint 1 Revision 4.1 — Hoàn tất R4.1 Contract & Evidence Closeout (Khép lại Blocker C-01 & E-01)  

---

## CHECKPOINT 1 — VÒNG SỬA ĐỔI 4.1 (CONTRACT & EVIDENCE CLOSEOUT)

### 1. Xử lý triệt để 2 Blocker C-01 & E-01
- [x] **Blocker C-01 (Wire Protocol Catalog):** Đã loại bỏ khỏi baseline toàn bộ các tên không có trong spec (`ERROR_RESPONSE`, `RATE_LIMIT_WARNING`, `SESSION_EXPIRED`, `CONFIG_PUSH`, `CONFIG_ACK`, `AGENT_RESTART`, `COMMAND_CANCEL`, `GATEWAY_HELLO`, `GATEWAY_ACK`, `ROUTE_REGISTER`, `ROUTE_UNREGISTER`, `GATEWAY_HEARTBEAT`, `COLLECTOR_HANDSHAKE`, `COLLECTOR_ACK`, `AGGREGATE_BATCH`, `BATCH_PERSISTED`). Khôi phục chính xác tập hợp 69 message types từ `03_PROTOCOL_SPEC.md` (59 Core + 10 Internal P1). Tạo script `scratch/verify_protocol_catalog.py` kiểm tra tự động không hardcode, đối chiếu tập hợp đạt `missing = ∅, extra = ∅`.
- [x] **Blocker E-01 (Gói Bàn Giao Thật):** Tạo file ZIP thực tế `LABGUARD_CHECKPOINT_1_REV4_1_HANDOFF.zip` chứa đầy đủ mã nguồn LaTeX, WBS CSV 30 cột, `CONTRACT_MATRIX.md`, `03_PROTOCOL_SPEC.md`, 3 handbook, research ledger, 2 script verify, 3 log thực thi thực tế, C# frame codec harness, `AUDIT_HANDOFF.md`, `AUDIT_RESPONSE_CHECKPOINT_1.md` và `MANIFEST_SHA256.txt`.
- [x] **Cập nhật trạng thái audit chuẩn xác:** R4-03 (`AUTHOR-VERIFIED / AUDITOR NOT RUN`), R4-04 (`AUTHOR-VERIFIED / AUDITOR NOT RUN`), R4-09 (`OPEN — DELIVERY NOT RECEIVED`). Không tự đổi sang `AUDITOR-VERIFIED`.
- [x] **Sửa đổi các chi tiết kỹ thuật nhỏ:**
  - `rowversion`: Token nhị phân opaque 8 byte, được SQL Server tự động tạo giá trị mới khi dòng được cập nhật (không gọi một cột `binary(8)` thông thường là `rowversion`).
  - Liveness: Không nhận heartbeat hợp lệ trong khoảng thời gian lớn hơn 20 giây (`> 20s`).
  - Workload table: Tham chiếu bằng nhãn `tab:workload`, hiện nằm tại trang 15 của `main.pdf`.
  - Mô tả executable: "P0 gồm 3 ứng dụng chính và Shared.Protocol; P1 bổ sung Gateway và Collector App".
  - `RequestDigest`: Băm SHA-256 từ tuple nghiệp vụ chuẩn hóa `(commandType, incidentId, expectedRowVersion, commandParams, commandId)`. Cùng ID: cùng digest trả kết quả cũ, khác digest báo lỗi xung đột / `ERR_IDEMPOTENCY_KEY_REUSE`.
- [x] **Văn phong kỹ thuật:** Loại bỏ toàn bộ các từ khẳng định tuyệt đối, sửa các chuỗi bị tab escape.

---

## 2. BỘ HỒ SƠ BÀN GIAO CHECKPOINT 1 REVISION 4.1
- [x] `main.pdf`: Đề cương chi tiết NT106, đúng 30 trang A4.
- [x] `WBS_TASKS.csv`: Master WBS gồm đúng 60 task x 30 cột đặc tả chi tiết.
- [x] `scratch/verify_wbs.py` & `scratch/wbs_verification_log.txt`: Script và log kiểm chứng tự động 7/7 test passed.
- [x] `CONTRACT_MATRIX.md`: Giao ước kỹ thuật đông kết, đầy đủ 9 nhóm wire catalog, PBKDF2 600k, dedup persistence.
- [x] `PARALLEL_WORK_PLAN.md`: Kế hoạch 3 làn, bảng giờ chuẩn 211.5h, giải trình toán học 12h/tuần.
- [x] `TASK_MIGRATION_MAP.md`: Bản đồ chuyển đổi 60 công việc theo 11 trường, đồng bộ 100% với Master WBS.
- [x] `DECISIONS.md`: 8 quyết định kiến trúc ADR-001 đến ADR-008.
- [x] `handbooks/HANDBOOK_A/B/C.md`: 3 cẩm nang vai trò cá nhân kèm 90 câu hỏi vấn đáp viva.
- [x] `AUDIT_RESPONSE_CHECKPOINT_1.md` & `AUDIT_HANDOFF.md`: Báo cáo phản hồi chi tiết và hồ sơ bàn giao.
- [x] **GATE DISCIPLINE:** Dừng lại tại Checkpoint 1, chờ ChatGPT Work tái đối soát độc lập.

---

## 3. CHECKPOINT 3 — IMPLEMENTATION PHASE (P0 MVP)
- [x] **GATE 1 (Solution Skeleton):** Khởi tạo `LabGuard.sln`, 5 dự án `src/`, 3 dự án `tests/`, `database/schema.sql`, `database/seed.sql`. Build 0 warning, 0 error.
- [x] **GATE 2 (`FRAMECODEC_GATE`):** Hoàn thành `FrameCodec.cs` (Length-Prefix 4-byte UInt32 Big-Endian, max 256 KiB, reject $N==0$ & $N>262144$), 17/17 unit tests PASS tại `LabGuard.Protocol.Tests`.
- [ ] **GATE 3 (`HELLO/AUTH Real TCP Spike — Micro-Hardening Session 02B`):**
  - Chuẩn hóa đường ống `RequestDigest`: 6 thành phần string pipe `commandType|incidentId|expectedRowVersion|assignedToUserId|resolutionNote|commandId` + Known Vector Test độc lập.
  - Loại bỏ 100% hardcoded secret khỏi `src/`: Constructor yêu cầu token tường minh, kiểm thử quét tự động mã nguồn PASS.
  - Khắc phục triệt để race condition đăng ký kết nối (`ConnectionRuntime` đăng ký vào dictionary trước khi kích hoạt `ProcessConnectionAsync`, test 50 lần ngắt kết nối lập tức PASS, active count = 0).
  - Thắt chặt `EnvelopeValidator.IsCanonicalUuidV4`: Xác thực ngữ nghĩa RFC 4122 (version 4, variant 8/9/a/b), loại bỏ UUIDv1.
  - Thắt chặt `sentAtUtc`: Bắt buộc offset UTC bằng 0 (Z hoặc +00:00), từ chối múi giờ khác (+07:00).
  - Xóa bỏ hoàn toàn giá trị mặc định máy chủ khỏi DTO dây truyền (`HelloAgentPayload`, `HelloAdminPayload` khởi tạo `string.Empty`).
  - Đối soát `GOODBYE_ACK`: Ràng buộc cả `MessageType == GOODBYE_ACK` lẫn `RequestId == goodbye requestId`.
  - 20/20 Protocol tests PASS, 24/24 Central tests PASS, 2/2 Agent tests PASS, toàn bộ 46/46 tests PASS toàn solution.
  - **Trạng thái Gate 3:** `READY FOR FINAL INDEPENDENT RE-AUDIT`.
  - **Last Safe Checkpoint:** `CP3 SESSION 01 / GATE 2` (FROZEN & ACCEPTED).
  - **STOP CONDITION:** Dừng lại, KHÔNG bắt đầu GATE 4, chờ kiểm toán độc lập nghiệm thu.
