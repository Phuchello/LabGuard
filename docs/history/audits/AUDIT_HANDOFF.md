> **SUPERSEDED / HISTORICAL ARTIFACT**  
> Current freeze candidate: AUDIT_HANDOFF_REV4_2_1.md

# AUDIT HANDOFF — CHECKPOINT 1 (REVISION 4.2)
**Vòng bàn giao:** Checkpoint 1 Revision 4.2 Independent Audit Patch  
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Nhóm thực hiện:** 03 sinh viên (Kế hoạch 5 tuần)  
**Người chuẩn bị bàn giao:** Antigravity (Lead Technical Researcher & Network Software Architect)  
**Người nhận audit:** ChatGPT Work (Auditor độc lập)  
**Thời điểm:** Checkpoint 1 Revision 4.2 — Bàn giao gói hồ sơ thực tế (13/09/2026 16:35:00+07:00)  
**Trạng thái bàn giao:** HOÀN TẤT VÒNG SỬA REVISION 4.2 — SẴN SÀNG CHO FINAL AUDIT FREEZE (`CHECKPOINT 1 REV4.2 READY FOR FINAL AUDIT`)

---

## I. TỔNG QUAN HỒ SƠ & NGUYÊN TẮC BÀN GIAO REVISION 4.1

1. **Tuân thủ phân định vai trò:**
   - **Antigravity:** Tác giả chính, chịu trách nhiệm nghiên cứu, thiết kế kiến trúc, tính toán khối lượng công việc, xây dựng test harness mẫu, biên tập hồ sơ đề cương LaTeX và xuất gói ZIP bàn giao thật.
   - **ChatGPT Work:** Auditor độc lập, thực hiện phản biện, kiểm toán tính đúng đắn, phạm vi môn học và tính khả thi. Antigravity cung cấp đầy đủ bằng chứng thực tế (`[AUTHOR-VERIFIED]`), để trạng thái `AUDITOR NOT RUN` chờ Work đánh giá.
2. **Ranh giới Checkpoint 1:**
   - Không triển khai code sản phẩm tại Checkpoint 1 (`LabGuard.Agent`, `LabGuard.Server`, `LabGuard.Admin` hay `LabGuard.Collector`).
   - Toàn bộ 60 công việc trong `WBS_TASKS.csv` mang trạng thái minh bạch: `"TODO vì đây là checkpoint thiết kế; việc triển khai MVP bắt đầu theo kế hoạch sau khi contract được freeze."`.
3. **Kỷ luật ngôn từ & Nguồn dẫn:**
   - Sử dụng văn phong kỹ thuật khách quan, không dùng các khẳng định tuyệt đối hóa mang tính chủ quan.
   - Giáo trình NT106 UIT được gắn nhãn nguồn: `[COURSE-ARTIFACT; CURRENT-CLASS UNVERIFIED]` trong tài liệu tham khảo.

---

## II. CÁC ĐẶC TẢ KỸ THUẬT ĐÃ ĐỒNG BỘ & KHÓA CHẶT (REV 4.1 BASELINE)

### 1. Bản chất TCP Byte Stream & Đóng khung thông điệp (RFC 9293, RFC 8259)
- **Cơ chế:** TCP là dòng byte liên tục (Byte Stream), không có ranh giới bản tin (Message Boundary). Một lần đọc `ReadAsync` có thể nhận dữ liệu bị phân mảnh (Partial Read) hoặc nhiều bản tin dính liền (Packet Coalescing).
- **Khuôn mẫu Frame:** 4-byte tiền tố Big-Endian (Network Byte Order) biểu diễn độ dài payload $N$ byte ($0 < N \le 262,144$ byte = 256 KiB, $N$ không bao gồm 4 byte header) + $N$ byte payload JSON UTF-8.
- **Envelope chuẩn hóa (6 trường):**
  - `version` (int): Phiên bản giao thức (hiện tại: 1).
  - `messageType` (string): Tên thông điệp in hoa chuẩn.
  - `requestId` (UUIDv4 string): Correlation ID chu trình Request-Response (không dùng làm khóa mutation).
  - `deviceId` (string): Định danh thiết bị gửi.
  - `sentAtUtc` (string ISO-8601 UTC): Thời điểm gửi tại client.
  - `payload` (object): Nội dung nghiệp vụ chi tiết.
- **Vị trí cờ isReplay:** Nằm bên trong payload của `METRIC_BATCH` và `SERVICE_STATUS`, không đặt ở envelope root để tránh sai ngữ cảnh đối với các thông điệp điều khiển/phi telemetry.
- **Xử lý ngắt dòng:** Hàm `ReadExactlyBytesAsync` ném `EndOfStreamException` khi kết nối đóng giữa chừng, hủy kết nối an toàn mà theo invariant được kiểm thử, không chuyển byte cắt cụt sang JSON parser.

### 2. Danh mục Wire Message Catalog Chuẩn (Khép lại Blocker C-01)
- **Nguồn sự thật duy nhất:** `03_PROTOCOL_SPEC.md`, các mục `Message catalog` và `Internal protocol P1`.
- **Nguyên tắc đối chiếu:** Catalog được đối chiếu bằng phép so sánh tập hợp tên message với `03_PROTOCOL_SPEC.md`.
- **Tập hợp 69 thông điệp chuẩn (59 Core + 10 Internal P1):**
  - *Handshake & Greeting (3):* `HELLO_AGENT`, `HELLO_ADMIN`, `HELLO_ACK`.
  - *Agent Auth (2):* `AUTH_AGENT`, `AUTH_RESULT`.
  - *Heartbeat & Liveness (2):* `HEARTBEAT`, `HEARTBEAT_ACK`.
  - *Telemetry, Service Status & Config (6):* `METRIC_BATCH`, `BATCH_ACK`, `SERVICE_STATUS`, `CONFIG_UPDATE`, `CONFIG_RESULT`, `SERVICE_CHECK_REQUEST`.
  - *Disconnect & Keepalive (4):* `AGENT_GOODBYE`, `GOODBYE_ACK`, `PING`, `PONG`.
  - *Admin Auth & Identity (8):* `LOGIN`, `LOGIN_RESULT`, `SIGN_UP`, `SIGN_UP_RESULT`, `CREATE_INVITE`, `INVITE_RESULT`, `LOGOUT`, `LOGOUT_RESULT`.
  - *Device & Metrics Queries (6):* `LIST_DEVICES`, `DEVICE_PAGE`, `GET_DEVICE`, `DEVICE_DETAIL`, `GET_METRICS`, `METRICS_RESULT`.
  - *Incident Queries, History & Reports (10):* `LIST_INCIDENTS`, `INCIDENT_PAGE`, `GET_INCIDENT`, `INCIDENT_DETAIL`, `GET_HISTORY`, `HISTORY_PAGE`, `GET_SERVER_HEALTH`, `SERVER_HEALTH`, `GET_REPORT`, `REPORT_RESULT`.
  - *Subscription & Snapshot Sync (8):* `SUBSCRIBE_DEVICES`, `SUBSCRIBE_INCIDENTS`, `SUBSCRIBE_HEALTH`, `SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END`, `UNSUBSCRIBE`, `UNSUBSCRIBE_RESULT`.
  - *Realtime Event Broadcast (4):* `DEVICE_UPDATE`, `INCIDENT_OPEN`, `INCIDENT_UPDATE`, `HEALTH_UPDATE`.
  - *Incident Actions & Commands (6):* `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`, `COMMAND_RESULT`, `ERROR`.
  - *Internal Protocol P1 (10):* `HELLO_COLLECTOR`, `AUTH_COLLECTOR`, `FORWARD_OPEN`, `FORWARD_DATA`, `FORWARD_RESULT`, `FORWARD_CLOSE`, `CLOSE_SESSION`, `COLLECTOR_HEARTBEAT`, `HEALTH_PROBE`, `HEALTH_RESULT`.
- **Đã loại bỏ khỏi baseline:** Các tên không thuộc spec (`ERROR_RESPONSE`, `RATE_LIMIT_WARNING`, `SESSION_EXPIRED`, `CONFIG_PUSH`, `CONFIG_ACK`, `AGENT_RESTART`, `COMMAND_CANCEL`, `GATEWAY_HELLO`, `GATEWAY_ACK`, `ROUTE_REGISTER`, `ROUTE_UNREGISTER`, `GATEWAY_HEARTBEAT`, `COLLECTOR_HANDSHAKE`, `COLLECTOR_ACK`, `AGGREGATE_BATCH`, `BATCH_PERSISTED`).
- **Script kiểm chứng tự động:** `scratch/verify_protocol_catalog.py` kiểm tra tập hợp giữa `CONTRACT_MATRIX.md` và `03_PROTOCOL_SPEC.md` đạt kết quả: `missing = ∅`, `extra = ∅` (nhật ký thực thi: `scratch/protocol_catalog_verification_log.txt`).
- *Ghi chú đề cương PDF:* Bảng tại Phụ lục B của `main.pdf` là bảng tóm tắt đại diện các bản tin cốt lõi.

### 3. Cơ chế Khử trùng lặp (Deduplication) & RequestDigest Chuẩn Hóa
- **Telemetry & Service Status:** Khóa composite `DeviceId + BootId + BatchId` được lưu bền vững vào bảng SQL Server `ProcessedBatches` với TTL 24 giờ. Bản tin trùng bị bỏ qua phần ghi CSDL nhưng vẫn phản hồi `BATCH_ACK` để Agent giải phóng hàng đợi.
- **Command nghiệp vụ Admin:** Khóa composite `UserId + CommandId` kèm mã băm `RequestDigest` được lưu bền vững vào bảng SQL Server `ProcessedCommands` với TTL 7 ngày.
  - **Quy tắc tính `RequestDigest` chuẩn hóa:**
    1. Tạo dedicated command DTO theo schema cố định với các trường: `commandType`, `incidentId`, `expectedRowVersion`, `commandParams`, `commandId`.
    2. Chuẩn hóa `commandParams` bằng thứ tự field xác định (deterministic property ordering) và loại bỏ các trường không thuộc nghiệp vụ.
    3. Serialize chuỗi JSON sang UTF-8 không BOM, không chứa khoảng trắng (whitespace) tùy ý giữa các token.
    4. Băm SHA-256 trên chuỗi byte UTF-8 chuẩn hóa để tạo ra 64 ký tự hex của `RequestDigest`.
  - **Xử lý hai request có cùng `UserId + CommandId`:**
    - Digest giống nhau $\rightarrow$ Trả lại kết quả đã lưu trước đó (Idempotent replay);
    - Digest khác nhau $\rightarrow$ Từ chối bằng lỗi xung đột `ERR_IDEMPOTENCY_KEY_REUSE`;
    - Không bao giờ thực thi mutation lần thứ hai.
- **Phân định kiến trúc:** Ingest Ring Buffer (`Channel<T>`) trong bộ nhớ RAM chỉ đóng vai trò điều tiết áp lực ghi (backpressure), không phải nơi chống trùng lặp bền vững. Các bảng `AuditLogs` và `IncidentHistory` phục vụ truy vết con người, không dùng làm bảng idempotency kỹ thuật.

### 4. Chu kỳ lấy mẫu, Ngưỡng phát hiện & Deadband chống Flapping
- **HIGH_CPU:** Kích hoạt cảnh báo khi CPU $\ge 90\%$ trong 3 mẫu liên tiếp (chu kỳ 5s; khoảng thời gian đo tối thiểu 10 giây: $t=0, 5, 10$s). Tự động thu hồi cảnh báo khi CPU $\le 75\%$ trong 3 mẫu liên tiếp. Dải trễ 15% (Deadband) ngăn chặn hiện tượng rung giật cảnh báo.
- **SERVICE_DOWN:** Kích hoạt khi probe TCP thất bại 2 lần liên tiếp (timeout 2s, chu kỳ 5s). Tự động thu hồi khi probe TCP thành công 2 lần liên tiếp.
- **Liveness Central Server:** Cảnh báo `DEVICE_OFFLINE` kích hoạt khi Central Server **không nhận heartbeat hợp lệ trong khoảng thời gian lớn hơn 20 giây (`> 20s`)**, được tính bằng `Stopwatch` / monotonic clock tại server, không phụ thuộc vào đồng hồ client.

### 5. Kiến trúc Trạng thái kép (Dual-State Machine)
- **ConditionState (Trạng thái kỹ thuật đo lường):** `ACTIVE`, `RECOVERED`, `UNKNOWN`.
- **WorkflowState (Trạng thái can thiệp con người):** `OPEN`, `ACKNOWLEDGED`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED`.
- Hình 2 trong `main.pdf` hiển thị trạng thái kép với nhãn chuyển trạng thái `relapse (tái phát)` nằm ngang trên nền trắng (`sloped=false, fill=white`), rõ ràng và không bị nét vẽ đè lấp.

### 6. Mô hình Concurrency & Thread-Safety
- **Central Server:** Sử dụng Async I/O trên ThreadPool của .NET (`SocketAsyncEventArgs` / `ReadAsync`), không tạo 1 Thread OS riêng cho mỗi kết nối.
- **Single Writer Pattern:** Mỗi socket có 1 Task đọc và 1 Task Single Writer độc quyền rút thông điệp từ `Channel<byte[]>` có giới hạn để ghi tuần tự ra socket, ngăn ngừa xung đột dữ liệu trên `NetworkStream.WriteAsync`.
- **WinForms UI Marshaling:** Mọi callback từ luồng mạng nền cập nhật giao diện Admin WinForms bắt buộc phải marshal qua `Control.Invoke`, `Control.BeginInvoke` hoặc `InvokeAsync` của WinForms.

### 7. CSDL & Concurrency Control (OCC) với `rowversion`
- **Hệ quản trị CSDL:** SQL Server 2022 Express được khóa làm CSDL duy nhất.
- **Bản chất `rowversion`:** `rowversion` là kiểu dữ liệu đặc thù của SQL Server, là **token nhị phân opaque 8 byte, được SQL Server tự động tạo giá trị mới khi một dòng chứa cột `rowversion` được cập nhật**. Không gọi một cột `binary(8)` thông thường là `rowversion` vì kiểu nhị phân thông thường không có cơ chế tự động tăng cấp CSDL này.

### 8. Chuẩn mật mã PBKDF2 Baseline
- **Thuật toán & Khuyến nghị chuẩn mực:** OWASP recommends Argon2id for new systems generally. If FIPS-140 compliance is required, OWASP recommends PBKDF2-HMAC-SHA256 with a work factor of 600,000 or more. LabGuard deliberately chooses PBKDF2 for BCL availability and course-scope simplicity; 600,000 is therefore the project baseline referenced from OWASP and must still be measured on the actual lab hardware.
- **Tham số:** Salt ngẫu nhiên 16 byte (`RandomNumberGenerator`), khóa đầu ra 32 byte (256-bit).
- **So sánh:** Sử dụng `CryptographicOperations.FixedTimeEquals` chống tấn công đo thời gian (Timing Attacks).
- **Lưu trữ CSDL:** Work factor (số vòng lặp 600,000) và chuỗi salt được lưu kèm hash mật khẩu trong bảng `Users`.

### 9. Phân công 3 Làn Song Song & Ranh giới Sở hữu (ADR-007)
- **Sinh viên A (Agent & Networking Infrastructure):**
  - Sở hữu: Windows Service Agent (`LabGuard.Agent.exe`), Network Load Simulator (20+ nodes), Test Harness giả lập Central Server.
  - Phần mở rộng P1: L4 TCP Gateway (`LabGuard.Gateway.exe`), cấu hình và triển khai 02 instances của Collector.
- **Sinh viên B (Central Server & Core Engine):**
  - Sở hữu: Central Server (`LabGuard.Server.exe`), CSDL SQL Server 2022 Express, Rule Engine, Snapshot/Fanout Engine, Command Deduplication (`ProcessedCommands`).
  - Phân định bảo mật: Toàn quyền sở hữu Server-side Authentication & Authorization (PBKDF2 600k rounds, tạo session token, kiểm tra quyền RBAC của mọi request bắt buộc tại server).
- **Sinh viên C (Admin Client & Ingestion):**
  - Sở hữu: WinForms Admin Client (`LabGuard.Admin.exe`), Client RPC & Local Memory Cache, Multi-Admin Concurrency Test Harness.
  - Phân định giao diện: Sở hữu WinForms Auth UI (Form đăng nhập, lưu session token trong RAM, điều khiển ẩn/hiện nút bấm giao diện theo vai trò người dùng). Lưu ý: Việc ẩn nút chỉ là trải nghiệm người dùng (UX), mọi quyết định phân quyền thực tế bắt buộc do Server của SV B thực thi.
  - Phần mở rộng P1: Thiết kế và hiện thực **DUY NHẤT 01 ứng dụng Collector binary** (`LabGuard.Collector.exe`), dùng chung cho cả 2 instances mà SV A cấu hình và triển khai.

---

## III. TÍNH TOÁN KHỐI LƯỢNG CÔNG VIỆC & PHƯƠNG ÁN 12H/TUẦN

### 1. Nguồn chuẩn duy nhất về khối lượng công việc (`tab:workload`)
Khối lượng công việc toàn dự án được chốt chặt chẽ tại Bảng ma trận phân bổ ngân sách giờ kỹ thuật (nhãn `tab:workload`, hiện nằm tại trang 15 của `main.pdf`):
- **P0 Core:** 76.5h (SV A: 24.0h, SV B: 27.0h, SV C: 25.5h).
- **Harness:** 28.0h (SV A: 10.0h, SV B: 9.0h, SV C: 9.0h).
- **Tích hợp:** 18.0h (SV A: 6.0h, SV B: 6.0h, SV C: 6.0h).
- **Vấn đáp:** 15.0h (SV A: 5.0h, SV B: 5.0h, SV C: 5.0h).
- **P1 Mở rộng:** 47.0h (SV A: 15.0h, SV B: 16.0h, SV C: 16.0h).
- **Dự phòng (Buffer):** 27.0h (SV A: 9.0h, SV B: 9.0h, SV C: 9.0h).
- **Tổng toàn bộ:** **211.5h** (SV A: 69.0h, SV B: 72.0h, SV C: 70.5h).
- **Mô tả cấu trúc:** P0 gồm 3 ứng dụng chính và Shared.Protocol; P1 bổ sung Gateway và Collector App. Tách phụ thuộc: *dependency graph tách P0 khỏi P1 để việc trì hoãn nhánh P1 không chặn đường hoàn thành MVP*.

### 2. Kế hoạch dự phòng 12 giờ/người/tuần (Chuẩn hóa công thức toán học)
- **Trần năng lực nhóm (Capacity Ceiling):** 3 sinh viên \times 12h/tuần \times 5 tuần = **180.0h**.
- **Khối lượng cam kết khi hoãn P1 (DEFER P1, -47.0h):**
  $$\text{Committed Workload} = 211.5\text{h} - 47.0\text{h} = 164.5\text{h}$$
  - SV A: $69.0\text{h} - 15.0\text{h} = 54.0\text{h}$ (đã bao gồm 9.0h buffer).
  - SV B: $72.0\text{h} - 16.0\text{h} = 56.0\text{h}$ (đã bao gồm 9.0h buffer).
  - SV C: $70.5\text{h} - 16.0\text{h} = 54.5\text{h}$ (đã bao gồm 9.0h buffer).
- **Biên độ năng lực nhàn rỗi (Capacity Headroom):**
  $$\text{Capacity Headroom} = 180.0\text{h} - 164.5\text{h} = 15.5\text{h}$$
- **Khẳng định nguyên tắc tính toán:**
  - 15.5h headroom là khoảng trống năng lực chưa cam kết của trần 180h so với 164.5h cam kết; **không cộng dồn vào khối lượng cam kết và không tính trùng lặp với 27h buffer**.
  - Buffer 27.0h đã nằm trọn vẹn bên trong con số 164.5h cam kết.

### 3. Master WBS 30 Cột (`WBS_TASKS.csv`)
- File `WBS_TASKS.csv` gồm đầy đủ **30 cột** (đáp ứng trọn vẹn yêu cầu $\ge 27$ cột) và **đúng 60 công việc** (mỗi sinh viên đúng 20 công việc).
- Script tự động `scratch/verify_wbs.py` đối soát bottom-up từng dòng và xác nhận **7/7 hạng mục kiểm tra đều PASSED** (nhật ký thực thi: `scratch/wbs_verification_log.txt`).

---

## IV. BẰNG CHỨNG KIỂM CHỨNG & THÔNG TIN BÀN GIAO THỰC TẾ (BLOCKER E-01)

### 1. Thông số gói bàn giao vật lý thực tế
- **Tên file nén:** `LABGUARD_CHECKPOINT_1_REV4_2_HANDOFF.zip`
- **Đường dẫn tuyệt đối trên ổ đĩa:** `c:\Users\lyle3\OneDrive\Documents\Môn học\Lập trình mạng\Đồ án\LABGUARD_DeCuong_NT106_LaTeX_v3\LABGUARD_CHECKPOINT_1_REV4_2_HANDOFF.zip`
- **Số lượng file bên trong:** `40 files` (39 files thành phần + 1 file `MANIFEST_SHA256.txt`)
- **SHA-256 của `main.pdf`:** `a7baf89f538ce00e288f38a66ebf8dd44dcb0931184f5c9f931319dfe60190f6` (310,758 bytes, 30 trang A4)
- **Thời điểm đóng gói:** 13/09/2026 16:35:00+07:00
- **Chứng thực tính toàn vẹn (Integrity Attestation):** Theo nguyên lý hàm băm mật mã học (RFC 6234), tệp nén không thể tự lưu trữ giá trị băm của chính nó bên trong. Do đó, kích thước byte và mã băm SHA-256 của file ZIP thành phẩm được đo lường thực tế trên artifact và công bố chính thức tại Bảng tổng kết bàn giao. Toàn bộ 39 tệp tin thành phần bên trong gói bàn giao được bảo toàn và chứng thực 100% qua tệp `MANIFEST_SHA256.txt`.

### 2. Bảng tổng kết đo lường artifact bàn giao thực tế
| Deliverable Artifact | Kích thước byte | SHA-256 Checksum | Ghi chú kiểm chứng |
| :--- | :---: | :--- | :--- |
| `LABGUARD_CHECKPOINT_1_REV4_2_HANDOFF.zip` | 486,438 | `f00107a44081872ab424a776f41497a5c1ece8b8c07f0736b9420a7fdfafd60b` | Gói bàn giao vật lý đầy đủ 40 files |
| `main.pdf` | 310,758 | `a7baf89f538ce00e288f38a66ebf8dd44dcb0931184f5c9f931319dfe60190f6` | Đề cương PDF 30 trang A4 biên dịch từ source LaTeX |
| `03_PROTOCOL_SPEC.md` | 15,316 | `4821a8d11cbe2d3cbef17887e5b15320577908b8b9821d3f74229b47eebc3f25` | Đặc tả giao thức baseline 69 messages (34 P0-MVP) |
| `CONTRACT_MATRIX.md` | 31,489 | `ffcba668efbfb22aa89ca64d0a92f0fc9fc0cfdbeeeea612ebbe15291b94548e` | Hợp đồng giao thức đồng bộ tập hợp (0 extra, 0 missing) |

### 3. Kết quả kiểm tra đối soát sau khi giải nén vào thư mục tạm (`temp_extract_test`)
Toàn bộ gói ZIP đã được giải nén sang thư mục tạm độc lập `temp_extract_test` và thực thi kiểm thử trực tiếp trên các file giải nén:
- **Xác minh Manifest:** Toàn bộ 38 file thành phần giải nén khớp 100% với `MANIFEST_SHA256.txt` (0 mismatch, 0 missing).
- **Chạy Test 1 (`verify_wbs.py`):** Exit code `0` | 7/7 checks PASSED (30 cột, 60 task TODO, 211.5h khớp `tab:workload`).
- **Chạy Test 2 (`verify_protocol_catalog.py`):** Exit code `0` | PASS (69/69 message types khớp `03_PROTOCOL_SPEC.md`, missing = ∅, extra = ∅).
- **Chạy Test 3 (`test_frame_codec`):** Exit code `0` | 5/5 tests PASSED (Partial read header/payload, Concatenated frames, Oversized rejection, Unexpected EOF).

### 4. Nhật ký thực thi các script kiểm chứng độc lập
| Script / Test Harness | Lệnh chạy | Môi trường | Exit Code | Kết quả ghi nhận | Trạng thái |
| :--- | :--- | :--- | :---: | :--- | :---: |
| **WBS Verification** | `python scratch/verify_wbs.py` | Python 3.12.10 (Win x64) | `0` | 7/7 checks PASSED, 30 cột, 60 task TODO, 211.5h khớp `tab:workload` | `[AUTHOR-VERIFIED]` |
| **Protocol Catalog** | `python scratch/verify_protocol_catalog.py` | Python 3.12.10 (Win x64) | `0` | PASS: 69/69 message types khớp 03_PROTOCOL_SPEC.md, missing = ∅, extra = ∅ | `[AUTHOR-VERIFIED]` |
| **Frame Codec Harness** | `dotnet run --project scratch/test_frame_codec/test_frame_codec.csproj` | .NET 9.0 (Win x64) | `0` | 5/5 tests PASSED (Partial read, Coalescing, Oversized reject, EOF) | `[AUTHOR-VERIFIED]` |
| **Biên dịch Đề cương PDF** | `pdflatex main.tex; biber main; pdflatex main.tex` | MiKTeX 25.12 | `0` | PDF hiện có 30 trang A4, `pdftitle` set, `pdfstartview=Fit` | `[AUTHOR-VERIFIED]` |

### 5. Quy tắc tạo `MANIFEST_SHA256.txt`
File `MANIFEST_SHA256.txt` được tạo trước khi nén và chứa mã băm SHA-256 của đúng 38 file thành phần được đóng gói trong ZIP (loại trừ chính manifest). Mã băm của file nén ZIP và `main.pdf` được đo lường độc lập ở bên ngoài ZIP và ghi nhận chính xác tại bảng tổng kết bàn giao.

---

## V. GIỚI HẠN ĐÃ BIẾT (KNOWN LIMITATIONS)

1. **Giới hạn phạm vi Checkpoint 1:**
   - Toàn bộ hồ sơ ở Checkpoint 1 là thiết kế kỹ thuật, đề cương đồ án, hợp đồng giao thức và test harness mẫu. Không triển khai code sản phẩm tại Checkpoint 1 (`LabGuard.Agent`, `LabGuard.Server`, `LabGuard.Admin`, `LabGuard.Collector`). Tất cả 60 công việc đều ở trạng thái `TODO`.
2. **Giới hạn môi trường kiểm thử Frame Codec:**
   - Bộ test harness `test_frame_codec` kiểm tra tính đúng đắn của thuật toán giải mã dòng byte và xử lý phân mảnh dựa trên `MemoryStream` có kiểm soát tốc độ đọc chunk, chưa mô phỏng độ trễ mạng vật lý thực tế qua cáp LAN hay hiện tượng mất gói tin trên môi trường nhiều máy vật lý.
3. **Nguồn tham khảo Giáo trình UIT NT106:**
   - Giáo trình Thực hành Lập trình mạng căn bản UIT (2022) là tài liệu lưu hành nội bộ của trường, được gắn cờ `[COURSE-ARTIFACT; CURRENT-CLASS UNVERIFIED]` do auditor độc lập chưa tiếp cận được bản gốc vật lý.
4. **Trạng thái thẩm định:**
   - Revision 4.1 đã được tác giả kiểm tra và chuyển giao file thực tế; trạng thái nghiệm thu cuối cùng chờ ChatGPT Work giải nén và tái lập kiểm chứng (`DELIVERED / AUDITOR NOT RUN`).

---

## VI. DANH MỤC HỒ SƠ CHI TIẾT TRONG GÓI BÀN GIAO

| STT | Tên file / Thư mục | Mô tả nội dung |
| :---: | :--- | :--- |
| 1 | `main.pdf` | Đề cương đồ án hoàn chỉnh (PDF hiện có 30 trang A4, `pdfstartview=Fit`) |
| 2 | `main.tex` & các thư mục LaTeX | Toàn bộ mã nguồn LaTeX (`template/`, `sections/`, `appendices/`, `bibliography.bib`) |
| 3 | `03_PROTOCOL_SPEC.md` | Baseline đặc tả giao thức mạng và danh mục 69 message types |
| 4 | `CONTRACT_MATRIX.md` | Hợp đồng giao thức chuẩn hóa đối chiếu tập hợp với 03_PROTOCOL_SPEC.md |
| 5 | `WBS_TASKS.csv` | Master WBS 30 cột $\times$ 60 công việc (đầy đủ thông tin, trạng thái TODO) |
| 6 | `PARALLEL_WORK_PLAN.md` | Kế hoạch 3 làn làm việc song song, chuẩn hóa công thức 12h/tuần (164.5h + 15.5h) |
| 7 | `TASK_MIGRATION_MAP.md` | Bảng ánh xạ 60 công việc đối chiếu đồng bộ với WBS và `tab:workload` |
| 8 | `DECISIONS.md` | 8 biên bản quyết định kiến trúc (ADR-001 đến ADR-008) |
| 9 | `handbooks/` | 3 sổ tay thành viên (`HANDBOOK_A.md`, `HANDBOOK_B.md`, `HANDBOOK_C.md`) kèm 90 câu viva |
| 10 | `research/` | 5 tài liệu nghiên cứu kỹ thuật nền tảng (`RESEARCH_LEDGER.md`, v.v.) |
| 11 | `scratch/verify_wbs.py` | Script Python tự động đối soát Master WBS |
| 12 | `scratch/wbs_verification_log.txt` | Nhật ký chi tiết kết quả chạy đối soát Master WBS (7/7 pass) |
| 13 | `scratch/verify_protocol_catalog.py` | Script Python tự động đối soát tập hợp danh mục bản tin wire |
| 14 | `scratch/protocol_catalog_verification_log.txt` | Nhật ký chi tiết kết quả chạy đối soát catalog (69/69 pass) |
| 15 | `scratch/test_frame_codec/` | Mã nguồn C# (.NET 9) kiểm chứng bộ giải mã khung TCP |
| 16 | `scratch/test_frame_codec_run.log` | Nhật ký console thực tế chạy test harness (5/5 pass) |
| 17 | `AUDIT_RESPONSE_CHECKPOINT_1.md` | Báo cáo phản hồi chi tiết ma trận finding với 5 trạng thái chuẩn |
| 18 | `AUDIT_HANDOFF.md` | Biên bản bàn giao hồ sơ Checkpoint 1 Revision 4.1 |
| 19 | `MANIFEST_SHA256.txt` | Bảng mã băm SHA-256 của toàn bộ 38 file thành phần bên trong ZIP |
