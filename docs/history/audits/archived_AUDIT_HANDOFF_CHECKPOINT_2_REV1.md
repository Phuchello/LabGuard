> **SUPERSEDED / HISTORICAL ARTIFACT**  
> This document has been superseded and preserved for historical audit trail only.  
> Current authoritative candidate: [CP2_DECISIONS_REV2.md](../CP2_DECISIONS_REV2.md) / [AUDIT_HANDOFF_CHECKPOINT_2_REV2.md](../AUDIT_HANDOFF_CHECKPOINT_2_REV2.md).

---

# LABGUARD — BIÊN BẢN BÀN GIAO TÁI KIỂM TOÁN ĐỘC LẬP CHECKPOINT 2 (REV1)
**Mã tài liệu:** `AUDIT_HANDOFF_CHECKPOINT_2_REV1.md`  
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Đơn vị thực hiện:** Antigravity (Lead Technical Researcher & Network Software Architect)  
**Đơn vị kiểm toán độc lập:** ChatGPT Work (Independent Technical Auditor)  
**Trạng thái bàn giao:** **CHECKPOINT 2 REV1 — READY FOR INDEPENDENT RE-AUDIT**  
**Thời điểm lập:** 14/09/2026  

---

## 1. MỤC TIÊU & TỔNG KẾT VÒNG REVISION 1

Thực hiện nghiêm túc kết luận từ vòng kiểm toán độc lập (*"CHECKPOINT 2 = REVISE REQUIRED. DO NOT FREEZE. DO NOT START PRODUCT IMPLEMENTATION"*), toàn bộ hồ sơ Checkpoint 2 đã trải qua vòng rà soát kỹ thuật toàn diện Revision 1:
- Chuyển đổi toàn bộ trạng thái sang **FREEZE CANDIDATE (READY FOR INDEPENDENT RE-AUDIT)**, tuyệt đối tuân thủ kỷ luật không tự ý đóng băng trước khi có phê duyệt của kiểm toán viên độc lập.
- Khôi phục tính nhất quán tuyệt đối của **Danh mục 69 bản tin wire chuẩn** theo `03_PROTOCOL_SPEC.md` và `CONTRACT_MATRIX.md`.
- Sửa chữa và chuẩn hóa toàn bộ 20 phát hiện kiểm toán (Audit Findings).
- Bổ sung 2 công cụ kiểm chứng tự động (`scratch/validate_cp2_protocol_names.py` và `scratch/validate_cp2_decisions.py`) để kiểm toán viên có thể tái kiểm chứng độc lập.

---

## 2. MA TRẬN ĐỐI CHIẾU XỬ LÝ 20 PHÁT HIỆN KIỂM TOÁN (AUDIT RESPONSE MATRIX)

| STT | Phát hiện Kiểm toán | Giải pháp Kỹ thuật Triệt để tại Revision 1 | Tệp Tài liệu Điều chỉnh |
|:---:|:---|:---|:---|
| **1** | **Status Discipline** | Chuyển toàn bộ tiêu đề và trạng thái phiên bản trước sang **"FREEZE CANDIDATE (READY FOR INDEPENDENT RE-AUDIT)"**. | Toàn bộ 9 tài liệu CP2 |
| **2** | **Restore Canonical Protocol** | Loại bỏ hoàn toàn các tên tự phát minh (các biến thể đuôi không chuẩn). Khôi phục chính xác danh mục 69 bản tin chuẩn (`LOGIN`, `LOGIN_RESULT`, `INCIDENT_ACK`, `INCIDENT_RESOLVE`, `LIST_DEVICES`, `DEVICE_UPDATE`, `INCIDENT_OPEN`, `INCIDENT_UPDATE`, v.v.). | `03`, `04`, `05`, `06`, `CP2_DECISIONS_REV1`, `TEST_MATRIX` |
| **3** | **Max Frame Size Semantics** | Chuẩn hóa chính xác: **`MaxPayloadSize = 262,144 bytes (256 KiB)`**, header 4 byte Big-Endian, khung wire tối đa là **$262,148\text{ bytes}$**. Không định nghĩa gộp 256 KiB bao gồm header. | `03`, `CP2_DECISIONS_REV1`, `HANDOFF`, `CHO_NHOM` |
| **4** | **Per-Device Processing Order** | Cố định phương án **Single Logical Ingress Worker** tuần tự hóa xử lý theo thiết bị cho các bản tin đột biến trạng thái (`METRIC_BATCH`, `HEARTBEAT`, `SERVICE_STATUS`, `INCIDENT_*`); chuyển giao bất đồng bộ cho các truy vấn chỉ đọc. Thêm cơ chế kiểm tra và hủy frame mang `ConnectionGeneration` cũ. | `research/03_CONCURRENCY_RELIABILITY.md`, `CP2_DECISIONS_REV1` |
| **5** | **Queue Policy Contradiction** | Phân định rõ ràng: Khi CONNECTED dùng `AgentOutboxChannel` (256, `Wait`). Khi DISCONNECTED hủy outbox cũ, tạm dừng Heartbeat timer, đưa telemetry vào ReplayBuffer. Khi RECONNECTED tạo outbox mới, gửi Heartbeat mới sau `AUTH_RESULT`. | `research/03_CONCURRENCY_RELIABILITY.md`, `CP2_DECISIONS_REV1` |
| **6** | **Remove Disk from MVP** | **Xóa bỏ hoàn toàn cột chỉ số Disk** khỏi bảng `MetricSamples` trong CSDL DDL và mọi tài liệu mô tả. Telemetry MVP chỉ gồm CPU, RAM, Uptime, Services. | `research/06_DATABASE.md`, DDL script |
| **7** | **Fix Windows RAM API** | Loại bỏ khuyến nghị hàm đo bộ nhớ không phù hợp. Khuyến nghị chính thức sử dụng hàm Win32 API cấp hệ điều hành **`GlobalMemoryStatusEx`** (hoặc Windows Performance Counter). | `CP2_CHO_NHOM_3_NGUOI_REV1.md` |
| **8** | **Freeze BootId / BatchId** | Khóa cứng định dạng: **`BootId` là kiểu `Guid`** sinh một lần cho mỗi tiến trình Agent; **`BatchId` là kiểu `Guid` (UUIDv4)** sinh cho mỗi batch và giữ nguyên qua retry/replay. | `03`, `05`, `06`, `CP2_DECISIONS_REV1` |
| **9** | **Fix Reconnect Success Condition** | Chuẩn hóa logic kết nối Agent: Bộ đếm backoff $k$ **chỉ được reset về 0 sau khi nhận `AUTH_RESULT` với `success == true`**. | `research/03_CONCURRENCY_RELIABILITY.md`, `CP2_DECISIONS_REV1` |
| **10** | **Fix Jitter Terminology** | Không dùng thuật ngữ Jitter toàn phần. Gọi chính xác là **"Truncated Exponential Backoff với Bounded Uniform Jitter $\pm 10\%$"**. | `03`, `CP2_DECISIONS_REV1`, `TEST_MATRIX` |
| **11** | **Fix Liveness Rationale** | Diễn đạt chuẩn xác: **"OfflineThreshold = 20.0s = 4 chu kỳ heartbeat."** Máy chủ tính toán dựa trên thời gian đơn điệu trôi qua (`Stopwatch.GetTimestamp()`), không dùng bộ đếm số lần lỡ (not a missed-counter). | `research/04_MONITORING_DETECTION.md` |
| **12** | **Deterministic Request Digest** | Đặc tả thuật toán chuẩn mực: Tuần tự hóa các trường cố định (`commandType|incidentId|expectedRowVersion|assignedToUserId|resolutionNote|commandId`) thành chuỗi byte UTF-8 xác định rồi băm SHA-256. | `research/05_DELIVERY_SEMANTICS.md` |
| **13** | **Delivery Semantics Table** | Tách riêng các dòng cho từng bản tin chuẩn: `HEARTBEAT`, `METRIC_BATCH`, `SERVICE_STATUS`, `LOGIN`, `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`. Thu hẹp phạm vi tuyên bố sang "hiệu ứng lưu trữ có thể quan sát". | `research/05_DELIVERY_SEMANTICS.md` |
| **14** | **Database RowVersion Wording** | Khẳng định rõ: `rowversion` là kiểu dữ liệu riêng của SQL Server (8-byte auto-generated binary token), **không phải binary(8) tĩnh, không phải ngày giờ**. | `research/06_DATABASE.md`, `CP2_DECISIONS_REV1` |
| **15** | **Degraded DB Mode** | Áp dụng **Mô hình Thử lại do Agent quản lý (Agent-Owned Retry Model)**: Central giữ TCP socket, nhận Heartbeat trên RAM, không gửi ACK cho `METRIC_BATCH` (Agent lưu trong ReplayBuffer), từ chối lệnh Admin với `ERR_DATABASE_UNAVAILABLE`. Không đệm hàng đợi RAM thứ hai tại Server. | `research/06_DATABASE.md`, `CP2_DECISIONS_REV1` |
| **16** | **Async Dispatch Safety** | Loại bỏ `_ = Task.Run(...)` tự do. Mọi tác vụ nền đều có `CancellationToken`, xử lý ngoại lệ, và shutdown tuần tự. | `research/03_CONCURRENCY_RELIABILITY.md` |
| **17** | **Remove Unsupported Claims** | Loại bỏ hoàn toàn các cụm từ quảng bá chủ quan, thiếu số liệu định lượng kỹ thuật. | Toàn bộ các file |
| **18** | **CP2 Test Matrix Inclusion** | Hoàn thiện `research/CP2_TEST_MATRIX.md` gồm 20 kịch bản kiểm thử chi tiết bao phủ 100% các yêu cầu của kiểm toán viên. | `research/CP2_TEST_MATRIX.md` |
| **19** | **Automated Validators** | Tạo 2 script kiểm thử tự động trong `scratch/`: `validate_cp2_protocol_names.py` và `validate_cp2_decisions.py`. | `scratch/validate_*.py` |
| **20** | **Status After Patch** | Đặt trạng thái cuối cùng là **CHECKPOINT 2 REV1 — READY FOR INDEPENDENT RE-AUDIT** và dừng lại, không tự tiện chuyển sang CP3 hay viết code. | Toàn bộ deliverables |

---

## 3. DANH MỤC HỒ SƠ BÀN GIAO REVISION 1

| STT | Tên Tệp Tài liệu | Đường dẫn tương đối | Vai trò Kỹ thuật & Trạng thái |
|:---:|:---|:---|:---:|
| 1 | **Biên bản Bàn giao Tái kiểm toán** | `AUDIT_HANDOFF_CHECKPOINT_2_REV1.md` | Báo cáo chi tiết kết quả xử lý 20 phát hiện kiểm toán (Tệp này). |
| 2 | **Sổ Quyết định Kỹ thuật CP2** | `CP2_DECISIONS_REV1.md` | Sổ ghi nhận 16 quyết định kỹ thuật và 9 kiến trúc chi tiết (Freeze Candidate). |
| 3 | **Sổ tay Bỏ túi Nhóm 3 người** | `CP2_CHO_NHOM_3_NGUOI_REV1.md` | Hướng dẫn kỹ thuật thực chiến tinh gọn cho Bạn A, Bạn B, Bạn C. |
| 4 | **Concurrency & Reliability** | `research/03_CONCURRENCY_RELIABILITY.md` | Đặc tả luồng ghi đơn, tuần tự hóa theo thiết bị, hàng đợi và backoff. |
| 5 | **Monitoring & Detection** | `research/04_MONITORING_DETECTION.md` | Đặc tả Monotonic liveness, 4 quy tắc P0 (Streak 3, Hysteresis 15%), FSM 2 tầng. |
| 6 | **Delivery Semantics** | `research/05_DELIVERY_SEMANTICS.md` | Ma trận ngữ nghĩa 69 bản tin chuẩn, Dedup telemetry, Deterministic digest. |
| 7 | **Database Architecture** | `research/06_DATABASE.md` | Script DDL 10 bảng T-SQL (loại Disk), `rowversion` OCC, Degraded mode. |
| 8 | **CP2 Test Matrix** | `research/CP2_TEST_MATRIX.md` | Ma trận 20 kịch bản kiểm thử toàn diện có bằng chứng nghiệm thu và viva. |
| 9 | **CP2 Open Issues Log** | `research/CP2_OPEN_ISSUES.md` | Nhật ký đối soát đóng băng các quyết định kỹ thuật. |
| 10 | **Validator: Protocol Names** | `scratch/validate_cp2_protocol_names.py` | Công cụ tự động quét vi phạm danh mục bản tin. |
| 11 | **Validator: Decisions & Consistency** | `scratch/validate_cp2_decisions.py` | Công cụ tự động kiểm tra tính nhất quán tham số. |

---

## 4. THẺ TRA CỨU NHANH CÁC THÔNG SỐ ỨNG VIÊN ĐÓNG BĂNG

- **Khung truyền:** `MaxPayloadSize = 262,144 bytes (256 KiB)`. Khung wire tối đa là $262,148\text{ bytes}$ ($4 + 262,144$).
- **Hàng đợi Gửi Agent (`AgentOutboxChannel`):** Dung lượng **256**, `Wait`. Khi mất kết nối: tạm dừng Heartbeat, đưa metric vào ReplayBuffer.
- **Hàng đợi Nhận Server (`CentralIngressChannel`):** Dung lượng **1,000**, `Wait`. **Single Logical Ingress Worker** tuần tự hóa theo thiết bị cho state-mutating messages; concurrent dispatch cho read queries.
- **Bộ nhớ đệm Gửi bù (`ReplayBuffer`):** Dung lượng **50 batches** (250s), RAM FIFO Drop-Oldest, cờ `"isReplay": true`. Không lưu `HEARTBEAT`.
- **Chuỗi Thử lại (Backoff):** $1\text{s}, 2\text{s}, 4\text{s}, 8\text{s}, 16\text{s}, 30\text{s}$ kèm Bounded Uniform Jitter $\pm 10\%$. Reset $k=0$ sau `AUTH_RESULT(success == true)`.
- **Liveness:** Heartbeat chu kỳ **5.0s**. `OfflineThreshold = 20.0s` (4 chu kỳ heartbeat), đo bằng `Stopwatch.GetTimestamp()` trên Server, không dùng missed-counter. `CheckInterval = 2.5s`.
- **Quy tắc Sự cố P0:**
  - `HIGH_CPU`: Bật $\ge 90\%$ (3 mẫu liên tiếp); Phục hồi $\le 75\%$ (3 mẫu liên tiếp). Hysteresis delta = $15\%$.
  - `HIGH_MEMORY`: Bật $\ge 85\%$ (3 mẫu liên tiếp); Phục hồi $\le 70\%$ (3 mẫu liên tiếp). Hysteresis delta = $15\%$.
  - `SERVICE_DOWN`: Báo lỗi 2 lần probe fail; Phục hồi 2 lần probe pass.
  - Cooldown tối thiểu: **15 giây** giữa 2 lần đổi trạng thái sự cố.
- **Khử trùng lặp Telemetry:** Khóa `(DeviceId, BootId, BatchId)`. `BootId` = `Guid`, `BatchId` = `Guid` (v4). Bảng `ProcessedBatches` (TTL 24h). Trùng lặp: không chèn `MetricSamples`, gửi lại `BATCH_ACK`.
- **Idempotency Lệnh Quản trị:** Khóa `(UserId, CommandId)` + SHA-256 `RequestDigest` trên các trường cố định. Bảng `ProcessedCommands` (TTL 7 ngày). Trùng ID khác digest báo `ERR_IDEMPOTENCY_KEY_REUSE`.
- **Thứ tự Gửi ACK:** Nguyên tắc **Commit-Then-Respond** (chỉ gửi ACK ra socket sau khi `SqlTransaction.CommitAsync` thành công).
- **Khóa Lạc quan (OCC):** Cột kiểu `rowversion` (8-byte auto-generated binary token) trên bảng `Incidents`. `@@ROWCOUNT == 0` trả về lỗi `ERR_CONCURRENCY_VIOLATION`.
- **Chế độ Suy giảm khi DB lỗi:** Agent-Owned Retry Model. Server giữ kết nối TCP, nhận Heartbeat trên RAM, không gửi ACK cho `METRIC_BATCH` (Agent lưu trong ReplayBuffer), trả `ERR_DATABASE_UNAVAILABLE` cho lệnh Admin. Không duy trì hàng đợi RAM thứ hai tại Server.

---

**KẾT LUẬN:**  
Hồ sơ **CHECKPOINT 2 REV1 ĐÃ HOÀN THIỆN VÀ SẴN SÀNG CHO VÒNG TÁI KIỂM TOÁN ĐỘC LẬP (READY FOR INDEPENDENT RE-AUDIT).**
