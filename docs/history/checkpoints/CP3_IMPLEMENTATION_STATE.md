# LABGUARD — CHECKPOINT 3 IMPLEMENTATION STATE
**Mã tài liệu:** `docs/CP3_IMPLEMENTATION_STATE.md`  
**Chuyên đề:** Theo dõi tiến độ triển khai mã nguồn sản phẩm P0 MVP  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  

---

## 1. TIẾN ĐỘ THEO CÁC GATE KIỂM SOÁT (TEST-FIRST GATES)

| Gate ID | Tên Cổng Kiểm soát | Trạng thái | Ghi chú nghiệm thu & Bằng chứng |
|:---|:---|:---:|:---|
| **GATE 1** | **Solution Skeleton Builds** | **PASS** | LabGuard.sln gồm 5 src projects, 3 test projects, database/schema.sql, database/seed.sql; build 0 warning, 0 error. |
| **GATE 2** | **FrameCodec Tests Pass (FRAMECODEC_GATE)** | **PASS** | 17/17 tests PASS (toàn bộ 7 kịch bản CP2-TEST-001: partial header/payload, coalescing, oversize, zero-length, EOF mid-frame). |
| **GATE 3** | **HELLO/AUTH Real TCP Pass** | **READY FOR FINAL RE-AUDIT** | Micro-hardening hoàn tất sau re-audit: Thắt chặt IsCanonicalUuidV4 (ngữ nghĩa RFC 4122), kiểm tra offset UTC zero, xóa bỏ 100% server defaults trong DTOs, xóa bỏ hoàn toàn race condition đăng ký kết nối bằng ConnectionRuntime, đối soát tương quan GOODBYE_ACK; 20/20 Protocol, 24/24 Central, 2/2 Agent, tổng 46/46 tests PASS. |
| **GATE 4** | **Heartbeat/Liveness Pass** | PENDING | Heartbeat 5s, timeout 20s (Monotonic Clock), quét 2.5s. |
| **GATE 5** | **METRIC_BATCH -> SQL -> BATCH_ACK Pass** | PENDING | Transaction ACID, nạp số liệu hiệu năng. |
| **GATE 6** | **Lost ACK / Duplicate Batch Pass (DEDUP_GATE)** | PENDING | Kiểm tra ProcessedBatches, gửi lại ACK. |
| **GATE 7** | **Reconnect / Replay Pass** | PENDING | ReplayBuffer 50 mẻ đo, Backoff + Jitter ±10%. |
| **GATE 8** | **Rule Engine Pass (RULE_ENGINE_GATE)** | PENDING | 4 quy tắc P0 (Streak 3 & Hysteresis 15%). |
| **GATE 9** | **Admin Login / Subscription Pass** | PENDING | ClientOutbox 128, LOGIN, SUBSCRIBE. |
| **GATE 10** | **Incident Commands + OCC Pass (OCC_GATE)** | PENDING | rowversion OCC, RequestDigest idempotency. |
| **GATE 11** | **DB Failure Degradation Pass** | PENDING | Giữ TCP, nhận Heartbeat trên RAM, báo lỗi DB. |
| **GATE 12** | **20-Agent Integrated Demo Pass** | PENDING | Giả lập 20 máy trạm, kịch bản tải thực tế. |

---

## 2. NHẬT KÝ CÁC PHIÊN THỰC THI (IMPLEMENTATION SESSIONS)

### Phiên 1 (Session 01)
- **Mục tiêu phiên:** Hoàn thành GATE 1 (Solution Build) và GATE 2 (`FRAMECODEC_GATE`).
- **Phạm vi code:**
  - Cấu trúc thư mục `src/`, `tests/`, `database/`, `docs/`.
  - `database/schema.sql` (10 bảng theo `research/06_DATABASE.md`) và `database/seed.sql`.
  - Project `LabGuard.Protocol` với `WireEnvelope`, `FrameCodec`, Canonical Message Catalog, ErrorCodes, DTOs.
  - Project `LabGuard.Protocol.Tests` với đầy đủ 17 kịch bản kiểm thử byte stream.
- **Nghiệm thu:** Đã được kiểm toán độc lập ACCEPTED.
- **Last Safe Checkpoint:** `CP3 SESSION 01 / GATE 2` (FROZEN & ACCEPTED).

### Phiên 2 (Session 02)
- **Mục tiêu phiên:** Triển khai bước đầu GATE 3 (`HELLO/AUTH Real TCP Spike`).
- **Kết quả kiểm toán:** REVISE REQUIRED (Cần khắc phục các vấn đề về RequestDigest drift, hardcoded secret, task tracking, envelope validation).

### Phiên 2A (Session 02A — Code-Level Hardening)
- **Mục tiêu phiên:** Khắc phục triệt để toàn bộ 11 phát hiện kiểm toán tại GATE 3.
- **Phạm vi code đã hoàn tất:** RequestDigest chuẩn 6-part string pipe, xóa bỏ secret khỏi src, tracking task lifecycle, envelope validator, Hello payload validation, auth identity binding, negative tests.
- **Kết quả kiểm thử:** 39/39 tests PASS.
- **Kết quả kiểm toán:** ALMOST PASS (Chỉ còn 5 điểm residual micro-hardening).

### Phiên 2B (Session 02B — Final Micro-Hardening)
- **Mục tiêu phiên:** Khắc phục triệt để 5 tồn đọng cuối cùng từ Independent Source Re-Audit:
  1. **Xác thực ngữ nghĩa UUIDv4:** `EnvelopeValidator.IsCanonicalUuidV4` kiểm tra nghiêm ngặt version 4 (`value[14] == '4'`) và RFC 4122 variant (`value[19] in [8, 9, a, b, A, B]`), từ chối UUIDv1 và chuỗi sai định dạng.
  2. **Kiểm tra offset UTC Zero:** `sentAtUtc` bắt buộc offset bằng 0 (`TimeSpan.Zero`, chấp nhận `Z` và `+00:00`), từ chối dứt khoát múi giờ khác (`+07:00`).
  3. **Xóa bỏ triệt để giá trị mặc định máy chủ khỏi DTO dây truyền:** `HelloAgentPayload` và `HelloAdminPayload` khởi tạo `string.Empty`; client bắt buộc gửi đủ `AgentVersion` và `Hostname`, nếu rỗng hoặc `{}` sẽ bị server từ chối ngay.
  4. **Xóa bỏ Race Condition đăng ký kết nối:** Triển khai `ConnectionRuntime` được thêm vào `_activeConnections` TRƯỚC KHI kích hoạt `ProcessConnectionAsync`; kiểm thử kết nối/ngắt kết nối tức thì 50 lần liên tục đảm bảo `ActiveConnectionCount == 0`.
  5. **Đối soát tương quan GOODBYE_ACK:** `AgentSocketClient.DisconnectAsync` bắt buộc kiểm tra đồng thời `MessageType == GOODBYE_ACK` và `RequestId == goodbye requestId`.
- **Kết quả kiểm thử:** 46/46 tests PASS (Protocol: 20, Central: 24, Agent: 2).
- **Trạng thái Gate 3:** `READY FOR FINAL INDEPENDENT RE-AUDIT`.
- **Last Safe Checkpoint:** `CP3 SESSION 01 / GATE 2` (FROZEN & ACCEPTED).
