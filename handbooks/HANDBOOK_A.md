# SỔ TAY KỸ THUẬT & VẤN ĐÁP — SINH VIÊN A (HANDBOOK A)
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Vai trò:** Lead Systems & Network Transport Engineer  
**Mảng sở hữu (Ownership):** `Agent Windows Core` + `Length-Prefix FrameCodec` + `Agent Simulator (20+ nodes)` + `Network Load Harness` + `Gateway L4 (P1)` + `Triển khai & Thử nghiệm 2 Collector Instances`  

---

## I. TỔNG QUAN VAI TRÒ & RANH GIỚI TRÁCH NHIỆM

### 1. Tôi sở hữu chương trình / module nào?
- **Thư viện Giao thức dùng chung (`Shared.Protocol`):**
  * Chủ trì thiết kế và cài đặt `FrameCodec`: Bộ mã hóa và giải mã khung truyền nhị phân Length-Prefix 4 byte Big-Endian kèm chuỗi JSON UTF-8 (RFC 8259).
  * Đảm bảo xử lý triệt để phân mảnh dòng byte (Partial Read) và dính gói (Frame Coalescing). Ném `EndOfStreamException` khi đứt kết nối giữa chừng để hủy stream an toàn.
  * Định nghĩa toàn bộ DTOs của 16 loại MessageType wire chuẩn theo `CONTRACT_MATRIX.md`.
- **Dịch vụ giám sát máy trạm (`LabGuard.Agent`):**
  * Chương trình Windows Background Service chạy ngầm trên máy tính phòng máy.
  * Thu thập chỉ số phần cứng thật (CPU host qua Pdh/PerformanceCounter, RAM vật lý, lưu lượng mạng NIC, Uptime hệ thống) định kỳ 5 giây/lần.
  * Thăm dò trạng thái cổng TCP cục bộ (Service Probe) theo danh sách allowlist tối đa 5 cổng, timeout 2s/cổng.
  * Quản lý kết nối socket TCP duplex tới Central Server (hoặc Gateway/Collector): Single Writer Loop qua `Channel<byte[]>`, bảo đảm an toàn đa luồng trên `NetworkStream.WriteAsync`.
  * Bộ lập lịch gửi bản tin nhịp tim (`HEARTBEAT`) chu kỳ 5 giây và bản tin mẻ chỉ số (`METRIC_BATCH`).
  * Hàng đợi lưu tạm tại bộ nhớ máy trạm (Replay Queue) và thuật toán Reconnect Exponential Backoff (1s, 2s, 4s... max 30s) có bổ sung jitter ngẫu nhiên để chống bão kết nối (reconnect storm).
- **Bộ giả lập mạng tải cao (`LabGuard.Simulator`):**
  * Khởi tạo đồng thời 10, 20 đến 50 kết nối socket độc lập trên một máy tính vật lý để mô phỏng toàn bộ phòng máy, kiểm thử độ chịu tải và chống nghẽn của Central Server.
- **Mở rộng P1 (Gateway & Deployment):**
  * `LabGuard.Gateway.exe`: Bộ điều phối L4 reverse proxy, sử dụng thuật toán ít kết nối nhất (least-connections) và polling kiểm tra sức khỏe Collector (Health Check).
  * Cấu hình, triển khai và kiểm thử **hai instance độc lập** của ứng dụng Collector dùng chung (do Sinh viên C lập trình), thực hiện kịch bản hạ gục node (Collector Kill) để đo thời gian failover p95 $\le 45$ giây.

### 2. Tôi bắt đầu được task nào ngay sau khi contract chốt?
- Bắt đầu ngay **LG-A-001** (Hoàn thiện `CONTRACT_MATRIX.md` và bộ dữ liệu mẫu JSON fixtures).
- Bắt đầu ngay **LG-A-002** (Lập trình `FrameCodec.cs` và bộ unit test kiểm thử phân mảnh byte stream).
- Bắt đầu ngay **LG-A-003** (Xây dựng kết nối TCP socket Agent kèm hàng đợi Single Writer Channel).
- Bắt đầu ngay **LG-A-004** (Cài đặt TLS Client qua `SslStream` và lưu token định danh máy trạm).
- Bắt đầu ngay **LG-A-005** (Lập trình module đọc chỉ số CPU, RAM, NIC, Uptime trên Windows).

### 3. Tôi tự kiểm tra độc lập bằng công cụ gì?
- Sử dụng **Mini Test Server Harness** (chương trình TCP console giả lập phía server):
  * Lắng nghe port 5000, bắt tay TLS với Agent.
  * Nhận các frame nhị phân, kiểm tra giải mã JSON envelope 6 trường.
  * Phản hồi `HEARTBEAT_ACK` và `BATCH_ACK` theo đúng chuẩn.
  * Chủ động gửi thiếu byte hoặc ngắt kết nối đột ngột để kiểm tra xem Agent có tự kích hoạt Replay Queue và Backoff hay không.
  * **Tuyệt đối không cần chờ Central Server thật của Người B để hoàn thiện và kiểm thử Agent.**

### 4. Tôi cần người khác cung cấp những gì và vào lúc nào?
- **Từ Người B (Central Server):**
  * Cuối Tuần 1: Endpoint Central Server thật để kết nối thử nghiệm luồng mỏng (Thin Slice M1).
  * Cuối Tuần 2: Xác nhận Central Server tiếp nhận ổn định tải từ 20 socket giả lập của Simulator.
- **Từ Người C (Admin / Collector):**
  * Cuối Tuần 3: Phối hợp chạy 1 Agent thật trên máy A để dữ liệu hiển thị lên Dashboard của C.
  * Đầu Tuần 4 (nếu GO P1): Cung cấp file thực thi `LabGuard.Collector.exe` để A triển khai 2 instance và cấu hình sau Gateway.

### 5. Tôi bàn giao những gì cho các mốc tích hợp?
- **Mốc M0 (Thứ 3 W1):** Thư viện `Shared.Protocol.dll` gồm `FrameCodec`, DTOs và Enum MessageType chuẩn.
- **Mốc M1 (Cuối W1):** Mã nguồn Agent kết nối và gửi thành công 1 mẻ metric thật vào Central Server.
- **Mốc M2 (Cuối W2):** Ứng dụng `LabGuard.Simulator.exe` tạo 20 kết nối đồng thời chạy ổn định trong 30 phút.
- **Mốc M4 (Cuối W3):** Báo cáo kết quả kiểm thử Agent thật trên 2 máy tính vật lý trong mạng LAN phòng máy UIT.
- **Mốc M5 (W4 nếu P1):** Bộ điều phối `LabGuard.Gateway.exe` và báo cáo kiểm thử failover Collector kill.
- **Mốc M6 (W5):** Script đóng gói bộ cài đặt tự động `PackageRelease.ps1`, file mã băm `SHA256SUMS` và video minh chứng tải mạng.

---

## II. BỘ 30 CÂU HỎI VẤN ĐÁP CÁ NHÂN — SINH VIÊN A

1. **Tại sao TCP được gọi là giao thức hướng dòng byte (Byte Stream)?**  
   *Trả lời:* TCP truyền một chuỗi byte liên tục và bảo đảm tin cậy, có thứ tự; tuy nhiên TCP hoàn toàn không giữ ranh giới thông điệp (Message Boundary) của tầng ứng dụng. Một lần gửi có thể bị chia nhỏ hoặc dính liền khi nhận.
2. **Hiện tượng phân mảnh (Partial Read) và dính gói (Frame Coalescing) trong TCP là gì?**  
   *Trả lời:* Partial Read là hiện tượng hàm `ReadAsync` chỉ đọc được một phần thông điệp do gói tin bị phân mảnh trên mạng hoặc bộ đệm socket chưa đầy; Frame Coalescing là hiện tượng nhiều thông điệp được gửi liên tiếp bị dính chùm vào cùng một bộ đệm đọc.
3. **Tại sao giải pháp phân khung (Framing) bằng Length-Prefix 4 byte lại tối ưu hơn ký tự kết thúc dòng (Newline Framing)?**  
   *Trả lời:* Vì dữ liệu trao đổi là JSON (RFC 8259) có thể chứa ký tự xuống dòng `\n` bên trong chuỗi string đã escape; Length-Prefix cho biết chính xác số byte cần đọc trước, giúp cấp phát bộ đệm chính xác và tránh duyệt quét từng ký tự tốn CPU.
4. **Quy ước thứ tự byte Big-Endian (Network Byte Order) là gì và tại sao bắt buộc phải dùng?**  
   *Trả lời:* Big-Endian đặt byte có trọng số lớn nhất ở địa chỉ bộ nhớ thấp nhất (đầu tiên trên đường truyền). Vì các kiến trúc CPU khác nhau (x86 dùng Little-Endian, ARM có thể dùng Big/Little) biểu diễn số nguyên khác nhau, bắt buộc phải dùng trật tự chuẩn mạng để giải mã độ dài nhất quán.
5. **Phương thức `ReadExactlyBytesAsync` hoạt động như thế nào khi gặp ngắt kết nối giữa chừng (EOF)?**  
   *Trả lời:* Phương thức đọc lặp trong vòng lặp cho đến khi đủ số byte yêu cầu. Nếu socket bị đóng sớm (`ReadAsync` trả về 0 byte trước khi đủ độ dài), phương thức ném `EndOfStreamException` và hủy stream ngay lập tức, ngăn ngừa việc chuyển mẩu byte rác sang bộ giải mã JSON.
6. **Tại sao lại giới hạn kích thước khung truyền tối đa `MaxFrameSize = 256 KiB`?**  
   *Trả lời:* Để phòng chống tấn công từ chối dịch vụ (DoS / OOM attack). Nếu kẻ xấu gửi 4 byte độ dài giả mạo là 2 GB, server không bị lừa cấp phát mảng byte khổng lồ làm sập bộ nhớ.
7. **Tại sao cờ `Socket.Connected` không phản ánh chính xác trạng thái kết nối mạng thời gian thực?**  
   *Trả lời:* Cờ `Connected` chỉ phản ánh trạng thái sau lần thao tác I/O gần nhất. Nếu client bị mất điện đột ngột hoặc rút dây mạng mà server chưa gọi lệnh đọc/ghi, cờ này vẫn trả về `true` cho đến khi TCP Keep-Alive timeout hoặc I/O thất bại.
8. **Tại sao hệ thống phải cài đặt cơ chế Heartbeat ở tầng ứng dụng?**  
   *Trả lời:* Vì TCP Keep-Alive mặc định của hệ điều hành có thể mất tới 2 giờ để phát hiện sự cố; Heartbeat tầng ứng dụng chu kỳ 5s giúp Central Server xác định chính xác liveness của máy trạm và kích hoạt cảnh báo trong vòng 20 giây.
9. **Tại sao việc đo lường liveness phải dùng đồng hồ đơn điệu (Monotonic Clock) tại server?**  
   *Trả lời:* Dùng `Stopwatch.GetTimestamp()` tại server để đo khoảng cách thời gian trôi qua độc lập với đồng hồ hệ thống (Wall-clock), loại trừ hoàn toàn nguy cơ sai lệch hoặc nhảy lùi thời gian do đồng bộ NTP của client.
10. **Single Writer Pattern trên socket được cài đặt như thế nào và tại sao cần thiết?**  
    *Trả lời:* Tài liệu chính thức từ Microsoft ghi rõ: lớp `NetworkStream` trong .NET chỉ hỗ trợ an toàn cho 1 thao tác đọc và 1 thao tác ghi diễn ra đồng thời. `NetworkStream` KHÔNG hỗ trợ nhiều tác vụ ghi (`WriteAsync`) đồng thời; nếu nhiều luồng cùng gọi `WriteAsync` mà không tự đồng bộ sẽ gây đan xen byte và làm hỏng frame trên wire. Do đó LabGuard sử dụng `Channel<byte[]>` (`System.Threading.Channels`) làm hàng đợi ghi đơn luồng (Single-Writer Queue), bảo đảm chỉ có đúng một Task nền duy nhất đọc từ Channel và ghi tuần tự các frame vào socket.
11. **Sự khác biệt giữa `Channel<T>` có giới hạn (Bounded) và không giới hạn (Unbounded)?**  
    *Trả lời:* Bounded Channel giới hạn số phần tử trong hàng đợi (ví dụ: 100 mẩu tin); khi đầy, luồng ghi sẽ tạm hoãn bất đồng bộ (Backpressure), ngăn chặn việc ứng dụng tiêu tốn RAM vô hạn khi mạng bị nghẽn.
12. **Cơ chế Reconnect Exponential Backoff kết hợp Jitter hoạt động ra sao?**  
    *Trả lời:* Khi mất kết nối, Agent chờ một khoảng thời gian tăng theo cấp số nhân ($1\text{s}, 2\text{s}, 4\text{s}, 8\text{s}... \max 30\text{s}$) cộng thêm một khoảng ngẫu nhiên (jitter), tránh hiện tượng hàng trăm máy trạm kết nối lại cùng một tích tắc gây bão kết nối (reconnect storm).
13. **Tại sao cờ `isReplay` được đặt bên trong `payload` thay vì ở envelope root?**  
    *Trả lời:* Vì cờ này chỉ có ý nghĩa nghiệp vụ đối với dữ liệu đo lường (`METRIC_BATCH`) và trạng thái dịch vụ (`SERVICE_STATUS`) khi phát lại từ bộ nhớ đệm; các bản tin điều khiển hoặc đăng nhập không bao giờ có ngữ cảnh replay.
14. **Khóa khử trùng lặp (Deduplication Composite Key) của telemetry gồm những trường nào?**  
    *Trả lời:* Bộ ba `DeviceId + BootId + BatchId`. `BootId` (UUID) sinh mới mỗi lần máy khởi động để tránh trùng số thứ tự `BatchId` khi bộ đếm bắt đầu lại từ 1.
15. **Phương thức thu thập chỉ số CPU trên Windows bằng `Pdh` hoặc `PerformanceCounter` cần chú ý điều gì?**  
    *Trả lời:* Việc đọc lần đầu tiên cần khởi tạo phiên và mẫu thứ hai sau một khoảng thời gian mới tính được phần trăm CPU; tác vụ này cần chạy trên luồng nền riêng để không làm trễ chu kỳ gửi nhịp tim mạng.
16. **Cơ chế thăm dò dịch vụ cục bộ (Local TCP Service Probe) có thể gây blocking không?**  
    *Trả lời:* Có thể gây block nếu dùng phương thức đồng bộ hoặc timeout mặc định của hệ điều hành (20s). Do đó bắt buộc dùng `ConnectAsync` với `CancellationTokenSource` hủy sau đúng 2 giây.
17. **Sự khác biệt cốt lõi giữa Agent thật và Agent Simulator là gì?**  
    *Trả lời:* Simulator thật 100% về socket TCP, mã hóa TLS, phân khung Length-Prefix và định dạng bản tin gửi đi; điểm giả lập duy nhất là chỉ số CPU/RAM được sinh ngẫu nhiên theo kịch bản thay vì đọc từ phần cứng thật.
18. **Làm thế nào để một tiến trình Simulator tạo được 50 kết nối socket đồng thời mà không nghẽn tài nguyên?**  
    *Trả lời:* Sử dụng các phương thức bất đồng bộ `async/await` trên ThreadPool của .NET, các socket dùng chung luồng I/O thay vì tạo 50 Thread riêng của hệ điều hành.
19. **Bộ cân bằng tải Gateway L4 khác gì so với Gateway L7 (Reverse Proxy)?**  
    *Trả lời:* Gateway L4 chỉ chuyển tiếp luồng byte nhị phân thô (TCP byte relay) dựa trên địa chỉ IP và port mà không giải mã TLS hay đọc nội dung JSON bên trong; Gateway L7 phải giải mã giao thức ứng dụng, tốn nhiều CPU hơn.
20. **Thuật toán Least-Connections trong Gateway L4 được cài đặt như thế nào?**  
    *Trả lời:* Gateway duy trì một biến đếm số kết nối TCP đang mở (Active Connections) tới từng Collector; khi có client mới kết nối, Gateway chọn Collector có biến đếm nhỏ nhất và đang có trạng thái Health Check là `Up`.
21. **Tại sao hệ thống không yêu cầu di chuyển socket đang mở (Socket Migration) khi Collector gặp sự cố?**  
    *Trả lời:* Việc di chuyển socket TCP đang mở giữa các server yêu cầu hạ tầng mạng chuyên dụng rất phức tạp và vượt quá phạm vi đồ án NT106; đồ án áp dụng mô hình chuẩn công nghiệp: cho phép Agent ngắt kết nối và tự động kết nối lại sang node khác.
22. **Mục tiêu thiết kế thời gian failover p95 $\le 45$ giây được kiểm chứng như thế nào?**  
    *Trả lời:* Đo khoảng thời gian từ lúc tắt tiến trình Collector 01 cho đến khi Agent nhận ra lỗi kết nối, áp dụng backoff, kết nối lại thành công qua Gateway tới Collector 02 và gửi mẻ tin telemetry tiếp theo.
23. **Tại sao log hệ thống phải được cài đặt cơ chế xoay vòng (Log Rotation)?**  
    *Trả lời:* Tránh việc file log phình to vô hạn làm cạn kiệt dung lượng ổ đĩa của máy trạm; hệ thống quy định mỗi file tối đa 10 MB và chỉ lưu giữ tối đa 5 file gần nhất.
24. **Khi kết nối qua mạng Internet bằng Port Forwarding hoặc Tunnel, Agent cần cấu hình gì?**  
    *Trả lời:* Cần cấu hình tên miền hoặc địa chỉ IP công khai kèm cổng NAT; đồng thời thiết lập tên máy chủ (Server Name) trong `SslStream.AuthenticateAsClient` khớp với Common Name (CN) hoặc SAN của chứng thư số TLS.
25. **Làm thế nào để phân biệt giữa lỗi mạng tạm thời (Transient Network Error) và máy trạm bị tắt nguồn thật sự?**  
    *Trả lời:* Lỗi mạng tạm thời sẽ khiến heartbeat bị trễ nhưng khi có mạng lại Agent sẽ gửi kèm mẻ tin replay; nếu quá 20s không có bất kỳ tín hiệu nào và probe TCP thất bại, Central mới xác định máy trạm Offline.
26. **Vai trò của việc tạo mã băm SHA-256 cho gói bàn giao (Release Package) là gì?**  
    *Trả lời:* Bảo đảm tính toàn vẹn của mã nguồn và tệp thực thi, chứng minh tệp giao nộp cho giảng viên không bị chỉnh sửa, thiếu sót hoặc nhiễm mã độc sau khi hoàn tất kiểm thử.
27. **Tại sao không nên sử dụng giao thức UDP cho việc truyền dữ liệu giám sát phòng máy?**  
    *Trả lời:* UDP không có cơ chế bảo đảm thứ tự và không tin cậy; dữ liệu sự cố và lệnh quản trị cần tính chính xác tuyệt đối, việc tự cài đặt cơ chế ACK và retransmit trên UDP sẽ làm phức tạp hóa đồ án không cần thiết.
28. **Kỹ thuật Bounded Channel giải quyết vấn đề gì trong Writer Loop của Agent?**  
    *Trả lời:* Khi mạng bị chập chờn, các mẻ tin thu thập được đẩy vào Channel; nếu Channel đạt giới hạn 100 mẻ tin, tác vụ đẩy tin sẽ tạm dừng (Backpressure) để kích hoạt cơ chế gộp mẻ hoặc lưu bộ nhớ đệm phụ thay vì làm tràn RAM.
29. **Nếu một máy trạm Agent bị đổi địa chỉ IP động qua DHCP, hệ thống nhận diện máy trạm ra sao?**  
    *Trả lời:* Hệ thống định danh thiết bị duy nhất dựa trên trường `deviceId` trong Envelope và Session Token sau khi xác thực, hoàn toàn độc lập với địa chỉ IP tầng mạng.
30. **Khi demo trước hội đồng, nếu mạng Internet tại trường bị chập chờn, phương án dự phòng là gì?**  
    *Trả lời:* Đồ án chuẩn bị sẵn mạng LAN nội bộ phòng máy (hoặc phát qua Router Wi-Fi riêng / Switch cơ học) kết nối 3 laptop; kịch bản demo trên mạng LAN độc lập 100% với đường truyền Internet bên ngoài.
