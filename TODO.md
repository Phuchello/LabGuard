# LABGUARD — BẢNG THEO DÕI CÔNG VIỆC NHÓM (TODO)

> **Bảng theo dõi tiến độ công việc dành cho 4 thành viên nhóm (A, B, C, D).**
> Chi tiết từng việc, file cần mở, trạng thái và cách thử được cập nhật tại file Excel **[`LabGuard_Task_Tracker.xlsx`](LabGuard_Task_Tracker.xlsx)** và tài liệu **[`START_HERE.md`](START_HERE.md)**.
>
> *Lưu ý quan trọng (Đã đồng bộ sau PR #4, #5, #7, #8, #9):*
> - **Đã nghiệm thu (DONE / ACCEPTED):** A-001..A-004, B-001 (PR #7), D-002 (PR #8) và bộ công cụ Mock Harness (PR #9).
> - **Đang làm (IN PROGRESS):** B-005 (DDL `database/schema.sql` đã merge qua PR #5; C# persistence và SQL Server đang chờ hoàn thiện), C-001 (khung UI 0.1 đã merge qua PR #4; nghiệm thu toàn diện UI & data binding đang chờ thực hiện).
> - **Chưa làm (TODO):** A-005+ (tiếp theo của A: A-005 Reconnect), B-002+ (tiếp theo của B: B-002 Bắt tay HELLO/AUTH), C-002+ (tiếp theo của C: C-002 AdminClient), D-001 (chưa có báo cáo), D-003+ (tiếp theo của D: D-003 TCP Relay).
> - **Bộ kiểm thử tự động cục bộ:** Toàn bộ **165/165 tests PASS** (Protocol 107, Central 9, Agent 34, MockHarness 15; đối soát Windows Reference 10/10 PASS). Lưu ý: Đây là số liệu kiểm thử cục bộ trước khi merge; hệ thống GitHub CI chưa được thiết lập.

---

## 1. NHIỆM VỤ ĐÃ NGHIỆM THU CHÍNH THỨC (OFFICIALLY ACCEPTED / CLOSED)
- [x] **16 bản tin JSON mẫu chuẩn (A-001 — DONE / ACCEPTED):** Khung phong bì 6 trường cố định (`EnvelopeValidator.cs`), 16 file mẫu JSON canonical trong `tests/LabGuard.Protocol.Tests/Fixtures/` kiểm thử đạt 100% PASS.
- [x] **Đóng khung gói tin TCP 4 byte độ dài (A-002 — DONE / ACCEPTED):** Tiền tố độ dài 4-byte UInt32 Big-Endian (`FrameCodec.cs`), xử lý phân mảnh TCP (partial reads), ném lỗi khi frame <= 0 hoặc > 262,144 bytes. Bộ kiểm thử tự động giao thức đạt 100% PASS (107/107 tests PASS).
- [x] **Kết nối TCP & hàng đợi Single-Writer (A-003 — DONE / ACCEPTED):** Hàng đợi `Channel<byte[]>` dung lượng 256 items, `BoundedChannelFullMode.Wait`, một luồng ghi duy nhất chống đan xen byte (`AgentSocketClient.cs`, 16/16 tests PASS).
- [x] **Thu thập CPU, RAM, Uptime hệ thống (A-004 — DONE / ACCEPTED / CLOSED):** Gọi trực tiếp Win32 API (`GetSystemTimes`, `GlobalMemoryStatusEx`, `GetTickCount64`), dịch vụ định kỳ 5s `MetricsService` và `HeartbeatService`. **18/18 A-004 xUnit cases PASS** (tổng `Agent.Tests` 34/34 PASS), **đối soát tự động tham chiếu Windows Reference 10/10 PASS**. Nghiệm thu chính thức hoàn tất.
- [x] **Mở cổng Socket TCP 5000 đón nhận kết nối (B-001 — DONE / ACCEPTED):** `CentralServer.cs` mở TcpListener port 5000, xử lý đa client bất đồng bộ, đọc envelope bằng FrameCodec, xử lý ngắt kết nối an toàn, **9/9 Central.Tests PASS** (Merged qua PR #7).
- [x] **Viết thử chương trình TCP Echo đơn giản (D-002 — DONE / ACCEPTED):** `tools/TcpEcho/Program.cs` mở Socket TCP echo chuỗi dữ liệu, xử lý đa client đồng thời, shutdown và giải phóng socket an toàn (Merged qua PR #8).
- [x] **Bộ công cụ giả lập kiểm thử độc lập Mock Harness (TOOLING — MERGED):** `tools/mock_harness/FakeAgent` và `FakeCentral` kiểm thử giao thức độc lập, **15/15 MockHarness.Tests PASS** (Merged qua PR #9; công cụ bổ trợ, không thay thế D-003).

---

## 2. NHIỆM VỤ CỦA TỪNG THÀNH VIÊN

### Sinh viên A — Máy trạm Agent & Giao thức
- [x] **A-001:** 16 bản tin JSON mẫu chuẩn *(DONE / ACCEPTED — Đã nghiệm thu chính thức)*.
- [x] **A-002:** Đóng khung gói tin TCP tiền tố 4 byte *(DONE / ACCEPTED — Đã nghiệm thu chính thức)*.
- [x] **A-003:** Cho dữ liệu gửi đi xếp hàng tuần tự (`AgentSocketClient.cs`, BoundedChannel 256, FullMode.Wait) *(DONE / ACCEPTED — Đã nghiệm thu chính thức)*.
- [x] **A-004:** Đo CPU, RAM và Uptime thật từ Windows (`MetricsCollector.cs`, `MetricsService.cs`, `HeartbeatService.cs`) *(DONE / ACCEPTED — Đã nghiệm thu chính thức: 18/18 tests PASS, Windows Reference 10/10 PASS)*.
- [ ] **A-005 [CHƯA LÀM — TIẾP THEO]:** Tự động kết nối lại khi rớt mạng với Exponential Backoff + Jitter (`AgentSocketClient.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **A-006 [CHƯA LÀM]:** Bộ nhớ giữ dữ liệu chưa gửi được (`ReplayBuffer.cs`, tối đa 50 batches, `METRIC_BATCH` duy nhất, FIFO Drop-Oldest, `isReplay = true`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **A-007 [CHƯA LÀM]:** Giám sát dịch vụ mạng cổng 80 và 8080 (`ServiceMonitor.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **A-008 [CHƯA LÀM]:** Bổ sung kịch bản ngắt kết nối đột ngột để kiểm tra đóng socket an toàn *(Kịch bản kiểm thử)*.
- [ ] **A-009 [CHƯA LÀM]:** Thử nghiệm kiểm tra dịch vụ mạng trên máy ảo Ubuntu thật *(Kịch bản kiểm thử)*.

### Sinh viên B — Máy chủ Central & CSDL
- [x] **B-001:** Mở cổng Socket TCP 5000 đón nhận kết nối (`CentralServer.cs`) *(DONE / ACCEPTED — Merged qua PR #7, 9/9 Central.Tests PASS)*.
- [ ] **B-002 [CHƯA LÀM — TIẾP THEO]:** Bắt tay và xác thực Agent (`ClientConnection.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-003 [CHƯA LÀM]:** Nhận nhịp tim và theo dõi máy tắt sau 20s (`LivenessMonitor.cs`, `MonotonicLivenessRegistry.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-004 [CHƯA LÀM]:** Phân quyền tài khoản máy chủ RBAC: chỉ Admin mới được gán người sửa (`ClientConnection.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-005 [ĐANG LÀM]:** Lưu số liệu máy vào CSDL SQL Server bảng `dbo.MetricSamples`, `dbo.ProcessedBatches`, `dbo.Incidents` (`DatabaseRepository.cs`, `database/schema.sql` DDL đã merge qua PR #5; C# persistence và kiểm thử SQL Server đang chờ hoàn thiện).
- [ ] **B-006 [CHƯA LÀM]:** Bộ quy tắc phát hiện sự cố CPU/RAM/Service với Hysteresis (`RuleEngine.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-007 [CHƯA LÀM]:** Vòng đời sự cố và chống sửa đè OCC bằng `RowVersion` (`DatabaseRepository.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **B-008 [CHƯA LÀM]:** Tách riêng CSDL kiểm thử `LabGuardTestDb` *(Cấu hình kiểm thử)*.
- [ ] **B-009 [CHƯA LÀM]:** Ghi nhận UserId của kỹ thuật viên xử lý vào bảng sự cố `dbo.Incidents` *(File sẽ được tạo khi thực hiện nhiệm vụ)*.

### Sinh viên C — Giao diện quản trị Admin WinForms
- [ ] **C-001 [ĐANG LÀM]:** Màn hình chính WinForms hiển thị bảng dữ liệu (`DashboardForm.cs`) *(Layout 0.1 đã merge qua PR #4, docs/evidence/admin_dashboard_c001.png; nghiệm thu toàn diện UI & data binding đang chờ thực hiện)*.
- [ ] **C-002 [CHƯA LÀM — TIẾP THEO]:** Kết nối ngầm tới máy chủ Central (`AdminClient.cs`) *(File sẽ được tạo khi thực hiện nhiệm vụ)*.
- [ ] **C-003 [CHƯA LÀM]:** Bảng danh sách thiết bị DataGridView (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-004 [CHƯA LÀM]:** Bảng danh sách sự cố DataGridView (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-005 [CHƯA LÀM]:** Nút tiếp nhận sự cố ACK (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-006 [CHƯA LÀM]:** Nút bắt đầu sửa Start (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-007 [CHƯA LÀM]:** Nút hoàn tất sửa Resolve (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-008 [CHƯA LÀM]:** Thêm cột người xử lý và hộp thoại popup chọn kỹ thuật viên khi bấm `Assign` *(Hoàn thiện trên form)*.
- [ ] **C-010 [CHƯA LÀM]:** Khóa nút bấm theo phân quyền khi đăng nhập Viewer (`DashboardForm.cs`) *(Hoàn thiện trên form)*.
- [ ] **C-011 [CHƯA LÀM]:** Hiển thị hộp thoại báo lỗi thân thiện khi máy chủ từ chối thao tác (`DashboardForm.cs`) *(Hoàn thiện trên form)*.

### Sinh viên D — Phần mở rộng sau này (Gateway & Collector)
- [ ] **D-001 [CHƯA LÀM]:** Chạy bộ kiểm thử giao thức (`dotnet test`), đọc hiểu `FrameCodec.cs`, xem xét 16 tệp mẫu JSON trong fixtures, nắm vững phong bì 6 trường và đóng khung TCP 4 byte *(Chưa có báo cáo tìm hiểu)*.
- [x] **D-002:** Tự viết một chương trình Console nhỏ (TCP Echo) trong thư mục riêng `tools/TcpEcho/Program.cs` *(DONE / ACCEPTED — Merged qua PR #8)*.
- [ ] **D-003 [CHƯA LÀM — TIẾP THEO]:** Thử nghiệm nhận và chuyển tiếp gói tin TCP Relay (`tools/TcpRelay/`, BoundedChannel 100, FullMode.Wait). *(Lưu ý: Mock Harness trong `tools/mock_harness/` đã merge qua PR #9 là công cụ bổ trợ, không phải D-003)*.
- [ ] **D-004 [CHƯA LÀM / DỰ KIẾN]:** Tạo dự án `LabGuard.Gateway` (bộ cân bằng tải L4 TCP) sau khi các tính năng cốt lõi của A, B, C ổn định.
- [ ] **D-005 [CHƯA LÀM / DỰ KIẾN]:** Tạo dự án `LabGuard.Collector` (trạm gom dữ liệu phân tán) sau khi các tính năng cốt lõi của A, B, C ổn định.
- [ ] **D-006 [CHƯA LÀM / DỰ KIẾN]:** Viết logic sinh tải 20–50 máy trạm cho `LabGuard.Simulator`.

---

## 3. VIỆC CẢ NHÓM
- [ ] Chạy thử kịch bản phối hợp hoàn chỉnh: Bật SQL -> Central -> Agent -> Admin -> Kích hoạt quá tải CPU -> Kỹ thuật viên xử lý -> Hoàn tất sửa sự cố.
- [ ] Chạy toàn bộ bài kiểm thử tự động (`dotnet test`) đảm bảo 100% pass trước mỗi lần nộp bài.
- [ ] Cập nhật kết quả vào file theo dõi `LabGuard_Task_Tracker.xlsx`.
