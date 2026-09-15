> **SUPERSEDED / HISTORICAL ARTIFACT**  
> This document has been superseded and preserved for historical audit trail only.  
> Current authoritative candidate: [CP2_DECISIONS_REV2.md](../CP2_DECISIONS_REV2.md) / [AUDIT_HANDOFF_CHECKPOINT_2_REV2.md](../AUDIT_HANDOFF_CHECKPOINT_2_REV2.md).

---

# LABGUARD — SỔ TAY THỰC CHIẾN CHECKPOINT 2 DÀNH CHO NHÓM 3 SINH VIÊN (REV1)
**Tài liệu:** `CP2_CHO_NHOM_3_NGUOI_REV1.md`  
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Đối tượng:** Ba sinh viên Bạn A (Agent), Bạn B (Server/DB), Bạn C (Admin WinForms)  
**Trạng thái:** CHECKPOINT 2 — FREEZE CANDIDATE (READY FOR INDEPENDENT RE-AUDIT)  
**Mục tiêu:** Mở file ra trong 5 phút là biết chính xác mình cần code những class nào, gọi hàm gì, tham số bao nhiêu và tránh những lỗi kinh điển khi bảo vệ đồ án!  

---

## PHẦN 1: BỨC TRANH TOÀN CẢNH — 3 BẠN KẾT NỐI VỚI NHAU THẾ NÀO?

Trước khi mở Visual Studio gõ code, cả 3 bạn cùng nhìn vào sơ đồ dòng chảy dữ liệu chuẩn mực này:

```
[BẠN A: AGENT TRÊN MÁY TRẠM]
  │  (Gửi Heartbeat 5s, Gửi Telemetry 5s, Thử lại Backoff + Jitter, Gửi bù Replay)
  │
  ▼ [TCP Socket + 4-byte Big-Endian Length + JSON UTF-8 Envelope]
  │
[BẠN B: CENTRAL SERVER & SQL SERVER]
  │  (Đọc Frame <= 256 KiB, Ingress Channel 1000, Single Ingress Worker, 
  │   Lọc trùng ProcessedBatches, Monotonic Clock 20s, Streak 3, Dapper Transaction)
  │
  ▲ [TCP Socket + 4-byte Big-Endian Length + JSON UTF-8 Envelope]
  │
[BẠN C: ADMIN WINFORMS DASHBOARD]
  │  (Đăng nhập LOGIN, Nhận Event DEVICE_UPDATE/INCIDENT_UPDATE, 
  │   Quản lý Sự cố 2 tầng, Khóa lạc quan OCC rowversion, An toàn luồng UI Invoke)
```

- **Nguyên tắc làm việc chung số 1:** Cả 3 bạn dùng chung một thư viện class mang tên `LabGuard.Protocol` (do Bạn A chủ trì khởi tạo). Thư viện này chứa class `FrameCodec` và các DTO dùng chung. Danh mục bản tin tuân thủ tuyệt đối danh mục 69 bản tin chuẩn từ `03_PROTOCOL_SPEC.md`. Không ai được tự chế thêm tên bản tin mới!
- **Nguyên tắc số 2:** Không ai được dùng lệnh chặn luồng (Blocking call) như `Thread.Sleep()`, `.Result`, `.Wait()`. Toàn bộ đều dùng `await ...Async()`.

---

## PHẦN 2: SỔ TAY THỰC CHIẾN CHO BẠN A (AGENT & SIMULATOR)

> **Trọng trách của Bạn A:** Viết phần mềm chạy ngầm trên máy trạm Windows, lấy thông số CPU/RAM thật, định kỳ gửi về Server, tự kết nối lại nếu rớt mạng, và viết công cụ giả lập 20 máy trạm để test tải.

### 1. Các Class chính Bạn A cần tạo:
1. `FrameCodec.cs` (Nằm trong project `LabGuard.Protocol`):
   - Hằng số: `MaxPayloadSize = 262144;` (256 KiB).
   - Hàm `WriteFrameAsync(Stream stream, byte[] payloadJsonBytes, CancellationToken ct)`: Ghi 4 byte độ dài Big-Endian rồi ghi tiếp payload.
   - Hàm `ReadFrameAsync(Stream stream, int maxPayloadSize, CancellationToken ct)`: Đọc đúng 4 byte độ dài Big-Endian, kiểm tra $0 < N \le 262,144$, sau đó đọc đủ số byte payload bằng `ReadExactlyAsync`. Nếu gặp EOF giữa chừng thì ném `EndOfStreamException`. Chiều dài tối đa của toàn bộ khung trên mạng là $262,148\text{ bytes}$ ($4 + 262,144$).
2. `AgentSocketClient.cs`:
   - Quản lý vòng đời kết nối TCP tới Central Server (mặc định Port `5000`).
   - Cài đặt **Mô hình Single Writer Loop**: Tạo `Channel<byte[]> _outbox = Channel.CreateBounded<byte[]>(256)`.
   - Một background task duy nhất chạy vòng lặp đọc từ `_outbox` và ghi ra `NetworkStream`. Mọi nơi khác muốn gửi tin chỉ được gọi `_outbox.Writer.WriteAsync(frame)`.
   - **Khi mất kết nối:** Hủy outbox channel cũ, tạm dừng timer Heartbeat, định tuyến các mẻ đo sang `ReplayBuffer`.
   - **Khi kết nối lại:** Tạo outbox channel mới, gửi `HELLO_AGENT` $\rightarrow$ nhận `HELLO_ACK`, gửi `AUTH_AGENT` $\rightarrow$ nhận `AUTH_RESULT`. Sau khi `AUTH_RESULT` thành công mới bật lại timer gửi `HEARTBEAT`.
3. `HeartbeatTimer.cs`:
   - Dùng `PeriodicTimer` chu kỳ đúng **5.0 giây**.
   - Mỗi tick tạo bản tin `HEARTBEAT` đẩy vào `_outbox`.
4. `MetricsCollector.cs`:
   - **Đo CPU:** Dùng `System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total")`.
   - **Đo RAM toàn hệ thống:** **Tuyệt đối không dùng hàm đo bộ nhớ Garbage Collector của .NET** (vì hàm đó chỉ đo bộ nhớ của riêng runtime .NET, không đo RAM toàn máy tính). Sử dụng Win32 API cấp hệ điều hành **`GlobalMemoryStatusEx`** qua P/Invoke hoặc Performance Counter `Memory / % Committed Bytes In Use`.
   - Chu kỳ **5.0 giây**, gom thành bản tin `METRIC_BATCH`.
   - Gán `BatchId = Guid.NewGuid()` (UUIDv4) và `BootId = _processBootId` (`Guid` sinh một lần khi Agent bật).
5. `ReplayBuffer.cs`:
   - Một hàng đợi RAM FIFO giới hạn **50 mẻ đo**.
   - Nếu mất mạng, các mẻ `METRIC_BATCH` thu thập được đẩy vào đây (nếu vượt quá 50 thì loại bỏ mẻ cũ nhất).
   - Khi có mạng lại, xả tuần tự từng mẻ kèm cờ `"isReplay": true`. **Tuyệt đối không lưu và không gửi bù bản tin `HEARTBEAT`!**
6. `ReconnectHandler.cs`:
   - Cài đặt thuật toán lùi lũy tiến:
     $$\text{delay}_k = \min\left(30.0, 1.0 \times 2^k\right) \times \left(1 + \text{UniformRandom}[-0.10, +0.10]\right)$$
   - Chuỗi thời gian: $1\text{s} \rightarrow 2\text{s} \rightarrow 4\text{s} \rightarrow 8\text{s} \rightarrow 16\text{s} \rightarrow 30\text{s}$.
   - Nhận được `AUTH_RESULT(success == true)` thì reset $k = 0$.
7. `AgentSimulator.exe`:
   - Tạo ứng dụng Console cho phép nhập số lượng máy ảo (ví dụ: `20`).
   - Khởi tạo 20 kết nối TCP đồng thời gửi `HEARTBEAT` và `METRIC_BATCH` về Server để test tải.

### 2. Các cạm bẫy Bạn A cần tránh:
- ❌ **Bẫy 1:** Gọi trực tiếp `stream.WriteAsync` ở nhiều luồng $\rightarrow$ Dính lỗi xé khung làm vỡ gói tin của Bạn B.
- ❌ **Bẫy 2:** Không cộng Jitter $\pm 10\%$ khi kết nối lại $\rightarrow$ Khi Server restart, 20 máy ào vào cùng 1 giây gây bão kết nối (Reconnect Storm).
- ❌ **Bẫy 3:** Dùng hàm đo bộ nhớ Garbage Collector để đo RAM hệ thống $\rightarrow$ Bị trừ điểm vì số liệu RAM chỉ phản ánh riêng tiến trình .NET chứ không phản ánh RAM thật của Windows.
- ❌ **Bẫy 4:** Lưu `HEARTBEAT` vào ReplayBuffer $\rightarrow$ Khi có mạng bắn lên hàng chục gói nhịp tim cũ rích làm Server phán đoán sai liveness.

---

## PHẦN 3: SỔ TAY THỰC CHIẾN CHO BẠN B (SERVER & DATABASE)

> **Trọng trách của Bạn B:** "Trái tim" của hệ thống. Quản lý kết nối mạng, hàng đợi nhận tin, lọc trùng lặp, chấm điểm sống chết máy trạm, đánh giá sự cố và lưu cơ sở dữ liệu SQL Server.

### 1. Các việc Bạn B cần làm:
1. **Chạy kịch bản SQL Server:**
   - Chạy toàn bộ file script trong `research/06_DATABASE.md` để sinh đủ 10 bảng. Chú ý: Bảng `MetricSamples` không có cột chỉ số Disk; bảng `Incidents` có cột `RowVersion ROWVERSION NOT NULL`.
2. **Xây dựng `CentralServer.cs`:**
   - Dùng `TcpListener` lắng nghe trên cổng `5000`.
   - Vòng lặp `AcceptTcpClientAsync` đón kết nối. Mỗi kết nối sinh ra một Reader Task riêng.
3. **Xây dựng `CentralIngressPipeline.cs`:**
   - Tạo hàng đợi trung tâm: `Channel<IncomingEnvelope> _ingressChannel = Channel.CreateBounded<IncomingEnvelope>(1000)`.
   - Các Reader Task đọc được frame nào từ client thì đẩy ngay vào channel này rồi quay lại đọc tiếp.
   - **Xử lý Tuần tự theo Thiết bị (Per-Device Ordering):** Sử dụng **Single Logical Ingress Worker** đọc tuần tự từ `_ingressChannel`.
     - Với các bản tin làm thay đổi trạng thái (`METRIC_BATCH`, `HEARTBEAT`, `SERVICE_STATUS`, `INCIDENT_ACK`, `INCIDENT_RESOLVE`, v.v.): Xử lý tuần tự theo thiết bị để bảo toàn thứ tự chuỗi mẫu (Streak) và trạng thái FSM.
     - Với các truy vấn chỉ đọc (`LIST_DEVICES`, `LIST_INCIDENTS`, `GET_HISTORY`, v.v.): Dispatcher chuyển giao cho `Task.Run` thực thi bất đồng bộ trên ThreadPool.
4. **Xây dựng `MonotonicLivenessRegistry.cs`:**
   - Lưu thời điểm nhận tin gần nhất bằng Monotonic Clock: `device.LastSeenTimestamp = Stopwatch.GetTimestamp()`.
   - Chạy một `PeriodicTimer` chu kỳ **2.5 giây**: Nếu máy nào có `(Now - LastSeen) > 20.0 giây` (tương đương 4 chu kỳ heartbeat):
     - Đổi trạng thái trong CSDL: `Status = 'OFFLINE'`.
     - Kích hoạt sự cố `DEVICE_OFFLINE` (Severity: `CRITICAL`).
     - Phát sóng bản tin `DEVICE_UPDATE` và `INCIDENT_OPEN` cho Bạn C.
5. **Xây dựng `RuleEngine.cs` (Streak & Hysteresis):**
   - Lưu vết 3 mẫu gần nhất của từng máy:
     - `HIGH_CPU`: Cả 3 mẫu liên tiếp $\ge 90\%$ $\rightarrow$ Kích hoạt `ConditionState = ACTIVE`. Nếu đang lỗi mà có 3 mẫu liên tiếp $\le 75\%$ $\rightarrow$ Tự phục hồi `ConditionState = RECOVERED`.
     - `HIGH_MEMORY`: Bật khi $\ge 85\%$ (3 mẫu); Tắt khi $\le 70\%$ (3 mẫu).
     - `SERVICE_DOWN`: Bật khi 2 lần probe fail; Tắt khi 2 lần probe pass.
6. **Xây dựng `DatabaseRepository.cs` (Dapper + Transactions):**
   - Khi nhận `METRIC_BATCH`: Mở 1 `SqlTransaction`.
   - Kiểm tra bảng `ProcessedBatches`. Nếu đã có `(DeviceId, BootId, BatchId)` $\rightarrow$ Rollback không ghi `MetricSamples`, nhưng **vẫn phát `BATCH_ACK` cho Bạn A**.
   - Nếu chưa có $\rightarrow$ Ghi `MetricSamples`, chạy Rule Engine, ghi `Incidents`, ghi `ProcessedBatches`.
   - **GỌI `tx.CommitAsync()` XONG MỚI ĐƯỢC GỬI `BATCH_ACK` RA MẠNG!**
7. **Xử lý Khóa Lạc quan (OCC):**
   - Khi nhận lệnh đổi trạng thái sự cố (`INCIDENT_ACK`, `INCIDENT_RESOLVE`):
     ```sql
     UPDATE Incidents SET WorkflowState = @state 
     WHERE IncidentId = @id AND RowVersion = @expectedVersion;
     ```
   - Nếu `@@ROWCOUNT == 0` thì trả về bản tin `COMMAND_RESULT` kèm mã lỗi `ERR_CONCURRENCY_VIOLATION`.

### 2. Các cạm bẫy Bạn B cần tránh:
- ❌ **Bẫy 1:** Dùng $N$ worker cùng rút dữ liệu Ingress song song mà không khóa theo thiết bị $\rightarrow$ Dẫn đến mẻ đo đến sau bị xử lý trước, làm đảo lộn thứ tự mẫu CPU/RAM.
- ❌ **Bẫy 2:** Dùng `DateTime.UtcNow` để tính 20s offline $\rightarrow$ Nếu giờ máy tính bị đổi thì hệ thống báo offline sai toàn bộ phòng máy.
- ❌ **Bẫy 3:** Thấy batch trùng lặp thì lẳng lặng bỏ qua không gửi ACK $\rightarrow$ Bạn A sẽ tưởng Server chưa nhận được và retry liên tục làm nghẽn mạng.
- ❌ **Bẫy 4:** Khi CSDL bị mất kết nối lại cố tạo thêm một hàng đợi RAM thứ hai $\rightarrow$ Gây trùng lặp dữ liệu và nguy cơ tràn RAM; hãy để Agent tự quản lý dữ liệu trong ReplayBuffer của mình!

---

## PHẦN 4: SỔ TAY THỰC CHIẾN CHO BẠN C (ADMIN WINFORMS)

> **Trọng trách của Bạn C:** Bộ mặt của đồ án. Giảng viên sẽ nhìn vào phần mềm WinForms của Bạn C để bấm thử. Bạn C cần hiển thị phòng máy trực quan, cập nhật số liệu thời gian thực mượt mà và xử lý sự cố chuẩn xác.

### 1. Các việc Bạn C cần làm:
1. **Xây dựng `AdminTcpClient.cs`:**
   - Kết nối TCP tới Server của Bạn B.
   - Gửi bản tin đăng nhập `LOGIN` (nhận lại `LOGIN_RESULT` chứa session token lưu tạm trong RAM).
   - Đăng ký nhận sự kiện: Gửi `SUBSCRIBE_DEVICES` và `SUBSCRIBE_INCIDENTS`.
2. **Cơ chế Quản lý Phản hồi Lệnh (`PendingRequestRegistry.cs`):**
   - Khi Admin bấm một nút (ví dụ: nút "Tiếp nhận sự cố"):
     - Tạo `requestId = Guid.NewGuid().ToString()`.
     - Tạo `TaskCompletionSource<WireEnvelope>` lưu vào từ điển theo `requestId`.
     - Gửi frame lên Server và `await task.WithTimeout(TimeSpan.FromSeconds(10))`.
     - Khi luồng đọc nhận được bản tin phản hồi `COMMAND_RESULT` có `requestId` tương ứng, kích hoạt task hoàn thành.
3. **Quy tắc An toàn Luồng trên Giao diện (Thread-Safe UI):**
   - Khi Server đẩy về sự kiện `DEVICE_UPDATE` hoặc `INCIDENT_UPDATE`, sự kiện này chạy trên Worker Thread.
   - **Bắt buộc dùng `Invoke`:**
     ```csharp
     if (this.InvokeRequired) {
         this.Invoke(new Action(() => UpdateDeviceStatus(evt)));
     } else {
         UpdateDeviceStatus(evt);
     }
     ```
4. **Giao diện Quản lý Sự cố 2 Tầng:**
   - Bảng danh sách sự cố phải hiển thị rõ 2 cột:
     - **Tình trạng kỹ thuật (`ConditionState`):** Màu đỏ (`ACTIVE`), Xanh lá (`RECOVERED`), Xám (`UNKNOWN`).
     - **Tiến độ vận hành (`WorkflowState`):** `OPEN`, `ACKNOWLEDGED`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED`.
   - Các nút bấm tương ứng bản tin chuẩn:
     - "Tiếp nhận" $\rightarrow$ Gửi `INCIDENT_ACK`.
     - "Bắt đầu xử lý" $\rightarrow$ Gửi `INCIDENT_START`.
     - "Đóng sự cố" $\rightarrow$ Gửi `INCIDENT_RESOLVE`.
   - Lưu lại `expectedRowVersion = incident.RowVersion` để gửi kèm khi bấm nút.
5. **Bắt lỗi Khóa Lạc quan (OCC Handling):**
   - Nếu Bạn B trả về `COMMAND_RESULT` có lỗi `ERR_CONCURRENCY_VIOLATION`:
     - Hiện thông báo: *"Dữ liệu sự cố đã được cập nhật bởi quản trị viên khác. Đang làm mới dữ liệu..."*
     - Tự động gọi `LIST_INCIDENTS` để lấy lại danh sách mới nhất từ server.

---

## PHẦN 5: BỘ CÂU HỎI VẤN ĐÁP TRỌNG TÂM NT106

1. **Hỏi Bạn A:** *"Tại sao em gửi JSON qua TCP mà phải thêm 4 byte độ dài ở đầu?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, TCP là một byte stream liên tục, không có ranh giới tin nhắn. Nếu không có 4 byte độ dài (Length-prefix), khi truyền trên mạng các gói tin có thể bị dính liền vào nhau hoặc bị đọc dở dang làm bộ phân tích cú pháp JSON bị lỗi. 4 byte độ dài Big-Endian cho bên nhận biết trước chính xác cần đọc bao nhiêu byte để bóc tách đúng một message hoàn chỉnh ạ."*

2. **Hỏi Bạn A:** *"Tại sao khi mất mạng, phần mềm của em không kết nối lại ngay lập tức sau mỗi 1 giây?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, nếu 20 máy trạm cùng kết nối lại sau mỗi 1 giây thì khi Server vừa khởi động lại sẽ gặp hiện tượng Bão kết nối lại (Reconnect Storm), làm tràn hàng đợi kết nối. Em dùng thuật toán Truncated Exponential Backoff tăng dần thời gian chờ từ 1s đến 30s và cộng thêm độ lệch ngẫu nhiên Jitter $\pm 10\%$ để các máy kết nối rải rác, bảo vệ máy chủ an toàn ạ."*

3. **Hỏi Bạn B:** *"Làm sao Server biết máy trạm bị rút dây mạng nếu hàm `Socket.Connected` của C# vẫn báo true?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, thuộc tính `Socket.Connected` chỉ phản ánh kết quả sau thao tác gửi/nhận gần nhất, không thể phát hiện ngắt cáp đột ngột (Half-Open Connection). Em xây dựng bản tin `HEARTBEAT` chu kỳ 5s và dùng Monotonic Clock (`Stopwatch`) trên server để đo. Cứ quá 20s (4 chu kỳ) không thấy tín hiệu là server phán quyết máy OFFLINE, giải phóng tài nguyên và phát cảnh báo ạ."*

4. **Hỏi Bạn B:** *"Tại sao Server của em nhận cùng một mẻ đo 2 lần lại không lưu vào CSDL nhưng vẫn gửi lại ACK?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, mẻ đo gửi lặp chứng tỏ lần trước Server đã lưu CSDL thành công nhưng gói `BATCH_ACK` gửi về bị rớt mạng giữa đường khiến Agent tưởng Server chưa nhận được nên mới gửi lại. Nếu Server không gửi lại ACK, Agent sẽ tiếp tục thử lại vô hạn làm nghẽn mạng. Do đó, Server kiểm tra bảng `ProcessedBatches`, bỏ qua việc lưu số liệu nhưng bắt buộc phải gửi lại ACK để Agent xóa mẻ đo đó khỏi bộ nhớ đệm ReplayBuffer ạ."*

5. **Hỏi Bạn C:** *"Nếu hai người quản trị cùng bấm sửa một sự cố cùng một lúc thì phần mềm của em xử lý thế nào?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, nhóm em sử dụng cơ chế Khóa lạc quan (Optimistic Concurrency Control) thông qua cột `rowversion` của SQL Server. Mỗi khi một Admin sửa thành công, SQL Server tự động sinh số phiên bản mới. Admin thứ hai gửi lệnh mang phiên bản cũ thì câu lệnh UPDATE sẽ tác động lên 0 dòng. Server phát hiện xung đột và trả về mã lỗi `ERR_CONCURRENCY_VIOLATION`. Phần mềm WinForms của em bắt lỗi này, hiển thị thông báo và tự động tải lại dữ liệu mới nhất ạ."*

6. **Hỏi Bạn C:** *"Tại sao khi Server đẩy sự kiện về, em không gán thẳng dữ liệu vào Label hay DataGridView mà phải dùng `Invoke`?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, các sự kiện mạng được nhận trên các Worker Thread của ThreadPool. Trong Windows Forms, mọi thao tác cập nhật giao diện bắt buộc phải thực thi trên luồng chính (UI Thread). Nếu truy cập chéo luồng sẽ vi phạm an toàn luồng và ném lỗi `InvalidOperationException`. Lệnh `this.Invoke()` giúp chuyển giao việc cập nhật giao diện về đúng UI Thread an toàn ạ."*
