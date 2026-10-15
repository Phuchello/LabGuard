# BẰNG CHỨNG KIỂM THỬ LG-D-002: TCP ECHO SERVER

- **Nhiệm vụ:** `LG-D-002 — Viết thử chương trình TCP Echo đơn giản`
- **Môn học:** NT106 — Lập trình mạng
- **Thời điểm kiểm thử:** 15/10/2026
- **Vị trí mã nguồn:** `tools/TcpEcho/Program.cs`
- **Môi trường chạy:** .NET 9.0 (C# 13), Windows 11 loopback TCP Socket

---

## 1. Mục tiêu kiểm thử chất lượng (Quality & Concurrency Pass)

1. **Khởi động và Báo lỗi cổng bị chiếm dụng (Occupied Port):** Trả về mã lỗi khác 0 (`ExitCode = 1`) và log thông tin chi tiết vào `stderr` khi `listener.Start()` thất bại do cổng đã được tiến trình khác sử dụng (`SocketError.AddressAlreadyInUse`).
2. **Triệt tiêu Race Condition trong Quản lý Client Task:**
   - Loại bỏ hoàn toàn closure capture của biến tác vụ cục bộ (`clientTask`) trong khối `finally`.
   - Sử dụng đăng ký trước vào `ConcurrentDictionary<Task, byte>` kết hợp `clientTask.ContinueWith(t => activeClientTasks.TryRemove(t, out _), ...)`. Tham số `t` được runtime TPL truyền an toàn, bảo đảm không bị race condition khi tác vụ kết thúc cực nhanh.
   - Đảm bảo giải phóng `client.Close()` ngay cả khi việc điều phối tác vụ gặp ngoại lệ trước khi dispatch.
3. **Độ chính xác dữ liệu tuyệt đối (Exact Byte-for-Byte Equality):** Kiểm tra chuỗi tiếng Việt Unicode có dấu `"Xin chào phòng máy UIT!"` (26 bytes UTF-8) phản hồi nguyên vẹn từng byte.
4. **Giao tiếp đồng thời 2 Client (True Concurrency):** Chứng minh hai client kết nối và truyền nhận dữ liệu độc lập, client đang giữ kết nối không làm nghẽn client mới.
5. **Xử lý ngắt kết nối an toàn (Zero-Crash):** Xử lý cả ngắt kết nối bình thường (EOF/FIN qua `bytesRead == 0`) và ngắt đột ngột (gói tin TCP RST qua `LingerOption(true, 0)`).
6. **Stress Test kiểm tra xung đột điều phối (Scheduling Stress Test):** Thực hiện 100 chu kỳ kết nối/ngắt kết nối cấp tập (bao gồm cả RST) chạy song song đa luồng, chứng minh server không bị rò rỉ tác vụ hay treo luồng.

---

## 2. Kết quả kiểm thử thực tế

### Kịch bản 1: Cổng bị chiếm dụng (Occupied Port Startup Failure)
- **Thực hiện:** Chiếm dụng trước cổng `5095` bằng một `TcpListener` mẫu, sau đó khởi chạy `TcpEcho 5095`.
- **Output ghi nhận từ stderr:**
  ```text
  [EchoServer] [LỖI KHỞI ĐỘNG] Không thể lắng nghe tại cổng 5095: Only one usage of each socket address (protocol/network address/port) is normally permitted. (Mã lỗi: AddressAlreadyInUse)
  ```
- **Mã thoát (ExitCode):** `1` (Khác 0, báo hiệu lỗi rõ ràng).
- **Kết luận:** **PASS**.

---

### Kịch bản 2: Khớp chính xác từng byte UTF-8 chuỗi tiếng Việt và ký tự đặc biệt
- **Thực hiện:** Gửi chuỗi tiếng Việt Unicode có dấu và so sánh chi tiết mảng byte gửi đi và nhận về:
  - Chuỗi gửi: `"Xin chào phòng máy UIT!"`
  - Độ dài: `26 bytes`
  - Hex gửi: `58-69-6E-20-63-68-C3-A0-6F-20-70-68-C3-B2-6E-67-20-6D-C3-A1-79-20-55-49-54-21`
  - Chuỗi nhận: `"Xin chào phòng máy UIT!"`
  - Hex nhận: `58-69-6E-20-63-68-C3-A0-6F-20-70-68-C3-B2-6E-67-20-6D-C3-A1-79-20-55-49-54-21`
- **Kết quả:** Toàn bộ 26/26 bytes khớp 100%, không bị lỗi font hay suy hao ký tự Unicode.
- **Kết luận:** **PASS**.

---

### Kịch bản 3: Hai Client giao tiếp đồng thời (True Concurrency)
- **Thực hiện:**
  - Client A kết nối đến server, gửi gói tin đầu tiên và **giữ nguyên kết nối**.
  - Trong lúc socket Client A đang mở, Client B mở kết nối mới và gửi dữ liệu.
  - Client B nhận dữ liệu echo tức thì chỉ trong **3 ms** mà không hề bị Client A chặn.
  - Client A gửi gói tin thứ hai thành công và đóng kết nối.
- **Kết luận:** **PASS** (Server xử lý non-blocking đa luồng độc lập).

---

### Kịch bản 4: Xử lý ngắt kết nối bình thường (EOF / FIN) và đột ngột (RST)
- **Ngắt bình thường (EOF):** Client gửi dữ liệu rồi đóng luồng ghi (`SocketShutdown.Both`), server phát hiện `bytesRead == 0` và giải phóng tài nguyên sạch sẽ.
- **Ngắt đột ngột (TCP RST):** Client bật `LingerOption(true, 0)` và đóng socket ngay lập tức để gửi cờ RST. Server bắt an toàn `SocketError.ConnectionReset` / `ConnectionAborted`, ghi log thông tin và dọn dẹp kết nối, hoàn toàn không làm sập server.
- **Kiểm tra liveness:** Ngay sau khi gặp RST, client kế tiếp kết nối và nhận phản hồi bình thường.
- **Kết luận:** **PASS**.

---

### Kịch bản 5: Stress Test 100 chu kỳ kết nối/ngắt kết nối cấp tập (Scheduling Stress Test)
- **Thực hiện:** Dùng `Parallel.For` (10 luồng song song) thực hiện 100 chu kỳ kết nối, gửi dữ liệu, đọc echo và đóng socket (với 1/3 số kết nối ngắt đột ngột bằng cờ RST) trên cổng `5096`.
- **Kết quả đo đạc:**
  - Số chu kỳ hoàn thành thành công: **100 / 100** (Tỷ lệ 100%).
  - Tổng thời gian hoàn thành: **40 ms**.
  - Kiểm tra hậu stress: Client mới kết nối gửi ping kiểm tra sau stress test nhận phản hồi bình thường tức thì.
  - Không có task rác bị treo lại trong danh sách theo dõi.
- **Kết luận:** **PASS**.

---

## 3. Tổng kết

| STT | Kịch bản kiểm thử | Kết quả | Ghi chú |
|:---:|---|:---:|---|
| 1 | Khởi động trùng cổng (AddressAlreadyInUse) | **PASS** | Ghi log stderr, trả về ExitCode = 1 |
| 2 | Khớp byte UTF-8 tiếng Việt ("Xin chào phòng máy UIT!") | **PASS** | Khớp 100% (26/26 bytes) |
| 3 | 2 Client giao tiếp đồng thời | **PASS** | Client B phản hồi trong 3 ms khi Client A đang giữ socket |
| 4 | Client ngắt kết nối bình thường (EOF) & đột ngột (RST) | **PASS** | Zero-Crash, server duy trì trạng thái ổn định |
| 5 | Stress Test 100 chu kỳ cấp tập | **PASS** | 100/100 chu kỳ thành công trong 40 ms, triệt tiêu race condition |
| 6 | Toàn bộ kiểm thử tự động của solution (`dotnet test`) | **PASS** | **150 / 150 tests PASS (100%)** |
| 7 | Biên dịch toàn bộ Solution (`dotnet build`) | **PASS** | **0 Warning(s), 0 Error(s)** |
