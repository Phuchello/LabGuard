# SỔ TAY KỸ THUẬT & VẤN ĐÁP — SINH VIÊN C (HANDBOOK C)
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Vai trò:** Lead Frontend Engineer, Admin Client Networking & Concurrency Test Specialist  
**Mảng sở hữu (Ownership):** `WinForms Admin Application` + `Admin TCP/TLS RPC Client` + `Realtime Event Reception & UI Marshalling` + `Kiểm thử Đa Admin & Kịch bản Demo` + `Ứng dụng Collector dùng chung (P1)`  

---

## I. TỔNG QUAN VAI TRÒ & RANH GIỚI TRÁCH NHIỆM

### 1. Tôi sở hữu chương trình / module nào?
- **Ứng dụng Quản trị WinForms (`LabGuard.Admin`):**
  * Thiết kế và hoàn thiện toàn bộ giao diện người dùng native: Form Đăng nhập (`LoginForm`), Form Đăng ký qua mã mời, Màn hình Tổng quan (`DashboardForm`), Sơ đồ phòng máy trực quan (`LabViewControl`), Chi tiết thiết bị (`DeviceDetailForm`), Quản lý phiếu sự cố (`IncidentManagerForm`), Báo cáo & Tra cứu lịch sử (`ReportViewerForm`), Sức khỏe máy chủ (`ServerHealthForm`).
  * Tuân thủ quy chuẩn thiết kế sản phẩm: Nền sáng chuyên nghiệp, control native chuẩn WinForms, trạng thái máy trạm biểu diễn bằng cả chữ, icon và màu sắc thay vì chỉ dựa vào màu nền.
  * Phía Client: Lưu trữ Token phiên làm việc an toàn trong bộ nhớ RAM, ẩn/hiện các nút chức năng trên giao diện tương ứng theo vai trò người dùng (Admin, Operator, Viewer). **Lưu ý: Logic băm mật khẩu PBKDF2 và phân quyền server-side thuộc về Sinh viên B.**
- **Mạng kết nối Admin Client & RPC (`AdminRpcClient`):**
  * Khởi tạo và duy trì kết nối TCP duplex bảo mật qua TLS tới Central Server.
  * Quản lý việc gửi các lệnh yêu cầu và nhận kết quả bất đồng bộ thông qua `RpcCorrelationManager`: Ánh xạ tương quan giữa Request và Response bằng `TaskCompletionSource<T>` dựa trên mã `requestId` (UUID v4), quản lý thời gian chờ timeout 5s.
- **Bộ nhớ đệm cục bộ & Đồng bộ thời gian thực (Local Cache & Realtime Sync):**
  * Xử lý luồng nạp trạng thái ban đầu: Gửi `SUBSCRIBE_DEVICES` / `SUBSCRIBE_INCIDENTS`, tiếp nhận chuỗi bản tin snapshot phân trang (`SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END`) để khởi tạo `LocalCache`.
  * Tiếp nhận các sự kiện delta thời gian thực (`DEVICE_UPDATE`, `INCIDENT_OPEN`, `INCIDENT_UPDATE`, `HEALTH_UPDATE`) để cập nhật bộ nhớ đệm và hiển thị mượt mà trên UI.
  * Điều phối luồng an toàn (UI Marshalling): Toàn bộ dữ liệu nhận từ luồng mạng ngầm (Background Thread) bắt buộc phải chuyển giao vào UI Thread thông qua `Control.Invoke` / `Control.BeginInvoke` (hoặc `InvokeAsync` trên .NET 9+ WinForms).
- **Kiểm thử Đa Admin & Kịch bản Demo:**
  * Chủ trì kịch bản kiểm thử 2 ứng dụng Admin cùng mở một sự cố và cùng bấm xử lý trong vòng 1 giây; ghi lại video minh chứng việc xử lý thông báo `409 Conflict` thân thiện và tự động tải lại dữ liệu mới nhất.
  * Soạn thảo kịch bản demo 15 phút, quay video dự phòng (Demo Backup Video) và biên soạn tài liệu hướng dẫn cài đặt `README.md`.
- **Mở rộng P1 (Ứng dụng Collector dùng chung):**
  * Phát triển mã nguồn **một ứng dụng Collector duy nhất (`LabGuard.Collector.exe`)**: Tiếp nhận kết nối từ Agent (Agent-facing host), phân khung Length-Prefix, ghép kênh chuyển tiếp dữ liệu lên Central Server (Upstream Multiplexer) và báo cáo trạng thái sẵn sàng (Readiness / Health Check).
  * Ứng dụng này được Sinh viên A triển khai thành 2 instance độc lập (Collector 01 và Collector 02) để phục vụ cân bằng tải.

### 2. Tôi bắt đầu được task nào ngay sau khi contract chốt?
- Bắt đầu ngay **LG-C-001** (Xây dựng khung giao diện WinForms UI Shell, thanh điều hướng và Form mẫu).
- Bắt đầu ngay **LG-C-002** (Lập trình `AdminRpcClient` kết nối TCP duplex qua SslStream).
- Bắt đầu ngay **LG-C-003** (Lập trình màn hình Đăng nhập/Đăng ký và lưu trữ Session Token an toàn tại client).
- Bắt đầu ngay **LG-C-004** (Lập trình bộ ánh xạ tương quan Request/Response qua `TaskCompletionSource`).

### 3. Tôi tự kiểm tra độc lập bằng công cụ gì?
- Sử dụng **Mock Central Server Harness** (chương trình socket console giả lập server):
  * Phát chuỗi bản tin `SNAPSHOT_BEGIN`, `SNAPSHOT_PAGE`, `SNAPSHOT_END` mẫu với 50 thiết bị để kiểm tra tốc độ nạp dữ liệu vào lưới phòng máy Lab View.
  * Phát định kỳ các sự kiện `DEVICE_UPDATE` và `INCIDENT_OPEN` để kiểm tra các ô màu trên sơ đồ có nhấp nháy cập nhật mượt mà qua `Control.Invoke` hay không.
  * Giả lập phản hồi mã lỗi `ERR_CONCURRENCY_VIOLATION` (409 Conflict) khi bấm nút Resolve để kiểm tra UI có hiện hộp thoại cảnh báo và tự cập nhật lại state hay không.
  * Giả lập ngắt socket đột ngột để kiểm tra cơ chế Reconnect Backoff và thanh trạng thái hiển thị "Đang kết nối lại...".
  * **Tuyệt đối không cần chờ Central Server thật của Người B để hoàn thiện và kiểm thử giao diện.**

### 4. Tôi cần người khác cung cấp những gì và vào lúc nào?
- **Từ Người A (Agent Developer):**
  * Đầu Tuần 1: Cung cấp thư viện `Shared.Protocol.dll`.
  * Cuối Tuần 3: Chạy Agent thật trên mạng LAN để hiển thị chỉ số thật lên Admin Dashboard.
- **Từ Người B (Central Server Developer):**
  * Cuối Tuần 1: Cung cấp endpoint Central Server thật để thử nghiệm đăng nhập thành công (Thin Vertical Slice M1).
  * Cuối Tuần 2: Cung cấp cổng phát Snapshot và Realtime Events thật.
  * Giữa Tuần 3: Cung cấp API cập nhật trạng thái sự cố có bắt xung đột `rowversion`.

### 5. Tôi bàn giao những gì cho các mốc tích hợp?
- **Mốc M1 (Cuối W1):** Ứng dụng WinForms đăng nhập thành công và hiển thị 1 dòng dữ liệu từ Central.
- **Mốc M2 (Cuối W2):** Giao diện Lab View và Dashboard cập nhật realtime khi Agent Simulator bơm dữ liệu.
- **Mốc M3/M4 (Cuối W3):** Toàn bộ giao diện Incident List/Detail thao tác được các nút ACK/START/RESOLVE và hiển thị lịch sử đầy đủ; chủ trì biên bản nghiệm thu Gate M4.
- **Mốc M5 (W4 nếu P1):** Ứng dụng `LabGuard.Collector.exe` dùng chung cho 2 instance.
- **Mốc M6 (W5):** Video demo dự phòng (MP4), bộ ảnh chụp màn hình minh chứng và file Hướng dẫn sử dụng `README.md`.

---

## II. BỘ 30 CÂU HỎI VẤN ĐÁP CÁ NHÂN — SINH VIÊN C

1. **Kiến trúc luồng của Windows Forms được thiết kế theo nguyên lý nào?**  
   *Trả lời:* Windows Forms hoạt động theo mô hình Single-Threaded Apartment (STA), trong đó chỉ có đúng một luồng duy nhất (UI Thread) được sở hữu vòng lặp thông điệp Windows (Message Loop) và được phép truy cập, sửa đổi các thành phần giao diện Win32.
2. **Tại sao việc thao tác control WinForms trực tiếp từ luồng mạng (Background Thread) lại gây lỗi?**  
   *Trả lời:* Vì vi phạm tính an toàn đa luồng của hệ thống cửa sổ Windows; truy cập đồng thời từ luồng khác sẽ gây xung đột bộ nhớ đồ họa GDI, làm sai lệch trạng thái hiển thị và ném ngoại lệ `InvalidOperationException: Cross-thread operation not valid`.
3. **Sự khác biệt kỹ thuật giữa `Control.Invoke` và `Control.BeginInvoke` là gì?**  
   *Trả lời:* `Control.Invoke` là cuộc gọi đồng bộ, luồng mạng sẽ bị block chờ cho đến khi UI Thread thực thi xong delegate; `Control.BeginInvoke` là cuộc gọi bất đồng bộ, luồng mạng đẩy delegate vào Message Queue của UI rồi tiếp tục chạy ngay mà không chờ đợi.
4. **Khi nào nên dùng `Control.BeginInvoke` thay vì `Control.Invoke` trong ứng dụng realtime?**  
   *Trả lời:* Khi nhận các sự kiện delta thời gian thực liên tục từ mạng (`DEVICE_UPDATE`, `INCIDENT_UPDATE`), nên dùng `BeginInvoke` để luồng mạng không bị chặn nếu UI Thread đang bận vẽ đồ thị, tránh làm nghẽn bộ đệm socket.
5. **Tại sao thuộc tính `CheckForIllegalCrossThreadCalls = false` bị nghiêm cấm trong sản phẩm chuẩn?**  
   *Trả lời:* Vì câu lệnh này chỉ tắt cờ kiểm tra cảnh báo của .NET Framework chứ hoàn toàn không làm cho mã nguồn an toàn đa luồng; ứng dụng vẫn tiềm ẩn nguy cơ race condition nghiêm trọng, vẽ lỗi form hoặc tự sập ngẫu nhiên.
6. **Phương thức `InvokeAsync` trong .NET 9+ WinForms mang lại lợi ích gì?**  
   *Trả lời:* Cho phép await trực tiếp một thao tác điều phối giao diện từ bất kỳ Task nền nào mà không cần tạo delegate thủ công, giúp mã nguồn bất đồng bộ sạch sẽ và tận dụng triệt để `IProgress<T>` hoặc `SynchronizationContext`.
7. **Cơ chế RPC bất đồng bộ trên một kết nối TCP duy nhất được cài đặt như thế nào?**  
   *Trả lời:* Khi gửi một request, client tạo một `TaskCompletionSource<TResponse>`, lưu vào `ConcurrentDictionary` với key là `requestId` (UUID v4) rồi gửi frame. Luồng đọc socket khi nhận được response sẽ bóc tách `requestId`, tìm `TaskCompletionSource` tương ứng và gọi `SetResult()` để hoàn tất Task.
8. **Làm thế nào để xử lý Timeout cho các yêu cầu RPC khi mạng bị ngắt hoặc server không phản hồi?**  
   *Trả lời:* Sử dụng `CancellationTokenSource` với thời gian chờ (ví dụ 5 giây): `cts.CancelAfter(5000)`. Đăng ký callback khi token bị hủy sẽ gọi `TrySetCanceled()` trên `TaskCompletionSource` và gỡ bản ghi khỏi Dictionary.
9. **Tại sao ứng dụng Admin không gửi truy vấn định kỳ (Polling) mà sử dụng Snapshot + Realtime Delta?**  
   *Trả lời:* Polling liên tục tạo tải CPU và băng thông mạng rất lớn cho Central Server, đồng thời gây trễ thông tin. Snapshot + Delta chỉ nạp dữ liệu đầy đủ 1 lần lúc mở máy, sau đó server chủ động đẩy đúng các thay đổi nhỏ, giảm 90% lưu lượng mạng.
10. **Quá trình nạp dữ liệu ban đầu bằng Snapshot phân trang (`SNAPSHOT_BEGIN/PAGE/END`) diễn ra ra sao?**  
    *Trả lời:* Client nhận `SNAPSHOT_BEGIN` để biết tổng số thiết bị và khởi tạo bộ nhớ tạm; nhận tuần tự các trang `SNAPSHOT_PAGE` để nạp danh sách thiết bị; khi nhận `SNAPSHOT_END`, client khóa bộ nhớ đệm `LocalCache`, cập nhật một lần lên UI và bắt đầu tiêu thụ các delta event.
11. **Nếu một delta event đến trong khi ứng dụng đang nạp Snapshot phân trang, client xử lý thế nào?**  
    *Trả lời:* Client đưa delta event vào một hàng đợi chờ (Pending Event Queue); sau khi xử lý xong `SNAPSHOT_END` và xác định chỉ số `initialSequence`, client mới tuần tự áp dụng các delta event có sequence lớn hơn để không làm sai lệch trạng thái.
12. **Màn hình sơ đồ phòng máy Lab View được thiết kế trực quan như thế nào?**  
    *Trả lời:* Thiết kế dạng lưới các ô máy tính (Grid Layout). Mỗi ô hiển thị tên máy (ví dụ `PC-07`), địa chỉ IP, trạng thái dạng chữ (`ONLINE`, `OFFLINE`, `WARNING`), kèm icon sinh động và viền màu tương ứng; không bao giờ chỉ dựa vào một màu nền đơn điệu.
13. **Tại sao trạng thái trên Lab View phải xuất phát từ sự kiện mạng thật thay vì timer giả lập?**  
    *Trả lời:* Để bảo đảm tính trung thực của một đồ án mạng máy tính. Khi máy tính phòng máy bị rút dây mạng, ô hiển thị chuyển sang `OFFLINE` là do Central phát sự kiện `DEVICE_UPDATE` sau khi hết deadline heartbeat 20s, không phải do hàm timer trên WinForms tự đếm giờ.
14. **Optimistic Concurrency Control (OCC) được thể hiện trên giao diện Admin như thế nào khi gặp lỗi 409 Conflict?**  
    *Trả lời:* Khi Admin bấm nút Resolve nhưng sự cố đã bị người khác thao tác trước đó, server trả về `status = "CONFLICT"`. UI bắt mã lỗi này, hiển thị thông báo: "Sự cố đã được Admin khác cập nhật", đồng thời tự động cập nhật lại thông tin mới nhất trên form mà không làm mất dữ liệu của người dùng.
15. **Tại sao client phải gửi kèm `expectedRowVersion` trong các lệnh thao tác sự cố (`INCIDENT_ACK`, `INCIDENT_RESOLVE`)?**  
    *Trả lời:* Để Central Server so sánh với giá trị `rowversion` hiện tại trong CSDL. Nếu hai giá trị khớp nhau, chứng tỏ dữ liệu Admin đang nhìn thấy là mới nhất; nếu khác nhau, chứng tỏ đã có người khác sửa đổi trước đó.
16. **Phân quyền người dùng (RBAC) được thể hiện ở giao diện WinForms ra sao?**  
    *Trả lời:* Sau khi đăng nhập, căn cứ vào role trong `SessionToken`: Nếu là `Viewer` (chế độ chỉ xem), toàn bộ các nút mutation (ACK, Start, Resolve, Assign, Invite) bị ẩn hoặc disable; nếu là `Operator`, được hiển thị và thao tác các nút xử lý sự cố trực ban (ACK, Start, Resolve); nếu là `Admin`, có toàn quyền Operator kèm theo nút phân công (Assign) và quản trị tài khoản / lời mời (Invite / User Management). Mọi thao tác đều được Central Server kiểm tra quyền độc lập.
17. **Tại sao việc ẩn nút trên WinForms không thay thế được việc kiểm tra bảo mật phía server?**  
    *Trả lời:* Vì kẻ xấu có thể can thiệp bộ nhớ ứng dụng hoặc viết một script TCP độc lập để gửi lệnh trực tiếp vào socket. Giao diện chỉ mang tính chất hỗ trợ trải nghiệm người dùng (UX), quyền lực bảo mật tối thượng bắt buộc thuộc về server.
18. **Cơ chế Reconnect Backoff phía Admin Client hoạt động như thế nào khi mất kết nối tới Central?**  
    *Trả lời:* Client chuyển giao diện sang trạng thái vô hiệu hóa các nút bấm, hiện thanh thông báo màu vàng "Mất kết nối, đang thử lại sau Xs...". Thời gian thử lại tăng dần (1s, 2s, 4s... max 30s) và tự động yêu cầu Snapshot mới khi kết nối lại thành công.
19. **Tại sao sau khi Reconnect thành công, Admin Client bắt buộc phải yêu cầu Snapshot mới thay vì dùng tiếp cache cũ?**  
    *Trả lời:* Trong khoảng thời gian mất mạng, client đã bỏ lỡ nhiều delta events; việc tiếp tục dùng cache cũ sẽ dẫn đến hiển thị sai lệch hoặc gửi sai `expectedRowVersion` gây lỗi xung đột liên tục.
20. **Bộ nhớ đệm cục bộ `LocalCache` trong WinForms được quản lý bằng cấu trúc dữ liệu nào?**  
    *Trả lời:* Sử dụng `ConcurrentDictionary<string, DeviceViewModel>` và `ConcurrentDictionary<string, IncidentViewModel>`, bảo đảm các thao tác đọc từ UI Thread và ghi từ luồng mạng diễn ra an toàn mà không gây deadlock.
21. **Kỹ thuật Virtual Mode trên `DataGridView` hoặc `ListView` giải quyết vấn đề gì khi hiển thị hàng nghìn bản ghi?**  
    *Trả lời:* Không nạp sẵn hàng nghìn object đồ họa vào bộ nhớ; điều khiển chỉ yêu cầu dữ liệu cho đúng số dòng đang hiển thị trên màn hình qua sự kiện `CellValueNeeded`, giúp ứng dụng mở tức thì và tiêu tốn cực ít RAM.
22. **Làm thế nào để xuất dữ liệu báo cáo lịch sử sự cố ra file CSV chuẩn UTF-8?**  
    *Trả lời:* Sử dụng `StreamWriter` với encoding `new UTF8Encoding(true)` (có ký tự BOM - Byte Order Mark) để Microsoft Excel trên Windows có thể mở file CSV và hiển thị Tiếng Việt có dấu chính xác mà không bị lỗi font.
23. **Tại sao Sinh viên C chỉ phát triển một ứng dụng Collector dùng chung cho cả 2 instance ở P1?**  
    *Trả lời:* Để bảo đảm tính nhất quán của giao thức và mã nguồn. Nếu hai thành viên viết hai bản Collector riêng sẽ dễ dẫn đến lệch pha xử lý khung truyền; việc viết một bản chuẩn và chạy thành 2 instance là mô hình chuẩn mực công nghiệp.
24. **Ứng dụng Collector trung gian (`LabGuard.Collector.exe`) thực hiện nhiệm vụ gì?**  
    *Trả lời:* Đóng vai trò proxy gom mẻ: Lắng nghe kết nối TCP từ Agent, phân khung Length-Prefix, xác thực token ban đầu, sau đó ghép kênh (Multiplexing) chuyển tiếp dữ liệu lên Central Server qua một kết nối socket duy nhất.
25. **Cơ chế Upstream Multiplexer trong Collector hoạt động ra sao?**  
    *Trả lời:* Hàng chục Agent gửi dữ liệu vào Collector; Collector gom các mẻ tin này vào một hàng đợi Channel duy nhất và gửi tuần tự lên Central Server, kèm theo cơ chế định tuyến (Reply Router) để trả đúng ACK về cho từng Agent tương ứng.
26. **Collector báo cáo trạng thái sẵn sàng (Readiness / Health Check) tới Gateway L4 như thế nào?**  
    *Trả lời:* Collector mở một cổng riêng hoặc lắng nghe tín hiệu thăm dò từ Gateway; nếu bộ đệm Ingress hoặc kết nối Upstream tới Central gặp sự cố, Collector trả về trạng thái không sẵn sàng để Gateway không điều phối thêm kết nối mới.
27. **Làm thế nào để kiểm thử kịch bản hai Admin cùng xử lý một sự cố (Multi-Admin Concurrency)?**  
    *Trả lời:* Khởi chạy đồng thời 2 tiến trình `LabGuard.Admin.exe` trên 2 màn hình, cùng mở một phiếu sự cố `INC-001`. Cả 2 người cùng bấm nút "Acknowledge" trong cùng 1 giây. Kết quả: 1 bên thành công, 1 bên hiện hộp thoại Conflict và tự làm mới dữ liệu.
28. **Các biện pháp chống tràn giao diện (UI Responsiveness) khi có bão sự kiện gửi về là gì?**  
    *Trả lời:* Sử dụng kỹ thuật gom mẻ cập nhật (Batch UI Updates) hoặc Debounce/Throttle: Thay vì mỗi event gọi một lần `Invoke`, client gom các event trong khoảng 100ms và gọi một lần `BeginInvoke` để cập nhật toàn bộ bảng.
29. **Tại sao cần chuẩn bị video demo dự phòng (Demo Backup Video) trước ngày vấn đáp?**  
    *Trả lời:* Phòng ngừa các rủi ro bất khả kháng tại phòng thi (mất điện, đứt cáp mạng LAN, lỗi xung đột IP phòng máy của trường); video quay sẵn rõ nét từng kịch bản giúp chứng minh sản phẩm đã chạy thực tế hoàn hảo.
30. **Quy trình đóng gói và tài liệu hướng dẫn `README.md` cần cung cấp những thông tin gì?**  
    *Trả lời:* Cung cấp cấu hình phần cứng yêu cầu, chuỗi kết nối CSDL mẫu, tài khoản Admin/Operator mặc định, thứ tự khởi động các ứng dụng (Central Server trước $\rightarrow$ Agent $\rightarrow$ Admin Client) và danh mục các kịch bản demo mẫu.
