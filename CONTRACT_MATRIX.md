# MA TRẬN GIAO ƯỚC KỸ THUẬT (CONTRACT MATRIX) — LABGUARD PROTOCOL v1.0
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Phiên bản Giao ước (Contract Version):** `1.0.0-frozen` (Ngày 13/09/2026)  
**Tổ chức phụ trách:** Sinh viên A chủ trì `Shared.Protocol`; Sinh viên B và Sinh viên C tham gia review.  

---

## I. NGUYÊN TẮC CỐT LÕI

1. `Shared.Protocol` là thư viện mã nguồn dùng chung duy nhất cho cả 3 thành phần: Agent, Central Server, Admin Client.
2. `Shared.Protocol` chỉ chứa: `FrameCodec`, DTO (Data Transfer Objects), hằng số ranh giới/kích thước, danh mục Enum loại bản tin chuẩn, và các mã lỗi (Error Codes).
3. `Shared.Protocol` không chứa mã nguồn truy vấn CSDL (SQL/Dapper/EF), mã nguồn WinForms UI, hay logic quy tắc sự cố (Rule Engine).
4. Catalog được đối chiếu bằng phép so sánh tập hợp tên message với `03_PROTOCOL_SPEC.md` (bao gồm toàn bộ Core và Internal P1); bảng trong đề cương `main.pdf` là danh sách rút gọn các bản tin tiêu biểu.

---

## II. CẤU TRÚC KHUNG TRUYỀN DÙNG CHUNG (WIRE ENVELOPE)

### 1. Phân khung nhị phân (Binary Length-Prefix Framing)
- **4 byte Header:** Số nguyên 32-bit không dấu (`uint32`) theo thứ tự byte mạng Big-Endian (RFC 1700), biểu diễn độ dài payload $N$ byte ($0 < N \le 256\text{ KiB} = 262,144\text{ bytes}$).
- **$N$ byte Payload:** Chuỗi JSON mã hóa UTF-8 tuân thủ RFC 8259.
- **Ranh giới khung truyền:** Độ dài $N$ không tính 4 byte header. Tổng số byte trên socket là $4 + N$.
- **Xử lý ngắt kết nối (EOF):** Bộ đọc khung truyền sử dụng `ReadExactlyBytesAsync`. Nếu kết nối bị đóng trước khi nhận đủ 4 byte prefix hoặc đủ $N$ byte payload, phương thức ném ngay ngoại lệ `EndOfStreamException` và hủy stream an toàn; không bao giờ chuyển dữ liệu bị cắt cụt sang bộ giải mã JSON.

### 2. Envelope JSON thống nhất (Outer Envelope)
Mọi bản tin wire trao đổi đều dùng đúng 6 trường envelope cố định:
```json
{
  "version": 1,
  "messageType": "<EXACT_MESSAGE_TYPE_UPPERCASE>",
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "deviceId": "PC-LAB1-07",
  "sentAtUtc": "2026-09-13T07:15:30.1234567Z",
  "payload": { }
}
```
*Ghi chú chuẩn hóa:*
- `sentAtUtc`: Mốc thời gian ISO-8601 UTC do bên gửi tạo ra. Không dùng tên trường `timestamp`.
- `requestId`: Định danh UUID v4 duy nhất của frame phục vụ đối soát tương quan (Correlation ID) giữa Request và Response; không dùng làm khóa khử trùng lặp nghiệp vụ.
- `isReplay`: Đối với bản tin telemetry và service status, cờ `isReplay` nằm **bên trong `payload`**, không đặt ở root envelope.

---

## III. DANH MỤC BẢN TIN WIRE CHUẨN (ĐỐI CHIẾU 03_PROTOCOL_SPEC.MD)

Catalog được đối chiếu bằng phép so sánh tập hợp tên message với `03_PROTOCOL_SPEC.md` gồm toàn bộ 69 thông điệp (59 Core + 10 Internal P1).

### 1. Handshake & Transport Greeting
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Agent $\rightarrow$ Central | SV A | SV B | `HELLO_AGENT` | `HELLO_ACK` | Bắt tay tầng ứng dụng sau kết nối TLS | `requestId` correlation |
| Admin $\rightarrow$ Central | SV C | SV B | `HELLO_ADMIN` | `HELLO_ACK` | Bắt tay tầng ứng dụng từ Admin Client | `requestId` correlation |
| Central $\rightarrow$ Node | SV B | SV A/C | `HELLO_ACK` | *(Hoàn tất)* | Chấp nhận phiên kết nối tầng ứng dụng | Session gắn socket |

### 2. Agent Authentication
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Agent $\rightarrow$ Central | SV A | SV B | `AUTH_AGENT` | `AUTH_RESULT` | Xác thực máy trạm bằng DeviceId + Token | `requestId` correlation |
| Central $\rightarrow$ Agent | SV B | SV A | `AUTH_RESULT` | *(Hoàn tất)* | Cấp sessionId và chu kỳ nhịp tim | SessionStore |

### 3. Heartbeat & Liveness
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Agent $\rightarrow$ Central | SV A | SV B | `HEARTBEAT` | `HEARTBEAT_ACK` | Nhịp tim liveness chu kỳ 5s; cập nhật deadline monotonic | Không dedup |
| Central $\rightarrow$ Agent | SV B | SV A | `HEARTBEAT_ACK` | *(Hoàn tất)* | Xác nhận nhịp tim, đồng bộ serverTimeUtc | `requestId` correlation |

### 4. Telemetry, Service Status & Configuration
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Agent $\rightarrow$ Central | SV A | SV B | `METRIC_BATCH` | `BATCH_ACK` | Mẻ CPU, RAM, NIC, Uptime; cờ isReplay trong payload | `DeviceId+BootId+BatchId` (TTL 24h) |
| Central $\rightarrow$ Agent | SV B | SV A | `BATCH_ACK` | *(Xóa replay)* | Xác nhận mẻ telemetry đã commit CSDL | `batchId` |
| Agent $\rightarrow$ Central | SV A | SV B | `SERVICE_STATUS` | `BATCH_ACK` | Báo cáo probe TCP allowlist ($\le 5$ cổng, timeout 2s) | `DeviceId+BootId+BatchId` (TTL 24h) |
| Central $\rightarrow$ Agent | SV B | SV A | `CONFIG_UPDATE` | `CONFIG_RESULT` | Cập nhật cấu hình chu kỳ và allowlist cổng | Version cấu hình |
| Agent $\rightarrow$ Central | SV A | SV B | `CONFIG_RESULT` | *(Hoàn tất)* | Báo cáo kết quả áp dụng cấu hình mới | `requestId` correlation |
| Central $\rightarrow$ Agent | SV B | SV A | `SERVICE_CHECK_REQUEST` | `SERVICE_STATUS` | Yêu cầu Agent probe tức thời một dịch vụ | `requestId` correlation |

### 5. Disconnect & Keepalive
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Agent $\rightarrow$ Central | SV A | SV B | `AGENT_GOODBYE` | `GOODBYE_ACK` | Báo ngắt kết nối chủ động (tắt máy/reboot) | Xóa session an toàn |
| Central $\rightarrow$ Agent | SV B | SV A | `GOODBYE_ACK` | *(Đóng socket)* | Xác nhận giải phóng tài nguyên phiên | Xóa session |
| Node $\leftrightarrow$ Central | SV A/B/C | SV B/A/C | `PING` | `PONG` | Thăm dò sức khỏe kết nối TCP và đo RTT | Sequence number |
| Node $\leftrightarrow$ Central | SV A/B/C | SV B/A/C | `PONG` | *(Ghi nhận RTT)* | Phản hồi gói tin thăm dò kèm mốc thời gian | Sequence number |

### 6. Admin Auth & Identity Management
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Admin $\rightarrow$ Central | SV C | SV B | `LOGIN` | `LOGIN_RESULT` | Đăng nhập Admin/Operator/Viewer (pre-auth; PBKDF2 600k rounds) | `requestId` correlation |
| Central $\rightarrow$ Admin | SV B | SV C | `LOGIN_RESULT` | *(Lưu token)* | Trả sessionToken và vai trò người dùng | Cấp session token |
| User $\rightarrow$ Central | SV C | SV B | `SIGN_UP` | `SIGN_UP_RESULT` | Đăng ký tài khoản mới (pre-auth; bắt buộc `inviteCode` hợp lệ, consume once) | `username` UNIQUE |
| Central $\rightarrow$ User | SV B | SV C | `SIGN_UP_RESULT` | *(Hoàn tất)* | Trả kết quả tạo tài khoản mới | `requestId` correlation |
| Admin $\rightarrow$ Central | SV C | SV B | `CREATE_INVITE` | `INVITE_RESULT` | Admin tạo mã mời đăng ký người dùng (Admin-only) | `inviteCode` (TTL 48h) |
| Central $\rightarrow$ Admin | SV B | SV C | `INVITE_RESULT` | *(Hoàn tất)* | Trả mã mời đã tạo kèm quyền hạn | `inviteCode` |
| Admin $\rightarrow$ Central | SV C | SV B | `LOGOUT` | `LOGOUT_RESULT` | Đăng xuất người dùng, hủy session token | Xóa session token |
| Central $\rightarrow$ Admin | SV B | SV C | `LOGOUT_RESULT` | *(Đóng phiên)* | Xác nhận đã hủy phiên làm việc trên server | `requestId` correlation |

### 7. Device & Metrics Queries
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Admin $\rightarrow$ Central | SV C | SV B | `LIST_DEVICES` | `DEVICE_PAGE` | Truy vấn phân trang danh sách máy trạm | `requestId` correlation |
| Central $\rightarrow$ Admin | SV B | SV C | `DEVICE_PAGE` | *(Hiển thị)* | Trả trang danh sách máy trạm | `pageIndex + pageSize` |
| Admin $\rightarrow$ Central | SV C | SV B | `GET_DEVICE` | `DEVICE_DETAIL` | Truy vấn thông tin chi tiết một máy trạm | `requestId` correlation |
| Central $\rightarrow$ Admin | SV B | SV C | `DEVICE_DETAIL` | *(Hiển thị)* | Trả chi tiết cấu hình và trạng thái máy trạm | `deviceId` |
| Admin $\rightarrow$ Central | SV C | SV B | `GET_METRICS` | `METRICS_RESULT` | Truy vấn lịch sử chỉ số CPU/RAM trong 48h | `requestId` correlation |
| Central $\rightarrow$ Admin | SV B | SV C | `METRICS_RESULT` | *(Vẽ đồ thị)* | Trả danh sách mẫu chỉ số tài nguyên | `requestId` correlation |

### 8. Incident Queries, History & Reports
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Admin $\rightarrow$ Central | SV C | SV B | `LIST_INCIDENTS` | `INCIDENT_PAGE` | Truy vấn phân trang danh sách sự cố | `requestId` correlation |
| Central $\rightarrow$ Admin | SV B | SV C | `INCIDENT_PAGE` | *(Nạp bảng)* | Trả trang dữ liệu danh sách sự cố | `pageIndex + pageSize` |
| Admin $\rightarrow$ Central | SV C | SV B | `GET_INCIDENT` | `INCIDENT_DETAIL` | Truy vấn chi tiết một phiếu sự cố | `requestId` correlation |
| Central $\rightarrow$ Admin | SV B | SV C | `INCIDENT_DETAIL` | *(Hiển thị)* | Trả chi tiết sự cố kèm dòng thời gian | `incidentId` |
| Admin $\rightarrow$ Central | SV C | SV B | `GET_HISTORY` | `HISTORY_PAGE` | Truy vấn phân trang nhật ký kiểm toán | `requestId` correlation |
| Central $\rightarrow$ Admin | SV B | SV C | `HISTORY_PAGE` | *(Nạp bảng)* | Trả trang nhật ký kiểm toán hệ thống | `pageIndex + pageSize` |
| Admin $\rightarrow$ Central | SV C | SV B | `GET_SERVER_HEALTH` | `SERVER_HEALTH` | Truy vấn trạng thái sức khỏe Central Server | `requestId` correlation |
| Central $\rightarrow$ Admin | SV B | SV C | `SERVER_HEALTH` | *(Hiển thị)* | Trả chỉ số CPU, RAM, CSDL, socket Central | `sentAtUtc` |
| Admin $\rightarrow$ Central | SV C | SV B | `GET_REPORT` | `REPORT_RESULT` | Truy vấn báo cáo thống kê phòng máy | `requestId` correlation |
| Central $\rightarrow$ Admin | SV B | SV C | `REPORT_RESULT` | *(Hiển thị)* | Trả số liệu tổng hợp báo cáo sự cố | `reportId` |

### 9. Subscription & Snapshot Sync
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Admin $\rightarrow$ Central | SV C | SV B | `SUBSCRIBE_DEVICES` | `SNAPSHOT_BEGIN` | Đăng ký nhận luồng biến động máy trạm | `sessionToken` |
| Admin $\rightarrow$ Central | SV C | SV B | `SUBSCRIBE_INCIDENTS` | `SNAPSHOT_BEGIN` | Đăng ký nhận luồng biến động sự cố | `sessionToken` |
| Admin $\rightarrow$ Central | SV C | SV B | `SUBSCRIBE_HEALTH` | `SNAPSHOT_BEGIN` | Đăng ký nhận luồng biến động sức khỏe | `sessionToken` |
| Central $\rightarrow$ Admin | SV B | SV C | `SNAPSHOT_BEGIN` | *(Khởi tạo barrier)* | Bắt đầu truyền snapshot phân trang | `snapshotId` |
| Central $\rightarrow$ Admin | SV B | SV C | `SNAPSHOT_PAGE` | *(Nạp cache)* | Trang dữ liệu snapshot nạp vào cache | `snapshotId + pageIndex` |
| Central $\rightarrow$ Admin | SV B | SV C | `SNAPSHOT_END` | *(Mở khóa UI)* | Kết thúc snapshot, xả delta events | `snapshotId + totalItems` |
| Admin $\rightarrow$ Central | SV C | SV B | `UNSUBSCRIBE` | `UNSUBSCRIBE_RESULT` | Hủy đăng ký nhận kênh sự kiện | `channel` enum |
| Central $\rightarrow$ Admin | SV B | SV C | `UNSUBSCRIBE_RESULT` | *(Hoàn tất)* | Xác nhận hủy đăng ký kênh sự kiện | `requestId` correlation |

### 10. Realtime Event Broadcast
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Central $\rightarrow$ Admin | SV B | SV C | `DEVICE_UPDATE` | *(One-way Event)* | Đẩy biến động máy trạm (Online/Offline/High CPU) | `eventSeq` đơn điệu |
| Central $\rightarrow$ Admin | SV B | SV C | `INCIDENT_OPEN` | *(One-way Event)* | Phát sóng sự cố mới mở do Rule Engine | `incidentId + eventSeq` |
| Central $\rightarrow$ Admin | SV B | SV C | `INCIDENT_UPDATE` | *(One-way Event)* | Cập nhật ConditionState hoặc WorkflowState | `incidentId + eventSeq` |
| Central $\rightarrow$ Admin | SV B | SV C | `HEALTH_UPDATE` | *(One-way Event)* | Báo cáo định kỳ sức khỏe máy chủ / Collector | `nodeId + sentAtUtc` |

### 11. Incident Actions & Error Handling
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Admin $\rightarrow$ Central | SV C | SV B | `INCIDENT_ACK` | `COMMAND_RESULT` | Tiếp nhận sự cố kèm `expectedRowVersion` | `UserId+CommandId` + Digest (TTL 7d) |
| Admin $\rightarrow$ Central | SV C | SV B | `INCIDENT_ASSIGN` | `COMMAND_RESULT` | Phân công nhân sự xử lý sự cố | `UserId+CommandId` + Digest (TTL 7d) |
| Admin $\rightarrow$ Central | SV C | SV B | `INCIDENT_START` | `COMMAND_RESULT` | Bắt đầu xử lý sự cố (*IN_PROGRESS*) | `UserId+CommandId` + Digest (TTL 7d) |
| Admin $\rightarrow$ Central | SV C | SV B | `INCIDENT_RESOLVE` | `COMMAND_RESULT` | Đóng sự cố (*RESOLVED*) khi đã *RECOVERED* | `UserId+CommandId` + Digest (TTL 7d) |
| Central $\rightarrow$ Admin | SV B | SV C | `COMMAND_RESULT` | *(Hoàn tất RPC)* | Kết quả thực thi lệnh quản trị | `commandId` |
| Central/Peer $\rightarrow$ Client | SV B/A/C | SV A/B/C | `ERROR` | *(Xử lý lỗi)* | Thông báo lỗi giao thức hoặc từ chối xử lý | `errorCode + requestId` |

### 12. Internal Protocol P1 (Gateway / Collector Routing)
| Chiều gửi | Bên Gửi | Bên Nhận | MessageType Gửi | MessageType Phản Hồi | Mục Đích Kỹ Thuật | Khóa Dedup & Lưu Trữ |
|:---|:---:|:---:|:---|:---|:---|:---|
| Collector $\rightarrow$ GW/Central | SV C | SV A/B | `HELLO_COLLECTOR` | `HELLO_ACK` | [P1] Khởi động phiên nội bộ Collector | Secret token nội bộ |
| Collector $\rightarrow$ Central | SV C | SV B | `AUTH_COLLECTOR` | `AUTH_RESULT` | [P1] Xác thực node Collector nội bộ | Secret token nội bộ |
| Gateway $\rightarrow$ Collector | SV A | SV C | `FORWARD_OPEN` | `FORWARD_RESULT` | [P1] Mở phiên chuyển tiếp client mới | `clientId + routeId` |
| Gateway $\leftrightarrow$ Collector | SV A | SV C | `FORWARD_DATA` | `FORWARD_RESULT` | [P1] Chuyển tiếp khung dữ liệu qua socket gom | `clientId + framePayload` |
| Gateway/Collector | SV A/C | SV C/A | `FORWARD_RESULT` | *(Hoàn tất)* | [P1] Xác nhận kết quả chuyển tiếp dữ liệu | `requestId` correlation |
| Gateway $\rightarrow$ Collector | SV A | SV C | `FORWARD_CLOSE` | `FORWARD_RESULT` | [P1] Đóng phiên chuyển tiếp của một client | `clientId` |
| Admin/Central $\rightarrow$ Node | SV B | SV A/C | `CLOSE_SESSION` | `COMMAND_RESULT` | [P1] Lệnh cưỡng bức đóng phiên client | `sessionId` |
| Collector $\rightarrow$ Central | SV C | SV B | `COLLECTOR_HEARTBEAT` | `HEARTBEAT_ACK` | [P1] Báo cáo nhịp tim và tải của Collector | `collectorId + activeClients` |
| Gateway $\rightarrow$ Collector | SV A | SV C | `HEALTH_PROBE` | `HEALTH_RESULT` | [P1] Thăm dò readiness của Collector | Monotonic timestamp |
| Collector $\rightarrow$ Gateway | SV C | SV A | `HEALTH_RESULT` | *(Ghi nhận)* | [P1] Báo cáo độ sẵn sàng phục vụ | `isReady + currentLoad` |

---

## IV. QUY TẮC KHỬ TRÙNG LẶP & LƯU TRỮ BỀN VỮNG (DEDUPLICATION & PERSISTENCE)

### 1. Phân tách rành mạch các tầng khử trùng lặp
- **Ingest Ring Buffer (Bộ nhớ RAM tại Central Server):** Là hàng đợi bộ nhớ có giới hạn (`System.Threading.Channels.Channel<T>` Bounded Queue) để điều tiết áp lực ghi đĩa (backpressure) giữa luồng mạng và luồng ghi CSDL. Ingest Ring Buffer không phải nơi chống trùng lặp bền vững vì dữ liệu sẽ mất khi máy chủ restart.
- **Bảng `ProcessedBatches` (Lưu trữ CSDL SQL Server):**
  - **Khóa chính (PK):** `(DeviceId, BootId, BatchId)`.
  - **Các cột:** `DeviceId` (NVARCHAR(64)), `BootId` (UNIQUEIDENTIFIER), `BatchId` (BIGINT), `ReceivedUtc` (DATETIME2), `Status` (TINYINT).
  - **TTL & Purge Policy:** Lưu vết 24 giờ. Job định kỳ dọn dẹp bản ghi cũ theo `ReceivedUtc < DATEADD(hour, -24, SYSUTCDATETIME())`.
  - **Hành vi xử lý:** Khi Central nhận `METRIC_BATCH` hoặc `SERVICE_STATUS`, kiểm tra nếu khóa đã tồn tại trong `ProcessedBatches`: Bỏ qua bước ghi telemetry mới, nhưng **bắt buộc gửi lại `BATCH_ACK`** để Agent xóa mẻ tin khỏi hàng đợi replay.
- **Bảng `ProcessedCommands` (Lưu trữ CSDL SQL Server):**
  - **Khóa chính (PK):** `(UserId, CommandId)`.
  - **Các cột:** `UserId` (NVARCHAR(64)), `CommandId` (UNIQUEIDENTIFIER), `RequestDigest` (CHAR(64) - mã băm SHA-256 của byte serialized canonical), `ResponsePayload` (NVARCHAR(MAX) - chuỗi JSON kết quả đã trả), `ExecutedUtc` (DATETIME2), `Status` (NVARCHAR(32)).
  - **Thuật toán Server-side Canonical RequestDigest (Deterministic Algorithm):**
    1. Server deserialize payload thành đối tượng strongly-typed `CommandPayloadDto` với tập thuộc tính chuẩn mực cố định: `commandId`, `commandParams`, `commandType`, `expectedRowVersion`, `incidentId`.
    2. Chuẩn hóa `commandParams`: các khóa sắp xếp tăng dần theo mã ASCII; giá trị null xử lý tường minh; chuỗi chuẩn hóa UTF-8 NFC; giá trị số không chứa ký tự mũ; loại bỏ các trường không thuộc nghiệp vụ.
    3. Tuần tự hóa đối tượng DTO trên thành mảng byte UTF-8 theo thứ tự trường xác định cố định: `commandId` $\rightarrow$ `commandParams` $\rightarrow$ `commandType` $\rightarrow$ `expectedRowVersion` $\rightarrow$ `incidentId`, không chèn khoảng trắng phân cách tùy ý giữa các token JSON.
    4. Băm SHA-256 (`SHA256.HashData(canonicalBytes)`) trên chuỗi byte chuẩn hóa để tạo ra 64 ký tự hex viết thường của `RequestDigest`. Thuật toán server-side này hoàn toàn độc lập với thứ tự key hay khoảng trắng từ JSON client gửi lên.
  - **TTL & Purge Policy:** Lưu vết 7 ngày theo đúng blueprint CSDL.
  - **Hành vi xử lý:** Khi nhận lệnh biến động (`INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`), Central kiểm tra:
    1. Nếu khóa `(UserId, CommandId)` đã tồn tại và `RequestDigest` trùng khớp: Trả lại ngay `ResponsePayload` đã lưu trước đó (Idempotent replay), không thực thi lại mutation lần thứ hai.
    2. Nếu khóa `(UserId, CommandId)` đã tồn tại nhưng `RequestDigest` khác: Từ chối lệnh với mã lỗi `ERR_IDEMPOTENCY_KEY_REUSE` (Conflict - phát hiện sửa đổi tham số trên cùng một ID).
- **Bảng `AuditLogs` và `IncidentHistory`:** Chỉ phục vụ truy vết lịch sử nghiệp vụ cho con người theo dõi, không được dùng thay thế cho bảng chống trùng kỹ thuật `ProcessedCommands`.

### 2. Chuẩn mực băm mật khẩu PBKDF2 & Quản lý danh tính
- **Bối cảnh chuẩn mực & Khuyến nghị OWASP:** OWASP recommends Argon2id for new systems generally. If FIPS-140 compliance is required, OWASP recommends PBKDF2-HMAC-SHA256 with a work factor of 600,000 or more. LabGuard deliberately chooses PBKDF2 for BCL availability and course-scope simplicity; 600,000 is therefore the project baseline referenced from OWASP and must still be measured on the actual lab hardware.
- **Lý do lựa chọn:** LabGuard chọn `PBKDF2-HMAC-SHA256` dựa trên tính sẵn có tự nhiên trong .NET BCL (`Rfc2898DeriveBytes`), tính đơn giản không phụ thuộc native C/C++ binaries, và phù hợp với phạm vi đồ án môn học NT106.
- **Tham số thiết kế dự án (Project Design Parameters):**
  - Salt: 16 byte (128-bit) ngẫu nhiên bằng `RandomNumberGenerator.GetBytes(16)` cho mỗi người dùng.
  - Khóa dẫn xuất (Derived Key Length): 32 byte (256-bit).
  - Work factor: 600,000 vòng lặp (iterations).
- **Lưu trữ CSDL:** Lưu rõ ràng thuật toán, số vòng lặp, salt và hash (3 cột riêng: `PasswordHash` VARBINARY(32), `PasswordSalt` VARBINARY(16), `Iterations` INT).
- **So sánh hash an toàn:** Sử dụng `CryptographicOperations.FixedTimeEquals` để so sánh byte mảng thời gian không đổi, chống hoàn toàn tấn công đo thời gian (Timing Attack).
- **Phân định an ninh Server-Side:** Central Server bắt buộc phải kiểm tra quyền (Authorization) trước khi thực thi bất kỳ command nào; việc ẩn/hiện nút trên WinForms Client chỉ là tối ưu hóa trải nghiệm người dùng (UX optimization).

### 3. Kiểm soát tương tranh lạc quan (OCC) với `rowversion`
- **Bản chất kỹ thuật:** `rowversion` là datatype riêng của SQL Server. Giá trị lưu trữ có kích thước 8 byte và do SQL Server tự sinh khi rowversion row được insert/update. Kiểu `binary(8)` thông thường KHÔNG có semantics tự cập nhật này. `rowversion` KHÔNG phải date/time.
- **Xử lý phía Client:** Ứng dụng WinForms coi giá trị `rowversion` là một token nhị phân opaque 8 byte (chuỗi hex `0x...`), không tự ý phân tích hay chỉnh sửa giá trị nhị phân này.
- **Câu lệnh kiểm tra OCC tại Server:**
  ```sql
  UPDATE Incidents 
  SET WorkflowState = @NewState, AssignedTo = @UserId, LastModifiedUtc = SYSUTCDATETIME()
  WHERE IncidentId = @IncidentId AND RowVersion = @ExpectedRowVersion;
  ```
  Nếu số dòng ảnh hưởng (`@@ROWCOUNT`) bằng 0, Central Server ném ngay lỗi `ERR_CONCURRENCY_VIOLATION` (409 Conflict) và trả về trạng thái sự cố mới nhất.

### 4. Điều phối luồng giao diện WinForms (UI Thread Marshaling)
- **Bản chất STA Thread:** Mọi control WinForms gắn liền với luồng giao diện chính (Single Threaded Apartment).
- **Kỹ thuật điều phối:**
  - `Control.Invoke`: Điều phối đồng bộ (chặn luồng gọi cho đến khi UI cập nhật xong), chỉ dùng khi luồng mạng bắt buộc phải chờ kết quả từ UI.
  - `Control.BeginInvoke`: Điều phối bất đồng bộ không chặn (Post message vào message loop của UI), được khuyến nghị sử dụng cho các cập nhật delta event thời gian thực nhằm tránh nguy cơ deadlock giữa luồng I/O mạng và luồng giao diện.
  - `Control.InvokeAsync`: Phương thức mở rộng trên nền tảng .NET hiện đại hỗ trợ mô hình `async/await` mượt mà.

### 5. Cơ chế Liveness & Heartbeat Timeout
- Agent gửi gói tin `HEARTBEAT` định kỳ 5 giây một lần.
- Cảnh báo `DEVICE_OFFLINE` kích hoạt khi Central Server không nhận heartbeat hợp lệ trong khoảng thời gian lớn hơn 20 giây (`> 20s`), đo bằng đồng hồ đơn điệu `Stopwatch.GetTimestamp()` tại server, độc lập với đồng hồ client.

### 6. Ma trận Phân quyền Chuẩn mực (Canonical RBAC Matrix)
| Chức năng / Nhóm thông điệp | Viewer | Operator | Admin | Xử lý an ninh phía Server |
|:---|:---:|:---:|:---:|:---|
| **Xác thực danh tính** (`LOGIN`) | Pre-auth | Pre-auth | Pre-auth | Xác thực username/password trước khi cấp session token |
| **Đăng ký tài khoản** (`SIGN_UP`) | Pre-auth | Pre-auth | Pre-auth | Endpoint tiền xác thực; bắt buộc `inviteCode` hợp lệ, kiểm tra atomic & consume 1 lần |
| **Giám sát chỉ đọc** (`LIST_DEVICES`, `GET_DEVICE`, `LIST_INCIDENTS`, `GET_INCIDENT`) | Cho phép | Cho phép | Cho phép | Trả dữ liệu phân trang hoặc đối tượng chi tiết |
| **Đăng ký nhận Realtime Events** (`SUBSCRIBE_DEVICES`, `SUBSCRIBE_INCIDENTS`) | Cho phép | Cho phép | Cho phép | Mở kênh Snapshot phân trang và đẩy delta event |
| **Xử lý sự cố trực ban** (`INCIDENT_ACK`, `INCIDENT_START`, `INCIDENT_RESOLVE`) | Từ chối | Cho phép | Cho phép | Server kiểm tra quyền; Viewer bị trả `ERR_UNAUTHORIZED` (403) |
| **Phân công sự cố** (`INCIDENT_ASSIGN`) | Từ chối | Từ chối | Cho phép | Chỉ Admin có quyền điều phối nhân sự xử lý |
| **Quản trị hệ thống** (`CREATE_INVITE`, `CONFIG_UPDATE`) | Từ chối | Từ chối | Cho phép | Quản trị tạo mã mời (Admin-only) và cấu hình Agent (Admin-only) |

*Nguyên tắc:* Toàn bộ quyền hạn kiểm tra bắt buộc tại Central Server (Server-Side Authorization). Việc ẩn/disable nút bấm trên giao diện WinForms chỉ là hỗ trợ trải nghiệm người dùng (UX optimization).

### 7. Phân định Cổng phạm vi giao thức (Protocol Scope Gate)
- **`P0-MVP REQUIRED` (34 thông điệp):** Cam kết bắt buộc hoàn thành cho nghiệm thu MVP 5 tuần. Xác thực danh tính trong MVP vận hành với tài khoản Admin được khởi tạo sẵn trong CSDL (pre-seeded/bootstrap Admin) $\rightarrow$ `LOGIN` $\rightarrow$ vận hành hệ thống bình thường. Gồm các nhóm: Handshake/Auth Agent (5), Liveness/Telemetry Core (5: `HEARTBEAT`, `HEARTBEAT_ACK`, `METRIC_BATCH`, `BATCH_ACK`, `SERVICE_STATUS`), Disconnect (2), Admin Auth (4), Core Queries (4), Subscriptions/Snapshot (5), Realtime Broadcast (3), Incident Actions/Commands/Error (6).
- **`P0 OPTIONAL` (25 thông điệp):** Mở rộng phi chặn. Quy trình đăng ký mở rộng tùy chọn: Admin `CREATE_INVITE` $\rightarrow$ người dùng chưa xác thực `SIGN_UP(inviteCode)` $\rightarrow$ `LOGIN`. Nhóm thông điệp: Config push/request, Ping/Pong, Sign-up/Invite, Metric queries chi tiết, Incident history/reports, Server health & health sub.
- **`P1 CONDITIONAL` (10 thông điệp):** Giao thức nội bộ chuyển tiếp và thăm dò cụm Gateway/Collector (`HELLO_COLLECTOR`, `AUTH_COLLECTOR`, `FORWARD_*`, `COLLECTOR_HEARTBEAT`, `HEALTH_PROBE`, `HEALTH_RESULT`).

---

## V. CHI TIẾT ĐẶC TẢ PAYLOAD & PHẢN HỒI (DTO FIXTURES)

### 1. `HEARTBEAT` (Agent $\rightarrow$ Central) & `HEARTBEAT_ACK`
- **Envelope:** `messageType = "HEARTBEAT"`
- **Payload `HEARTBEAT`:**
  ```json
  {
    "bootId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "uptimeSeconds": 14250
  }
  ```
- **Envelope Phản hồi:** `messageType = "HEARTBEAT_ACK"`
- **Payload `HEARTBEAT_ACK`:**
  ```json
  {
    "status": "OK",
    "serverTimeUtc": "2026-09-13T07:15:30.1300000Z"
  }
  ```

### 2. `METRIC_BATCH` (Agent $\rightarrow$ Central) & `BATCH_ACK`
- **Envelope:** `messageType = "METRIC_BATCH"`
- **Payload `METRIC_BATCH`:**
  ```json
  {
    "bootId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "batchId": 1042,
    "isReplay": false,
    "cpuPercent": 92.4,
    "memoryUsedBytes": 6442450944,
    "memoryTotalBytes": 8589934592,
    "networkBytesSent": 1250440,
    "networkBytesRecv": 4512000,
    "uptimeSeconds": 14250
  }
  ```
- **Envelope Phản hồi:** `messageType = "BATCH_ACK"`
- **Payload `BATCH_ACK`:**
  ```json
  {
    "batchId": 1042,
    "status": "COMMITTED"
  }
  ```

### 3. `SERVICE_STATUS` (Agent $\rightarrow$ Central) & `BATCH_ACK`
- **Envelope:** `messageType = "SERVICE_STATUS"`
- **Payload `SERVICE_STATUS`:**
  ```json
  {
    "bootId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "batchId": 501,
    "isReplay": false,
    "services": [
      { "port": 80, "serviceName": "HTTP", "isUp": true, "responseTimeMs": 4 },
      { "port": 8080, "serviceName": "InternalApp", "isUp": false, "responseTimeMs": 2000 }
    ]
  }
  ```
- **Envelope Phản hồi:** `messageType = "BATCH_ACK"`

### 4. `LOGIN` (Admin $\rightarrow$ Central) & `LOGIN_RESULT`
- **Envelope:** `messageType = "LOGIN"`
- **Payload `LOGIN`:**
  ```json
  {
    "username": "operator01",
    "password": "CleartextPasswordOverTLS"
  }
  ```
- **Envelope Phản hồi:** `messageType = "LOGIN_RESULT"`
- **Payload `LOGIN_RESULT`:**
  ```json
  {
    "status": "SUCCESS",
    "sessionToken": "b4e82f5a-3d21-4f9e-9182-123456789abc",
    "user": {
      "userId": "usr-001",
      "username": "operator01",
      "fullName": "Nguyễn Văn Vận Hành",
      "role": "Operator"
    },
    "serverTimeUtc": "2026-09-13T07:15:30.2000000Z"
  }
  ```

### 5. `SUBSCRIBE_DEVICES` & Snapshot Flow (`SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END`)
- **Envelope Đăng ký:** `messageType = "SUBSCRIBE_DEVICES"`
- **Payload Đăng ký:** `{ "sessionToken": "b4e82f5a-3d21-4f9e-9182-123456789abc" }`
- **Luồng Snapshot phân trang từ Central:**
  1. `SNAPSHOT_BEGIN`:
     ```json
     {
       "snapshotId": "snap-9876",
       "scope": "DEVICES",
       "totalItems": 45,
       "pageSize": 20,
       "totalPages": 3
     }
     ```
  2. `SNAPSHOT_PAGE` (lặp qua các trang):
     ```json
     {
       "snapshotId": "snap-9876",
       "pageIndex": 1,
       "totalPages": 3,
       "items": [
         { "deviceId": "PC-01", "ipAddress": "192.168.1.101", "status": "ONLINE", "cpuPercent": 14.5, "memoryPercent": 45.2, "uptimeSeconds": 14250 }
       ]
     }
     ```
  3. `SNAPSHOT_END`:
     ```json
     {
       "snapshotId": "snap-9876",
       "totalItemsReceived": 45,
       "initialSequence": 1024
     }
     ```

### 6. Incident Commands (`INCIDENT_ACK`, `INCIDENT_START`, `INCIDENT_RESOLVE`) & `COMMAND_RESULT`
- **Envelope:** `messageType = "INCIDENT_ACK"` (hoặc `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`)
- **Payload Command:**
  ```json
  {
    "commandId": "cmd-uuid-001",
    "incidentId": "INC-2026-0001",
    "expectedRowVersion": "0x00000000000007D1",
    "comment": "Đã tiếp nhận xử lý sự cố"
  }
  ```
- **Phản hồi Thành công `COMMAND_RESULT`:**
  ```json
  {
    "commandId": "cmd-uuid-001",
    "status": "SUCCESS",
    "incidentId": "INC-2026-0001",
    "newWorkflowState": "ACKNOWLEDGED",
    "newRowVersion": "0x00000000000007D2"
  }
  ```
- **Phản hồi Xung đột Concurrency (409 Conflict):**
  ```json
  {
    "commandId": "cmd-uuid-001",
    "status": "CONFLICT",
    "errorCode": "ERR_CONCURRENCY_VIOLATION",
    "message": "Sự cố đã bị quản trị viên khác cập nhật trước đó.",
    "currentIncidentState": {
      "workflowState": "IN_PROGRESS",
      "assignedTo": "admin02",
      "rowVersion": "0x00000000000007D5"
    }
  }
  ```

---

## VI. BẢNG MÃ LỖI CHUẨN (STANDARD PROTOCOL ERROR CODES)

| Error Code | Ý Nghĩa Kỹ Thuật | Phía Bắt & Xử Lý | Hành Vi Hệ Thống |
|:---|:---|:---:|:---|
| `ERR_FRAME_TOO_LARGE` | Frame length $N > 256\text{ KiB}$ | Central / Agent / Admin | Đóng socket ngay, ghi log bảo mật |
| `ERR_MALFORMED_JSON` | Payload không parse được JSON hợp lệ | Phía nhận | Bỏ qua payload, ném JsonException, không crash |
| `ERR_STREAM_TRUNCATED` | Đứt kết nối giữa chừng khi đọc frame | `FrameCodec` | Bắt EndOfStreamException, hủy stream, kết nối lại |
| `ERR_UNAUTHENTICATED` | Chưa xác thực hoặc Token hết hạn | Admin / Agent | Yêu cầu đăng nhập lại / chặn gửi telemetry |
| `ERR_UNAUTHORIZED` | Quyền không đủ (Viewer thực hiện Resolve) | Central Server | Từ chối lệnh, trả lỗi 403 Forbidden |
| `ERR_CONCURRENCY_VIOLATION` | Sai expectedRowVersion (Xung đột OCC) | Admin Client UI | Báo Conflict 409, reload dữ liệu mới nhất từ Central |
| `ERR_IDEMPOTENCY_KEY_REUSE` | Cùng CommandId nhưng khác RequestDigest | Central Server | Từ chối lệnh, ghi audit log cảnh báo xung đột |
| `ERR_DEVICE_OFFLINE` | Thiết bị mất liên lạc quá deadline > 20s | Central Server | Kích hoạt rule tạo phiếu Incident |

---

## VII. KẾ HOẠCH BẢO VỆ TÍNH TƯƠNG THÍCH (COMPATIBILITY TEST SUITE)

Mọi thay đổi trong `Shared.Protocol` bắt buộc phải vượt qua 5 bài test tự động trước khi commit:
1. **TC-COMPAT-01 (Catalog Serialization):** Kiểm tra serialize/deserialize toàn bộ các loại MessageType wire chuẩn theo schema fixture.
2. **TC-COMPAT-02 (Big-Endian Order):** Kiểm tra mã hóa và giải mã 4-byte uint32 prefix trên các nền tảng phần cứng x86 và ARM.
3. **TC-COMPAT-03 (Frame Boundaries & Partial Reads):** Kiểm thử mô phỏng đọc từng byte (1-byte chunking) và dính gói nhiều frame liền mạch.
4. **TC-COMPAT-04 (Max Frame Bounds):** Kiểm tra ném ngoại lệ InvalidDataException khi frame vượt quá 256 KiB.
5. **TC-COMPAT-05 (UTC Timestamp & Envelope Standard):** Kiểm tra bắt buộc trường `sentAtUtc` chuẩn ISO-8601 UTC và cờ `isReplay` nằm trong payload telemetry.
