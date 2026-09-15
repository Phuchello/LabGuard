# LABGUARD — SỔ GHI NHẬN QUYẾT ĐỊNH THIẾT KẾ (ARCHITECTURE DECISION RECORDS - ADR)
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (UIT)  
**Trạng thái:** Đã cập nhật sau vòng Audit của ChatGPT Work — Checkpoint 1  

---

## ADR-001: Lựa chọn cơ chế phân khung thông điệp (Message Framing)
- **Ngày:** 13/09/2026 (Cập nhật sau Audit Work)
- **Trạng thái:** ACCEPTED
- **Bối cảnh:** TCP là dòng byte liên tục (byte stream) không giữ ranh giới thông điệp. Cần cơ chế phân tách message an toàn, hiệu năng cao và tương thích với chuỗi JSON UTF-8.
- **Quyết định:** Sử dụng **Length-Prefix Framing: 4-byte uint32 Big-Endian Length $N$ + Payload JSON Envelope UTF-8**. Kích thước $N$ chỉ tính riêng phần JSON envelope UTF-8, không tính 4 byte header tiền tố. Giới hạn độ dài tối đa `MaxFrameSize = 256 KiB`.
- **Hệ quả:**
  - Loại bỏ các lỗi phân mảnh dòng byte của tầng giao vận (Partial Read & Concatenated Frames).
  - Không bị ảnh hưởng bởi định dạng dữ liệu bên trong payload.
  - Các lỗi JSON sai cú pháp, EOF giữa chừng hoặc length vượt ngưỡng vẫn phải được bắt và xử lý bằng các exception riêng biệt.

---

## ADR-002: Mô hình đơn luồng ghi trên mỗi Socket (Single Writer per Socket)
- **Ngày:** 13/09/2026 (Cập nhật sau Audit Work)
- **Trạng thái:** ACCEPTED
- **Bối cảnh:** Tài liệu chính thức của Microsoft đảm bảo một unique reader và một unique writer có thể hoạt động đồng thời (full-duplex). Tuy nhiên, nhiều concurrent writers nằm ngoài pattern sử dụng an toàn được chứng thực và sẽ tạo race condition khiến các byte của nhiều frame bị xen kẽ (interleaving) làm hỏng framing.
- **Quyết định:** Mỗi kết nối Socket chỉ duy trì một vòng lặp ghi bất đồng bộ duy nhất (`Single Writer Loop`). Các task khác đẩy dữ liệu vào một hàng đợi có giới hạn (`System.Threading.Channels.Channel<byte[]>`) với `FullMode = BoundedChannelFullMode.Wait`. LabGuard áp dụng mô hình này để việc serialization frame rõ ràng và có tính dự đoán cao.

---

## ADR-003: Giám sát sống chết bằng Heartbeat tầng ứng dụng và đồng hồ đơn điệu
- **Ngày:** 13/09/2026 (Cập nhật sau Audit Work)
- **Trạng thái:** ACCEPTED
- **Bối cảnh:** Cờ `Socket.Connected` chỉ phản ánh trạng thái của thao tác I/O gần nhất, không phát hiện được ngắt cáp đột ngột. Cần cơ chế liveness tầng ứng dụng chính xác.
- **Quyết định:**
  - Agent gửi nhịp tim `HEARTBEAT` định kỳ mỗi **5 giây**.
  - Central Server đánh giá liveness dựa trên **đồng hồ đơn điệu** (Monotonic Clock: `Stopwatch.GetTimestamp()` hoặc `Environment.TickCount64`): Thiết bị bị coi là `OFFLINE` khi quá **20 giây** kể từ heartbeat hợp lệ gần nhất.
  - Bộ quét liveness scheduler có chu kỳ tick $\leq 1$ giây (`[DESIGN TARGET]`).
  - Bỏ cụm từ "phục hồi dưới 20 giây".
- **Hệ quả:** Phản ánh chính xác tình trạng sống chết của máy trạm mà không bị ảnh hưởng bởi việc điều chỉnh đồng hồ hệ thống NTP.

---

## ADR-004: Khóa phạm vi MVP (P0) và Điều kiện kích hoạt Gateway/Collector (P1)
- **Ngày:** 13/09/2026
- **Trạng thái:** ACCEPTED
- **Bối cảnh:** Đồ án môn học 5 tuần của 3 sinh viên có nguy cơ cháy tiến độ nếu dàn trải làm hệ thống phân tán quá sớm.
- **Quyết định:**
  - **MVP (P0):** Bắt buộc hoàn thiện đầy đủ: Agent Windows $\rightarrow$ Central Server $\rightarrow$ SQL Server $\rightarrow$ Admin WinForms (bảo mật TLS, test LAN trên 2–3 laptop thật, Agent Simulator 10/20/50 kết nối).
  - **P1 (Gateway + 2 Collector + Load Balancing):** Chỉ được phép làm sau khi Milestone M4 (cuối tuần 3) đạt tiêu chuẩn "0 lỗi Sev 1 / Sev 2" đi kèm các bài test MVP bắt buộc đã chạy thật, và còn đủ thời gian dự phòng.
  - **Quy tắc cắt bỏ (DEFER P1):** Nếu cuối Tuần 3 chưa hoàn tất MVP, P1 lập tức chuyển sang trạng thái **DEFERRED**.

---

## ADR-005: Cơ sở dữ liệu baseline SQL Server 2022 Express
- **Ngày:** 13/09/2026 (Bổ sung sau Audit Work)
- **Trạng thái:** ACCEPTED
- **Bối cảnh:** Cân nhắc giữa SQL Server 2022 Express (chuẩn theo Master Plan) và SQLite.
- **Quyết định:** Giữ vững **SQL Server 2022 Express** làm baseline duy nhất của đề tài. Tầng lưu trữ được thiết kế qua interface `IRepository` để phục vụ cấu trúc code sạch và viết unit test. **TUYỆT ĐỐI KHÔNG** hỗ trợ phương án "chuyển nóng sang SQLite khi đi bảo vệ đồ án". Nếu muốn đổi sang SQLite, phải là quyết định chính thức tại Milestone M0 có điều chỉnh schema, concurrency và migration đầy đủ.
- **Hệ quả:** Giữ vững tính nhất quán với Master Plan và bảo toàn cơ chế `rowversion` chuẩn mực của SQL Server.

---

## ADR-006: Quy tắc tháo gỡ phụ thuộc đồ thị công việc (DAG Decoupling) khi DEFER P1
- **Ngày:** 13/09/2026 (Bổ sung sau Audit Work)
- **Trạng thái:** ACCEPTED
- **Bối cảnh:** Nếu P1 bị hoãn (DEFER), các task kiểm thử và đóng gói Release Candidate ở Tuần 5 không được phép phụ thuộc vào các task P1 ở Tuần 4.
- **Quyết định:** Khi kích hoạt quyết định DEFER P1 ở mốc M4, sơ đồ DAG tự động chuyển đổi các phụ thuộc:
  - **LG-A-016** (Internet deployment & fallback): Chuyển sang phụ thuộc vào **LG-A-012** (MVP Agent LAN trên máy thật) thay vì LG-A-015.
  - **LG-B-016** (Tune SQL index & ingest budget): Chuyển sang phụ thuộc vào **LG-B-012** (MVP concurrency & acceptance) thay vì LG-B-015.
  - **LG-C-016** (Multi-admin concurrency tests): Chuyển sang phụ thuộc vào **LG-C-011** (MVP UI integration) thay vì LG-C-015.
  - Toàn bộ các test case P1 được ghi nhận trạng thái `SKIPPED`, Milestone M5 ghi nhận trạng thái `DEFERRED`.
- **Hệ quả:** Đảm bảo đồ thị công việc không bị gãy vỡ, nhóm vẫn tiến thẳng đến Release Candidate (M6) và Viva Ready (M7) đúng hạn.

---

## ADR-007: Tái cấu trúc Ownership theo 3 Lane song song, Phân định ranh giới Server/Client và Binary P1 Collector duy nhất
- **Ngày:** 13/09/2026 (Cập nhật sau Round 3 Audit)
- **Trạng thái:** ACCEPTED
- **Bối cảnh:** Phân công ban đầu phân tán code server cho nhiều người; P1 phân chia nhầm "Collector 01 cho A, Collector 02 cho C" gây khó hiểu về mặt kỹ thuật phần mềm (hai instance cùng chung logic nhưng lại chia hai người viết).
- **Quyết định:** Tái cấu trúc ownership theo 3 lane độc lập, phân định ranh giới rõ ràng:
  1. **Người A — AGENT + SIMULATOR + TRIỂN KHAI + P1 GATEWAY & TẢI:**
     - Sở hữu Windows Agent (CPU/RAM/NIC/Uptime), Local TCP probe, kết nối TCP/TLS phía Agent, Heartbeat/Telemetry sender, Auto-reconnect & Replay buffer.
     - Sở hữu Agent Simulator (10/20/50 sockets), kịch bản tải/bão mạng, hướng dẫn triển khai LAN/Internet.
     - Chủ trì `Shared.Protocol` (FrameCodec, DTO, hằng số).
     - **P1 Ownership:** Viết L4 Gateway (TCP load balancer phân phối kết nối sang các Collector); cấu hình, triển khai và kiểm thử **02 instances** của cùng một binary `LabGuard.Collector.exe` (do C viết) tại cổng `5001` và `5002`.
  2. **Người B — CENTRAL SERVER + DATABASE + AUTH/RBAC SERVER-SIDE:**
     - Sở hữu toàn bộ máy chủ Central Server: Listener TCP/TLS, Session registry, Ingest & Deduplication engine (`ProcessedBatches`, `ProcessedCommands`), Liveness evaluator (Monotonic clock), Rule engine (Streak/Hysteresis), Incident state machine, Snapshot & Realtime fanout.
     - Sở hữu SQL Server schema, migration scripts, repository interfaces và Transactional Dapper implementation.
     - **Xác thực & Phân quyền:** Sở hữu toàn bộ logic Server-side: PBKDF2 with HMAC-SHA256 (600,000 rounds + salt 128-bit, 32-byte output, CryptographicOperations.FixedTimeEquals), verification signup/invitation token, cấp phát session token, và kiểm tra quyền RBAC bắt buộc phía server trước khi thực thi command.
     - **P1 Ownership:** Hỗ trợ giao thức ingestion từ Collector về Central Server.
  3. **Người C — ADMIN CLIENT + KIỂM THỬ ĐA ADMIN + P1 COLLECTOR BINARY:**
     - Sở hữu WinForms Admin UI (Dashboard, Lab View, Devices, Incidents, History, Health), TCP/TLS client phía Admin, correlation requestId/response, quản lý pending timeout, đồng bộ local cache, chuyển luồng an toàn qua `Control.Invoke`/`BeginInvoke`/`InvokeAsync`.
     - **Xác thực & Phân quyền phía Client:** Sở hữu UI Login/Signup, lưu giữ session token tạm thời trên RAM client, tối ưu hóa trải nghiệm người dùng (UX optimization) qua ẩn/hiện/disable các nút bấm giao diện theo role (Admin, Operator, Viewer) trước khi gửi lệnh.
     - Sở hữu bài test 2 Admin đồng thời (race condition / OCC `rowversion`), Slow-client test, Demo rehearsal, User manual.
     - **P1 Ownership:** Thiết kế và hiện thực **duy nhất 01 ứng dụng Collector** (`LabGuard.Collector.exe`), tiếp nhận telemetry từ Agent qua L4 Gateway và chuyển tiếp an toàn về Central Server.
- **Hệ quả:** Mỗi sinh viên chịu trách nhiệm độc lập cho một artifact phần mềm, kiểm thử được bằng harness riêng trước khi tích hợp qua contract chung.

---

## ADR-008: Chuẩn hóa Wire Protocol Envelope, Catalog MessageType, Persistence Khử trùng lặp và Rule Engine
- **Ngày:** 13/09/2026 (Cập nhật sau Round 3 Audit)
- **Trạng thái:** ACCEPTED
- **Bối cảnh:** Work phát hiện catalog MessageType bị sai lệch so với Master Plan (`03_PROTOCOL_SPEC.md`); cơ chế dedup nhầm lẫn giữa hàng đợi RAM và bảng CSDL lưu trữ; ngưỡng rule 3 mẫu cần chính xác về khung thời gian.
- **Quyết định:**
  1. **Cấu trúc Envelope chuẩn hóa:**
     - Envelope gồm 6 trường bắt buộc: `version` (int), `messageType` (string uppercase enum), `requestId` (UUID v4), `deviceId` (string), `sentAtUtc` (ISO-8601 UTC string), `payload` (JSON object).
     - Cờ `isReplay` nằm bên trong payload của `METRIC_BATCH` và `SERVICE_STATUS`, không nằm trên envelope ngoài.
  2. **Catalog MessageType chính thức (Khôi phục đầy đủ 9 nhóm từ 03_PROTOCOL_SPEC.md & CONTRACT_MATRIX.md):**
     - Bao gồm đầy đủ 9 nhóm nghiệp vụ: Handshake/Auth, Heartbeat, Goodbye, Ping/Pong, Telemetry/Status/Config, Admin Login/Logout/Signup/Invite, Subscribe/Unsubscribe/Snapshot, Realtime Events, và Incident Actions / Queries / Errors / P1 Internal.
     - Tuyệt đối loại bỏ các tên tự sáng tác trái spec như `LOGIN_REQ`, `INCIDENT_EVENT`, `METRIC_EVENT`.
  3. **Lưu trữ & Khử trùng lặp (Deduplication Persistence):**
     - **Telemetry Dedup:** Khóa bộ ba `DeviceId + BootId + BatchId` được lưu trữ cố định trong bảng SQL Server `ProcessedBatches` với TTL 24 giờ.
     - **Admin Command Dedup:** Khóa `UserId + CommandId` đi kèm `RequestDigest` (SHA-256 nội dung command) được lưu trữ cố định trong bảng SQL Server `ProcessedCommands` với TTL 7 ngày để chống gửi lặp và phát hiện tấn công giả mạo payload.
     - **Phân định rõ ràng:** Ingest Ring Buffer (`System.Threading.Channels.Channel<T>`) chỉ là hàng đợi RAM để chống tràn bộ nhớ và điều tiết backpressure, không phải nơi lưu dedup. Các bảng `AuditLogs` và `IncidentHistory` chỉ là nhật ký kiểm toán cho con người, không phải bảng idempotency kỹ thuật.
  4. **Khung thời gian Rule Engine & Chu kỳ Streak:**
     - `HIGH_CPU`: CPU $\ge 90\%$ trong 3 mẫu liên tiếp (chu kỳ 5s); phục hồi khi CPU $\le 75\%$ trong 3 mẫu liên tiếp. Ba mẫu cách nhau 5s trải qua đúng **10 giây** giữa mẫu đầu và mẫu cuối (tại 0s, 5s, 10s); không diễn đạt là "chắc chắn 15 giây liên tục".
     - `SERVICE_DOWN`: Thất bại **2 lần probe liên tiếp**; phục hồi sau **2 lần probe thành công liên tiếp**.
  5. **Hệ thống Trạng thái phân tách (Dual State):**
     - `ConditionState`: `ACTIVE`, `RECOVERED`, `UNKNOWN`.
     - `WorkflowState`: `OPEN`, `ACKNOWLEDGED`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED`.
  6. **Bắt lỗi EOF:** `FrameCodec.ReadExactlyBytesAsync` phải ném `EndOfStreamException` ngay tại tầng đọc byte khi gặp EOF bất ngờ. Tuyệt đối không chuyển payload thiếu sang JSON parser.
- **Hệ quả:** Đồng bộ thống nhất giữa Wire Protocol Spec, Master Plan, Database Schema và Tài liệu Đề cương.

