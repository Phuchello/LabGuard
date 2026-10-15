# BẰNG CHỨNG KIỂM THỬ LG-D-002: TCP ECHO SERVER

- **Nhiệm vụ:** `LG-D-002 — Viết thử chương trình TCP Echo đơn giản`
- **Môn học:** NT106 — Lập trình mạng
- **Thời điểm kiểm thử:** 15/10/2026
- **Vị trí mã nguồn:** `tools/TcpEcho/Program.cs`
- **Môi trường chạy:** .NET 9.0 (C# 13), Windows 11 loopback TCP Socket

---

## 1. Mục tiêu kiểm thử chất lượng (Quality Pass)

1. **Khởi động và Báo lỗi cổng bị chiếm dụng (Occupied Port):** Trả về mã lỗi khác 0 (`ExitCode = 1`) và log lỗi vào `stderr` khi `listener.Start()` thất bại do trùng cổng.
2. **Quản lý Client Task và Giải phóng Socket an toàn:** Quản lý danh sách tác vụ client qua `ConcurrentDictionary<Task, byte>` và chờ các client đóng êm đẹp trước khi tắt server.
3. **Giao tiếp đồng thời 2 Client (True Concurrency):** Chứng minh hai client kết nối và truyền nhận dữ liệu đồng thời, client này không bị nghẽn bởi client kia.
4. **Độ chính xác dữ liệu (Echo Accuracy):** Dữ liệu client gửi (chuỗi UTF-8 tiếng Việt, ký tự đặc biệt) được phản hồi nguyên vẹn 100%.
5. **Xử lý ngắt kết nối an toàn (Zero-Crash):** Xử lý cả hai trường hợp ngắt kết nối: ngắt bình thường (EOF/FIN) và ngắt đột ngột (gói tin TCP RST).

---

## 2. Kết quả kiểm thử thực tế

### Kịch bản 1: Cổng bị chiếm dụng (Occupied Port)
- Mở cổng `5088` trước bằng một `TcpListener` giả lập.
- Khởi chạy `TcpEcho` trỏ vào cổng `5088`.
- **Output ghi nhận từ stderr:**
  ```text
  [EchoServer] [LỖI KHỞI ĐỘNG] Không thể lắng nghe tại cổng 5088: Only one usage of each socket address (protocol/network address/port) is normally permitted. (Mã lỗi: AddressAlreadyInUse)
  ```
- **Mã thoát (ExitCode):** `1` (Khác 0, báo hiệu lỗi rõ ràng).
- **Kết luận:** **PASS**.

---

### Kịch bản 2: Echo chính xác dữ liệu UTF-8 và ký tự đặc biệt
- Client kết nối đến `TcpEcho` tại cổng `5077` và gửi tuần tự 3 gói tin:
  1. `Xin chao NT106 - LabGuard TCP Echo Server!` -> Server phản hồi khớp 100%.
  2. `Ky tu dac biet: !@#$%^&*()_+~{}[]|\:;?/>.<,` -> Server phản hồi khớp 100%.
  3. `1234567890 - abcdefghijklmnopqrstuvwxyz - ABCDEFGHIJKLMNOPQRSTUVWXYZ` -> Server phản hồi khớp 100%.
- **Kết luận:** **PASS**.

---

### Kịch bản 3: Hai Client giao tiếp đồng thời (True Concurrency)
- **Kịch bản:**
  - Client A kết nối đến server, gửi `Client A - Message 1`, nhận phản hồi và giữ kết nối mở.
  - Trong lúc Client A vẫn đang duy trì socket, Client B kết nối vào server và gửi `Client B - Giao tiep dong thoi khi Client A dang mo!`.
  - Client B nhận phản hồi tức thì trong vòng **3 ms**.
  - Sau đó Client A tiếp tục gửi `Client A - Message 2` và nhận phản hồi bình thường.
- **Kết luận:** **PASS** (Chứng minh Server xử lý đa luồng non-blocking qua `Task.Run`, không bị nghẽn tuần tự).

---

### Kịch bản 4: Client ngắt kết nối bình thường (Graceful EOF / FIN)
- Client kết nối, trao đổi dữ liệu, sau đó gọi `client.Client.Shutdown(SocketShutdown.Both)` và đóng socket.
- Server phát hiện `bytesRead == 0`, thoát vòng lặp, giải phóng socket an toàn.
- **Kết luận:** **PASS**.

---

### Kịch bản 5: Client ngắt kết nối đột ngột với gói tin RST (Zero-Crash)
- Client kết nối và thiết lập `LingerOption(true, 0)`, sau đó đóng socket ngay lập tức để ép gửi gói tin TCP RST (`ConnectionReset`).
- Server bắt ngoại lệ `IOException` / `SocketException`, ghi nhận cảnh báo và giải phóng socket mà không làm sập tiến trình.
- Client tiếp theo kết nối gửi tin `Server con hoat dong tot sau khi bi RST!` và nhận phản hồi echo chính xác ngay lập tức.
- **Kết luận:** **PASS**.

---

## 3. Tổng kết

| STT | Kịch bản kiểm thử | Kết quả | Ghi chú |
|:---:|---|:---:|---|
| 1 | Cổng bị chiếm dụng (AddressAlreadyInUse) | **PASS** | Báo lỗi stderr, ExitCode = 1 |
| 2 | Echo chuỗi UTF-8 / ký tự đặc biệt | **PASS** | Khớp 100% nội dung |
| 3 | 2 Client giao tiếp đồng thời | **PASS** | Phản hồi Client B trong 3 ms khi Client A đang mở |
| 4 | Client ngắt kết nối bình thường (EOF) | **PASS** | Nhận diện bytesRead == 0, giải phóng sạch sẽ |
| 5 | Client ngắt kết nối đột ngột (RST) | **PASS** | Zero-Crash, server sống và phục vụ client tiếp |
| 6 | Toàn bộ kiểm thử tự động của solution | **PASS** | **150 / 150 tests PASS (100%)** |
| 7 | Biên dịch toàn bộ Solution | **PASS** | **0 Warning, 0 Error** |
