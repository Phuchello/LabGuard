# SỔ TAY KỸ THUẬT & VẤN ĐÁP — SINH VIÊN B (HANDBOOK B)
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Vai trò:** Lead Backend Architect, Database & Concurrency Specialist  
**Mảng sở hữu (Ownership):** `Central Server Core` + `CSDL SQL Server 2022 Express` + `Incident Rule Engine & Liveness Checker` + `Auth Server & RBAC (PBKDF2 + Salt)` + `Snapshot & Realtime Fanout` + `P1 Central Internal Dispatcher`  

---

## I. TỔNG QUAN VAI TRÒ & RANH GIỚI TRÁCH NHIỆM

### 1. Tôi sở hữu chương trình / module nào?
- **Máy chủ trung tâm (`LabGuard.Central`):**
  * Chương trình daemon lắng nghe socket TCP bất đồng bộ trên luồng ThreadPool của .NET (`SocketAsyncEventArgs` / `ReadAsync`), tiếp nhận đồng thời hàng chục kết nối từ Agent và Admin.
  * Thiết lập kênh truyền bảo mật TLS 1.3 qua `SslStream`, quản lý chứng thư số x509 phía máy chủ.
  * Hàng đợi nhận mẻ tin Ingest Ring Buffer sử dụng `Channel<byte[]>` có chặn trên (Bounded Channel) để điều tiết áp lực ghi đĩa (Backpressure).
- **Hệ thống Quản lý Cơ sở dữ liệu (SQL Server 2022 Express):**
  * Thiết kế và thực thi toàn bộ script tạo bảng: `Users`, `Sessions`, `Devices`, `Services`, `Metrics`, `Incidents`, `IncidentHistory`, `ProcessedBatches`, `ProcessedCommands`, `AuditLogs`.
  * Kiểm soát tương tranh lạc quan (Optimistic Concurrency Control - OCC) trên bảng sự cố bằng cột `rowversion` (datatype riêng của SQL Server, kích thước 8 byte, tự sinh khi row được insert/update).
- **Bộ máy quy tắc sự cố & Liveness (Rule Engine & Liveness Checker):**
  * Đánh giá nhịp tim máy trạm bằng đồng hồ đơn điệu `Stopwatch.GetTimestamp()` tại server, kích hoạt `DEVICE_OFFLINE` khi quá deadline 20 giây.
  * Đánh giá ngưỡng `HIGH_CPU`: Mở sự cố khi CPU $\ge 90\%$ trong 3 mẫu liên tiếp (chu kỳ 5s, trải dài khoảng thời gian 10s: $t=0\text{s}, 5\text{s}, 10\text{s}$); tự động phục hồi khi CPU $\le 75\%$ trong 3 mẫu liên tiếp (Deadband 15% loại trừ rung giật trạng thái).
  * Đánh giá `SERVICE_DOWN`: Kích hoạt khi probe TCP thất bại 2 lần liên tiếp; phục hồi khi probe TCP thành công 2 lần liên tiếp.
  * Ràng buộc duy nhất một phiếu sự cố mở (Unique Active Incident) trên mỗi cặp `(DeviceId, RuleType)`.
- **Máy chủ Xác thực & Phân quyền (Auth Server & RBAC):**
  * Xác thực người dùng bằng thuật toán băm mật khẩu một chiều PBKDF2 kết hợp Salt ngẫu nhiên 16 byte (chống Rainbow Table và Precomputed Hashes).
  * Cấp phát và quản lý Session Token, kiểm tra quyền hạn (Role-Based Access Control) ở mức máy chủ (Admin, Operator, Viewer); từ chối mutation khi quyền không đủ.
- **Bộ phát Snapshot & Sự kiện thời gian thực (Snapshot & Realtime Fanout):**
  * Xử lý bản tin `SUBSCRIBE_DEVICES` và `SUBSCRIBE_INCIDENTS` từ Admin: Phát sinh chuỗi bản tin snapshot phân trang (`SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END`) để tránh nghẽn socket.
  * Đẩy các sự kiện delta thời gian thực (`DEVICE_UPDATE`, `INCIDENT_OPEN`, `INCIDENT_UPDATE`, `HEALTH_UPDATE`) tới toàn bộ các kết nối Admin đang đăng ký.
- **Bộ xử lý lệnh nghiệp vụ & Khử trùng lặp bền vững (Command Processor & Dedup):**
  * Lưu trữ bền vững việc khử trùng lặp mẻ telemetry vào bảng `ProcessedBatches` (TTL 24 giờ).
  * Lưu trữ kết quả xử lý lệnh Admin (`INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`) vào bảng `ProcessedCommands` kèm mã băm tham số `RequestDigest` SHA-256 (TTL 7 ngày).
- **Mở rộng P1 (Internal Dispatcher & Routing):**
  * Mở cổng socket nội bộ tiếp nhận dữ liệu gom mẻ từ các node Collector chuyển lên; quản lý thế hệ phiên (Session Generation) và fencing để loại trừ update từ phiên cũ.

### 2. Tôi bắt đầu được task nào ngay sau khi contract chốt?
- Bắt đầu ngay **LG-B-001** (Viết script tạo bảng CSDL SQL Server 2022 Express và nạp dữ liệu mẫu ban đầu Seed Data).
- Bắt đầu ngay **LG-B-002** (Cài đặt các lớp Repository và bao bọc khối giao dịch Transaction Boundary).
- Bắt đầu ngay **LG-B-003** (Xây dựng khung Central Server chấp nhận TLS và hàng đợi Ingress Bounded Channel).
- Bắt đầu ngay **LG-B-004** (Cài đặt Session Registry, dịch vụ xác thực mật khẩu PBKDF2 + salt và kiểm tra quyền RBAC).

### 3. Tôi tự kiểm tra độc lập bằng công cụ gì?
- Sử dụng **Simulated Ingest Generator & Headless Client Runner** (chương trình test console chuyên dụng):
  * Bơm hàng nghìn bản tin JSON telemetry từ file fixture để đo tốc độ ghi CSDL theo mẻ (Batch Insert).
  * Mô phỏng ngắt gửi nhịp tim của máy trạm để kiểm tra Liveness Checker có kích hoạt sự cố `DEVICE_OFFLINE` đúng sau 20s hay không.
  * Khởi chạy 2 tác vụ bất đồng bộ cùng gửi lệnh `INCIDENT_ACK` trên 1 sự cố để kiểm chứng cơ chế bắt xung đột `rowversion` và trả mã lỗi `409 Conflict`.
  * **Tuyệt đối không cần chờ giao diện WinForms thật của Người C hay Agent thật của Người A để kiểm thử backend.**

### 4. Tôi cần người khác cung cấp những gì và vào lúc nào?
- **Từ Người A (Agent):**
  * Đầu Tuần 1: Cung cấp thư viện `Shared.Protocol.dll`.
  * Cuối Tuần 2: Chạy bộ giả lập `LabGuard.Simulator.exe` tạo 20 kết nối để kiểm tra tải máy chủ.
- **Từ Người C (Admin):**
  * Giữa Tuần 3: Phối hợp kiểm thử kịch bản 2 ứng dụng WinForms thao tác xử lý sự cố cùng thời điểm.

### 5. Tôi bàn giao những gì cho các mốc tích hợp?
- **Mốc M1 (Cuối W1):** Central Server tiếp nhận và giải mã thành công bản tin `METRIC_BATCH`, lưu vào SQL Server (Thin Vertical Slice).
- **Mốc M2 (Cuối W2):** Rule Engine phát hiện chính xác sự cố khi có 20 simulated agents gửi dữ liệu; phát Snapshot và delta events mượt mà.
- **Mốc M3/M4 (Cuối W3):** Xử lý lệnh Admin an toàn với Optimistic Concurrency Control; hoàn tất kiểm thử 33 scenario của Master Plan.
- **Mốc M5 (W4 nếu P1):** Internal socket listener chấp nhận kết nối từ các Collector node.
- **Mốc M6 (W5):** Script sao lưu/phục hồi CSDL (`Backup.bak`), báo cáo chứng minh tính nhất quán sau sự cố crash và tài liệu kiến trúc.

---

## II. BỘ 30 CÂU HỎI VẤN ĐÁP CÁ NHÂN — SINH VIÊN B

1. **Kiến trúc luồng (Threading Model) của Central Server được thiết kế như thế nào?**  
   *Trả lời:* Central Server sử dụng mô hình Async I/O dựa trên ThreadPool của .NET (`SocketAsyncEventArgs` / `ReadAsync`), không tạo 1 Thread riêng của hệ điều hành cho mỗi kết nối socket. Khi socket nhàn rỗi, luồng được hoàn trả cho ThreadPool để phục vụ tác vụ khác.
2. **Tại sao không nên tạo một Thread riêng (`new Thread()`) cho mỗi client kết nối?**  
   *Trả lời:* Mỗi OS Thread trong Windows tiêu tốn mặc định khoảng 1 MB bộ nhớ stack và gây áp lực lớn cho CPU khi chuyển ngữ cảnh luồng (Context Switching). Với hàng trăm client, mô hình 1-thread-per-client sẽ làm cạn kiệt tài nguyên hệ thống.
3. **Kỹ thuật Ingest Ring Buffer sử dụng `Channel<T>` có giới hạn nhằm giải quyết vấn đề gì?**  
   *Trả lời:* Đóng vai trò bộ đệm điều tiết tốc độ (Backpressure Buffer) giữa luồng nhận mạng cực nhanh và luồng ghi CSDL có độ trễ I/O đĩa. Khi bộ đệm đầy (ví dụ 500 mẻ tin), luồng đọc socket tạm hoãn để bảo vệ server không bị cạn kiệt RAM.
4. **Tại sao Ingest Ring Buffer không được coi là nơi chống trùng lặp bền vững?**  
   *Trả lời:* Vì Ring Buffer nằm trên bộ nhớ RAM; nếu Central Server bị mất điện hoặc khởi động lại đột ngột, toàn bộ dữ liệu trong RAM sẽ bị mất. Nơi chống trùng bền vững bắt buộc phải nằm trên đĩa CSDL (bảng `ProcessedBatches`).
5. **Cấu trúc bảng `ProcessedBatches` và chính sách dọn dẹp (TTL Policy) là gì?**  
   *Trả lời:* Bảng có khóa chính `(DeviceId, BootId, BatchId)` và cột `ReceivedUtc`. Dữ liệu được lưu vết trong 24 giờ để lọc trùng lặp khi Agent gửi lại; một job nền định kỳ xóa các bản ghi cũ hơn 24 giờ.
6. **Khi phát hiện một mẻ tin đã tồn tại trong `ProcessedBatches`, server xử lý như thế nào?**  
   *Trả lời:* Server bỏ qua việc phân tích và ghi telemetry mới vào CSDL, nhưng **bắt buộc phải gửi lại `BATCH_ACK`** về cho Agent để Agent biết mẻ tin đã tới đích an toàn và xóa khỏi hàng đợi replay.
7. **Cấu trúc bảng `ProcessedCommands` bảo đảm tính lũy thừa (Idempotency) của lệnh Admin ra sao?**  
   *Trả lời:* Bảng lưu khóa `(UserId, CommandId)` kèm mã băm `RequestDigest` (SHA-256) và kết quả đã trả `ResponsePayload`. Nếu Admin bấm lặp lại lệnh cùng `CommandId`, server trả ngay kết quả cũ đã commit mà không thực thi lại mutation.
8. **Tại sao không dùng bảng `AuditLogs` hoặc `IncidentHistory` làm bảng chống trùng lệnh?**  
   *Trả lời:* `AuditLogs` và `IncidentHistory` phục vụ nhật ký lịch sử cho người quản trị theo dõi (mỗi lần bấm là một dòng log mới); bảng chống trùng lệnh (`ProcessedCommands`) có cấu trúc khóa duy nhất trên `CommandId` và TTL 7 ngày dành riêng cho logic máy.
9. **Mã băm `RequestDigest` trong bảng `ProcessedCommands` ngăn chặn rủi ro gì?**  
   *Trả lời:* Ngăn chặn rủi ro tấn công hoặc nhầm lẫn tham số: nếu client gửi cùng một `CommandId` nhưng thay đổi nội dung lệnh (ví dụ đổi action từ ACK sang RESOLVE), server phát hiện mã băm khác nhau và ném lỗi `ERR_INVALID_COMMAND_DIGEST`.
10. **Liveness Checker của Central Server phát hiện máy trạm Offline dựa trên nguyên lý nào?**  
    *Trả lời:* Duy trì bảng đăng ký nhịp tim trong bộ nhớ kèm mốc thời gian nhận gần nhất đo bằng `Stopwatch.GetTimestamp()`. Định kỳ mỗi giây, một tác vụ nền quét kiểm tra: nếu thời gian trôi qua vượt quá 20 giây, server tự động kích hoạt sự cố `DEVICE_OFFLINE`.
11. **Quy tắc phát hiện lỗi CPU (`HIGH_CPU`) với Hysteresis và Streak được cài đặt như thế nào?**  
    *Trả lời:* Yêu cầu 3 mẫu liên tiếp có CPU $\ge 90\%$ (ở chu kỳ 5s, khoảng thời gian giữa mẫu 1 và 3 là đúng 10 giây). Khi đã mở sự cố, chỉ đóng sự cố khi nhận được 3 mẫu liên tiếp có CPU $\le 75\%$. Dải 15% Deadband ngăn hiện tượng flapping.
12. **Quy tắc phát hiện dịch vụ chết (`SERVICE_DOWN`) quy định số lần thất bại và phục hồi ra sao?**  
    *Trả lời:* Cảnh báo kích hoạt khi probe TCP thất bại 2 lần liên tiếp (ở chu kỳ 5s); sự cố tự động chuyển trạng thái phục hồi kỹ thuật khi probe TCP thành công trở lại 2 lần liên tiếp.
13. **Tại sao kiến trúc hệ thống phải phân tách thành `ConditionState` và `WorkflowState`?**  
    *Trả lời:* `ConditionState` (`ACTIVE`, `RECOVERED`, `UNKNOWN`) phản ánh hiện trạng kỹ thuật khách quan do Rule Engine đo lường từ Agent; `WorkflowState` (`OPEN`, `ACKNOWLEDGED`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED`) quản lý tiến trình xử lý nghiệp vụ của con người.
14. **Tình huống ``Tái phát sự cố'' (Relapse) xảy ra khi nào và state machine chuyển đổi ra sao?**  
    *Trả lời:* Khi sự cố đang ở `WorkflowState = RECOVERED` (lỗi kỹ thuật đã hết nhưng Admin chưa kịp đóng phiếu `RESOLVED`), nếu Agent lại báo lỗi trở lại (`ConditionState = ACTIVE`), sự cố sẽ tự động chuyển ngược về `WorkflowState = IN_PROGRESS` để tiếp tục theo dõi.
15. **Ràng buộc duy nhất một Incident hoạt động (Unique Active Incident Constraint) được đảm bảo ra sao?**  
    *Trả lời:* Bằng Unique Filtered Index trên SQL Server: `CREATE UNIQUE INDEX UX_ActiveIncident ON Incidents(DeviceId, RuleType) WHERE WorkflowState <> 'RESOLVED'`, bảo đảm một máy không bao giờ bị mở nhiều phiếu cho cùng một lỗi.
16. **Cột `rowversion` trong SQL Server là gì và hoạt động ra sao?**  
    *Trả lời:* `rowversion` là datatype riêng của SQL Server. Giá trị lưu trữ có kích thước 8 byte và do SQL Server tự sinh khi rowversion row được insert/update. Kiểu `binary(8)` thông thường KHÔNG có semantics tự cập nhật này. `rowversion` KHÔNG phải date/time. Nó đóng vai trò như một opaque version token phục vụ kiểm tra Optimistic Concurrency Control (OCC).
17. **Optimistic Concurrency Control (OCC) giải quyết bài toán hai Admin cùng bấm ACK một sự cố như thế nào?**  
    *Trả lời:* Admin gửi lệnh kèm `expectedRowVersion` (coi như một opaque 8-byte token đặc thù của SQL Server). Lệnh cập nhật SQL kiểm tra: `UPDATE Incidents SET WorkflowState='ACKNOWLEDGED' WHERE IncidentId=@Id AND RowVersion=@expected`. Người đầu tiên cập nhật thành công và đổi `rowversion`; người thứ hai có điều kiện WHERE không khớp, số dòng bị ảnh hưởng là 0 $\rightarrow$ Server ném lỗi `409 Conflict`.
18. **Tại sao nên dùng Optimistic Concurrency thay vì Pessimistic Locking (`SELECT FOR UPDATE`)?**  
    *Trả lời:* Pessimistic Locking giữ khóa bảng hoặc khóa dòng CSDL trong suốt thời gian người dùng mở form, làm nghẽn toàn bộ các kết nối khác. OCC không giữ khóa, cho phép đọc tự do và chỉ kiểm tra xung đột tại đúng thời điểm commit.
19. **Giao dịch Incident được bao bọc trong Transaction Boundary như thế nào?**  
    *Trả lời:* Khi chuyển trạng thái sự cố, server thực thi đồng thời trong một giao dịch ACID: Cập nhật dòng bảng `Incidents`, thêm một dòng ghi nhật ký vào `IncidentHistory`, và cập nhật bảng chống trùng `ProcessedCommands`. Nếu có bất kỳ lỗi nào, toàn bộ giao dịch bị rollback.
20. **Tại sao mật khẩu người dùng bắt buộc phải được băm kèm Salt bằng thuật toán PBKDF2?**  
    *Trả lời:* Salt (chuỗi ngẫu nhiên 16 byte / 128-bit) bảo đảm hai người dùng có cùng mật khẩu sẽ sinh ra hai chuỗi băm hoàn toàn khác nhau, vô hiệu hóa bảng tra cứu tính sẵn (Rainbow Table); kết hợp so sánh bằng `CryptographicOperations.FixedTimeEquals` để chống timing attack. Về mặt chuẩn mực: OWASP recommends Argon2id for new systems generally. If FIPS-140 compliance is required, OWASP recommends PBKDF2-HMAC-SHA256 with a work factor of 600,000 or more. LabGuard deliberately chooses PBKDF2 for BCL availability and course-scope simplicity; 600,000 is therefore the project baseline referenced from OWASP and must still be measured on the actual lab hardware.
21. **Tại sao việc kiểm tra quyền hạn (Authorization / RBAC) bắt buộc phải thực hiện ở phía server?**  
    *Trả lời:* Ẩn nút trên giao diện WinForms chỉ là hỗ trợ trải nghiệm người dùng; kẻ xấu có thể can thiệp socket hoặc dùng tool gửi bản tin wire trực tiếp lên server. Server bắt buộc phải kiểm tra role trong Session Token trước khi thực thi lệnh.
22. **Tại sao bản tin Snapshot gửi cho Admin phải được phân trang (`SNAPSHOT_PAGE`)?**  
    *Trả lời:* Nếu phòng máy có hàng trăm thiết bị và hàng nghìn lịch sử sự cố, gửi toàn bộ trong một bản tin JSON khổng lồ sẽ vượt trần `MaxFrameSize = 256 KiB` và làm nghẽn bộ đệm socket; phân trang giúp chia nhỏ dữ liệu và giải phóng luồng I/O.
23. **Cơ chế Realtime Event Fanout được cài đặt như thế nào trong Central Server?**  
    *Trả lời:* Central Server duy trì danh sách các kết nối Admin đang mở (Subscriber Registry); khi có biến động từ Rule Engine, máy chủ đóng gói bản tin delta event và gửi tuần tự qua Channel Single Writer của từng Admin connection.
24. **Kỹ thuật Slow-Client Isolation bảo vệ hệ thống như thế nào?**  
    *Trả lời:* Nếu một Admin Client có kết nối mạng quá chậm hoặc bị treo máy, hàng đợi gửi của Admin đó sẽ bị đầy; hệ thống kích hoạt chính sách drop event hoặc ngắt kết nối client đó sau một khoảng timeout, tuyệt đối không làm block các Admin Client khác.
25. **Chính sách lưu trữ dữ liệu (Data Retention) được cài đặt và vận hành ra sao?**  
    *Trả lời:* Telemetry thô (`Metrics`) được lưu trong 48 giờ; một Background Worker định kỳ chạy vào ban đêm thực hiện câu lệnh xóa theo mẻ (Batch Delete) dựa trên cột `ReceivedUtc` để không làm khóa bảng lâu dài.
26. **Kịch bản phục hồi thảm họa CSDL (Crash Recovery Proof) được kiểm chứng thế nào?**  
    *Trả lời:* Tiến hành sao lưu CSDL bằng lệnh `BACKUP DATABASE`, cố tình tắt đột ngột tiến trình SQL Server khi đang có tải ghi dồn dập, sau đó khởi động lại và chạy lệnh `RESTORE VERIFYONLY` để chứng minh CSDL tự phục hồi và dữ liệu không bị hỏng (corrupted).
27. **Trong kiến trúc P1, cơ chế Fencing và Session Generation giải quyết vấn đề gì?**  
    *Trả lời:* Khi Agent chuyển từ Collector 01 sang Collector 02, Central Server tăng chỉ số `SessionGeneration`; nếu các gói tin chậm trễ từ Collector 01 cũ vẫn còn bay trên mạng tới sau, Central sẽ loại bỏ các gói tin này vì có chỉ số generation cũ hơn, ngăn chặn ghi đè dữ liệu cũ (split-brain).
28. **Tại sao Central Server không lưu trạng thái phiên làm việc (Session) trong CSDL mà lưu trong RAM Registry?**  
    *Trả lời:* Kiểm tra token xác thực diễn ra ở mọi request với tần suất cực lớn; lưu Session Registry trong RAM (sử dụng `ConcurrentDictionary`) giúp truy xuất với độ trễ nano-giây, giảm tải triệt để cho CSDL SQL Server.
29. **Tại sao kiểu dữ liệu `rowversion` không thể chuyển đổi tương đương sang SQLite?**  
    *Trả lời:* SQLite không có kiểu dữ liệu nhị phân 8 byte tự tăng nguyên bản ở mức engine như SQL Server; việc giả lập bằng trigger hoặc timestamp trong SQLite dễ bị sai lệch concurrency, do đó việc khóa chặt SQL Server 2022 Express là quyết định kiến trúc đúng đắn.
30. **Khi Central Server bị khởi động lại, các Agent kết nối lại có làm sập server không?**  
    *Trả lời:* Nhờ thuật toán Exponential Backoff và Jitter phía Agent, các kết nối không ùa vào cùng một lúc; tại Central Server, hàng đợi Ingress Bounded Channel chỉ nhận số lượng kết nối tối đa cho phép, loại bỏ rủi ro nghẽn tài nguyên.
