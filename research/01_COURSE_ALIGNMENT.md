# LABGUARD — BÁO CÁO ĐỐI SOÁT & ALIGNMENT VỚI CHƯƠNG TRÌNH NT106 (UIT)
**Môn học:** NT106 — Lập trình mạng căn bản  
**Khoa:** Mạng máy tính và Truyền thông — Trường ĐH Công nghệ Thông tin, ĐHQG-HCM  
**Trạng thái:** Đã cập nhật sau vòng Audit của ChatGPT Work — Checkpoint 1  
**Tài liệu tham chiếu:**
1. `[COURSE]` *Giáo trình Thực hành Lập trình Mạng căn bản* — ThS. Đỗ Thị Hương Lan, ThS. Trần Hồng Nghi, NXB ĐHQG-HCM, 2022.
2. `[COURSE]` *Slide bài giảng Lý thuyết NT106 (TL1 đến TL8)* — Bộ môn Mạng máy tính & Truyền thông, UIT.
3. `[COURSE]` *Tài liệu hướng dẫn thực hành Lab 1 đến Lab 5* (C#, WinForms, File/Stream I/O, Socket TCP/UDP, Web, Mail).
4. `[REFERENCE]` *Đề cương & Đáp án Vấn đáp môn Lập trình mạng UIT tham khảo* (`van-dap-mon-lap-trinh-mang-1.pdf` trên Studocu).

---

## 1. MỤC TIÊU CỦA ĐỒ ÁN MÔN HỌC NT106 TẠI UIT

Theo đề cương môn học NT106 của Trường Đại học Công nghệ Thông tin (UIT), sinh viên sau khi hoàn thành môn học phải:
1. Nắm vững kiến thức nền tảng về mô hình Client/Server, kiến trúc phân tầng OSI và TCP/IP.
2. Hiểu rõ sự khác biệt giữa hai giao thức giao vận chủ đạo: TCP (hướng kết nối, tin cậy, dòng byte) và UDP (phi kết nối, không tin cậy, hướng thông điệp).
3. Thành thạo kỹ năng lập trình Socket mạng trên nền tảng .NET / C#, bao gồm cả Socket cấp thấp (`System.Net.Sockets.Socket`) và các lớp bao bọc cấp cao (`TcpListener`, `TcpClient`, `NetworkStream`).
4. Xử lý thành thạo các vấn đề đồng thời (Concurrency): Đa luồng (`Thread`), bất đồng bộ (`async/await`, `Task`) để phục vụ nhiều client cùng lúc mà không làm treo giao diện người dùng (UI freeze).
5. Thiết kế và hiện thực hóa được một **Giao thức tầng ứng dụng (Application-Layer Protocol)** tự định nghĩa để giải quyết bài toán nghiệp vụ mạng thực tế.
6. Hiểu và vận dụng được các nguyên lý bảo vệ an toàn dữ liệu trên đường truyền mạng (Hash, Salt, Mã hóa đối xứng, Mã hóa bất đối xứng, TLS/SSL).
7. Triển khai và kiểm thử được ứng dụng trong môi trường mạng cục bộ (LAN) trên các thiết bị vật lý thật.

---

## 2. CHỨNG MINH LABGUARD LÀ ĐỒ ÁN LẬP TRÌNH MẠNG (KHÔNG PHẢI PHẦN MỀM QUẢN LÝ CRUD)

Một sai lầm rất phổ biến của sinh viên khi làm đồ án NT106 là: Xây dựng một ứng dụng WinForms kết nối trực tiếp vào Database để thêm/xóa/sửa dữ liệu, hoặc chỉ tạo một ứng dụng Chat đơn giản sao chép từ bài thực hành Lab 3.

**LabGuard được thiết kế chuẩn mực như một hệ thống phần mềm mạng (Network Software System) hoàn chỉnh**:
- **Không có client nào kết nối trực tiếp đến Database**: Cả Agent (máy trạm) và Admin Client (máy quản trị) đều hoàn toàn tách biệt với Database. Mọi luồng dữ liệu đều phải đi qua kết nối Socket TCP với giao thức ứng dụng tự định nghĩa.
- **Trọng tâm kỹ thuật là giải quyết các thách thức cố hữu của mạng máy tính**:
  - Mạng là không tin cậy: rớt kết nối đột ngột, chập chờn, nghẽn mạng.
  - TCP là dòng byte liên tục: dính gói, vỡ gói, phân mảnh buffer.
  - Server phải phục vụ đồng thời nhiều kết nối: quản lý phiên, chống race condition, đồng bộ hóa thời gian thực.
  - Mất đồng bộ trạng thái: khi client rớt mạng rồi kết nối lại thì làm sao cập nhật dữ liệu mà không gây sai lệch?

---

## 3. BẢNG MA TRẬN ĐỐI SOÁT KIẾN THỨC NT106 & THÀNH PHẦN LABGUARD

| Thành phần trong LabGuard | Khối kiến thức NT106 | Bản chất kỹ thuật & Tại sao liên quan | Căn cứ tài liệu |
|:---|:---|:---|:---|
| **Agent $\leftrightarrow$ Central Connection** | Lập trình Socket TCP Client/Server | Sử dụng `TcpClient` / `Socket` để thiết lập kênh truyền tin cậy hai chiều; Client thực hiện `Connect()`, Server thực hiện `Bind()`, `Listen()`, `Accept()`. | `[COURSE]` Giáo trình TH Bài 4; Slide TL3; `[REFERENCE]` Viva Q10, Q15, Q16. |
| **Giao thức FrameCodec (4-byte Length + JSON)** | Byte Stream & Phân định biên thông điệp (Framing) | TCP không có ranh giới message. LabGuard hiện thực giao thức tầng ứng dụng dùng Length-Prefix để giải quyết bài toán Partial Read và Dính gói (Packet Coalescing). | `[COURSE]` Giáo trình TH Bài 2 & Bài 4; Slide TL2; `[REFERENCE]` Viva Q3, Q5, Q6. |
| **Heartbeat & Liveness Engine** | Cơ chế giám sát kết nối tầng ứng dụng | `Socket.Connected` chỉ phản ánh thao tác I/O trước đó. LabGuard gửi gói tin Heartbeat định kỳ (5s) kết hợp Timeout deadline (> 20s theo đồng hồ đơn điệu) để xác định máy sống/chết. | `[COURSE]` Slide TL3; `[REFERENCE]` Viva Q2, Q17. |
| **Local Service TCP Probe** | Kỹ thuật quét cổng & Kiểm tra dịch vụ mạng | Agent mở kết nối TCP dạng `ConnectAsync` có timeout (2s) tới cổng dịch vụ cục bộ (ví dụ: 8080, 3306) để phát hiện dịch vụ bị treo hoặc dừng hoạt động. | `[COURSE]` Giáo trình TH Bài 4 (Mục TCP Client Scanner); Slide TL3. |
| **Central Async Ingress & Writer Channel** | Lập trình đa nhiệm, Bất đồng bộ & Hàng đợi I/O | Sử dụng `async/await`, ThreadPool và `System.Threading.Channels` để xử lý hàng chục kết nối đồng thời mà không bị tốn thread; bảo đảm Single-Writer trên mỗi Socket. | `[COURSE]` Giáo trình TH Bài 3 (Đa luồng); Slide TL2 & TL3. |
| **Admin Realtime Sync (Snapshot + Delta)** | Mô hình phân phối sự kiện mạng (Event-driven Network) | Khi Admin kết nối: Server gửi Snapshot (toàn bộ trạng thái hiện tại). Khi có biến động: Server push các frame Delta tới danh sách Admin đang kết nối. | `[COURSE]` Giáo trình TH Bài 4 (Chat Multi-client); Slide TL3. |
| **Bảo mật kênh truyền (TLS / SslStream)** | An toàn & Bảo mật trong truyền thông mạng | Bọc `NetworkStream` trong `SslStream`, thực hiện bắt tay mã hóa TLS 1.2/1.3, xác thực chứng chỉ số (Certificate Validation) để chống nghe lén (Sniffing) và giả mạo (MitM). | `[COURSE]` Giáo trình TH Bài 7 (Bảo vệ dữ liệu); Slide TL8 (Network Security). |
| **Mật khẩu an toàn (PBKDF2-HMAC-SHA256 + Salt)** | Mật mã học: Hàm băm một chiều có độ trễ cao | Không lưu mật khẩu gốc. Áp dụng chuẩn bảo mật của NIST/OWASP để chống tấn công Rainbow Table và Brute-force mật khẩu quản trị. | `[COURSE]` Giáo trình TH Bài 7 (Hàm băm SHA, Salt); Slide TL8. |
| **WinForms UI Marshaling (`Invoke`)** | Đồng bộ luồng giữa Network Thread và UI Thread | Xử lý dữ liệu nhận từ Socket ngầm và hiển thị lên WinForms an toàn bằng `Control.Invoke`, `BeginInvoke` hoặc `.NET 9+ InvokeAsync`, loại bỏ thói quen xấu `CheckForIllegalCrossThreadCalls = false`. | `[COURSE]` Giáo trình TH Bài 1 & Bài 3; Slide TL2. |
| **Triển khai mạng LAN 2–3 Laptop vật lý** | Định tuyến, Địa chỉ IP & Firewall trong mạng nội bộ | Cấu hình Bind IP (`0.0.0.0` vs LAN IP `192.168.x.x`), mở cổng Windows Firewall, kiểm tra kết nối chéo giữa các máy thật. | `[COURSE]` Giáo trình TH Bài 4; Slide TL1. |
| **Agent Simulator (10/20/50 Sockets)** | Kiểm thử tải và khả năng chịu tải của Server mạng | Chương trình kiểm thử mở hàng chục Socket kết nối đồng thời, gửi dữ liệu đúng chuẩn giao thức để đo lường độ trễ và ổn định của Server. | `[COURSE]` Slide TL3 (Xử lý nhiều kết nối đồng thời). |
| **Gateway & Multi-Collector (P1 Conditional)** | Phân phối tải mạng tầng giao vận (L4 Load Balancing) | Gateway hoạt động như một bộ chuyển tiếp luồng byte TCP (Reverse Proxy tầng 4), phân phối các kết nối Agent mới tới 2 Collector theo thuật toán Least-Connections. | `[STANDARD]` NGINX L4 stream module / TCP Reverse Proxy. |

---

## 4. TỪ BÀI THỰC HÀNH CĂN BẢN ĐẾN ĐỒ ÁN LABGUARD: BƯỚC TIẾN KỸ THUẬT

### 4.1. Từ Delimiter sang Length-Prefix Framing
- **Thực hành Lab 3 UIT:** Hướng dẫn sinh viên đọc từng byte bằng `clientSocket.Receive(recv)` (với `recv = new byte[1]`) lặp lại cho đến khi gặp ký tự xuống dòng `\n`.
- **Phân tích chuẩn mực:**
  - Về lý thuyết, JSON hợp lệ (RFC 8259 §7) escape ký tự xuống dòng thành `\n`, nên single-line JSON vẫn dùng delimiter được.
  - Tuy nhiên, đọc từng byte lặp lại rất tốn CPU và kém linh hoạt.
- **Bước tiến của LabGuard:** Dùng **Length-Prefix (4 byte uint32 Big-endian)**. Phía nhận đọc đúng 4 byte để biết độ dài $N$, sau đó cấp phát buffer và đọc đúng $N$ byte. Vừa tối ưu I/O, vừa có contract nhị phân tường minh, vừa hỗ trợ chống DoS chủ động bằng `MaxFrameSize = 256 KiB`.

### 4.2. Từ `CheckForIllegalCrossThreadCalls = false` sang UI Dispatch
- **Thực hành Lab 3 UIT (trang 9):** Chứa đoạn code chữa cháy:
  ```csharp
  CheckForIllegalCrossThreadCalls = false;
  Thread serverThread = new Thread(new ThreadStart(StartUnsafeThread));
  serverThread.Start();
  ```
- **Hậu quả:** Tắt tính năng kiểm tra vi phạm đa luồng của .NET runtime trong Debug mode, nhưng hoàn toàn không làm cho code an toàn luồng; xung đột trạng thái trên Win32 control handle vẫn xảy ra.
- **Bước tiến của LabGuard:** Bắt buộc dispatch mọi cập nhật UI từ Background Task về UI Thread thông qua `Control.Invoke`, `Control.BeginInvoke` hoặc `.NET 9+ Control.InvokeAsync`.

### 4.3. Từ `while (socket.Connected)` sang Application Heartbeat
- **Thực hành Lab 3 UIT (trang 10):** Dùng `while (clientSocket.Connected)`.
- **Hậu quả:** Khi máy client bị mất nguồn hoặc đứt cáp đột ngột mà chưa có thao tác I/O mới, cờ `Connected` phía Server vẫn giữ nguyên `true`.
- **Bước tiến của LabGuard:** Hiện thực giao thức Heartbeat ở tầng ứng dụng: Agent gửi gói `HEARTBEAT` mỗi 5 giây; Central Server đánh giá liveness dựa trên đồng hồ đơn điệu: nếu quá 20 giây không có heartbeat, Central chủ động đánh dấu thiết bị `OFFLINE` và đóng Socket.
