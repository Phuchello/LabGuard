# LABGUARD — CHUYÊN ĐỀ NGỮ NGHĨA TRUYỀN TẢI, ACK & KHỬ TRÙNG LẶP
**Mã tài liệu:** `research/05_DELIVERY_SEMANTICS.md`  
**Chuyên đề:** Ngữ nghĩa Truyền tải (Delivery Semantics), Giới hạn Ngữ nghĩa Tầng Mạng, Khử trùng lặp Telemetry (`ProcessedBatches`), Idempotency Lệnh Quản trị (`ProcessedCommands`) & Thứ tự Gửi ACK  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Trạng thái:** CHECKPOINT 2 REV2 — FREEZE CANDIDATE (READY FOR FINAL INDEPENDENT AUDIT)  
**Đối tượng thụ hưởng:** Thành viên A (Agent/Replay), Thành viên B (Central Server/Deduplication Engine), Thành viên C (Admin Client)  

---

## 1. TỔNG QUAN & BẢN CHẤT NGỮ NGHĨA TRUYỀN TẢI

### 1.1. Giới hạn Ngữ nghĩa Tầng Giao vận & Tầng Ứng dụng
Nhiều sinh viên khi học môn Lập trình mạng thường hiểu nhầm rằng: *"Do TCP có cơ chế truyền lại và ACK ở tầng giao vận nên ứng dụng tự động đạt được ngữ nghĩa Chuyển giao đúng một lần (Exactly-Once Delivery)."*

Đây là một giả định không chính xác khi xét trên toàn bộ hệ thống phân tán:
1. **TCP chỉ bảo đảm tính tin cậy trên một kết nối socket cụ thể.** Khi cáp mạng bị rút, router ngắt kết nối, hoặc ứng dụng bị tắt đột ngột, kết nối TCP bị hủy. Mọi sự bảo đảm byte stream của phiên đó lập tức chấm dứt.
2. **Kịch bản Mất ACK Phổ biến:** Agent gửi một mẻ đo `METRIC_BATCH` lên Server. Server đã ghi thành công vào CSDL SQL Server và chuẩn bị gửi `BATCH_ACK` về thì đường truyền mạng bị ngắt.
   - Phía Server: Dữ liệu đã được lưu vào đĩa cứng.
   - Phía Agent: Hết thời gian chờ (Timeout 5.0s) không nhận được `BATCH_ACK`, Agent coi như truyền thất bại và sẽ gửi lại mẻ đo đó (`Retry`) khi có kết nối mới.
   - Nếu Server không có cơ chế khử trùng lặp ở tầng ứng dụng, dữ liệu số liệu sẽ bị nhân đôi.

### 1.2. Định nghĩa Chuẩn mực: Hiệu ứng Lưu trữ Nhất quán (Effectively Exactly-Once Effect)
LabGuard **không tuyên bố rằng mạng truyền tải đúng-một-lần**. Thay vào đó, LabGuard giới hạn phạm vi cam kết kỹ thuật:
$$\text{At-Least-Once Delivery} + \text{Idempotent Receiver} \equiv \text{Effectively Exactly-Once (Observable Persisted Effect)}$$
*(Cơ chế truyền ít nhất một lần kết hợp bên nhận xử lý bất biến giúp đạt được hiệu ứng lưu trữ có thể quan sát tương đương đúng-một-lần cho các tác vụ có cơ chế idempotency/dedup).*

---

## 2. MA TRẬN NGỮ NGHĨA TRUYỀN TẢI THEO DANH MỤC BẢN TIN CHUẨN

Ma trận delivery semantics cho các bản tin P0 cốt lõi, all message names conform to the canonical 69-message catalog:

| Bản tin (`messageType`) | Bản tin Phản hồi / ACK | Chính sách Thử lại (`Retry`) | Khóa Khử trùng lặp (`Dedup Key`) | Cơ chế Idempotency | Điểm Lưu trữ Bền vững | Hành vi khi Nhận Gói Trùng lặp |
|:---|:---|:---|:---|:---|:---|:---|
| `HEARTBEAT` | `HEARTBEAT_ACK` | **Không thử lại** (At-Most-Once / Keepalive). Chờ chu kỳ 5s kế tiếp. | Không cần khóa CSDL. | Cập nhật đè timestamp trong RAM (`Stopwatch.GetTimestamp()`). | Bộ nhớ RAM (Không ghi CSDL mỗi nhịp). | Cập nhật lại thời điểm `LastSeen` gần nhất. |
| `METRIC_BATCH` | `BATCH_ACK` | **Có thử lại** (At-Least-Once qua Unacknowledged Telemetry Queue `ReplayBuffer`, capacity 50 batches, timeout 5s). | Khóa bộ ba: `(DeviceId, BootId, BatchId)` | Kiểm tra bảng `ProcessedBatches`. | Bảng `MetricSamples` & `ProcessedBatches` trong 1 `SqlTransaction`. | **Bỏ qua ghi `MetricSamples`, nhưng LẬP TỨC gửi lại `BATCH_ACK`**. |
| `SERVICE_STATUS` | Không có ACK riêng | **Không thử lại** (Latest-State Telemetry, không đưa vào `ReplayBuffer`). Thăm dò định kỳ kế tiếp gửi trạng thái mới nhất. | `(DeviceId, ServiceName)` | Idempotent UPSERT trên bảng `Services`. | Bảng `Services` (Cập nhật cột `LastCheckedUtc`, `Status`). | Ghi đè trạng thái mới nhất; không nhân đôi dòng. |
| `LOGIN` | `LOGIN_RESULT` | **Có thử lại** (At-Least-Once, WinForms timeout 10s). | Không dùng `ProcessedCommands`. | Xác thực kiểm tra đọc mật khẩu (PBKDF2-HMAC-SHA256). | Bảng `Users` (Chỉ đọc) & Cấp Session Token tạm trong RAM. | Đăng nhập lại bình thường, cấp token phiên mới. |
| `INCIDENT_ACK` | `COMMAND_RESULT` | **Có thử lại** (At-Least-Once, timeout 10s). | `(UserId, CommandId)` | Kiểm tra `ProcessedCommands` kèm SHA-256 `RequestDigest`. | Bảng `Incidents`, `IncidentHistory`, `ProcessedCommands`. | Trùng digest: Trả lại kết quả cũ đã lưu. Khác digest: Lỗi `ERR_IDEMPOTENCY_KEY_REUSE`. |
| `INCIDENT_ASSIGN` | `COMMAND_RESULT` | **Có thử lại** (At-Least-Once, timeout 10s). | `(UserId, CommandId)` | Kiểm tra `ProcessedCommands` kèm SHA-256 `RequestDigest`. | Bảng `Incidents`, `IncidentHistory`, `ProcessedCommands`. | Trùng digest: Trả kết quả cũ. Khác digest: Báo lỗi `ERR_IDEMPOTENCY_KEY_REUSE`. |
| `INCIDENT_START` | `COMMAND_RESULT` | **Có thử lại** (At-Least-Once, timeout 10s). | `(UserId, CommandId)` | Kiểm tra `ProcessedCommands` kèm SHA-256 `RequestDigest`. | Bảng `Incidents`, `IncidentHistory`, `ProcessedCommands`. | Trùng digest: Trả kết quả cũ. Khác digest: Báo lỗi `ERR_IDEMPOTENCY_KEY_REUSE`. |
| `INCIDENT_RESOLVE` | `COMMAND_RESULT` | **Có thử lại** (At-Least-Once, timeout 10s). | `(UserId, CommandId)` | Kiểm tra `ProcessedCommands` kèm SHA-256 `RequestDigest`. | Bảng `Incidents`, `IncidentHistory`, `ProcessedCommands`. | Trùng digest: Trả kết quả cũ. Khác digest: Báo lỗi `ERR_IDEMPOTENCY_KEY_REUSE`. |

---

## 3. CƠ CHẾ KHỬ TRÙNG LẶP TELEMETRY BATCHES (`ProcessedBatches`)

### 3.1. Khóa Định danh Duy nhất (Dedup Key)
Mỗi mẻ đo đạc số liệu được gán một bộ ba khóa duy nhất ngay tại thời điểm Agent thu thập:
$$\text{TelemetryDedupKey} = (\text{DeviceId}, \text{BootId}, \text{BatchId})$$
- `DeviceId`: Chuỗi cấu hình định danh máy trạm (ví dụ: `"LAB01-PC05"`).
- `BootId`: Kiểu `Guid` sinh một lần duy nhất khi tiến trình Agent khởi động, không đổi trong suốt vòng đời tiến trình. Khi Agent khởi động lại sẽ sinh `Guid` mới.
- `BatchId`: Kiểu `Guid` (UUIDv4) sinh mới một lần duy nhất cho mỗi mẻ đo. Giá trị này **bắt buộc giữ nguyên không đổi qua các lần thử lại hoặc gửi bù**.

### 3.2. Bảng Lưu trữ Cố định `ProcessedBatches`
```sql
CREATE TABLE dbo.ProcessedBatches (
    DeviceId NVARCHAR(64) NOT NULL,
    BootId UNIQUEIDENTIFIER NOT NULL,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    ProcessedAtUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ProcessedBatches PRIMARY KEY CLUSTERED (DeviceId, BootId, BatchId)
);
CREATE NONCLUSTERED INDEX IX_ProcessedBatches_ProcessedAtUtc ON dbo.ProcessedBatches (ProcessedAtUtc);
```

### 3.3. Quy tắc Bắt buộc khi Gặp Gói tin Trùng lặp
- **Tình huống:** Central Server nhận một `METRIC_BATCH` có `(DeviceId, BootId, BatchId)` đã tồn tại trong `ProcessedBatches`.
- **Hành vi Xử lý:**
  1. **Không chèn lại bản ghi** vào bảng `MetricSamples`.
  2. **Không chuyển vào Rule Engine** để tránh làm sai lệch Streak và Hysteresis.
  3. **Bắt buộc gửi lại `BATCH_ACK` ngay lập tức** cho Agent!
  > *Giải thích:* Gói tin trùng lặp xuất hiện chứng tỏ Agent đã không nhận được `BATCH_ACK` trước đó do mạng chập chờn. Nếu Server bỏ qua không phản hồi, Agent sẽ tiếp tục thử lại vô hạn, gây lãng phí băng thông mạng.

### 3.4. Quyền Sở hữu Dữ liệu Đo đạc & Ngữ nghĩa SERVICE_STATUS
1. **Telemetry Ownership (`METRIC_BATCH`):**
   - Agent giữ quyền sở hữu dữ liệu `METRIC_BATCH` trong hàng đợi đo đạc chưa xác nhận (`Unacknowledged Telemetry Queue` / `ReplayBuffer`, sức chứa tối đa **50 batches**) cho tới khi nhận được `BATCH_ACK` có `batchId` tương ứng từ Central Server.
   - Khi hàng đợi đạt ngưỡng tối đa 50 batches mà kết nối vẫn chưa khôi phục, chính sách FIFO Drop-Oldest sẽ loại bỏ mẻ cũ nhất và bắt buộc ghi log sự kiện `TELEMETRY_DROPPED` kèm thông tin `batchId` bị hủy.
2. **Ngữ nghĩa `SERVICE_STATUS` (Latest-State Telemetry):**
   - `SERVICE_STATUS` là dữ liệu trạng thái tức thời mới nhất, **KHÔNG** phải bản tin At-Least-Once và **KHÔNG** đưa vào `ReplayBuffer`.
   - Nếu kết nối mạng bị gián đoạn, Agent không xếp hàng thử lại các bản tin `SERVICE_STATUS` cũ.
   - Lần thăm dò định kỳ tiếp theo sẽ đọc và gửi trạng thái dịch vụ mới nhất.
   - Phía Central Server thực hiện thao tác Idempotent UPSERT trên bảng `Services` theo khóa `(DeviceId, ServiceName)` (cập nhật trạng thái và `LastCheckedUtc`), không yêu cầu ACK riêng.

---

## 4. THUẬT TOÁN REQUEST DIGEST BẤT BIẾN CHO LỆNH QUẢN TRỊ (DETERMINISTIC CONTRACT)

### 4.1. Khóa Định danh và Bản băm (Request Digest)
Mỗi lệnh thay đổi trạng thái từ Quản trị viên (`INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`) được nhận diện bằng:
$$\text{CommandKey} = (\text{UserId}, \text{CommandId})$$
- `CommandId`: Chuỗi UUIDv4 sinh ra khi người dùng click chuột trên WinForms. Khi WinForms tự động gửi lại lệnh do quá hạn timeout, nó **giữ nguyên `CommandId` này**.
- `RequestDigest`: Chuỗi mã băm SHA-256 được tính toán từ các thuộc tính lệnh theo thứ tự chuẩn mực xác định trước.

### 4.2. Đặc tả Thuật toán Server-side Canonical RequestDigest
Để loại bỏ sự phụ thuộc vào định dạng chuỗi JSON hoặc thứ tự key ngẫu nhiên từ client, Server chuyển đổi payload sang một cấu trúc tuần tự hóa cố định:

1. **Chuẩn hóa các trường đầu vào theo thứ tự xác định:**
   - Trường 1: `commandType` (Chuỗi in hoa, ví dụ: `"INCIDENT_ACK"`, `"INCIDENT_RESOLVE"`).
   - Trường 2: `incidentId` (Số nguyên biểu diễn dạng chuỗi thập phân).
   - Trường 3: `expectedRowVersion` (Chuỗi hex 18 ký tự chuẩn hóa tiền tố `0x`, ví dụ: `"0x000000000001A2F4"`).
   - Trường 4: `assignedToUserId` (Số nguyên hoặc chuỗi `"null"` nếu không có giá trị).
   - Trường 5: `resolutionNote` (Chuỗi UTF-8 đã cắt tỉa khoảng trắng đầu cuối; nếu null thì chuyển thành `""`).
   - Trường 6: `commandId` (Chuỗi UUIDv4 viết thường 36 ký tự).
2. **Tạo mảng byte nhị phân UTF-8 (Canonical Byte Sequence):**
   - Nối các trường trên lại bằng dấu phân cách ký tự pipe `|`:
     `commandType|incidentId|expectedRowVersion|assignedToUserId|resolutionNote|commandId`
3. **Băm SHA-256:**
   - Tính toán `SHA256.HashData(canonicalBytes)` $\rightarrow$ Chuỗi hex 64 ký tự viết thường.
4. **Xử lý Xung đột:**
   - Nếu trùng `(UserId, CommandId)` và `RequestDigest` trùng khớp: Trả lại kết quả `COMMAND_RESULT` đã lưu trước đó trong bảng `ProcessedCommands`.
   - Nếu trùng `(UserId, CommandId)` nhưng `RequestDigest` khác: Từ chối lệnh với mã lỗi `ERR_IDEMPOTENCY_KEY_REUSE`.

```sql
CREATE TABLE dbo.ProcessedCommands (
    UserId INT NOT NULL,
    CommandId UNIQUEIDENTIFIER NOT NULL,
    RequestDigest CHAR(64) NOT NULL,
    ResponsePayloadJson NVARCHAR(MAX) NOT NULL,
    ProcessedAtUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ProcessedCommands PRIMARY KEY CLUSTERED (UserId, CommandId)
);
CREATE NONCLUSTERED INDEX IX_ProcessedCommands_ProcessedAtUtc ON dbo.ProcessedCommands (ProcessedAtUtc);
```

---

## 5. THỨ TỰ PHÁT SINH ACK (COMMIT-THEN-RESPOND RULE)

### 5.1. Nguyên tắc Bất biến
Gói tin xác nhận (`BATCH_ACK` hoặc `COMMAND_RESULT`) **chỉ được phép gửi ra socket mạng sau khi giao dịch cơ sở dữ liệu (`SqlTransaction`) đã commit thành công**.

```
[1. Tiếp nhận Frame]
        │
        ▼
[2. Mở SqlTransaction]
        ├── Kiểm tra ProcessedBatches / ProcessedCommands
        ├── Ghi dữ liệu nghiệp vụ (MetricSamples / Incidents / AuditLogs)
        └── Ghi bản ghi vào ProcessedBatches / ProcessedCommands
        │
        ▼
[3. CommitAsync() thành công]
        │
        ▼
[4. Gửi BATCH_ACK / COMMAND_RESULT ra Socket]
        │
        ▼
[5. Đẩy sự kiện Broadcast (DEVICE_UPDATE / INCIDENT_UPDATE)]
```

Nếu giao dịch bị rollback tại Bước 2 hoặc 3, Server không gửi ACK. Phía client sẽ timeout và thử lại một cách an toàn.
