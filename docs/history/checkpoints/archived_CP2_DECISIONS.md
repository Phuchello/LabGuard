> **SUPERSEDED / HISTORICAL ARTIFACT**  
> This document has been superseded and preserved for historical audit trail only.  
> Current authoritative candidate: [CP2_DECISIONS_REV2.md](../CP2_DECISIONS_REV2.md) / [AUDIT_HANDOFF_CHECKPOINT_2_REV2.md](../AUDIT_HANDOFF_CHECKPOINT_2_REV2.md).

---

# LABGUARD — SỔ GHI NHẬN QUYẾT ĐỊNH KỸ THUẬT CHECKPOINT 2 (CP2 DECISIONS)
**Mã tài liệu:** `CP2_DECISIONS.md`  
**Chuyên đề:** Bộ Quyết định Kỹ thuật Chính thức Trước Khi Viết Mã Nguồn (Pre-Coding Frozen Technical Decisions)  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Trạng thái:** CHECKPOINT 2 — ACCEPTED & FROZEN  
**Tài liệu nền tảng bổ trợ:**  
- `research/03_CONCURRENCY_RELIABILITY.md`  
- `research/04_MONITORING_DETECTION.md`  
- `research/05_DELIVERY_SEMANTICS.md`  
- `research/06_DATABASE.md`  
- `research/CP2_TEST_MATRIX.md`  
- `research/CP2_OPEN_ISSUES.md`  

---

## 1. BẢNG TỔNG HỢP CÁC QUYẾT ĐỊNH KỸ THUẬT CHECKPOINT 2 (MASTER DECISION MATRIX)

| Mã Quyết định | Hạng mục kỹ thuật | Quyết định Chính thức (Frozen Decision) | Giá trị tham số cụ thể | Phụ trách chính |
|:---:|:---|:---|:---:|:---:|
| **CP2-001** | Hàng đợi gửi Agent (`AgentOutboxChannel`) | Single Writer Channel, `BoundedChannelFullMode.Wait` | **256 items** | **Người A** |
| **CP2-002** | Hàng đợi nhận Server (`CentralIngressChannel`) | Central Ingress Channel, `BoundedChannelFullMode.Wait` | **1,000 items** | **Người B** |
| **CP2-003** | Bộ nhớ đệm gửi bù Agent (`ReplayBuffer`) | Hàng đợi RAM FIFO Drop-Oldest, gắn `isReplay: true` | **50 batches** (250s) | **Người A** |
| **CP2-004** | Thuật toán Thử lại (`Exponential Backoff`) | Truncated Exponential Backoff kèm Uniform Jitter $\pm 10\%$ | $1\text{s}, 2\text{s}, 4\text{s}, 8\text{s}, 16\text{s}$, trần $30\text{s}$ | **Người A** |
| **CP2-005** | Giám sát sống chết (`Liveness & Timeout`) | Ứng dụng Heartbeat + Server Monotonic Clock (`Stopwatch`) | Heartbeat = **5.0s**, Offline = **20.0s** | **Người B / A** |
| **CP2-006** | Quy tắc CPU (`HIGH_CPU`) | Streak 3 mẫu (khoảng 10s); Hysteresis delta $15\%$ | Kích hoạt $\ge 90\%$; Phục hồi $\le 75\%$ | **Người B** |
| **CP2-007** | Quy tắc Bộ nhớ (`HIGH_MEMORY`) | Streak 3 mẫu (khoảng 10s); Hysteresis delta $15\%$ | Kích hoạt $\ge 85\%$; Phục hồi $\le 70\%$ | **Người B** |
| **CP2-008** | Quy tắc Dịch vụ Mạng (`SERVICE_DOWN`) | Local TCP Health Probe định kỳ; Timeout 2.0s | Báo lỗi: 2 fail; Phục hồi: 2 pass | **Người A / B** |
| **CP2-009** | Khử trùng lặp Telemetry (`Dedup Engine`) | Khóa bộ ba lưu cố định trong bảng `ProcessedBatches` | `(DeviceId, BootId, BatchId)`, TTL 24h | **Người B** |
| **CP2-010** | Idempotency Lệnh Quản trị (`Admin Commands`) | Khóa lưu bảng `ProcessedCommands` kèm SHA-256 Digest | `(UserId, CommandId)` + `RequestDigest` | **Người B / C** |
| **CP2-011** | Thứ tự phát sinh gói tin xác nhận (`ACK Timing`) | Nguyên tắc Cam kết trước - Phản hồi sau (Commit-Then-Respond) | ACK chỉ phát sau `SqlTransaction.Commit` | **Người B** |
| **CP2-012** | Máy Trạng thái Sự cố Phân tách (`Dual-Layer FSM`) | Hai tầng độc lập: Vật lý (`ConditionState`) & Vận hành (`Workflow`) | 3 ConditionStates & 5 WorkflowStates | **Người B / C** |
| **CP2-013** | Cơ chế Khóa Lạc quan (`OCC`) | Kiểm tra token phiên bản tự động của SQL Server Engine | Cột kiểu `rowversion` (`binary(8)`) | **Người B / C** |
| **CP2-014** | Chế độ Suy giảm khi DB lỗi (`Degradation Mode`) | Giữ kết nối TCP, nhận Heartbeat trên RAM, đệm RAM | Đệm tối đa **500 batches**, báo `DEGRADED` | **Người B** |
| **CP2-015** | Giới hạn Khung truyền (`MaxFrameSize`) | 4-byte uint32 Big-Endian Length + JSON Payload UTF-8 | **262,144 bytes (256 KiB)** | **Người A / B / C** |
| **CP2-016** | Giới hạn Tệp Nhật ký Quay vòng (`Rolling Log`) | Tự động đổi file khi chạm ngưỡng dung lượng | MaxFileSize = **10 MB**, MaxFiles = **5** | **Người A** |

---

## 2. CÁC QUYẾT ĐỊNH THIẾT KẾ CHI TIẾT (CP2 ARCHITECTURE DECISIONS)

### CP2-ADR-01: Mô hình Đa luồng Bất đồng bộ & Hàng đợi Kênh (Channel Concurrency)
- **Bối cảnh:** Việc ghi đồng thời vào `NetworkStream` sẽ gây hiện tượng xé khung (Frame Interleaving). Đọc/ghi đồng bộ chặn luồng làm tê liệt ThreadPool.
- **Quyết định:**
  - Áp dụng mô hình **Single Writer Loop** trên mỗi kết nối Socket:
    - Phía Agent: Toàn bộ tác vụ gửi tin đẩy dữ liệu vào `AgentOutboxChannel` (`Channel<byte[]>` dung lượng 256). Một Background Task duy nhất chịu trách nhiệm đọc từ channel và ghi tuần tự vào `NetworkStream`.
    - Phía Central Server: Mỗi kết nối client có một Reader Task chuyên trách. Toàn bộ frame đọc được đẩy vào `CentralIngressChannel` (`Channel<IncomingEnvelope>` dung lượng 1,000) để Dispatcher phân phối cho Worker Pool.
  - Toàn bộ thao tác I/O sử dụng `async/await` không chặn (Non-blocking I/O). Tuyệt đối cấm `.Result`, `.Wait()`, `Thread.Sleep()`.
- **Hệ quả:** Đảm bảo an toàn luồng tuyệt đối, triệt tiêu race condition trên socket, tối ưu hiệu năng CPU và kích hoạt cơ chế Backpressure tự nhiên khi có quá tải.

### CP2-ADR-02: Thuật toán Tự phục hồi Kết nối & Exponential Backoff với Jitter
- **Bối cảnh:** Khi Central Server khởi động lại, nếu hàng chục máy trạm đồng thời kết nối lại cùng một giây sẽ gây ra hiện tượng Bão kết nối lại (Reconnect Storm / Thundering Herd Problem).
- **Quyết định:**
  - Áp dụng thuật toán **Truncated Exponential Backoff với Uniform Jitter**:
    $$\text{Delay}_k = \min(30.0, 1.0 \times 2^k) \times (1 + \text{UniformRandom}[-10\%, +10\%])$$
  - Chuỗi thời gian chờ cơ sở: $1\text{s} \rightarrow 2\text{s} \rightarrow 4\text{s} \rightarrow 8\text{s} \rightarrow 16\text{s} \rightarrow 30\text{s}$ (Trần tối đa).
  - Bộ đếm $k$ chỉ được reset về 0 khi và chỉ khi Agent nhận được bản tin xác nhận đăng nhập (`LOGIN_RESPONSE`) hoặc `BATCH_ACK` đầu tiên từ Server.
- **Hệ quả:** Phân tán thời điểm kết nối lại của các máy trạm rải rác trên trục thời gian, bảo vệ cổng dịch vụ của Server an toàn tuyệt đối khi vừa khởi động.

### CP2-ADR-03: Đồng hồ Đơn điệu (Monotonic Clock) cho Liveness & Offline Detection
- **Bối cảnh:** `DateTime.UtcNow` trên máy chủ có thể bị nhảy lùi/tiến do đồng bộ giờ mạng NTP. Giờ của máy trạm Client có thể bị sinh viên điều chỉnh sai lệch.
- **Quyết định:**
  - Agent định kỳ gửi `HEARTBEAT` mỗi **5.0 giây**.
  - Central Server đánh giá liveness hoàn toàn dựa trên **Đồng hồ đơn điệu của Server (`Stopwatch.GetTimestamp()`)**, tuyệt đối không dùng timestamp do client gửi lên để tính thời gian quá hạn.
  - Thiết bị bị phán quyết là `OFFLINE` khi khoảng cách thời gian vượt quá **20.0 giây** (tương đương 3 chu kỳ mất tin liên tiếp cộng 5 giây độ trễ mạng cho phép).
  - Phân định kết nối rác bằng bộ bốn: `(DeviceId, SessionId, BootId, ConnectionGeneration)`. Socket cũ lập tức bị đóng khi có thế hệ kết nối mới hơn từ cùng một máy trạm.
- **Hệ quả:** Loại bỏ hoàn toàn các lỗi phán đoán sống/chết sai do đồng hồ hệ thống thay đổi, giải phóng kịp thời các kết nối nửa mở (Half-Open Sockets).

### CP2-ADR-04: Ngưỡng Đánh giá Sự cố P0, Kỹ thuật Streak & Khoảng trễ Hysteresis
- **Bối cảnh:** Dữ liệu đo đạc hiệu năng máy tính thường có các gai tải đột biến (Spikes) nhất thời hoặc dao động nhỏ quanh ngưỡng (Flapping), gây ra bão báo động ảo làm phiền người vận hành.
- **Quyết định:**
  - Mọi quy tắc giám sát P0 đều áp dụng kỹ thuật **Chuỗi mẫu liên tiếp (Streak)** kết hợp **Khoảng trễ trôi dạt (Hysteresis)**:
    - `HIGH_CPU`: Mở khi $\text{CPU} \ge 90\%$ trong 3 mẫu liên tiếp (cửa sổ 10 giây); Tự động phục hồi khi $\text{CPU} \le 75\%$ trong 3 mẫu liên tiếp (Khoảng trễ an toàn $15\%$).
    - `HIGH_MEMORY`: Mở khi $\text{RAM} \ge 85\%$ trong 3 mẫu liên tiếp; Tự động phục hồi khi $\text{RAM} \le 70\%$ trong 3 mẫu liên tiếp (Khoảng trễ an toàn $15\%$).
    - `SERVICE_DOWN`: Báo lỗi khi Local TCP Probe thất bại 2 lần liên tiếp; Phục hồi khi Probe thành công 2 lần liên tiếp.
    - Thời gian làm nguội (Cooldown): Tối thiểu 15 giây giữa hai lần đổi trạng thái sự cố.
- **Hệ quả:** Triệt tiêu 100% hiện tượng báo động ảo tức thời, giữ vững tính ổn định và độ tin cậy của bảng điều khiển WinForms.

### CP2-ADR-05: Mô hình Máy Trạng thái Sự cố 2 Tầng (Dual-Layer Incident FSM)
- **Bối cảnh:** Trạng thái vật lý của máy móc và tiến trình can thiệp của con người là hai chiều thực tế độc lập, không thể gộp chung vào một cờ nhị phân đơn giản.
- **Quyết định:**
  - Phân tách thành hai tầng trạng thái độc lập trong bảng `Incidents`:
    - **Tầng 1 — Trạng thái Vật lý (`ConditionState`):** `ACTIVE`, `RECOVERED`, `UNKNOWN`. Do Server Rule Engine tự động quản lý dựa trên dữ liệu mạng.
    - **Tầng 2 — Trạng thái Vận hành (`WorkflowState`):** `OPEN`, `ACKNOWLEDGED`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED`. Do Quản trị viên điều khiển qua WinForms.
  - Chuẩn hóa xử lý 5 ca biên: Tự phục hồi khi đang OPEN; Tái phát trước khi RESOLVED; Chặn Admin đóng phiếu khi máy vẫn đang lỗi (`ERR_CONDITION_STILL_ACTIVE`); Tranh chấp 2 Admin bằng OCC; và Đánh dấu `UNKNOWN` khi máy bị mất mạng (`DEVICE_OFFLINE`).
- **Hệ quả:** Hệ thống phản ánh chính xác hiện trạng thực tế, ngăn ngừa sai sót vận hành và nâng cao tính chuyên nghiệp của đề tài.

### CP2-ADR-06: Khử trùng lặp Telemetry Bền vững & Idempotency Lệnh Quản trị
- **Bối cảnh:** Mạng chập chờn khiến gói tin ACK bị mất, buộc Agent hoặc Admin Client phải gửi lại (Retry). Nếu Server ghi nhận lại sẽ gây trùng lặp dữ liệu và sai lệch lịch sử.
- **Quyết định:**
  - Khử trùng lặp Telemetry: Khóa bộ ba `(DeviceId, BootId, BatchId)` lưu trong bảng `ProcessedBatches` (TTL 24h). Khi gặp trùng lặp: bỏ qua ghi dữ liệu nhưng **bắt buộc gửi lại `BATCH_ACK` ngay lập tức** để bên gửi giải phóng hàng đợi.
  - Idempotency Lệnh Quản trị: Khóa `(UserId, CommandId)` kèm chuỗi băm SHA-256 `RequestDigest` lưu trong `ProcessedCommands` (TTL 7 ngày). Phát hiện trùng ID khác Digest sẽ từ chối với lỗi bảo mật `ERR_IDEMPOTENCY_KEY_REUSE`.
  - Nguyên tắc phát sinh ACK: Tuân thủ nghiêm ngặt **Commit-Then-Respond** (chỉ gửi ACK sau khi transaction SQL Server đã commit thành công).
- **Hệ quả:** Đạt được ngữ nghĩa "tương đương đúng-một-lần" (Effectively Exactly-Once) mà không cần hệ thống phân tán phức tạp, bảo vệ an toàn toàn vẹn dữ liệu.

### CP2-ADR-07: Cơ sở Dữ liệu SQL Server 2022 Express & Khóa Lạc quan (OCC)
- **Bối cảnh:** Nhiều quản trị viên có thể cùng xem và thao tác trên cùng một sự cố, nguy cơ ghi đè dữ liệu của nhau (Lost Update).
- **Quyết định:**
  - Sử dụng hệ quản trị CSDL chuẩn **Microsoft SQL Server 2022 Express** với 10 bảng nghiệp vụ đã chuẩn hóa DDL.
  - Áp dụng cơ chế **Optimistic Concurrency Control (OCC)** thông qua cột kiểu `RowVersion` (`rowversion` trong SQL Server).
  - Lệnh cập nhật kiểm tra phiên bản dự kiến:
    ```sql
    UPDATE Incidents SET WorkflowState = @state WHERE IncidentId = @id AND RowVersion = @expectedVersion;
    ```
    Nếu `@@ROWCOUNT == 0`, Server từ chối và ném lỗi `ERR_CONCURRENCY_VIOLATION` để WinForms tự động refresh dữ liệu mới.
- **Hệ quả:** Xử lý triệt để bài toán đồng quy đa Admin mà không cần khóa bảng lâu dài, tối ưu tốc độ đọc/ghi.

### CP2-ADR-08: Chế độ Hoạt động Suy giảm khi Cơ sở Dữ liệu Sự cố (Degraded Mode)
- **Bối cảnh:** Dịch vụ SQL Server bị dừng đột ngột trong lúc hệ thống đang vận hành hoặc trong buổi bảo vệ đồ án.
- **Quyết định:**
  - Central Server không crash; giữ nguyên toàn bộ các kết nối TCP với Agent và Admin.
  - Tiếp tục duy trì tiếp nhận `HEARTBEAT` trên RAM để cập nhật trạng thái sống/chết tức thời.
  - Đệm các gói `METRIC_BATCH` vào hàng đợi RAM khẩn cấp (tối đa 500 batches).
  - Từ chối các lệnh Admin đòi hỏi ghi đĩa với mã lỗi `ERR_DATABASE_UNAVAILABLE`.
  - Phát sự kiện `SYSTEM_HEALTH_EVENT` thông báo trạng thái `DEGRADED`. Tự động xả dữ liệu vào DB khi SQL Server sống lại.
- **Hệ quả:** Giúp hệ thống đạt khả năng tự phục hồi (Resilience) và chịu lỗi (Fault-tolerance) đạt điểm xuất sắc trong buổi vấn đáp.
