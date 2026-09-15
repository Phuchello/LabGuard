# LABGUARD — NHẬT KÝ THEO DÕI VẤN ĐỀ CHECKPOINT 2 (CP2 OPEN ISSUES & RESOLUTIONS)
**Mã tài liệu:** `research/CP2_OPEN_ISSUES.md`  
**Mục tiêu:** Rà soát và chuẩn hóa toàn bộ các quyết định kỹ thuật từ Checkpoint 1, giải quyết các phát hiện từ vòng kiểm toán độc lập  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Trạng thái:** CHECKPOINT 2 REV2 — FREEZE CANDIDATE (READY FOR FINAL INDEPENDENT AUDIT)  

---

## 1. TỔNG KẾT TÌNH TRẠNG KỸ THUẬT CHECKPOINT 2 (REV2 PATCH)

Sau vòng tái kiểm toán độc lập của Kiểm toán viên, toàn bộ các vấn đề kỹ thuật còn lại của Checkpoint 2 đã được rà soát, điều chỉnh nhất quán và hoàn thiện thành phiên bản ứng viên đóng băng REV2 (**FREEZE CANDIDATE REV2**) sẵn sàng cho vòng nghiệm thu kiểm toán độc lập cuối cùng:
- Khôi phục tính chuẩn mực của danh mục 69 bản tin từ `03_PROTOCOL_SPEC.md`.
- Chuẩn hóa ngữ nghĩa khung truyền: `MaxPayloadSize = 262,144 bytes (256 KiB)`, khung wire tối đa là $262,148\text{ bytes}$ ($4 + 262,144$).
- Chuẩn hóa kiến trúc nạp: **Chính xác ONE Single Logical Ingress Worker** cho MVP P0 (không phân mảnh worker).
- Chuẩn hóa Client Outbox Channel: **Dung lượng 128 items**, Single Writer, không cần strict priority scheduler trong P0.
- Chuẩn hóa quyền sở hữu Telemetry: Agent sở hữu `METRIC_BATCH` trong Unacknowledged Telemetry Queue (`ReplayBuffer`, capacity 50 batches) cho tới khi nhận `BATCH_ACK`. Hàng đợi đầy thực hiện FIFO Drop-Oldest và log `TELEMETRY_DROPPED`.
- Chuẩn hóa ngữ nghĩa `SERVICE_STATUS`: **Latest-state telemetry**, không lưu trong `ReplayBuffer`, không thử lại ở tầng ứng dụng, Server Idempotent UPSERT trên bảng `Services`.
- Chuẩn hóa CSDL MVP: Thêm `UptimeSeconds BIGINT NOT NULL` vào bảng `MetricSamples`; loại trừ Disk.
- Chuẩn hóa ma trận test CP2: Zero Window là bằng chứng mạng tùy chọn bổ trợ; ngưỡng Offline là `> 20.0s and <= 22.5s`; Reconnect storm có tiêu chí đo lường rõ ràng.

---

## 2. BẢNG ĐỐI SOÁT CÁC QUYẾT ĐỊNH KỸ THUẬT ỨNG VIÊN ĐÓNG BĂNG

| Mã Quyết định | Hạng mục ban đầu | Kết quả Rà soát & Đề xuất Đóng băng (Freeze Candidate REV2) | Tài liệu Đặc tả Chi tiết |
|:---:|:---|:---|:---:|
| **CP2-001** | Hàng đợi gửi Agent (`AgentOutboxChannel`) | **Dung lượng = 256 items**, `BoundedChannelFullMode.Wait`. Khi ngắt kết nối: hủy outbox cũ, tạm dừng Heartbeat timer, chuyển `METRIC_BATCH` vào ReplayBuffer. Khi kết nối lại: tạo outbox mới, gửi Heartbeat mới sau khi nhận `AUTH_RESULT(success == true)`. | `research/03_CONCURRENCY_RELIABILITY.md` |
| **CP2-002** | Hàng đợi nhận Central Server (`CentralIngressChannel`) | **Dung lượng = 1,000 items**, `BoundedChannelFullMode.Wait`. Chuẩn hóa **ONE Single Logical Ingress Worker** processes the P0 pipeline (bao gồm cả bản tin trạng thái và truy vấn đọc; không dùng loose Task.Run). | `research/03_CONCURRENCY_RELIABILITY.md` |
| **CP2-003** | Bộ nhớ đệm gửi bù ngoại tuyến (`ReplayBuffer`) | **Dung lượng = 50 batches** (250s dữ liệu), hàng đợi RAM FIFO Drop-Oldest. Gắn cờ `isReplay: true`. Chỉ lưu `METRIC_BATCH`. Tuyệt đối không lưu và không gửi bù `HEARTBEAT` hay `SERVICE_STATUS`. | `research/03_CONCURRENCY_RELIABILITY.md` |
| **CP2-004** | Thuật toán Thử lại (`Exponential Backoff`) | **Truncated Exponential Backoff với Bounded Uniform Jitter $\pm 10\%$**. Chuỗi thời gian: $1\text{s}, 2\text{s}, 4\text{s}, 8\text{s}, 16\text{s}, 30\text{s}$. Reset $k = 0$ sau khi nhận bản tin `AUTH_RESULT` với `success == true`. | `research/03_CONCURRENCY_RELIABILITY.md` |
| **CP2-005** | Ngưỡng Offline Detection & Chu kỳ Heartbeat | **Heartbeat = 5.0s, OfflineThreshold = 20.0s (bằng 4 chu kỳ heartbeat)**. Máy chủ tính toán dựa trên thời gian đơn điệu trôi qua (`Stopwatch.GetTimestamp()`), không dùng bộ đếm số lần lỡ. `CheckInterval = 2.5s` (cửa sổ phát hiện $> 20.0\text{s}$ và $\le 22.5\text{s}$). | `research/04_MONITORING_DETECTION.md` |
| **CP2-006** | Ngưỡng Sự cố CPU/RAM (Streak & Hysteresis) | - `HIGH_CPU`: Bật khi $\ge 90\%$ (3 mẫu liên tiếp); Tắt khi $\le 75\%$ (3 mẫu liên tiếp).<br>- `HIGH_MEMORY`: Bật khi $\ge 85\%$ (3 mẫu); Tắt khi $\le 70\%$ (3 mẫu). Khoảng trễ Hysteresis $15\%$, Cooldown 15s. | `research/04_MONITORING_DETECTION.md` |
| **CP2-007** | Tham số Local TCP Health Probe | Probe chu kỳ 5.0s, Timeout 2.0s.<br>- Báo lỗi `SERVICE_DOWN`: Thất bại 2 lần liên tiếp.<br>- Phục hồi `HEALTHY`: Thành công 2 lần liên tiếp. | `research/04_MONITORING_DETECTION.md` |
| **CP2-008** | Khử trùng lặp Telemetry (`ProcessedBatches`) | **Khóa `(DeviceId, BootId, BatchId)`**. `BootId` là `Guid` sinh một lần cho mỗi tiến trình Agent; `BatchId` là `Guid` v4 sinh cho mỗi batch. Bảng `ProcessedBatches`, TTL 24h. Gặp trùng lặp: không chèn `MetricSamples`, gửi lại `BATCH_ACK`. | `research/05_DELIVERY_SEMANTICS.md` |
| **CP2-009** | Idempotency Lệnh Quản trị (`ProcessedCommands`) | **Khóa `(UserId, CommandId)`** kèm `RequestDigest` băm SHA-256 trên chuỗi trường chuẩn mực cố định. Bảng `ProcessedCommands`, TTL 7 ngày. Trùng ID khác digest báo `ERR_IDEMPOTENCY_KEY_REUSE`. | `research/05_DELIVERY_SEMANTICS.md` |
| **CP2-010** | Thứ tự phát sinh ACK và Transaction Boundary | **Nguyên tắc Commit-Then-Respond**. Gói tin xác nhận (`BATCH_ACK`, `COMMAND_RESULT`) chỉ được gửi ra mạng sau khi `SqlTransaction.CommitAsync` hoàn tất. | `research/05_DELIVERY_SEMANTICS.md` |
| **CP2-011** | CSDL SQL Server & Khóa Lạc quan (OCC) | Lược đồ 10 bảng T-SQL DDL. `MetricSamples` bao gồm `CpuPercent`, `MemoryPercent`, `UptimeSeconds BIGINT NOT NULL` (Disk loại khỏi MVP). `rowversion` là kiểu dữ liệu riêng của SQL Server (8 bytes tự động sinh đơn điệu). | `research/06_DATABASE.md` |
| **CP2-012** | Chế độ Suy giảm khi CSDL Sự cố (Degraded Mode) | **Mô hình Thử lại do Agent quản lý (Agent-Owned Retry Model)**: Central Server giữ kết nối TCP, nhận Heartbeat trên RAM, không gửi ACK cho `METRIC_BATCH` (Agent lưu trong ReplayBuffer), từ chối lệnh Admin với `ERR_DATABASE_UNAVAILABLE`. Không duy trì hàng đợi RAM thứ hai tại Server. | `research/06_DATABASE.md` |
| **CP2-013** | Giới hạn Khung truyền (`MaxFrameSize`) | **`MaxPayloadSize = 262,144 bytes (256 KiB)`**. Chiều dài khung truyền tối đa trên đường truyền là $262,148\text{ bytes}$ (4 byte header độ dài Big-Endian cộng $262,144\text{ bytes}$ payload). | `03_PROTOCOL_SPEC.md` |
| **CP2-014** | Giới hạn Tệp Nhật ký Quay vòng (`RollingLog`) | `MaxFileSize = 10 MB`, `MaxFiles = 5`, tổng dung lượng lưu trữ trên đĩa $\le 50\text{ MB}$. | Guide V3.1.2a |
| **CP2-015** | Ngữ nghĩa Telemetry `SERVICE_STATUS` | **Latest-State Telemetry**: Không lưu trong `ReplayBuffer`, không thử lại tầng ứng dụng. Server thực hiện Idempotent UPSERT trên bảng `Services` theo `(DeviceId, ServiceName)`. | `research/05_DELIVERY_SEMANTICS.md` |
| **CP2-016** | Quyền sở hữu Telemetry & Tràn Hàng đợi | Agent sở hữu `METRIC_BATCH` trong Unacknowledged Telemetry Queue (`ReplayBuffer`, sức chứa 50 batches). Khi đầy, FIFO Drop-Oldest và log sự kiện `TELEMETRY_DROPPED`. | `research/03_CONCURRENCY_RELIABILITY.md` |
| **CP2-017** | Hàng đợi Outbox Client (`ClientOutboxChannel`) | **Dung lượng = 128 items**, Single Writer Loop, `BoundedChannelFullMode.Wait`. Không yêu cầu strict priority scheduler trong P0. | `research/03_CONCURRENCY_RELIABILITY.md` |
| **CP2-018** | Phục hồi Sự cố `DEVICE_OFFLINE` Bắt buộc Xác thực | Sự cố `DEVICE_OFFLINE` chỉ phục hồi sau khi kết nối Agent mới hoàn tất `HELLO_AGENT` $\rightarrow$ `HELLO_ACK`, `AUTH_AGENT` $\rightarrow$ `AUTH_RESULT(success=true)`, sau đó nhận frame/`HEARTBEAT` hợp lệ đầu tiên. `HELLO_AGENT` đơn lẻ không phục hồi. | `research/04_MONITORING_DETECTION.md` |

---

## 3. CÁC HẠNG MỤC PHI KỸ THUẬT THEO DÕI

1. **Trao đổi Rubric/Barem chấm điểm chi tiết:** Sinh viên C chủ động gặp Giảng viên hướng dẫn trong giờ thực hành Tuần 1.
2. **Phương thức Demo mạng:** Ưu tiên mạng LAN cục bộ phòng lab (2–3 laptop thật) kèm phương án dự phòng qua VPN Overlay (WireGuard/Tailscale).
