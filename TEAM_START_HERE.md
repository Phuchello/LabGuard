# LABGUARD — BẮT ĐẦU TẠI ĐÂY (TEAM START HERE)

Tài liệu hướng dẫn nhanh dành cho 3 thành viên nhóm thực hiện đồ án môn Lập trình mạng (NT106).

---

## A. LABGUARD LÀ GÌ?

LabGuard là hệ thống phần mềm giám sát phòng máy tính tập trung viết bằng C# (.NET 9.0). Hệ thống giúp quản trị viên theo dõi tình trạng hoạt động của các máy trạm sinh viên từ xa theo thời gian thực. Mỗi máy trạm sẽ cài đặt một ứng dụng ngầm (Agent) để liên tục đo đạc thông số CPU, RAM và trạng thái dịch vụ, sau đó gửi về máy chủ trung tâm (Central Server) qua kết nối mạng TCP. Máy chủ tiếp nhận dữ liệu, lưu vào cơ sở dữ liệu SQL Server, đồng thời tự động phát hiện các sự cố bất thường (như quá tải tài nguyên, tắt máy đột ngột, dịch vụ bị dừng). Cuối cùng, quản trị viên sử dụng giao diện đồ họa (Admin WinForms) để giám sát và trực tiếp thao tác xử lý các sự cố đó.

---

## B. SẢN PHẨM HIỆN CÓ GÌ?

Hệ thống gồm 4 thành phần chính:
1. **Central Server (`LabGuard.Central`)**: Ứng dụng Console lắng nghe kết nối TCP trên cổng 5000, quản lý phiên làm việc, phân tích sự cố và ghi dữ liệu vào CSDL.
2. **Agent (`LabGuard.Agent`)**: Ứng dụng Console chạy trên máy trạm, tự động kết nối đến Central và gửi dữ liệu đo lường định kỳ.
3. **Admin Dashboard (`LabGuard.Admin`)**: Ứng dụng Windows Forms gồm các thẻ thông số KPI (Devices, Online, Offline), bảng danh sách máy trạm (DataGridView), bảng danh sách sự cố (DataGridView) và thanh công cụ thao tác sự cố.
4. **SQL Server Database (`LabGuardDb`)**: Lưu trữ danh mục máy trạm, tài khoản người dùng, lịch sử chỉ số viễn trắc (`dbo.MetricSamples`) và lịch sử sự cố (`dbo.Incidents`, `dbo.IncidentHistory`).

### Các tính năng và cơ chế hoạt động:
- **Đóng gói giao thức TCP (Framing Codec)**: Chiều dài tiền tố 4-byte unsigned big-endian integer (độ dài tối đa 262,144 bytes / 256 KiB) + N bytes payload định dạng UTF-8 JSON. Bắt tay `HELLO` và xác thực `AUTH`.
- **Giữ kết nối (Heartbeat & Liveness)**: Nhịp tim 5 giây/lần (`HEARTBEAT`), tự động chuyển máy sang trạng thái `OFFLINE` nếu mất tín hiệu quá 20 giây (`LIVENESS_TIMEOUT > 20s`).
- **Chỉ số phần cứng thực tế**: Đọc CPU thực của Windows qua `GetSystemTimes`, RAM thực qua `GlobalMemoryStatusEx` và thời gian hoạt động máy (`Environment.TickCount64`).
- **Lưu trữ CSDL**: Dữ liệu chỉ số được ghi nhận vào SQL Server, có chống trùng lặp gói tin (`dbo.ProcessedBatches`).
- **Tự kết nối lại & Lưu đệm (Reconnect / Replay)**: Khi ngắt kết nối, Agent tự động thử lại theo thuật toán lũy thừa (1s, 2s, 4s, 8s, 16s, tối đa 30s kèm ±10% jitter) và lưu đệm tối đa 50 gói tin gần nhất trong RAM (`ReplayBuffer`) có cờ `isReplay` để gửi bù khi kết nối phục hồi.
- **Tự động phát hiện sự cố (Rule Engine Hysteresis)**:
  - `HIGH_CPU`: Kích hoạt khi CPU >= 90% trong 3 mẫu liên tiếp; phục hồi khi CPU <= 75% trong 3 mẫu liên tiếp.
  - `HIGH_MEMORY`: Kích hoạt khi RAM >= 85% trong 3 mẫu liên tiếp; phục hồi khi RAM <= 70% trong 3 mẫu liên tiếp.
  - `SERVICE_DOWN`: Kích hoạt khi thăm dò dịch vụ thất bại 2 lần liên tiếp.
  - `DEVICE_OFFLINE`: Kích hoạt khi mất liên lạc quá 20 giây.
- **Vòng đời xử lý sự cố trên Dashboard**: Kỹ thuật viên thao tác các lệnh `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE` (kèm ghi chú giải quyết bắt buộc), có kiểm soát tranh chấp dữ liệu (Optimistic Concurrency Control - OCC bằng `RowVersion` 8-byte hex).

---

## C. CHẠY DỰ ÁN THẾ NÀO?

Để chạy toàn bộ hệ thống từ đầu, hãy mở 4 cửa sổ terminal (PowerShell hoặc Command Prompt) theo đúng thứ tự sau:

### Bước 1: Khởi động SQL Server
Đảm bảo dịch vụ SQL Server đang chạy. Nếu chạy lần đầu, hãy tạo database `LabGuardDb` và thực thi file script `database/schema.sql` cùng `database/seed.sql`.

### Bước 2: Chạy Central Server
Mở terminal thứ nhất tại thư mục gốc:
```powershell
dotnet run --project src/LabGuard.Central/LabGuard.Central.csproj
```
*Dấu hiệu đúng:* Server in thông báo đang lắng nghe trên cổng 5000 và kết nối CSDL thành công.

### Bước 3: Chạy Agent
Mở terminal thứ hai:
```powershell
dotnet run --project src/LabGuard.Agent/LabGuard.Agent.csproj
```
*Dấu hiệu đúng:* Agent in thông báo đã kết nối, xác thực thành công và bắt đầu gửi các đợt `METRIC_BATCH`.

### Bước 4: Chạy Admin Dashboard
Mở terminal thứ ba:
```powershell
dotnet run --project src/LabGuard.Admin/LabGuard.Admin.csproj
```
*Dấu hiệu đúng:* Cửa sổ WinForms hiện lên, hiển thị KPI thẻ, bảng thiết bị `PC01 - ONLINE` và bảng sự cố.

---

## D. PHÂN CÔNG 3 THÀNH VIÊN

### 👤 Sinh viên A — Phụ trách Agent & Simulator
- **Mục tiêu:** Giám sát máy trạm ổn định, tự phục hồi mạng, đo kiểm dịch vụ mạng và công cụ giả lập tải.
- **Các file chính cần nắm:**
  - `src/LabGuard.Agent/AgentSocketClient.cs`: Quản lý socket TCP, vòng lặp nhận/gửi và thuật toán thử lại kết nối (backoff).
  - `src/LabGuard.Agent/MetricsService.cs`: Thu thập chỉ số CPU, RAM và Uptime từ Windows.
  - `src/LabGuard.Agent/ReplayBuffer.cs`: Hàng đợi lưu đệm 50 gói tin viễn trắc khi mất mạng.
  - `src/LabGuard.Agent/ServiceMonitor.cs`: Thăm dò dịch vụ mạng (TCP/HTTP probe).
  - `src/LabGuard.Simulator/`: Công cụ sinh tải giả lập 20 máy trạm.

### 👤 Sinh viên B — Phụ trách Central Server & Database
- **Mục tiêu:** Xử lý nghiệp vụ máy chủ, kiểm soát quyền truy cập, lưu trữ lịch sử sự cố và bảo vệ CSDL.
- **Các file chính cần nắm:**
  - `src/LabGuard.Central/CentralServer.cs`: Quản lý danh sách kết nối TCP và vòng đời máy chủ.
  - `src/LabGuard.Central/ClientConnection.cs`: Xác thực phiên làm việc và điều hướng gói tin.
  - `src/LabGuard.Central/RuleEngine.cs`: Đánh giá ngưỡng chuỗi mẫu (hysteresis) để mở/đóng sự cố.
  - `src/LabGuard.Central/DatabaseRepository.cs`: Thao tác SQL Server, xử lý `RowVersion` và chống tranh chấp OCC.
  - `database/schema.sql` & `database/seed.sql`: Cấu trúc bảng (`Incidents`, `IncidentHistory`, `MetricSamples`,...) và dữ liệu khởi tạo.

### 👤 Sinh viên C — Phụ trách Admin WinForms
- **Mục tiêu:** Hoàn thiện giao diện giám sát, hiển thị người xử lý sự cố và tối ưu trải nghiệm thao tác.
- **Các file chính cần nắm:**
  - `src/LabGuard.Admin/DashboardForm.cs`: Bố cục giao diện KPI, bảng thiết bị, bảng sự cố và thanh công cụ xử lý.
  - `src/LabGuard.Admin/AdminClient.cs`: Kết nối mạng TCP từ giao diện tới Central, nhận broadcast `INCIDENT_UPDATE`.
  - `src/LabGuard.Admin/Program.cs`: Khởi tạo ứng dụng.

---

## E. VIỆC TIẾP THEO CỦA MỖI NGƯỜI

### Việc của Sinh viên A:
1. **Kiểm tra Reconnect / Replay trên mạng thật:** Chạy Agent, ngắt kết nối mạng hoặc tắt Central trong 20-30 giây rồi bật lại. Xác nhận Agent tự động backoff và gửi bù đủ các gói tin từ `ReplayBuffer`.
2. **Kiểm tra ServiceMonitor với máy ảo Ubuntu:** Cấu hình `ServiceMonitor` probe cổng dịch vụ trên máy ảo Ubuntu (port 22 hoặc 80), dừng dịch vụ trên máy ảo để kiểm tra Central có tạo sự cố `SERVICE_DOWN` hay không.
3. **Chạy thử Simulator:** Sử dụng `LabGuard.Simulator` bắn tải đồng thời 10-20 máy trạm để kiểm tra sức chịu tải của Central.

### Việc của Sinh viên B:
1. **Kiểm tra quyền truy cập (Server-side RBAC):** Trong `ClientConnection.cs`, kiểm tra ma trận vai trò người dùng trước khi thực thi các lệnh thao tác sự cố:
   - `Viewer`: Chỉ có quyền đọc/xem danh sách, tuyệt đối không được sửa đổi trạng thái sự cố.
   - `Operator`: Được phép thực hiện `INCIDENT_ACK`, `INCIDENT_START`, `INCIDENT_RESOLVE` (KHÔNG có quyền `INCIDENT_ASSIGN`).
   - `Admin`: Toàn quyền thao tác bao gồm cả `INCIDENT_ASSIGN`.
2. **Lưu vết người xử lý sự cố (`AssignedToUserId`):** Đảm bảo `AssignedToUserId` được cập nhật chính xác vào bảng `dbo.Incidents` và ghi nhật ký vào bảng `dbo.IncidentHistory`.
3. **Chuẩn hóa mã lỗi OCC & Tách biệt CSDL Test:** Trả về mã lỗi chuẩn `ERR_CONCURRENCY_VIOLATION` khi phát hiện xung đột `RowVersion`; tách biệt chuỗi kết nối của `LabGuard.Central.Tests` sang CSDL test riêng (`LabGuardTestDb`) thay vì dùng chung `LabGuardDb`.

### Việc của Sinh viên C:
1. **Hiển thị cột Người xử lý trên Dashboard:** Thêm cột `Assigned To` trên DataGridView sự cố trong `DashboardForm.cs`.
2. **Hộp thoại gán sự cố trực quan:** Khi bấm `Assign`, hiển thị hộp thoại cho phép nhập hoặc chọn tài khoản kỹ thuật viên thay vì gán giá trị mặc định.
3. **Cải thiện trải nghiệm thao tác sự cố:** Hiển thị thông báo `MessageBox` rõ ràng khi thao tác bị từ chối (xung đột dữ liệu 409, không đủ quyền 403, hoặc đóng sự cố khi máy còn quá tải).

---

## F. KHI BỊ LỖI THÌ LÀM GÌ?

Khi gặp lỗi trong quá trình lập trình hoặc chạy thử, hãy làm tuần tự theo 5 bước đơn giản:

1. **Biên dịch toàn bộ giải pháp**:
   ```powershell
   dotnet build LabGuard.sln
   ```
   Đọc kỹ thông báo lỗi ở dòng nào, file nào.
2. **Chạy kiểm thử tự động**:
   ```powershell
   dotnet test LabGuard.sln
   ```
   Xem test case nào bị trượt để khoanh vùng nguyên nhân.
3. **Chạy riêng lẻ từng module**:
   Chạy riêng Central trước, sau đó chạy Agent hoặc Admin để xác định lỗi xuất phát từ phía gửi hay phía nhận.
4. **Đọc log trên màn hình Console**:
   Các ứng dụng Console đều có in thông tin chi tiết về gói tin gửi/nhận, mã lỗi SQL hoặc lỗi Socket.
5. **Trao đổi với bạn cùng nhóm hoặc Mentor**:
   Gửi đoạn code vừa chỉnh sửa cùng thông báo lỗi chi tiết để được hướng dẫn giải quyết.

---

## G. NGUYÊN TẮC SỬ DỤNG TRỢ LÝ AI

- **AI đóng vai trò: Cố vấn (Mentor) / Người xem xét mã nguồn (Reviewer) / Hỗ trợ tìm lỗi (Debugger)**.
- **Sinh viên là người trực tiếp viết code**: Các bạn tự chỉnh sửa và làm chủ mã nguồn đồ án.
- **AI không tự ý viết thay toàn bộ milestone**: AI sẽ hướng dẫn giải thuật, chỉ ra vị trí file cần sửa, gợi ý khung code và review diff sau khi các bạn hoàn thành.
