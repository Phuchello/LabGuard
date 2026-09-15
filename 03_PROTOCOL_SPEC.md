# LABGUARD — WIRE PROTOCOL SPECIFICATION (03_PROTOCOL_SPEC.md)
**Dự án:** LABGUARD — Lightweight Network Monitoring & Incident Management System for Computer Labs  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Tài liệu:** Đặc tả giao thức truyền thông nhị phân & Danh mục bản tin chuẩn (Wire Protocol Specification)  
**Baseline:** Checkpoint 1 Revision 4.1 (Contract & Evidence Closeout)  
**Ngày chuẩn hóa:** 13/09/2026  

---

## 1. NGUYÊN TẮC ĐÓNG KHUNG & ENVELOPE (FRAMING & ROOT ENVELOPE)

### 1.1. Bản chất dòng byte & Đóng khung Length-Prefix
Giao thức mạng của LABGUARD hoạt động trên nền TCP (RFC 9293). Vì TCP là dòng byte liên tục (Byte Stream) không lưu giữ ranh giới bản tin, LABGUARD áp dụng cơ chế đóng khung Length-Prefix chuẩn hóa:
- **Header:** 4 byte số nguyên không dấu Big-Endian (Network Byte Order, RFC 8259), biểu diễn độ dài payload $N$ byte ($0 < N \le 262,144$ byte = 256 KiB). Header không bao gồm chính nó trong giá trị $N$.
- **Payload:** $N$ byte dữ liệu mã hóa ký tự UTF-8 biểu diễn đối tượng JSON hợp lệ (RFC 8259).
- **Quy tắc an toàn luồng:** Hàm đọc `ReadExactlyBytesAsync` chỉ cấp phát mảng byte bộ nhớ khi $0 < N \le 262,144$. Nếu stream bị ngắt kết nối giữa chừng, hệ thống ném `EndOfStreamException` và ngắt kết nối an toàn mà không bao giờ chuyển dữ liệu bị cắt cụt cho bộ phân tích cú pháp JSON.

### 1.2. Cấu trúc Root Envelope (6 trường cố định)
Mọi bản tin trao đổi qua socket (ngoại trừ các gói forward nội bộ P1 có wrapper riêng) đều sử dụng một cấu trúc Envelope JSON chuẩn hóa gồm đúng 6 trường:

```json
{
  "version": 1,
  "messageType": "MESSAGE_TYPE_NAME",
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "deviceId": "LAB01-PC05",
  "sentAtUtc": "2026-09-13T08:30:00.1234567Z",
  "payload": {}
}
```

- `version` (int): Phiên bản của định dạng envelope (hiện tại là 1).
- `messageType` (string): Tên định danh thông điệp viết hoa (Uppercase Enum) thuộc danh mục chuẩn.
- `requestId` (string, UUIDv4): Mã định danh tương quan giữa Request và Response (Correlation ID); không dùng làm khóa khử trùng lặp nghiệp vụ.
- `deviceId` (string): Tên hoặc mã định danh của máy trạm hoặc thiết bị gửi.
- `sentAtUtc` (string, ISO-8601 UTC): Mốc thời gian gửi bản tin từ client.
- `payload` (object): Đối tượng dữ liệu nghiệp vụ chi tiết.
- *Lưu ý về cờ `isReplay`:* Đối với bản tin telemetry và trạng thái dịch vụ, cờ `isReplay` nằm bên trong đối tượng `payload`, không nằm ở root envelope để tránh sai lệch ngữ cảnh với các thông điệp khác.

---

## 2. MESSAGE CATALOG (STANDARD CORE PROTOCOL)

Danh mục bản tin chuẩn cốt lõi (P0) bao gồm đầy đủ các cặp Request, Response và Event:

### 2.1. Handshake & Transport Greeting
- `HELLO_AGENT`: Agent gửi chào Central Server ngay sau khi bắt tay TLS thành công.
- `HELLO_ADMIN`: Admin WinForms gửi chào Central Server ngay sau khi bắt tay TLS thành công.
- `HELLO_ACK`: Central Server phản hồi chấp nhận phiên kết nối tầng ứng dụng, trả về thông tin server và capabilities.

### 2.2. Agent Authentication
- `AUTH_AGENT`: Agent gửi định danh `deviceId` và chuỗi pre-shared token để xác thực máy trạm.
- `AUTH_RESULT`: Central Server phản hồi kết quả xác thực Agent (thành công kèm `sessionId`, hoặc từ chối).

### 2.3. Heartbeat & Liveness
- `HEARTBEAT`: Agent gửi nhịp tim định kỳ (chu kỳ 5 giây) để duy trì trạng thái hoạt động.
- `HEARTBEAT_ACK`: Central Server phản hồi xác nhận nhịp tim và đồng bộ `serverTimeUtc`.
- *Quy tắc Liveness:* Thiết bị bị đánh dấu `DEVICE_OFFLINE` khi Central Server không nhận được heartbeat hợp lệ trong khoảng thời gian lớn hơn 20 giây (`> 20s`), đo bằng đồng hồ đơn điệu `Stopwatch.GetTimestamp()` tại server.

### 2.4. Telemetry, Service Status & Configuration
- `METRIC_BATCH`: Agent gửi mẻ chỉ số đo đạc chuẩn MVP (CPU, RAM, NIC, Uptime; Disk loại khỏi MVP để tránh lệch contract); cờ `isReplay` đặt trong payload; khử trùng lặp bằng khóa `DeviceId + BootId + BatchId` (bảng `ProcessedBatches`, TTL 24h).
- `BATCH_ACK`: Central Server phản hồi xác nhận đã lưu trữ mẻ telemetry vào CSDL, làm căn cứ để Agent giải phóng hàng đợi.
- `SERVICE_STATUS`: Agent gửi kết quả probe TCP tới các cổng allowlist nội bộ ($\le 5$ cổng, timeout 2s, chu kỳ 5s); cờ `isReplay` trong payload.
- `CONFIG_UPDATE`: Central Server đẩy bản tin cập nhật cấu hình chu kỳ lấy mẫu và danh sách cổng probe cho Agent.
- `CONFIG_RESULT`: Agent phản hồi kết quả áp dụng cấu hình mới.
- `SERVICE_CHECK_REQUEST`: Central Server yêu cầu Agent kiểm tra tức thời một dịch vụ cụ thể.

### 2.5. Disconnect & Keepalive
- `AGENT_GOODBYE`: Agent thông báo ngắt kết nối chủ động (tắt máy/khởi động lại) để server giải phóng tài nguyên mà không kích hoạt cảnh báo timeout giả.
- `GOODBYE_ACK`: Central Server phản hồi xác nhận đã nhận thông báo ngắt kết nối an toàn.
- `PING`: Thăm dò kết nối TCP và kiểm tra độ trễ mạng hai chiều (Round-Trip Time).
- `PONG`: Phản hồi gói tin thăm dò kèm mốc thời gian nhận.

### 2.6. Admin Auth & Identity Management
- `LOGIN`: Người dùng gửi thông tin đăng nhập (username + password) ở trạng thái pre-auth.
- `LOGIN_RESULT`: Central Server phản hồi kết quả đăng nhập; mật khẩu được kiểm tra bằng PBKDF2-HMAC-SHA256 (600,000 vòng lặp, salt 16 byte ngẫu nhiên, so sánh thời gian cố định `CryptographicOperations.FixedTimeEquals`), cấp session token và vai trò người dùng (Admin, Operator, Viewer).
- `SIGN_UP`: Điểm cuối đăng ký tiền xác thực (Pre-auth / Unauthenticated endpoint). Người dùng chưa xác thực gửi yêu cầu tạo tài khoản mới; bắt buộc cung cấp `inviteCode` hợp lệ, chưa sử dụng và còn hạn. Central Server xác thực atomic và tiêu thụ mã mời một lần duy nhất (consume once).
- `SIGN_UP_RESULT`: Central Server phản hồi kết quả đăng ký cho `SIGN_UP`.
- `CREATE_INVITE`: Admin tạo mã mời đăng ký người dùng mới với vai trò và thời hạn xác định (Admin-only).
- `INVITE_RESULT`: Central Server trả về mã mời đăng ký đã được tạo cho Admin.
- `LOGOUT`: Admin gửi yêu cầu đăng xuất khỏi hệ thống.
- `LOGOUT_RESULT`: Central Server xác nhận đã hủy phiên làm việc và thu hồi session token.

### 2.7. Device & Metrics Queries
- `LIST_DEVICES`: Admin truy vấn danh sách máy trạm có phân trang và bộ lọc.
- `DEVICE_PAGE`: Central Server trả về trang danh sách máy trạm.
- `GET_DEVICE`: Admin truy vấn thông tin chi tiết của một máy trạm cụ thể.
- `DEVICE_DETAIL`: Central Server trả về thông tin chi tiết phần cứng và trạng thái của máy trạm.
- `GET_METRICS`: Admin truy vấn lịch sử chỉ số tài nguyên của máy trạm theo khoảng thời gian.
- `METRICS_RESULT`: Central Server trả về danh sách mẫu chỉ số tài nguyên.

### 2.8. Incident Queries, History & Reports
- `LIST_INCIDENTS`: Admin truy vấn danh sách sự cố có phân trang và lọc theo trạng thái.
- `INCIDENT_PAGE`: Central Server trả về trang danh sách sự cố.
- `GET_INCIDENT`: Admin truy vấn chi tiết một phiếu sự cố cụ thể.
- `INCIDENT_DETAIL`: Central Server trả về chi tiết sự cố kèm lịch sử xử lý.
- `GET_HISTORY`: Admin truy vấn nhật ký kiểm toán và lịch sử biến động hệ thống.
- `HISTORY_PAGE`: Central Server trả về trang dữ liệu lịch sử kiểm toán.
- `GET_SERVER_HEALTH`: Admin truy vấn thông số sức khỏe và hiệu năng của chính Central Server.
- `SERVER_HEALTH`: Central Server trả về trạng thái CPU, bộ nhớ, số lượng kết nối đang mở và thông số CSDL.
- `GET_REPORT`: Admin truy vấn báo cáo tổng hợp sự cố và thời gian hoạt động của phòng máy.
- `REPORT_RESULT`: Central Server trả về số liệu thống kê báo cáo tổng hợp.

### 2.9. Subscription & Snapshot Sync
- `SUBSCRIBE_DEVICES`: Admin đăng ký nhận luồng sự kiện thời gian thực của máy trạm.
- `SUBSCRIBE_INCIDENTS`: Admin đăng ký nhận luồng sự kiện thời gian thực của phiếu sự cố.
- `SUBSCRIBE_HEALTH`: Admin đăng ký nhận luồng sự kiện sức khỏe hệ thống.
- `SNAPSHOT_BEGIN`: Central Server gửi tín hiệu bắt đầu truyền snapshot trạng thái, Admin thiết lập hàng rào (Barrier) lưu tạm delta event.
- `SNAPSHOT_PAGE`: Central Server truyền từng trang dữ liệu snapshot vào bộ nhớ cache client.
- `SNAPSHOT_END`: Central Server báo kết thúc snapshot, Admin giải phóng barrier và bắt đầu xử lý các delta event đã nhận.
- `UNSUBSCRIBE`: Admin hủy đăng ký nhận luồng sự kiện thời gian thực.
- `UNSUBSCRIBE_RESULT`: Central Server xác nhận hủy đăng ký kênh sự kiện.

### 2.10. Realtime Event Broadcast
- `DEVICE_UPDATE`: Central Server phát sóng biến động trạng thái máy trạm (Online, Offline, CPU/RAM cao).
- `INCIDENT_OPEN`: Central Server phát sóng sự cố mới được phát hiện bởi Rule Engine.
- `INCIDENT_UPDATE`: Central Server phát sóng cập nhật trạng thái kỹ thuật (ConditionState) hoặc trạng thái xử lý (WorkflowState) của sự cố.
- `HEALTH_UPDATE`: Central Server phát sóng định kỳ thông số sức khỏe của máy chủ hoặc Collector.

### 2.11. Incident Actions & Error Handling
- `INCIDENT_ACK`: Admin gửi lệnh tiếp nhận xử lý sự cố kèm `expectedRowVersion`.
- `INCIDENT_ASSIGN`: Admin gửi lệnh phân công sự cố cho kỹ thuật viên kèm `expectedRowVersion`.
- `INCIDENT_START`: Admin gửi lệnh bắt đầu khắc phục sự cố kèm `expectedRowVersion`.
- `INCIDENT_RESOLVE`: Admin gửi lệnh đóng sự cố kèm `expectedRowVersion` (chỉ cho phép khi máy đã `RECOVERED`).
- `COMMAND_RESULT`: Central Server phản hồi kết quả thực thi lệnh quản trị (SUCCESS hoặc 409 CONFLICT).
- `ERROR`: Thông báo lỗi giao thức hoặc từ chối xử lý request.

---

## 3. INTERNAL PROTOCOL P1 (GATEWAY / COLLECTOR ROUTING)

Các thông điệp nội bộ phân tán tầng P1 phục vụ kết nối giữa Gateway, Collector và Central Server:

- `HELLO_COLLECTOR`: Collector gửi chào Gateway hoặc Central Server khi khởi động phiên nội bộ.
- `AUTH_COLLECTOR`: Collector xác thực phiên làm việc nội bộ bằng chứng chỉ số hoặc pre-shared service key.
- `FORWARD_OPEN`: Gateway thông báo mở phiên chuyển tiếp kết nối từ một client mới.
- `FORWARD_DATA`: Gateway/Collector chuyển tiếp khung dữ liệu giữa client và backend qua socket gom.
- `FORWARD_RESULT`: Phản hồi kết quả xử lý chuyển tiếp dữ liệu.
- `FORWARD_CLOSE`: Gateway/Collector thông báo đóng phiên chuyển tiếp kết nối.
- `CLOSE_SESSION`: Lệnh đóng cưỡng bức một phiên kết nối cụ thể từ tầng quản trị.
- `COLLECTOR_HEARTBEAT`: Collector gửi nhịp tim định kỳ báo cáo sức khỏe và số lượng client đang phục vụ.
- `HEALTH_PROBE`: Gateway thăm dò tình trạng sẵn sàng phục vụ (Readiness Probe) của Collector.
- `HEALTH_RESULT`: Collector phản hồi trạng thái sức khỏe và tải hiện tại cho Gateway.

---

## 4. SEMANTICS & INVARIANTS

### 4.1. Khử trùng lặp & Thuật toán Canonical RequestDigest
- **Telemetry Ingestion:** Khóa `DeviceId + BootId + BatchId` được lưu trong bảng SQL Server `ProcessedBatches` với TTL 24 giờ. Bản tin trùng lặp sẽ bị bỏ qua bước ghi đĩa nhưng vẫn nhận lại `BATCH_ACK` để giải phóng hàng đợi phía client.
- **Admin Mutation Commands:** Khóa `UserId + CommandId` kèm mã băm `RequestDigest` được lưu trong bảng SQL Server `ProcessedCommands` với TTL 7 ngày.
- **Thuật toán Server-side Canonical RequestDigest (Deterministic Algorithm):**
  1. Phía Server nhận bản tin lệnh và deserialize payload thành đối tượng strongly-typed `CommandPayloadDto`.
  2. Trích xuất tập thuộc tính chuẩn mực cố định:
     - `commandType` (chuỗi in hoa bất biến: `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`).
     - `incidentId` (số nguyên `long`).
     - `expectedRowVersion` (chuỗi hex chuẩn hóa 18 ký tự `0x...` viết thường).
     - `commandParams` (từ điển tham số chuẩn hóa: các khóa sắp xếp theo thứ tự bảng chữ cái ASCII; giá trị null biểu diễn rõ ràng; chuỗi ký tự chuẩn hóa UTF-8 NFC; số nguyên/thực không chứa ký tự mũ).
     - `commandId` (chuỗi UUIDv4 viết thường 36 ký tự chuẩn).
  3. Tuần tự hóa đối tượng DTO trên thành mảng byte UTF-8 theo thứ tự trường xác định cố định: `commandId` $\rightarrow$ `commandParams` $\rightarrow$ `commandType` $\rightarrow$ `expectedRowVersion` $\rightarrow$ `incidentId`, không chèn khoảng trắng phân cách thừa giữa các token JSON.
  4. Tính toán mã băm SHA-256 (`SHA256.HashData(canonicalBytes)`) trên chuỗi byte chuẩn hóa để tạo ra 64 ký tự hex viết thường của `RequestDigest`. Thuật toán này hoàn toàn độc lập với thứ tự thuộc tính hoặc định dạng khoảng trắng tùy ý từ JSON client.
  5. Xử lý trùng lặp:
     - Nếu trùng `(UserId, CommandId)` và `RequestDigest` khớp: Trả lại ngay kết quả `CommandResult` đã lưu trước đó (Idempotent replay), không thực thi mutation lần thứ hai.
     - Nếu trùng `(UserId, CommandId)` nhưng `RequestDigest` khác: Từ chối lệnh với mã lỗi `ERR_IDEMPOTENCY_KEY_REUSE` (409 Conflict).

### 4.2. Optimistic Concurrency Control (OCC) với `rowversion`
- **Bản chất kỹ thuật:** `rowversion` là datatype riêng của SQL Server. Giá trị lưu trữ có kích thước 8 byte và do SQL Server tự sinh khi rowversion row được insert/update. Kiểu `binary(8)` thông thường KHÔNG có semantics tự cập nhật này. `rowversion` KHÔNG phải date/time.
- Khi Admin/Operator gửi lệnh mutation (`INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`), client gửi kèm `expectedRowVersion`.
- Câu lệnh SQL kiểm tra nguyên tử:
  ```sql
  UPDATE Incidents
  SET WorkflowState = @NewState, LastModifiedUtc = SYSUTCDATETIME()
  WHERE IncidentId = @IncidentId AND RowVersion = @ExpectedRowVersion;
  ```
  Nếu số dòng ảnh hưởng (`@@ROWCOUNT`) bằng 0, chứng tỏ dữ liệu đã bị sửa đổi bởi phiên làm việc khác; Server trả về mã lỗi `ERR_CONCURRENCY_VIOLATION` (409 Conflict) và gửi kèm dữ liệu mới nhất để client đồng bộ lại.

### 4.3. Ma trận Phân quyền Chuẩn mực (Canonical RBAC Matrix)
Hệ thống thiết lập 3 vai trò người dùng chuẩn hóa và bất biến trên toàn hệ thống:
- **`Viewer` (Chế độ chỉ xem):** Read-only. Được tra cứu danh sách thiết bị, xem chi tiết sự cố, đăng ký nhận sự kiện realtime. Hoàn toàn KHÔNG có quyền mutation; mọi lệnh thay đổi trạng thái đều bị Central Server từ chối với lỗi `ERR_UNAUTHORIZED` (403 Forbidden).
- **`Operator` (Kỹ thuật viên vận hành trực ban):** Toàn bộ quyền của `Viewer` + Quyền tác nghiệp xử lý sự cố trong phạm vi vận hành phòng máy: Tiếp nhận sự cố (`INCIDENT_ACK`), Bắt đầu xử lý (`INCIDENT_START`), và Đóng sự cố đã phục hồi (`INCIDENT_RESOLVE`). Không có quyền phân công người khác hoặc quản trị tài khoản.
- **`Admin` (Quản trị viên hệ thống):** Toàn bộ quyền của `Operator` + Quyền điều phối phân công sự cố (`INCIDENT_ASSIGN`) + Quyền tạo mã mời tài khoản (`CREATE_INVITE`) và cập nhật cấu hình hệ thống (`CONFIG_UPDATE`).
- **Điểm cuối tiền xác thực (Pre-auth / Unauthenticated Endpoints):**
  - `LOGIN`: Xác thực tài khoản người dùng thông thường (pre-auth).
  - `SIGN_UP`: Điểm cuối đăng ký tài khoản (pre-auth / unauthenticated), nhưng BẮT BUỘC phải kèm theo `inviteCode` hợp lệ, chưa sử dụng và còn hiệu lực. Central Server kiểm tra atomic và tiêu thụ mã mời 1 lần duy nhất (consume once). `SIGN_UP_RESULT` là phản hồi từ server.

| Chức năng / Thông điệp | Viewer | Operator | Admin | Ghi chú an ninh |
| :--- | :---: | :---: | :---: | :--- |
| `LOGIN` | Pre-auth | Pre-auth | Pre-auth | Xác thực danh tính ban đầu trước khi cấp session token |
| `SIGN_UP` | Pre-auth | Pre-auth | Pre-auth | Đăng ký tài khoản mới; bắt buộc `inviteCode` hợp lệ, tiêu thụ 1 lần |
| `LIST_DEVICES`, `GET_DEVICE`, `LIST_INCIDENTS`, `GET_INCIDENT` | Cho phép | Cho phép | Cho phép | Truy vấn dữ liệu chỉ đọc |
| `SUBSCRIBE_DEVICES`, `SUBSCRIBE_INCIDENTS` | Cho phép | Cho phép | Cho phép | Nhận Snapshot & Realtime Events |
| `INCIDENT_ACK`, `INCIDENT_START`, `INCIDENT_RESOLVE` | Từ chối | Cho phép | Cho phép | Mutation xử lý sự cố trong ca trực |
| `INCIDENT_ASSIGN` | Từ chối | Từ chối | Cho phép | Điều phối và phân công nhân sự |
| `CREATE_INVITE`, `CONFIG_UPDATE` | Từ chối | Từ chối | Cho phép | Quản trị tạo mã mời và cấu hình hệ thống (Admin-only) |

*Nguyên tắc tối thượng:* Mọi kiểm tra quyền RBAC bắt buộc phải được thực thi tại Central Server (Server-Side Authorization). Việc ẩn hoặc vô hiệu hóa nút bấm trên giao diện WinForms Admin Client chỉ đóng vai trò tối ưu hóa trải nghiệm người dùng (UX optimization).

### 4.4. Bối cảnh Chuẩn mật mã PBKDF2
- **Khuyến nghị chuẩn mực OWASP:** OWASP recommends Argon2id for new systems generally. If FIPS-140 compliance is required, OWASP recommends PBKDF2-HMAC-SHA256 with a work factor of 600,000 or more. LabGuard deliberately chooses PBKDF2 for BCL availability and course-scope simplicity; 600,000 is therefore the project baseline referenced from OWASP and must still be measured on the actual lab hardware.
- Dự án LabGuard quyết định lựa chọn `PBKDF2-HMAC-SHA256` dựa trên tính sẵn có tự nhiên trong thư viện chuẩn .NET BCL (`Rfc2898DeriveBytes`), tính đơn giản không phụ thuộc thư viện native C/C++ bên ngoài, và hoàn toàn phù hợp với phạm vi môn học NT106.
- Mốc **600,000 vòng lặp** là baseline kỹ thuật của dự án tham chiếu theo khuyến nghị OWASP; salt ngẫu nhiên 16 byte (128-bit) và derived key 32 byte (256-bit) là các tham số thiết kế chuẩn của hệ thống; so sánh hash an toàn bằng `CryptographicOperations.FixedTimeEquals`.

### 4.5. Phân định Cổng phạm vi giao thức (Protocol Scope Gate)
Danh mục 69 bản tin wire chuẩn được phân định thành 3 mức độ ưu tiên triển khai rõ ràng:
- **Xác thực P0-MVP REQUIRED:** Hệ thống sử dụng tài khoản Admin khởi tạo sẵn trong CSDL (pre-seeded/bootstrap Admin) -> Login -> vận hành bình thường.
- **Quy trình mở rộng tùy chọn (Optional enrollment extension):** Admin phát hành mã mời qua CREATE_INVITE -> người dùng chưa xác thực đăng ký qua SIGN_UP kèm mã mời hợp lệ -> Login.
1. **`P0-MVP REQUIRED` (34 bản tin - Cam kết bắt buộc cho nghiệm thu MVP 5 tuần):**
   - *Greeting & Auth Agent (5):* `HELLO_AGENT`, `HELLO_ADMIN`, `HELLO_ACK`, `AUTH_AGENT`, `AUTH_RESULT`.
   - *Liveness & Telemetry Core (5):* `HEARTBEAT`, `HEARTBEAT_ACK`, `METRIC_BATCH`, `BATCH_ACK`, `SERVICE_STATUS`.
   - *Disconnect (2):* `AGENT_GOODBYE`, `GOODBYE_ACK`.
   - *Admin Auth (4):* `LOGIN`, `LOGIN_RESULT`, `LOGOUT`, `LOGOUT_RESULT`.
   - *Core Queries (4):* `LIST_DEVICES`, `DEVICE_PAGE`, `LIST_INCIDENTS`, `INCIDENT_PAGE`.
   - *Subscriptions & Snapshot (5):* `SUBSCRIBE_DEVICES`, `SUBSCRIBE_INCIDENTS`, `SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END`.
   - *Realtime Broadcast (3):* `DEVICE_UPDATE`, `INCIDENT_OPEN`, `INCIDENT_UPDATE`.
   - *Incident Mutation & Error (6):* `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`, `COMMAND_RESULT`, `ERROR`.
2. **`P0 OPTIONAL` (25 bản tin - Mở rộng phi chặn, hiện thực khi còn thời gian):**
   - `CONFIG_UPDATE`, `CONFIG_RESULT`, `SERVICE_CHECK_REQUEST`, `PING`, `PONG`, `SIGN_UP`, `SIGN_UP_RESULT`, `CREATE_INVITE`, `INVITE_RESULT`, `GET_DEVICE`, `DEVICE_DETAIL`, `GET_METRICS`, `METRICS_RESULT`, `GET_INCIDENT`, `INCIDENT_DETAIL`, `GET_HISTORY`, `HISTORY_PAGE`, `GET_SERVER_HEALTH`, `SERVER_HEALTH`, `GET_REPORT`, `REPORT_RESULT`, `SUBSCRIBE_HEALTH`, `HEALTH_UPDATE`, `UNSUBSCRIBE`, `UNSUBSCRIBE_RESULT`.
3. **`P1 CONDITIONAL` (10 bản tin - Nội bộ phân tán Gateway/Collector):**
   - `HELLO_COLLECTOR`, `AUTH_COLLECTOR`, `FORWARD_OPEN`, `FORWARD_DATA`, `FORWARD_RESULT`, `FORWARD_CLOSE`, `CLOSE_SESSION`, `COLLECTOR_HEARTBEAT`, `HEALTH_PROBE`, `HEALTH_RESULT`.
