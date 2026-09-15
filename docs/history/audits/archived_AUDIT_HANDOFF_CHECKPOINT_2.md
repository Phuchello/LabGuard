> **SUPERSEDED / HISTORICAL ARTIFACT**  
> This document has been superseded and preserved for historical audit trail only.  
> Current authoritative candidate: [CP2_DECISIONS_REV2.md](../CP2_DECISIONS_REV2.md) / [AUDIT_HANDOFF_CHECKPOINT_2_REV2.md](../AUDIT_HANDOFF_CHECKPOINT_2_REV2.md).

---

# LABGUARD — BIÊN BẢN BÀN GIAO KIỂM TOÁN ĐỘC LẬP CHECKPOINT 2
**Mã tài liệu:** `AUDIT_HANDOFF_CHECKPOINT_2.md`  
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Đơn vị thực hiện:** Antigravity (Lead Technical Researcher & Network Software Architect)  
**Đơn vị kiểm toán độc lập:** ChatGPT Work (Independent Technical Auditor)  
**Trạng thái bàn giao:** **CHECKPOINT 2 READY FOR INDEPENDENT AUDIT**  
**Thời điểm bàn giao:** 14/09/2026  

---

## 1. MỤC TIÊU & PHẠM VI BÀN GIAO CHECKPOINT 2

Checkpoint 2 có mục tiêu tối thượng: **Chốt dứt điểm 100% các quyết định kỹ thuật và tham số đang mang nhãn `[CP2 TO VERIFY]` từ Checkpoint 1**, nhằm tạo ra một nền móng kiến trúc vững chắc, chi tiết đến từng dòng lệnh và công thức toán học, giúp nhóm 3 sinh viên năm 2 bước vào giai đoạn lập trình mà không phải tự suy đoán hay tranh cãi nội bộ.

### Cam kết Tuân thủ Tuyệt đối các Ràng buộc Bất biến (Invariants):
1. **Kiến trúc Tổng thể:** Giữ nguyên vẹn mô hình 3 tầng:
   $$\text{Windows Agent} \xrightarrow{\text{TCP/TLS}} \text{Central Server} \xrightarrow{\text{Dapper}} \text{SQL Server 2022 Express} \xleftarrow{\text{TCP/TLS}} \text{Admin WinForms}$$
2. **Cấu trúc Khung truyền:** Giữ nguyên chuẩn **Length-Prefix Framing: 4-byte Big-Endian Length + JSON UTF-8 Envelope**, `MaxFrameSize = 256 KiB`.
3. **Phân công & Khối lượng Công việc:** Giữ nguyên 60 Task ID, 3 Lane song song, ngân sách giờ (Người A: 69h, Người B: 72h, Người C: 70.5h, Tổng cộng: 211.5h, MVP P0: 160h, P1: 51.5h).
4. **Bảo toàn Deliverables Cũ:** Không sửa đổi, không tạo phiên bản mới của bộ tài liệu sinh viên đã freeze ở CP1 (`LabGuard_Task_Tracker.xlsx`, `LabGuard_Huong_Dan_Cho_Nhom.docx`, `LabGuard_Huong_Dan_Cho_Nhom.pdf`).
5. **Kỷ luật Triển khai:** Không viết code toàn bộ ứng dụng trước khi Checkpoint 2 được phê duyệt chính thức.

---

## 2. DANH MỤC HỒ SƠ BÀN GIAO (DELIVERABLES MANIFEST)

Toàn bộ các tài liệu kỹ thuật Checkpoint 2 được tổ chức bài bản tại thư mục gốc và thư mục `research/`:

| STT | Tên Tệp Tài liệu | Đường dẫn tương đối | Vai trò Kỹ thuật & Nội dung cốt lõi | Trạng thái |
|:---:|:---|:---|:---|:---:|
| 1 | **Concurrency & Reliability** | `research/03_CONCURRENCY_RELIABILITY.md` | Đặc tả mô hình Single Writer Channel, Backpressure, Dung lượng Queue, Thuật toán Exponential Backoff + Jitter, Hàng đợi gửi bù ReplayQueue (50 batches). | **HOÀN TẤT** |
| 2 | **Monitoring & Detection** | `research/04_MONITORING_DETECTION.md` | Đặc tả Liveness bằng Monotonic Clock (5s/20s), 4 Quy tắc Sự cố P0 (Streak 3 mẫu & Hysteresis 15%), Máy trạng thái 2 tầng (`ConditionState` vs `WorkflowState`), Xử lý 5 Ca biên. | **HOÀN TẤT** |
| 3 | **Delivery Semantics** | `research/05_DELIVERY_SEMANTICS.md` | Ma trận ngữ nghĩa truyền tải cho 9 nhóm message, Khử trùng lặp Telemetry (`ProcessedBatches`), Idempotency Lệnh Quản trị (`ProcessedCommands`), Thứ tự Commit-Then-Respond. | **HOÀN TẤT** |
| 4 | **Database Architecture** | `research/06_DATABASE.md` | Kịch bản T-SQL DDL hoàn chỉnh cho 10 bảng cốt lõi, Ranh giới Transaction nạp số liệu, Khóa lạc quan OCC bằng `rowversion`, Chế độ suy giảm khi DB lỗi (Degraded Mode). | **HOÀN TẤT** |
| 5 | **CP2 Test Matrix** | `research/CP2_TEST_MATRIX.md` | Ma trận 16 kịch bản kiểm thử toàn diện (SETUP, ACTION, EXPECTED, EVIDENCE, OWNER, VIVA) bao phủ 100% các quyết định CP2. | **HOÀN TẤT** |
| 6 | **CP2 Open Issues** | `research/CP2_OPEN_ISSUES.md` | Bảng đối soát rà soát toàn bộ các mục `[CP2 TO VERIFY]`, xác nhận 100% các điểm kỹ thuật đã được đóng dứt điểm (Resolved & Frozen). | **HOÀN TẤT** |
| 7 | **CP2 Master Decisions** | `CP2_DECISIONS.md` | Sổ ghi nhận 16 quyết định kỹ thuật chính thức (CP2-001 đến CP2-016) và 8 quyết định kiến trúc chi tiết (CP2-ADR-01 đến CP2-ADR-08). | **HOÀN TẤT** |
| 8 | **Biên bản Bàn giao** | `AUDIT_HANDOFF_CHECKPOINT_2.md` | Tài liệu tổng kết, hướng dẫn kiểm toán và chỉ số nghiệm thu dành cho Auditor ChatGPT Work. | **HOÀN TẤT** |
| 9 | **Sổ tay Bỏ túi Nhóm 3 người** | `CP2_CHO_NHOM_3_NGUOI.md` | Hướng dẫn cô đọng (10–15 trang), văn phong sinh viên UIT, phân tách chính xác việc Người A, Người B, Người C cần code những gì. | **HOÀN TẤT** |

---

## 3. THẺ TRA CỨU NHANH CÁC THÔNG SỐ KỸ THUẬT ĐÃ CHỐT (QUICK REFERENCE CARD)

Dành cho Auditor đối chiếu nhanh tính nhất quán giữa các tài liệu:

| Tham số / Cơ chế | Giá trị Cố định (Frozen Value) | Ghi chú & Rationale |
|:---|:---:|:---|
| `MaxFrameSize` | **256 KiB** ($262,144\text{ bytes}$) | 4 byte length header + tối đa $262,140\text{ bytes}$ JSON envelope. |
| `AgentOutboxChannel` | **256 items** | `BoundedChannelFullMode.Wait`, đảm bảo an toàn Single Writer Loop. |
| `CentralIngressChannel` | **1,000 items** | `BoundedChannelFullMode.Wait`, kích hoạt TCP backpressure khi quá tải. |
| `ReplayBufferCapacity` | **50 batches** ($250\text{s}$) | Hàng đợi RAM FIFO Drop-Oldest, gắn cờ `isReplay: true`, không replay Heartbeat. |
| `ExponentialBackoff` | $1\text{s} \rightarrow 2\text{s} \rightarrow 4\text{s} \rightarrow 8\text{s} \rightarrow 16\text{s} \rightarrow 30\text{s}$ | Cộng thêm Uniform Jitter $\pm 10\%$, reset khi nhận login ACK. |
| `HeartbeatInterval` | **5.0 giây** | Chu kỳ Agent gửi gói tin nhịp tim. |
| `OfflineThreshold` | **20.0 giây** | Đo bằng Server Monotonic Clock (`Stopwatch.GetTimestamp()`). |
| `LivenessCheckInterval` | **2.5 giây** | Chu kỳ quét của Server `PeriodicTimer`. |
| `HIGH_CPU` Rule | Bật $\ge 90\%$ / Tắt $\le 75\%$ | Streak = 3 mẫu (khoảng 10s); Hysteresis = $15\%$. |
| `HIGH_MEMORY` Rule | Bật $\ge 85\%$ / Tắt $\le 70\%$ | Streak = 3 mẫu (khoảng 10s); Hysteresis = $15\%$. |
| `SERVICE_DOWN` Rule | Báo lỗi 2 lần fail / Phục hồi 2 lần pass | Local TCP Health Probe chu kỳ 5s, timeout 2s. |
| `TelemetryDedupKey` | `(DeviceId, BootId, BatchId)` | Bảng `ProcessedBatches`, TTL 24h. Trùng lặp thì không chèn DB nhưng gửi lại ACK. |
| `CommandIdempotencyKey` | `(UserId, CommandId)` + SHA-256 Digest | Bảng `ProcessedCommands`, TTL 7 ngày. Trùng ID khác Digest báo `ERR_IDEMPOTENCY_KEY_REUSE`. |
| `ACKEmissionOrder` | **Commit-Then-Respond** | ACK chỉ được phát ra mạng sau khi `SqlTransaction.CommitAsync` thành công. |
| `IncidentStates` | 3 ConditionStates / 5 WorkflowStates | Vật lý (`ACTIVE`, `RECOVERED`, `UNKNOWN`) vs Vận hành (`OPEN`, `ACK`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED`). |
| `OCC Mechanism` | Cột kiểu `rowversion` (`binary(8)`) | T-SQL kiểm tra `RowVersion = @expected`, ném `ERR_CONCURRENCY_VIOLATION` nếu `@@ROWCOUNT == 0`. |
| `DBDegradedMode` | Giữ kết nối TCP | Tiếp nhận Heartbeat trên RAM, đệm tối đa 500 batches vào RAM, trả `ERR_DATABASE_UNAVAILABLE` cho lệnh Admin. |

---

## 4. HƯỚNG DẪN KIỂM TOÁN DÀNH CHO INDEPENDENT AUDITOR

Kính mời Auditor độc lập (ChatGPT Work) kiểm tra hồ sơ Checkpoint 2 theo 5 tiêu chí:
1. **Kiểm tra Tính Đầy đủ (Completeness):** Rà soát xem còn bất kỳ tham số kỹ thuật nào còn bị bỏ lửng hoặc chưa có giá trị cụ thể hay không.
2. **Kiểm tra Tính Nhất quán (Consistency):** Đối chiếu các thông số trên Thẻ Tra cứu Nhanh với nội dung trong các file `03`, `04`, `05`, `06`, `CP2_DECISIONS.md` và `CP2_TEST_MATRIX.md`.
3. **Kiểm tra Tính Sư phạm cho Sinh viên UIT (Pedagogical Clarity):** Đánh giá cấu trúc giải thích (Vấn đề $\rightarrow$ Trực giác $\rightarrow$ Công thức $\rightarrow$ Thuật ngữ $\rightarrow$ Giải pháp LabGuard $\rightarrow$ Viva) có phù hợp với sinh viên năm 2 làm đồ án mạng hay không.
4. **Kiểm tra Khả năng Kiểm thử (Testability):** 16 bài kiểm thử trong `CP2_TEST_MATRIX.md` có đầy đủ tiêu chí nghiệm thu và mã lệnh/công cụ để sinh viên tự kiểm chứng hay không.
5. **Kiểm tra Tính Chắc chắn của Kịch bản Lỗi (Failure Mode Robustness):** Các ca biên về đứt cáp mạng, sập nguồn máy chủ, mất kết nối cơ sở dữ liệu và tranh chấp đồng quy đã được xử lý triệt để hay chưa.

---

**KẾT LUẬN:**  
Toàn bộ tài liệu kỹ thuật của **CHECKPOINT 2 ĐÃ HOÀN TẤT VÀ SẴN SÀNG CHO VÒNG KIỂM TOÁN ĐỘC LẬP.**
