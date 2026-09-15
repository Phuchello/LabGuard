# LABGUARD — BÁO CÁO KIỂM THỬ TỰ ĐỘNG CHECKPOINT 2 REV2 (VALIDATION REPORT)
**Mã tài liệu:** `CP2_VALIDATION_REPORT_REV2.md`  
**Chuyên đề:** Báo cáo Kết quả Thực thi Kịch bản Kiểm thử Tính Chuẩn mực Giao thức & Tính Nhất quán Quyết định Kỹ thuật (REV2 Final Consistency Patch)  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Thời điểm thực thi:** 2026-09-14 08:19:14 UTC+7  
**Trạng thái kiểm định:** 100% PASS (EXIT CODE 0)  
**Trạng thái bàn giao:** CHECKPOINT 2 REV2 — READY FOR FINAL INDEPENDENT AUDIT  

---

## 1. MÔI TRƯỜNG THỰC THI & PHẠM VI KIỂM ĐỊNH

- **Hệ điều hành:** Microsoft Windows 11
- **Python Runtime:** Python 3.12 (UTF-8 console stream)
- **Thư mục cơ sở:** `c:\Users\lyle3\OneDrive\Documents\Môn học\Lập trình mạng\Đồ án\LABGUARD_DeCuong_NT106_LaTeX_v3`
- **Danh sách 9 tập tin trọng tâm thuộc phạm vi Checkpoint 2 REV2:**
  1. `research/03_CONCURRENCY_RELIABILITY.md`
  2. `research/04_MONITORING_DETECTION.md`
  3. `research/05_DELIVERY_SEMANTICS.md`
  4. `research/06_DATABASE.md`
  5. `research/CP2_TEST_MATRIX.md`
  6. `research/CP2_OPEN_ISSUES.md`
  7. `CP2_DECISIONS_REV2.md` (Văn kiện quyết định duy nhất có hiệu lực tại thư mục gốc)
  8. `AUDIT_HANDOFF_CHECKPOINT_2_REV2.md` (Biên bản bàn giao giải trình 12 hạng mục)
  9. `CP2_CHO_NHOM_3_NGUOI_REV2.md` (Sổ tay thực chiến cho nhóm 3 sinh viên)

---

## 2. KẾT QUẢ KIỂM ĐỊNH 1: TÍNH CHUẨN MỰC DANH MỤC GIAO THỨC (PROTOCOL NAME VALIDATION)

- **Kịch bản thực thi:** `python scratch/validate_cp2_protocol_names.py`
- **Bộ quy tắc kiểm tra:**
  - 100% tên bản tin tầng mạng phải thuộc danh mục 69 bản tin canonical từ `03_PROTOCOL_SPEC.md` (P0-MVP Required: 34; P0 Optional: 25; P1 Conditional: 10).
  - Nghiêm cấm các danh xưng giao thức tự chế cũ (ví dụ: `COLLECTOR_REGISTER`, `COLLECTOR_FORWARD`, `LOGIN_REQ`, `REALTIME_METRIC_EVENT`, v.v.).
  - Bắt các định dạng từ khóa nghi vấn mang hậu tố `_REQ`, `_RESP`, `_RESPONSE`, `_EVENT`, `_MSG`.

### Bằng chứng Đầu ra Thực tế (Console Output Evidence):
```
==================================================
RUNNING CHECKPOINT 2 PROTOCOL NAME VALIDATION
Base Directory: C:\Users\lyle3\OneDrive\Documents\Môn học\Lập trình mạng\Đồ án\LABGUARD_DeCuong_NT106_LaTeX_v3
==================================================
✅ research/03_CONCURRENCY_RELIABILITY.md: PASS
✅ research/04_MONITORING_DETECTION.md: PASS
✅ research/05_DELIVERY_SEMANTICS.md: PASS
✅ research/06_DATABASE.md: PASS
✅ research/CP2_TEST_MATRIX.md: PASS
✅ research/CP2_OPEN_ISSUES.md: PASS
✅ CP2_DECISIONS_REV2.md: PASS
✅ AUDIT_HANDOFF_CHECKPOINT_2_REV2.md: PASS
✅ CP2_CHO_NHOM_3_NGUOI_REV2.md: PASS
--------------------------------------------------
ALL CP2 FILES PASSED PROTOCOL NAME VALIDATION (0 errors).
```
**Kết luận:** ĐẠT (PASS) — 0 lỗi vi phạm.

---

## 3. KẾT QUẢ KIỂM ĐỊNH 2: TÍNH NHẤT QUÁN QUYẾT ĐỊNH KỸ THUẬT (DECISIONS & CONSISTENCY VALIDATION)

- **Kịch bản thực thi:** `python scratch/validate_cp2_decisions.py`
- **Bộ quy tắc kiểm tra:**
  - Kiểm tra dung lượng hàng đợi: `AgentOutboxChannel = 256`, `CentralIngressChannel = 1,000`, `ReplayBuffer = 50`.
  - Kiểm tra kiến trúc Ingress: ONE Single Logical Ingress Worker cho MVP P0.
  - Kiểm tra Telemetry Ownership & Dropped Queue: Agent sở hữu `METRIC_BATCH` trong hàng đợi 50 batches cho tới khi nhận `BATCH_ACK`; FIFO Drop-Oldest log `TELEMETRY_DROPPED`.
  - Kiểm tra `SERVICE_STATUS`: Latest-state telemetry, không lưu trong ReplayBuffer, Idempotent UPSERT trên bảng `Services`.
  - Kiểm tra thông số liveness: `Heartbeat = 5.0s`, `OfflineThreshold = 20.0s` (dựa trên Monotonic Clock elapsed time, không dùng missed-counter).
  - Kiểm tra framing: `MaxPayloadSize = 262,144 bytes` (256 KiB), wire maximum $= 262,148\text{ bytes}$ ($4 + 262,144$).
  - Kiểm tra CSDL: Bảng `MetricSamples` có `UptimeSeconds BIGINT NOT NULL`, loại trừ hoàn toàn `DiskPercent` khỏi DDL; kiểu dữ liệu `rowversion` chuẩn xác.
  - Kiểm tra kỷ luật trạng thái: Không chứa các cụm từ bị cấm (như `ACCEPTED & FROZEN`, các từ ngữ tuyệt đối hóa chưa đo đạc).

### Bằng chứng Đầu ra Thực tế (Console Output Evidence):
```
==================================================
RUNNING CHECKPOINT 2 DECISIONS & CONSISTENCY VALIDATION
Base Directory: C:\Users\lyle3\OneDrive\Documents\Môn học\Lập trình mạng\Đồ án\LABGUARD_DeCuong_NT106_LaTeX_v3
==================================================
✅ research/03_CONCURRENCY_RELIABILITY.md: PASS
✅ research/04_MONITORING_DETECTION.md: PASS
✅ research/05_DELIVERY_SEMANTICS.md: PASS
✅ research/06_DATABASE.md: PASS
✅ research/CP2_TEST_MATRIX.md: PASS
✅ research/CP2_OPEN_ISSUES.md: PASS
✅ CP2_DECISIONS_REV2.md: PASS
✅ AUDIT_HANDOFF_CHECKPOINT_2_REV2.md: PASS
✅ CP2_CHO_NHOM_3_NGUOI_REV2.md: PASS
--------------------------------------------------
ALL CP2 FILES PASSED DECISION & CONSISTENCY VALIDATION (0 errors).
```
**Kết luận:** ĐẠT (PASS) — 0 lỗi vi phạm.

---

## 4. TỔNG KẾT HẠNG MỤC THẨM ĐỊNH REV2

| Tiêu chí | Kết quả | Ghi chú nghiệm thu |
|:---|:---:|:---|
| **Canonical Protocol Conformance** | **100% PASS** | 69 bản tin canonical, không dùng tên giao thức tự chế. |
| **Architectural Consistency** | **100% PASS** | Đồng nhất 18 quyết định CP2-001 đến CP2-018 trên toàn bộ 9 tài liệu. |
| **Ingress Worker Architecture** | **100% PASS** | ONE Single Logical Ingress Worker cho P0 MVP; loại bỏ partitioning. |
| **Telemetry Ownership & Dropped Logs** | **100% PASS** | Agent sở hữu mẻ đo trong queue 50 batches; log `TELEMETRY_DROPPED`. |
| **Service Status Semantics** | **100% PASS** | Latest-state telemetry, không lưu trong ReplayBuffer, idempotent upsert. |
| **Database DDL Scope** | **100% PASS** | `UptimeSeconds BIGINT NOT NULL` có mặt trong DDL; `Disk` loại khỏi MVP. |
| **Liveness Detection Window** | **100% PASS** | Elapsed monotonic time $> 20.0\text{s}$ và $\le 22.5\text{s}$; log đầy đủ 4 trường. |
| **Backpressure Evidence** | **100% PASS** | Channel count $\le 1,000$ & RAM bounded là bắt buộc; Zero Window là tùy chọn. |
| **Client Outbox Configuration** | **100% PASS** | Dung lượng 128 items, Single Writer, không cần strict priority scheduler. |
| **Document Hierarchy & Archiving** | **100% PASS** | Duy nhất `CP2_DECISIONS_REV2.md` tại root; toàn bộ tài liệu cũ chuyển vào `archive/`. |
| **Status Discipline** | **100% PASS** | Trạng thái chuẩn xác: `CHECKPOINT 2 REV2 — READY FOR FINAL INDEPENDENT AUDIT`. |
