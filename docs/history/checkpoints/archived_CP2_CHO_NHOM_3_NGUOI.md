> **SUPERSEDED / HISTORICAL ARTIFACT**  
> This document has been superseded and preserved for historical audit trail only.  
> Current authoritative candidate: [CP2_DECISIONS_REV2.md](../CP2_DECISIONS_REV2.md) / [AUDIT_HANDOFF_CHECKPOINT_2_REV2.md](../AUDIT_HANDOFF_CHECKPOINT_2_REV2.md).

---

# LABGUARD — SỔ TAY THỰC CHIẾN CHECKPOINT 2 DÀNH CHO NHÓM 3 SINH VIÊN
**Tài liệu:** `CP2_CHO_NHOM_3_NGUOI.md`  
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Đối tượng:** Ba sinh viên Bạn A (Agent), Bạn B (Server/DB), Bạn C (Admin WinForms)  
**Mục tiêu:** Mở file ra trong 5 phút là biết chính xác mình cần code những class nào, gọi hàm gì, tham số bao nhiêu và tránh những lỗi ngớ ngẩn nào để lấy trọn điểm 10 vấn đáp!  

---

## PHẦN 1: BỨC TRANH TOÀN CẢNH — 3 BẠN KẾT NỐI VỚI NHAU THẾ NÀO?

Trước khi mở Visual Studio gõ code, cả 3 bạn hãy cùng nhìn vào sơ đồ dòng chảy dữ liệu đơn giản này:

```
[BẠN A: AGENT TRÊN MÁY TRẠM]
  │  (Gửi Heartbeat 5s, Gửi Telemetry 5s, Thử lại Backoff, Gửi bù Replay)
  │
  ▼ [TCP Socket + Length-Prefix 4 bytes + JSON UTF-8]
  │
[BẠN B: CENTRAL SERVER & SQL SERVER]
  │  (Đọc Frame, Hàng đợi Ingress 1000, Lọc trùng ProcessedBatches, 
  │   Đồng hồ Monotonic 20s, Rule Engine Streak 3, Ghi DB qua Transaction)
  │
  ▲ [TCP Socket + Length-Prefix 4 bytes + Realtime Events]
  │
[BẠN C: ADMIN WINFORMS DASHBOARD]
  │  (Màn hình Dashboard, Đổi màu máy Trạm, Quản lý Sự cố 2 tầng, 
  │   Khóa lạc quan OCC rowversion, An toàn luồng UI Control.Invoke)
```

- **Nguyên tắc làm việc chung số 1:** Cả 3 bạn dùng chung một thư viện class mang tên `LabGuard.Protocol` (do Bạn A chủ trì khởi tạo). Thư viện này chứa class `FrameCodec` và các DTO (Data Transfer Objects) như `WireEnvelope`, `MetricBatchPayload`, `IncidentEventDto`. Tuyệt đối không ai tự định nghĩa lại format gói tin riêng!
- **Nguyên tắc số 2:** Không ai được dùng lệnh chặn luồng (Blocking call) như `Thread.Sleep()`, `.Result`, `.Wait()`. Toàn bộ đều dùng `await ...Async()`.

---

## PHẦN 2: SỔ TAY THỰC CHIẾN CHO BẠN A (AGENT & SIMULATOR)

> **Trọng trách của Bạn A:** Viết phần mềm chạy ngầm trên máy trạm Windows, lấy thông số CPU/RAM thật, định kỳ gửi về Server, tự kết nối lại nếu rớt mạng, và viết công cụ giả lập 20–50 máy trạm để test tải.

### 1. Các Class chính Bạn A cần tạo:
1. `FrameCodec.cs` (Nằm trong project `LabGuard.Protocol`):
   - Hàm `WriteFrameAsync(Stream stream, byte[] payloadJsonBytes, CancellationToken ct)`: Ghi 4 byte độ dài Big-Endian rồi ghi tiếp payload.
   - Hàm `ReadFrameAsync(Stream stream, int maxFrameSize, CancellationToken ct)`: Đọc đúng 4 byte độ dài Big-Endian, kiểm tra xem có vượt quá 256 KiB không, sau đó đọc đủ số byte payload bằng vòng lặp `ReadExactlyAsync`. Nếu gặp EOF giữa chừng thì ném `EndOfStreamException`.
2. `AgentSocketClient.cs`:
   - Quản lý vòng đời kết nối TCP tới Central Server (mặc định IP Server, Port `5000`).
   - Cài đặt **Mô hình Single Writer Loop**: Tạo `Channel<byte[]> _outbox = Channel.CreateBounded<byte[]>(256)`.
   - Một background task duy nhất chạy vòng lặp đọc từ `_outbox` và ghi ra `NetworkStream`. Mọi nơi khác muốn gửi tin chỉ được gọi `_outbox.Writer.WriteAsync(frame)`.
3. `HeartbeatTimer.cs`:
   - Dùng `PeriodicTimer` chu kỳ đúng **5.0 giây**.
   - Mỗi tick tạo bản tin `HEARTBEAT` đẩy vào `_outbox`.
4. `MetricsCollector.cs`:
   - Dùng `System.Diagnostics.PerformanceCounter` hoặc API Windows `GetSystemTimes` để lấy phần trăm CPU thật; dùng `GC.GetGCMemoryInfo()` hoặc `GlobalMemoryStatusEx` để lấy RAM thật.
   - Chu kỳ **5.0 giây**, gom thành bản tin `METRIC_BATCH`.
   - Gán `BatchId = Guid.NewGuid().ToString()` và `BootId` cố định của máy.
5. `ReplayBuffer.cs`:
   - Một hàng đợi RAM `Queue<MetricBatchEnvelope>` giới hạn **50 phần tử**.
   - Nếu mất mạng, các batch thu thập được đẩy vào đây (nếu vượt quá 50 thì bỏ batch cũ nhất).
   - Khi có mạng lại, xả tuần tự từng batch kèm cờ `"isReplay": true`. **Tuyệt đối không lưu và không replay gói Heartbeat!**
6. `ReconnectHandler.cs`:
   - Cài đặt thuật toán lùi thời gian lũy tiến:
     $$\text{delay} = \min(30, 1.0 \times 2^k) \times (1 + \text{random}(-0.1, +0.1))$$
   - Chuỗi thời gian: $1\text{s} \rightarrow 2\text{s} \rightarrow 4\text{s} \rightarrow 8\text{s} \rightarrow 16\text{s} \rightarrow 30\text{s}$.
   - Nhận được login ACK thì reset $k = 0$.
7. `AgentSimulator.exe`:
   - Tạo một ứng dụng Console cho phép nhập số lượng máy ảo (ví dụ: gõ `20` hoặc `50`).
   - Khởi tạo 20–50 kết nối TCP đồng thời bắn Heartbeat và Metric về Server để phục vụ Bạn B và Bạn C test tải.

### 2. Ba cạm bẫy Bạn A tuyệt đối phải tránh:
- ❌ **Bẫy 1:** Gọi trực tiếp `stream.WriteAsync` ở nhiều hàm khác nhau $\rightarrow$ Dính lỗi xé khung làm vỡ gói tin của Bạn B.
- ❌ **Bẫy 2:** Không cộng Jitter $\pm 10\%$ khi kết nối lại $\rightarrow$ Khi Server restart, 50 máy ào vào cùng 1 giây gây bão kết nối (Reconnect Storm).
- ❌ **Bẫy 3:** Lưu gói Heartbeat vào ReplayBuffer $\rightarrow$ Khi có mạng bắn lên 50 cái Heartbeat cũ rích làm Server phán đoán sai liveness.

---

## PHẦN 3: SỔ TAY THỰC CHIẾN CHO BẠN B (SERVER & DATABASE)

> **Trọng trách của Bạn B:** "Trái tim" của hệ thống. Quản lý kết nối mạng, hàng đợi nhận tin, lọc trùng lặp, chấm điểm sống chết máy trạm, đánh giá sự cố và lưu cơ sở dữ liệu SQL Server.

### 1. Các việc Bạn B cần làm:
1. **Chạy kịch bản SQL Server:**
   - Mở SSMS, kết nối vào `localhost` SQL Server 2022 Express.
   - Chạy toàn bộ file script trong `research/06_DATABASE.md` để sinh đủ 10 bảng. Chú ý bảng `Incidents` phải có cột `RowVersion ROWVERSION NOT NULL`.
2. **Xây dựng `CentralServer.cs`:**
   - Dùng `TcpListener` lắng nghe trên cổng `5000`.
   - Vòng lặp `AcceptTcpClientAsync` đón kết nối. Mỗi kết nối sinh ra một `ClientConnectionContext` chứa `ConnectionId` và một Reader Task riêng.
3. **Xây dựng `CentralIngressPipeline.cs`:**
   - Tạo hàng đợi trung tâm: `Channel<IncomingEnvelope> _ingressChannel = Channel.CreateBounded<IncomingEnvelope>(1000)`.
   - Các Reader Task đọc được frame nào từ client thì đẩy ngay vào channel này rồi quay lại đọc tiếp (không đợi ghi DB tại luồng đọc!).
   - Tạo 1 hoặc nhiều Background Worker đọc từ `_ingressChannel` để xử lý logic nghiệp vụ.
4. **Xây dựng `MonotonicLivenessRegistry.cs`:**
   - Một từ điển trong bộ nhớ RAM: `ConcurrentDictionary<string, DeviceLivenessInfo>`.
   - Lưu thời điểm nhận tin gần nhất bằng Monotonic Clock: `device.LastSeenTimestamp = Stopwatch.GetTimestamp()`.
   - Chạy một `PeriodicTimer` chu kỳ **2.5 giây**: Nếu máy nào có `(Now - LastSeen) > 20.0 giây` thì:
     - Đổi trạng thái trong DB: `Status = 'OFFLINE'`.
     - Kích hoạt sự cố `DEVICE_OFFLINE` (Severity: `CRITICAL`).
     - Đẩy sự kiện thời gian thực cho Bạn C.
5. **Xây dựng `RuleEngine.cs` (Streak & Hysteresis):**
   - Lưu vết 3 mẫu gần nhất của từng máy:
     - `HIGH_CPU`: Nếu cả 3 mẫu liên tiếp $\ge 90\%$ $\rightarrow$ Kích hoạt `ConditionState = ACTIVE`. Nếu đang lỗi mà có 3 mẫu liên tiếp $\le 75\%$ $\rightarrow$ Tự phục hồi `ConditionState = RECOVERED`.
     - `HIGH_MEMORY`: Bật khi $\ge 85\%$ (3 mẫu); Tắt khi $\le 70\%$ (3 mẫu).
     - `SERVICE_DOWN`: Bật khi 2 lần probe fail; Tắt khi 2 lần probe pass.
6. **Xây dựng `DatabaseRepository.cs` (Dapper + Transactions):**
   - Khi nạp Metric: Mở 1 `SqlTransaction`.
   - Kiểm tra bảng `ProcessedBatches`. Nếu đã có `(DeviceId, BootId, BatchId)` $\rightarrow$ Rollback không ghi `MetricSamples`, nhưng **vẫn phát `BATCH_ACK` cho Bạn A**.
   - Nếu chưa có $\rightarrow$ Ghi `MetricSamples`, chạy Rule Engine, ghi `Incidents`, ghi `ProcessedBatches`.
   - **GỌI `tx.CommitAsync()` XONG MỚI ĐƯỢC GỬI GÓI TIN ACK RA MẠNG!**
7. **Xử lý Khóa Lạc quan (OCC):**
   - Khi nhận lệnh đổi trạng thái sự cố từ Bạn C:
     ```sql
     UPDATE Incidents SET WorkflowState = @state 
     WHERE IncidentId = @id AND RowVersion = @expectedVersion;
     ```
   - Nếu `@@ROWCOUNT == 0` thì trả về lỗi `ERR_CONCURRENCY_VIOLATION`.

### 2. Ba cạm bẫy Bạn B tuyệt đối phải tránh:
- ❌ **Bẫy 1:** Dùng `DateTime.UtcNow` để tính 20s offline $\rightarrow$ Lỡ giờ máy tính bị đổi thì hệ thống đơ hoặc báo offline toàn bộ phòng máy.
- ❌ **Bẫy 2:** Thấy batch trùng lặp thì lẳng lặng bỏ qua không gửi ACK $\rightarrow$ Bạn A sẽ tưởng Server chưa nhận được và retry liên tục làm tắc nghẽn mạng.
- ❌ **Bẫy 3:** Gửi ACK trước khi Transaction SQL Server Commit $\rightarrow$ Lỡ SQL Server bị lỗi deadlock thì mất dữ liệu vĩnh viễn.

---

## PHẦN 4: SỔ TAY THỰC CHIẾN CHO BẠN C (ADMIN WINFORMS)

> **Trọng trách của Bạn C:** Bộ mặt của đồ án. Người chấm thi sẽ nhìn vào phần mềm WinForms của Bạn C để bấm thử. Bạn C cần hiển thị phòng máy trực quan, cập nhật số liệu thời gian thực mượt mà và xử lý sự cố chuẩn xác.

### 1. Các việc Bạn C cần làm:
1. **Xây dựng `AdminTcpClient.cs`:**
   - Kết nối TCP tới Server của Bạn B.
   - Gửi bản tin đăng nhập `ADMIN_LOGIN_REQ` (nhận lại session token lưu tạm trong RAM).
   - Đăng ký nhận sự kiện: Gửi `SUBSCRIBE_REQ` (topic: `ALL`).
2. **Cơ chế Quản lý Phản hồi Lệnh (`PendingRequestRegistry.cs`):**
   - Khi Admin bấm một nút (ví dụ: nút "Tiếp nhận sự cố"):
     - Tạo một `requestId = Guid.NewGuid().ToString()`.
     - Tạo một `TaskCompletionSource<WireEnvelope>` và lưu vào `ConcurrentDictionary<string, TaskCompletionSource>`.
     - Gửi frame lên Server và `await task.WithTimeout(TimeSpan.FromSeconds(10))`.
     - Khi luồng đọc nhận được gói tin phản hồi có `requestId` tương ứng, kích hoạt task hoàn thành.
3. **Quy tắc An toàn Luồng trên Giao diện (Thread-Safe UI):**
   - Khi Server bắn về một sự kiện thời gian thực (ví dụ: máy PC-01 bị đỏ do quá tải CPU), sự kiện này chạy trên một Background Worker Thread.
   - **Tuyệt đối không được gán trực tiếp:** `lblCpu.Text = "95%";` (sẽ bị crash ứng dụng ngay lập tức do lỗi `Cross-thread operation not valid`).
   - **Bắt buộc dùng `Invoke`:**
     ```csharp
     if (this.InvokeRequired) {
         this.Invoke(new Action(() => UpdateDeviceStatus(evt)));
     } else {
         UpdateDeviceStatus(evt);
     }
     ```
4. **Thiết kế Giao diện Quản lý Sự cố 2 Tầng:**
   - Bảng danh sách sự cố phải hiển thị rõ 2 cột:
     - **Tình trạng máy móc (`ConditionState`):** Màu đỏ (`ACTIVE`), Xanh lá (`RECOVERED`), Xám (`UNKNOWN`).
     - **Tiến độ xử lý (`WorkflowState`):** `OPEN`, `ACKNOWLEDGED`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED`.
   - Các nút bấm: "Tiếp nhận (ACK)", "Bắt đầu xử lý (IN_PROGRESS)", "Đóng sự cố (RESOLVE)".
   - Lưu lại `byte[] currentVersion = incident.RowVersion` để gửi kèm khi bấm nút.
5. **Bắt lỗi Khóa Lạc quan (OCC Handling):**
   - Nếu Bạn B trả về mã lỗi `ERR_CONCURRENCY_VIOLATION`:
     - Hiện thông báo thân thiện: *"Sự cố này vừa được một quản trị viên khác cập nhật. Hệ thống đang tự làm mới dữ liệu..."*
     - Tự động gọi hàm lấy lại danh sách mới nhất từ server.
6. **Project P1 (Nếu làm kịp đến Tuần 4):**
   - Bạn C chịu trách nhiệm viết file chạy duy nhất `LabGuard.Collector.exe` để trung chuyển dữ liệu từ Agent về Central Server.

### 2. Ba cạm bẫy Bạn C tuyệt đối phải tránh:
- ❌ **Bẫy 1:** Quên dùng `Invoke` khi nhận event từ socket $\rightarrow$ App WinForms văng màu xanh chết chóc ngay trước mặt Giảng viên!
- ❌ **Bẫy 2:** Lưu cứng (Hardcode) mật khẩu trong file config $\rightarrow$ Bị trừ điểm bảo mật.
- ❌ **Bẫy 3:** Bấm nút gửi lệnh mà không vô hiệu hóa (Disable) nút bấm trong lúc chờ Server $\rightarrow$ Người dùng bấm liên tục 5 lần sinh ra race condition.

---

## PHẦN 5: KẾ HOẠCH TÍCH HỢP & TEST CHÉO GIỮA 3 BẠN

Để tránh tình trạng "trên máy tớ chạy ngon mà ghép lại không chạy", nhóm thực hiện tích hợp theo đúng 4 bước sau:

```
[BƯỚC 1: Ngày thứ 2 của Tuần 2]
Bạn A và Bạn B ngồi cạnh nhau: Chạy bài test CP2-TEST-001 (FrameCodec).
A gửi mảng byte chia nhỏ -> B đọc đúng nguyên vẹn chuỗi JSON. -> XONG TẦNG GIAO VẬN!

[BƯỚC 2: Ngày thứ 4 của Tuần 2]
Bạn A bật Agent thật -> Bắn Heartbeat và Metric lên Server Bạn B.
Bạn B mở SSMS kiểm tra bảng MetricSamples: Thấy số liệu CPU thật của máy A xuất hiện!

[BƯỚC 3: Cuối Tuần 2]
Bạn C mở WinForms kết nối vào Server Bạn B.
Nhìn thấy tên máy tính của Bạn A hiện màu Xanh lá trên màn hình Dashboard của Bạn C!

[BƯỚC 4: Cuối Tuần 3 (Mốc M4 Quyết định)]
Cả 3 bạn cùng chạy bài test CP2-TEST-007 (Đẩy tải CPU lên 95%):
A sinh tải -> B kích hoạt sự cố -> C thấy dòng sự cố nhấp nháy đỏ trên WinForms.
C bấm "Tiếp nhận" -> B cập nhật DB -> Bắn event cập nhật lại cho C!
```

---

## PHẦN 6: BÍ KÍP VẤN ĐÁP NT106 — TOP 6 CÂU HỎI THẦY CÔ HAY HỎI NHẤT

Khi thầy cô chỉ tay vào từng bạn và hỏi, các bạn chỉ cần trả lời ngắn gọn, rành rọt theo đúng các ý sau:

1. **Hỏi Bạn A:** *"Tại sao em gửi JSON qua TCP mà phải thêm 4 byte độ dài ở đầu làm gì cho mất công?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, TCP là một byte stream liên tục, không có ranh giới tin nhắn. Nếu không có 4 byte độ dài (Length-prefix), khi mạng lag các gói tin sẽ bị dính liền vào nhau hoặc bị đọc dở dang làm bộ phân tích JSON bị sập lỗi cú pháp. 4 byte độ dài Big-Endian giúp bên nhận biết trước chính xác cần đọc bao nhiêu byte để bóc tách đúng một message hoàn chỉnh ạ."*

2. **Hỏi Bạn A:** *"Tại sao khi mất mạng, phần mềm của em không kết nối lại ngay lập tức sau mỗi 1 giây?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, nếu 50 máy trạm cùng kết nối lại sau mỗi 1 giây thì khi Server vừa khởi động lại sẽ gặp hiện tượng Bão kết nối lại (Reconnect Storm), làm sập cổng mạng. Em dùng thuật toán Exponential Backoff tăng dần thời gian chờ từ 1s đến 30s và cộng thêm độ lệch ngẫu nhiên Jitter $\pm 10\%$ để các máy kết nối rải rác, bảo vệ server an toàn ạ."*

3. **Hỏi Bạn B:** *"Làm sao Server biết máy trạm bị rút dây mạng nếu hàm `Socket.Connected` của C# vẫn báo true?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, thuộc tính `Socket.Connected` chỉ thể hiện trạng thái sau thao tác gửi/nhận gần nhất, không thể phát hiện ngắt cáp đột ngột (Half-Open Connection). Em xây dựng Heartbeat 5s tầng ứng dụng và dùng Monotonic Clock (`Stopwatch`) trên server để đo. Cứ quá 20s không thấy tín hiệu là server chủ động phán quyết máy OFFLINE, giải phóng socket và phát cảnh báo ạ."*

4. **Hỏi Bạn B:** *"Tại sao Server của em nhận cùng một gói tin 2 lần lại không lưu vào Database nhưng vẫn gửi lại ACK?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, gói tin gửi lặp chứng tỏ lần trước Server đã lưu DB thành công nhưng gói tin ACK gửi về bị rớt mạng giữa đường khiến Agent tưởng Server chưa nhận được nên mới gửi lại. Nếu Server không gửi lại ACK, Agent sẽ tiếp tục retry vô hạn làm nghẽn mạng. Do đó, Server kiểm tra bảng `ProcessedBatches`, bỏ qua việc lưu số liệu nhưng bắt buộc phải gửi lại ACK để Agent xóa gói tin đó khỏi bộ nhớ đệm ạ."*

5. **Hỏi Bạn C:** *"Nếu hai người quản trị cùng bấm sửa một sự cố cùng một lúc thì phần mềm của em xử lý thế nào?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, nhóm em sử dụng cơ chế Khóa lạc quan (Optimistic Concurrency Control) thông qua cột `rowversion` của SQL Server. Mỗi khi một Admin sửa thành công, mã phiên bản sẽ tự động nhảy số. Admin thứ hai gửi lệnh mang phiên bản cũ thì câu lệnh UPDATE sẽ tác động lên 0 dòng. Server phát hiện xung đột và trả về mã lỗi `ERR_CONCURRENCY_VIOLATION`. Phần mềm WinForms của em bắt lỗi này, hiển thị thông báo nhẹ nhàng và tự động tải lại dữ liệu mới nhất ạ."*

6. **Hỏi Bạn C:** *"Tại sao khi Server đẩy sự kiện về, em không gán thẳng dữ liệu vào Label hay DataGridView mà phải dùng `Invoke`?"*
   - **Trả lời:** *"Dạ thưa Thầy/Cô, các sự kiện mạng được nhận và xử lý trên các Worker Thread của ThreadPool. Trong Windows Forms, mọi thao tác thay đổi giao diện bắt buộc phải thực thi trên luồng chính (UI Thread) đang sở hữu tay cầm điều khiển (Handle). Nếu truy cập chéo luồng sẽ vi phạm an toàn luồng và gây lỗi `InvalidOperationException: Cross-thread operation not valid`. Lệnh `this.Invoke()` giúp chuyển giao việc cập nhật giao diện về đúng UI Thread một cách an toàn ạ."*

---

> **LỜI NHẮN NHỦ CHO CẢ NHÓM:**  
> Toàn bộ các thông số kỹ thuật đã được chốt cứng. Các bạn không cần phải băn khoăn hay đoán mò nữa. Hãy mở Visual Studio, làm đúng theo các class và quy tắc trong sổ tay này, các bạn chắc chắn sẽ hoàn thành đồ án xuất sắc và tự tin đạt điểm tối đa môn NT106!
