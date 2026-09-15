# LABGUARD — SỔ GHI NHẬN QUYẾT ĐỊNH KỸ THUẬT CHECKPOINT 2 (REV2)
**Mã tài liệu:** `CP2_DECISIONS_REV2.md`  
**Chuyên đề:** Bộ Quyết định Kỹ thuật Ứng viên Đóng băng Trước Khi Viết Mã Nguồn (Pre-Coding Freeze Candidate Decisions — REV2 Final Consistency Patch)  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Trạng thái:** CHECKPOINT 2 REV2 — FREEZE CANDIDATE (READY FOR FINAL INDEPENDENT AUDIT)  
**Tài liệu tham chiếu:**  
- `03_PROTOCOL_SPEC.md` (Baseline 69 bản tin chuẩn)  
- `research/03_CONCURRENCY_RELIABILITY.md`  
- `research/04_MONITORING_DETECTION.md`  
- `research/05_DELIVERY_SEMANTICS.md`  
- `research/06_DATABASE.md`  
- `research/CP2_TEST_MATRIX.md`  
- `research/CP2_OPEN_ISSUES.md`  

---

## 1. BẢNG TỔNG HỢP CÁC QUYẾT ĐỊNH KỸ THUẬT CP2 (MASTER DECISION MATRIX)

| Mã Quyết định | Hạng mục kỹ thuật | Quyết định Kỹ thuật Ứng viên Đóng băng (Freeze Candidate REV2) | Giá trị tham số cụ thể | Phụ trách chính |
|:---:|:---|:---|:---:|:---:|
| **CP2-001** | Hàng đợi gửi Agent (`AgentOutboxChannel`) | Single Writer Channel, `BoundedChannelFullMode.Wait`. Khi ngắt kết nối: hủy outbox, tạm dừng Heartbeat timer, đưa telemetry vào ReplayBuffer. | **256 items** | **Người A** |
| **CP2-002** | Hàng đợi nhận Server (`CentralIngressChannel`) | Central Ingress Channel, `BoundedChannelFullMode.Wait`. **ONE Single Logical Ingress Worker** processes the P0 pipeline. Read-only queries are also handled through this controlled worker. No loose Task.Run/fire-and-forget dispatch is required in P0. | **1,000 items** | **Người B** |
| **CP2-003** | Bộ nhớ đệm gửi bù Agent (`ReplayBuffer`) | Hàng đợi RAM FIFO Drop-Oldest, gắn `isReplay: true`. Chỉ lưu `METRIC_BATCH`. Tuyệt đối không lưu `HEARTBEAT` hay `SERVICE_STATUS`. Khi tràn hàng đợi: FIFO Drop-Oldest và log `TELEMETRY_DROPPED`. | **50 batches** (250s) | **Người A** |
| **CP2-004** | Thuật toán Thử lại (`Exponential Backoff`) | Truncated Exponential Backoff kèm Bounded Uniform Jitter $\pm 10\%$. Reset $k = 0$ sau `AUTH_RESULT(success == true)`. | $1\text{s}, 2\text{s}, 4\text{s}, 8\text{s}, 16\text{s}$, trần $30\text{s}$ | **Người A** |
| **CP2-005** | Giám sát sống chết (`Liveness & Timeout`) | Gói tin `HEARTBEAT` + Server Monotonic Clock (`Stopwatch.GetTimestamp()`). Elapsed time calculation, không dùng missed-counter. Cửa sổ phát hiện $> 20.0\text{s}$ và $\le 22.5\text{s}$ với chu kỳ quét 2.5s. | Heartbeat = **5.0s**, Offline = **20.0s** (4 chu kỳ) | **Người B / A** |
| **CP2-006** | Quy tắc CPU (`HIGH_CPU`) | Streak 3 mẫu liên tiếp (khoảng 10s); Hysteresis delta $15\%$, Cooldown 15s. | Bật $\ge 90\%$; Phục hồi $\le 75\%$ | **Người B** |
| **CP2-007** | Quy tắc Bộ nhớ (`HIGH_MEMORY`) | Streak 3 mẫu liên tiếp (khoảng 10s); Hysteresis delta $15\%$, Cooldown 15s. | Bật $\ge 85\%$; Phục hồi $\le 70\%$ | **Người B** |
| **CP2-008** | Quy tắc Dịch vụ Mạng (`SERVICE_DOWN`) | Local TCP Health Probe định kỳ; Timeout 2.0s. | Báo lỗi: 2 fail; Phục hồi: 2 pass | **Người A / B** |
| **CP2-009** | Khử trùng lặp Telemetry (`Dedup Engine`) | Khóa bộ ba lưu trong bảng `ProcessedBatches`. `BootId` = `Guid`, `BatchId` = `Guid` (v4). | `(DeviceId, BootId, BatchId)`, TTL 24h | **Người B** |
| **CP2-010** | Idempotency Lệnh Quản trị (`Admin Commands`) | Khóa lưu bảng `ProcessedCommands` kèm SHA-256 `RequestDigest` trên các trường cố định. | `(UserId, CommandId)` + SHA-256 Digest | **Người B / C** |
| **CP2-011** | Thứ tự phát sinh xác nhận (`ACK Timing`) | Nguyên tắc Cam kết trước - Phản hồi sau (Commit-Then-Respond). | Phản hồi sau `SqlTransaction.CommitAsync` | **Người B** |
| **CP2-012** | Máy Trạng thái Sự cố Phân tách (`Dual-Layer FSM`) | Hai tầng độc lập: Kỹ thuật (`ConditionState`) & Vận hành (`WorkflowState`). | 3 ConditionStates & 5 WorkflowStates | **Người B / C** |
| **CP2-013** | Cơ chế Khóa Lạc quan (`OCC`) | Kiểm tra token phiên bản tự động của SQL Server Engine. | Cột kiểu `rowversion` (8-byte auto-generated) | **Người B / C** |
| **CP2-014** | Chế độ Suy giảm khi DB lỗi (`Degraded Mode`) | **Mô hình Thử lại do Agent quản lý**: Giữ TCP socket, nhận Heartbeat trên RAM, không gửi ACK cho `METRIC_BATCH` (Agent lưu trong ReplayBuffer), trả `ERR_DATABASE_UNAVAILABLE` cho lệnh Admin. | Không đệm hàng đợi RAM thứ hai tại Server | **Người B** |
| **CP2-015** | Giới hạn Khung truyền (`MaxFrameSize`) | 4-byte UInt32 Big-Endian Length + JSON Payload UTF-8. | **MaxPayloadSize = 262,144 bytes (256 KiB)**, Wire Max = 262,148 bytes | **Người A / B / C** |
| **CP2-016** | Giới hạn Tệp Nhật ký Quay vòng (`Rolling Log`) | Tự động quay vòng file khi chạm ngưỡng dung lượng. | MaxFileSize = **10 MB**, MaxFiles = **5** | **Người A** |
| **CP2-017** | Lược đồ CSDL MetricSamples & Uptime | Bảng `MetricSamples` bao gồm `CpuPercent`, `MemoryPercent`, và `UptimeSeconds BIGINT NOT NULL`. Chỉ số Disk loại khỏi MVP. Hàng đợi outbox Client dung lượng 128 items, Single Writer. | `UptimeSeconds BIGINT NOT NULL` (Disk excluded) | **Người B** |
| **CP2-018** | Ngữ nghĩa Telemetry `SERVICE_STATUS` | Latest-State Telemetry, không lưu trong `ReplayBuffer`, không thử lại tầng ứng dụng. Server Idempotent UPSERT trên bảng `Services` theo `(DeviceId, ServiceName)`. | Idempotent UPSERT by `(DeviceId, ServiceName)` | **Người A / B** |

---

## 2. CÁC QUYẾT ĐỊNH KIẾN TRÚC CHI TIẾT (CP2 ARCHITECTURE DECISIONS)

### CP2-ADR-01: Mô hình Đa luồng Bất đồng bộ & Tuần tự hóa theo Thiết bị
- **Bối cảnh:** Việc ghi đồng thời vào `NetworkStream` sẽ gây xé khung (Frame Interleaving). Đọc/ghi đồng bộ chặn luồng làm tê liệt ThreadPool. Hơn nữa, việc cho phép nhiều worker xử lý song song các mẻ đo từ cùng một thiết bị có thể làm đảo lộn thứ tự mẫu, phá vỡ logic chuỗi liên tiếp (Streak).
- **Quyết định:**
  - **Phía Agent:** Áp dụng mô hình **Single Writer Loop**: Toàn bộ tác vụ gửi tin đẩy dữ liệu vào `AgentOutboxChannel` (`Channel<byte[]>` dung lượng 256, `BoundedChannelFullMode.Wait`). Một Background Task duy nhất đọc từ channel và ghi tuần tự vào `NetworkStream`. Khi mất kết nối: hủy outbox cũ, tạm dừng timer Heartbeat, chuyển `METRIC_BATCH` vào ReplayBuffer.
  - **Phía Central Server:** Mỗi kết nối có một Reader Task chuyên trách. Toàn bộ frame đọc được đẩy vào `CentralIngressChannel` (`Channel<IncomingEnvelope>` dung lượng 1,000, `BoundedChannelFullMode.Wait`).
  - **Tuần tự hóa Xử lý theo Thiết bị (Per-Device Ordering):** Sử dụng **chính xác ONE Single Logical Ingress Worker** processes the P0 pipeline. Toàn bộ bản tin P0 (bao gồm cả các bản tin đột biến trạng thái và các truy vấn đọc) đều được xử lý qua worker có kiểm soát này. Read-only queries are also handled through this controlled worker. No loose Task.Run/fire-and-forget dispatch is required in P0. Kiến trúc phân vùng worker song song chỉ là giải pháp tối ưu cho tương lai (FUTURE optimization only).
  - Toàn bộ thao tác I/O sử dụng `await ...Async()`. Tuyệt đối cấm `.Result`, `.Wait()`, `Thread.Sleep()`.
- **Hệ quả:** Ngăn ngừa tranh chấp ghi trên socket, bảo toàn thứ tự chuỗi mẫu đo đạc theo từng máy trạm và kích hoạt cơ chế Backpressure tự nhiên khi có quá tải.

### CP2-ADR-02: Thuật toán Tự phục hồi Kết nối & Exponential Backoff với Jitter
- **Bối cảnh:** Khi Central Server khởi động lại, nếu hàng chục máy trạm đồng thời kết nối lại cùng một giây sẽ gây ra hiện tượng Bão kết nối lại (Reconnect Storm).
- **Quyết định:**
  - Áp dụng thuật toán **Truncated Exponential Backoff với Bounded Uniform Jitter $\pm 10\%$**:
    $$\text{Delay}_k = \min(30.0, 1.0 \times 2^k) \times \left(1 + \text{UniformRandom}[-0.10, +0.10]\right)$$
  - Chuỗi thời gian chờ cơ sở: $1\text{s} \rightarrow 2\text{s} \rightarrow 4\text{s} \rightarrow 8\text{s} \rightarrow 16\text{s} \rightarrow 30\text{s}$ (Trần tối đa).
  - Bộ đếm $k$ chỉ được reset về 0 khi và chỉ khi Agent nhận được bản tin `AUTH_RESULT` với `success == true` từ Server.
- **Hệ quả:** Phân tán thời điểm kết nối lại của các máy trạm rải rác trên trục thời gian, bảo vệ hàng đợi kết nối của Server khi vừa khởi động.

### CP2-ADR-03: Đồng hồ Đơn điệu (Monotonic Clock) cho Liveness & Offline Detection
- **Bối cảnh:** `DateTime.UtcNow` trên máy chủ có thể bị nhảy giờ do NTP. Giờ của máy trạm Client có thể bị sinh viên chỉnh sai lệch.
- **Quyết định:**
  - Agent định kỳ gửi `HEARTBEAT` mỗi **5.0 giây**.
  - Central Server đánh giá liveness hoàn toàn dựa trên **Đồng hồ đơn điệu của Server (`Stopwatch.GetTimestamp()`)**, tuyệt đối không dùng timestamp do client gửi lên để tính thời gian quá hạn.
  - Thiết bị bị phán quyết là `OFFLINE` khi khoảng cách thời gian vượt quá **20.0 giây** (bằng đúng 4 chu kỳ heartbeat). Máy chủ tính toán dựa trên thời gian đơn điệu trôi qua (elapsed monotonic time), không dùng bộ đếm số lần lỡ (not a missed-counter).
  - Chu kỳ quét liveness là **2.5 giây** (`CheckInterval = 2.5s`), do đó cửa sổ phát hiện `OFFLINE` thực tế xảy ra tại lần quét kế tiếp khi thời gian trôi qua $> 20.0\text{s}$ (xấp xỉ $\le 22.5\text{s}$). Bằng chứng nghiệm thu ghi nhận đầy đủ: `lastSeen`, `scanTimestamp`, `elapsed`, và `decision = DEVICE_OFFLINE`.
  - Phân định kết nối rác bằng bộ bốn: `(DeviceId, SessionId, BootId, ConnectionGeneration)`. Socket cũ lập tức bị đóng khi có thế hệ kết nối mới hơn từ cùng một máy trạm; frame mang thế hệ cũ bị hủy bỏ tại Dispatcher.
  - **Nguyên tắc Phục hồi Sự cố `DEVICE_OFFLINE` Bắt buộc Xác thực:** Sự cố `DEVICE_OFFLINE` chỉ được phục hồi sau khi kết nối Agent mới hoàn tất bắt tay và xác thực thành công (`HELLO_AGENT` $\rightarrow$ `HELLO_ACK`, `AUTH_AGENT` $\rightarrow$ `AUTH_RESULT(success=true)`), sau đó nhận được frame/`HEARTBEAT` đã xác thực hợp lệ đầu tiên để cập nhật liveness. `HELLO_AGENT` đơn lẻ khi chưa xác thực tuyệt đối KHÔNG phục hồi `DEVICE_OFFLINE`.
- **Hệ quả:** Loại bỏ hoàn toàn các lỗi phán đoán sống/chết sai do đồng hồ hệ thống thay đổi, giải phóng kịp thời các kết nối nửa mở (Half-Open Sockets).

### CP2-ADR-04: Ngưỡng Đánh giá Sự cố P0, Kỹ thuật Streak & Khoảng trễ Hysteresis
- **Bối cảnh:** Dữ liệu đo đạc hiệu năng máy tính thường có các đột biến nhất thời hoặc dao động nhỏ quanh ngưỡng (Flapping), gây ra bão báo động làm phiền người vận hành.
- **Quyết định:**
  - Mọi quy tắc giám sát P0 đều áp dụng kỹ thuật **Chuỗi mẫu liên tiếp (Streak)** kết hợp **Khoảng trễ (Hysteresis)**:
    - `HIGH_CPU`: Mở khi $\text{CPU} \ge 90\%$ trong 3 mẫu liên tiếp (cửa sổ 10 giây); Tự động phục hồi khi $\text{CPU} \le 75\%$ trong 3 mẫu liên tiếp (Khoảng trễ an toàn $15\%$).
    - `HIGH_MEMORY`: Mở khi $\text{RAM} \ge 85\%$ trong 3 mẫu liên tiếp; Tự động phục hồi khi $\text{RAM} \le 70\%$ trong 3 mẫu liên tiếp (Khoảng trễ an toàn $15\%$).
    - `SERVICE_DOWN`: Báo lỗi khi Local TCP Probe thất bại 2 lần liên tiếp; Phục hồi khi Probe thành công 2 lần liên tiếp.
    - Thời gian làm nguội (Cooldown): Tối thiểu 15 giây giữa hai lần đổi trạng thái sự cố.
- **Hệ quả:** Ngăn chặn báo động ảo tức thời, giữ vững tính ổn định của bảng điều khiển WinForms.

### CP2-ADR-05: Mô hình Máy Trạng thái Sự cố 2 Tầng (Dual-Layer Incident FSM)
- **Bối cảnh:** Trạng thái điều kiện kỹ thuật của máy móc và tiến trình can thiệp của con người là hai chiều thực tế độc lập, không thể gộp chung vào một cờ nhị phân đơn giản.
- **Quyết định:**
  - Phân tách thành hai tầng trạng thái độc lập trong bảng `Incidents`:
    - **Tầng 1 — Trạng thái Điều kiện Kỹ thuật (`ConditionState`):** `ACTIVE`, `RECOVERED`, `UNKNOWN`. Do Server Rule Engine tự động quản lý dựa trên dữ liệu mạng.
    - **Tầng 2 — Trạng thái Quy trình Vận hành (`WorkflowState`):** `OPEN`, `ACKNOWLEDGED`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED`. Do Quản trị viên điều khiển qua WinForms bằng các lệnh chuẩn: `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`.
  - Chuẩn hóa xử lý 5 ca biên: Tự phục hồi khi đang OPEN; Tái phát trước khi RESOLVE; Chặn Admin đóng phiếu khi máy vẫn đang lỗi (`ERR_CONDITION_STILL_ACTIVE`); Tranh chấp 2 Admin bằng OCC; và Đánh dấu `UNKNOWN` khi máy bị mất mạng (`DEVICE_OFFLINE`).
- **Hệ quả:** Hệ thống phản ánh chính xác hiện trạng thực tế, ngăn ngừa sai sót vận hành.

### CP2-ADR-06: Khử trùng lặp Telemetry & Idempotency Lệnh Quản trị
- **Bối cảnh:** Mạng chập chờn khiến gói tin ACK bị mất, buộc Agent hoặc Admin Client phải gửi lại (Retry). Nếu Server ghi nhận lại sẽ gây trùng lặp dữ liệu và sai lệch lịch sử.
- **Quyết định:**
  - **Quyền Sở hữu Telemetry (`METRIC_BATCH`):** Agent giữ quyền sở hữu dữ liệu trong Unacknowledged Telemetry Queue (`ReplayBuffer`, sức chứa 50 batches) cho tới khi nhận được `BATCH_ACK` có `batchId` tương ứng. Khi hàng đợi vượt quá 50 batches, chính sách FIFO Drop-Oldest loại bỏ mẻ cũ nhất và bắt buộc ghi log sự kiện `TELEMETRY_DROPPED`.
  - **Khử trùng lặp Telemetry:** Khóa bộ ba `(DeviceId, BootId, BatchId)` lưu trong bảng `ProcessedBatches` (TTL 24h). `BootId` là kiểu `Guid` sinh một lần cho mỗi tiến trình Agent; `BatchId` là kiểu `Guid` sinh mới cho mỗi mẻ đo. Khi gặp trùng lặp: bỏ qua ghi dữ liệu nhưng **bắt buộc gửi lại `BATCH_ACK` ngay lập tức**.
  - **Ngữ nghĩa `SERVICE_STATUS`:** Là dữ liệu trạng thái mới nhất (**Latest-State Telemetry**), **KHÔNG** đưa vào `ReplayBuffer`, không thử lại tầng ứng dụng. Server thực hiện Idempotent UPSERT trên bảng `Services` theo `(DeviceId, ServiceName)`.
  - **Idempotency Lệnh Quản trị:** Khóa `(UserId, CommandId)` kèm chuỗi băm SHA-256 `RequestDigest` trên các trường chuẩn mực cố định lưu trong `ProcessedCommands` (TTL 7 ngày). Trùng ID khác Digest sẽ bị từ chối với lỗi `ERR_IDEMPOTENCY_KEY_REUSE`.
  - **Nguyên tắc phát sinh ACK:** Tuân thủ nghiêm ngặt **Commit-Then-Respond** (chỉ gửi ACK sau khi transaction SQL Server đã commit thành công).
- **Hệ quả:** Đạt được hiệu ứng lưu trữ nhất quán có thể quan sát tương đương đúng-một-lần cho các tác vụ nghiệp vụ có cơ chế khử trùng lặp.

### CP2-ADR-07: Cơ sở Dữ liệu SQL Server 2022 Express & Khóa Lạc quan (OCC)
- **Bối cảnh:** Nhiều quản trị viên có thể cùng xem và thao tác trên cùng một sự cố, nguy cơ ghi đè dữ liệu của nhau (Lost Update).
- **Quyết định:**
  - Sử dụng hệ quản trị CSDL chuẩn **Microsoft SQL Server 2022 Express** với 10 bảng nghiệp vụ đã chuẩn hóa DDL. Bảng `MetricSamples` bao gồm các cột `CpuPercent`, `MemoryPercent`, và `UptimeSeconds BIGINT NOT NULL` (Chỉ số Disk hoàn toàn loại bỏ khỏi MVP P0 theo đúng baseline Checkpoint 1).
  - Áp dụng cơ chế **Optimistic Concurrency Control (OCC)** thông qua cột kiểu `RowVersion` (`rowversion` trong SQL Server, kích thước 8 bytes tự động sinh giá trị nhị phân mới khi INSERT/UPDATE, không phải kiểu binary(8) tĩnh, không phải ngày giờ).
  - Lệnh cập nhật kiểm tra phiên bản dự kiến:
    ```sql
    UPDATE Incidents SET WorkflowState = @state WHERE IncidentId = @id AND RowVersion = @expectedVersion;
    ```
    Nếu `@@ROWCOUNT == 0`, Server từ chối và phản hồi bản tin `COMMAND_RESULT` (Status: 409 Conflict) với mã lỗi `ERR_CONCURRENCY_VIOLATION`.
- **Hệ quả:** Xử lý triệt để bài toán đồng quy đa Admin mà không cần khóa bảng lâu dài, tối ưu tốc độ đọc/ghi.

### CP2-ADR-08: Chế độ Hoạt động Suy giảm khi Cơ sở Dữ liệu Sự cố (Degraded Mode)
- **Bối cảnh:** Dịch vụ SQL Server bị dừng đột ngột trong lúc hệ thống đang vận hành.
- **Quyết định:**
  - **Mô hình Thử lại do Agent quản lý (Agent-Owned Retry Model):**
    - Central Server không crash; giữ nguyên toàn bộ các kết nối socket TCP với Agent và Admin.
    - Tiếp tục duy trì tiếp nhận `HEARTBEAT` trên RAM để cập nhật trạng thái sống/chết tức thời.
    - Central Server **không lưu và không gửi `BATCH_ACK`** cho `METRIC_BATCH`. Agent là bên sở hữu duy nhất của dữ liệu chưa commit thông qua `ReplayBuffer`. Server không duy trì thêm hàng đợi RAM thứ hai để tránh rủi ro trùng lặp bản tin retry và tràn bộ nhớ.
    - Từ chối các lệnh Admin đòi hỏi ghi CSDL với mã lỗi `ERR_DATABASE_UNAVAILABLE`.
    - Phát sóng `HEALTH_UPDATE` thông báo trạng thái `DEGRADED`. Khi CSDL phục hồi, Agent timeout sẽ tự động gửi lại dữ liệu và Server commit bình thường.
- **Hệ quả:** Thiết kế rõ ràng, xác định, tránh tranh chấp dữ liệu giữa hai hàng đợi độc lập.

### CP2-ADR-09: Giới hạn Kích thước Khung truyền (Frame Size Semantics)
- **Bối cảnh:** Cần phân định rõ ràng giữa giới hạn kích thước payload JSON và tổng kích thước khung truyền trên dây mạng.
- **Quyết định:**
  - `MaxPayloadSize = 262,144 bytes (256 KiB)`.
  - Header khung truyền gồm 4 byte số nguyên không dấu Big-Endian biểu diễn độ dài payload $N$ ($0 < N \le 262,144$).
  - Chiều dài khung truyền tối đa trên đường truyền mạng (Wire Frame Maximum) là $4 + 262,144 = 262,148\text{ bytes}$.
- **Hệ quả:** Đồng bộ nhất quán với hợp đồng kiểm thử và tài liệu Checkpoint 1.

### CP2-ADR-010: Cấu hình Outbox Client Admin (`ClientOutboxChannel`)
- **Bối cảnh:** Admin WinForms Client gửi bản tin đăng nhập, truy vấn dữ liệu và lệnh điều khiển sự cố qua socket TCP.
- **Quyết định:**
  - `ClientOutboxChannel` có dung lượng **128 items**, áp dụng mô hình Single Writer Loop, `BoundedChannelFullMode.Wait`.
  - Đối với phạm vi MVP P0, không bắt buộc cài đặt bộ lập lịch ưu tiên khắt khe (strict priority scheduler); cấu trúc FIFO bounded đảm bảo tránh xé khung và an toàn tải.
- **Hệ quả:** Đơn giản hóa kiến trúc phía client trong khi vẫn bảo đảm tính an toàn đa luồng trên socket.
