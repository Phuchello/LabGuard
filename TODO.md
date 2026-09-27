# LABGUARD — BẢNG THEO DÕI CÔNG VIỆC NHÓM (TODO)

> **Bảng theo dõi tiến độ công việc dành cho 4 thành viên nhóm (A, B, C, D).**  
> Chi tiết từng việc, file cần mở, trạng thái và cách thử được cập nhật tại file Excel **[`LabGuard_Task_Tracker.xlsx`](LabGuard_Task_Tracker.xlsx)** và tài liệu **[`START_HERE.md`](START_HERE.md)**.  
>
> *Lưu ý quan trọng:* Hiện tại các nhiệm vụ **A-001**, **A-002** và **A-003** đã được chính thức nghiệm thu Xong (123/123 tests PASS). Toàn bộ các nhiệm vụ còn lại đều là **CHƯA LÀM**, các thành viên sẽ tự tay tạo mới và lập trình các file tính năng theo phân công.

---

## 1. NHIỆM VỤ ĐÃ NGHIỆM THU CHÍNH THỨC (OFFICIALLY CLOSED)
- [x] **Đóng khung gói tin TCP 4 byte độ dài (A-002):** Tiền tố độ dài 4-byte UInt32 Big-Endian (`FrameCodec.cs`), xử lý phân mảnh TCP (partial reads), ném lỗi khi frame <= 0 hoặc > 262,144 bytes. Bộ kiểm thử tự động giao thức đạt 100% PASS (Protocol tests PASS).
- [x] **16 bản tin JSON mẫu chuẩn (A-001):** Khung phong bì 6 trường cố định (`EnvelopeValidator.cs`), 16 file mẫu JSON canonical trong `tests/LabGuard.Protocol.Tests/Fixtures/` kiểm thử đạt 100% PASS.

---

## 2. NHIỆM VỤ CỦA TỪNG THÀNH VIÊN

### Sinh viên A — Máy trạm Agent & Giao thức
- [x] **A-001:** 16 bản tin JSON mẫu chuẩn *(Đã nghiệm thu Xong)*.
- [x] **A-002:** Đóng khung gói tin TCP tiền tố 4 byte *(Đã nghiệm thu Xong)*.
- [x] **A-003:** Cho dữ liệu gửi đi xếp hàng tuần tự (`AgentSocketClient.cs`, BoundedChannel 256, FullMode.Wait) *(Đã nghiệm thu Xong)*.
- [ ] **A-004 [CHƯA LÀM — TIẾP THEO]:** Đo CPU, RAM và Uptime thật từ Windows (`MetricsCollector.cs`, `MetricsService.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **A-005 [CHƯA LÀM]:** Tự động kết nối lại khi rớt mạng với Exponential Backoff + Jitter (`AgentSocketClient.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **A-006 [CHƯA LÀM]:** Bộ nhớ giữ dữ liệu chưa gửi được (`ReplayBuffer.cs`, tối đa 50 batches, `METRIC_BATCH` duy nhất, FIFO Drop-Oldest, `isReplay = true`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **A-007 [CHƯA LÀM]:** Giám sát dịch vụ mạng cổng 80 và 8080 (`ServiceMonitor.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **A-008 [CHƯA LÀM]:** Bổ sung kịch bản ngắt kết nối đột ngột để kiểm tra đóng socket an toàn *(Kịch bản kiểm thử)*.
- [ ] **A-009 [CHƯA LÀM]:** Thử nghiệm kiểm tra dịch vụ mạng trên máy ảo Ubuntu thật *(Kịch bản kiểm thử)*.

### Sinh viên B — Máy chủ Central & CSDL
> *Toàn bộ công việc của B là CHƯA LÀM. B bắt đầu từ B-001.*
- [ ] **B-001 [BẮT ĐẦU LÀM]:** Mở cổng Socket TCP 5000 đón nhận kết nối (`CentralServer.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-002 [CHƯA LÀM]:** Bắt tay và xác thực Agent (`ClientConnection.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-003 [CHƯA LÀM]:** Nhận nhịp tim và theo dõi máy tắt sau 20s (`LivenessMonitor.cs`, `MonotonicLivenessRegistry.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-004 [CHƯA LÀM]:** Phân quyền tài khoản máy chủ RBAC: chỉ Admin mới được gán người sửa (`ClientConnection.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-005 [CHƯA LÀM]:** Lưu số liệu máy vào CSDL SQL Server bảng `dbo.MetricSamples` (`DatabaseRepository.cs`, `schema.sql`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-006 [CHƯA LÀM]:** Bộ quy tắc phát hiện sự cố CPU/RAM/Service với Hysteresis (`RuleEngine.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-007 [CHƯA LÀM]:** Vòng đời sự cố và chống sửa đè OCC bằng `RowVersion` (`DatabaseRepository.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-008 [CHƯA LÀM]:** Tách riêng CSDL kiểm thử `LabGuardTestDb` *(Cấu hình kiểm thử)*.
- [ ] **B-009 [CHƯA LÀM]:** Ghi nhận UserId của kỹ thuật viên xử lý vào bảng sự cố `dbo.Incidents` *(File sẽ được tạo khi thực hiện nhiệm vụ)*.

### Sinh viên C — Giao diện quản trị Admin WinForms
> *Toàn bộ công việc của C là CHƯA LÀM. C gồm đúng 10 nhiệm vụ (C-001 đến C-008, C-010, C-011; nhiệm vụ C-009 được thu hồi/dự phòng). C bắt đầu từ C-001 và C-002.*
- [ ] **C-001 [BẮT ĐẦU LÀM]:** Màn hình chính WinForms hiển thị bảng dữ liệu (`DashboardForm.cs`) *(C tự tay thiết kế trên form rỗng hiện có)*.
- [ ] **C-002 [BẮT ĐẦU LÀM]:** Kết nối ngầm tới máy chủ Central (`AdminClient.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **C-003 [CHƯA LÀM]:** Bảng danh sách thiết bị DataGridView (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-004 [CHƯA LÀM]:** Bảng danh sách sự cố DataGridView (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-005 [CHƯA LÀM]:** Nút tiếp nhận sự cố ACK (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-006 [CHƯA LÀM]:** Nút bắt đầu sửa Start (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-007 [CHƯA LÀM]:** Nút hoàn tất sửa Resolve (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-008 [CHƯA LÀM]:** Thêm cột người xử lý và hộp thoại popup chọn kỹ thuật viên khi bấm `Assign` *(Hoàn thiện trên form)*.
- [ ] **C-010 [CHƯA LÀM]:** Khóa nút bấm theo phân quyền khi đăng nhập Viewer (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-011 [CHƯA LÀM]:** Hiển thị hộp thoại báo lỗi thân thiện khi máy chủ từ chối thao tác (`DashboardForm.cs`) *(Hoàn thiện trên form)*.

### Sinh viên D — Phần mở rộng sau này (Gateway & Collector)
> *Toàn bộ công việc của D là CHƯA LÀM. D bắt đầu làm quen socket TCP trong tools/.*
- [ ] **D-001 [BẮT ĐẦU LÀM]:** Chạy bộ kiểm thử giao thức (`dotnet test`), đọc hiểu `FrameCodec.cs`, xem xét 16 tệp mẫu JSON trong fixtures, nắm vững phong bì 6 trường và đóng khung TCP 4 byte.
- [ ] **D-002 [BẮT ĐẦU LÀM]:** Tự viết một chương trình Console nhỏ (TCP Echo) trong thư mục riêng `tools/` để làm quen với Socket TCP trong C#.
- [ ] **D-003 [CHƯA LÀM]:** Thử nghiệm nhận và chuyển tiếp gói tin TCP Relay (`tools/`, BoundedChannel 100, FullMode.Wait).
- [ ] **D-004 [CHƯA LÀM / DỰ KIẾN]:** Tạo dự án `LabGuard.Gateway` (bộ cân bằng tải L4 TCP) sau khi các tính năng cốt lõi của A, B, C ổn định.
- [ ] **D-005 [CHƯA LÀM / DỰ KIẾN]:** Tạo dự án `LabGuard.Collector` (trạm gom dữ liệu phân tán) sau khi các tính năng cốt lõi của A, B, C ổn định.
- [ ] **D-006 [CHƯA LÀM / DỰ KIẾN]:** Viết logic sinh tải 20–50 máy trạm cho `LabGuard.Simulator`.

---

## 3. VIỆC CẢ NHÓM
- [ ] Chạy thử kịch bản phối hợp hoàn chỉnh: Bật SQL -> Central -> Agent -> Admin -> Kích hoạt quá tải CPU -> Kỹ thuật viên xử lý -> Hoàn tất sửa sự cố.
- [ ] Chạy toàn bộ bài kiểm thử tự động (`dotnet test`) đảm bảo 100% pass trước mỗi lần nộp bài.
- [ ] Cập nhật kết quả vào file theo dõi `LabGuard_Task_Tracker.xlsx`.
