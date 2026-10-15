# BẢN ĐỒ TOÀN BỘ TẬP TIN DỰ ÁN (FILE MAP)

> **Tài liệu tra cứu cấu trúc mã nguồn thực tế của dự án LabGuard.**
> Hiện tại, các phân hệ đã bắt đầu được hiện thực và merge theo các Pull Request chính thức.
> **Đã nghiệm thu chính thức (`DONE / ACCEPTED / CLOSED`):**
> - Thư viện giao thức (`LabGuard.Protocol` — A-001, A-002: 107 tests PASS).
> - Kết nối TCP Agent và hàng đợi (`LabGuard.Agent` — A-003: 16 tests PASS).
> - Thu thập CPU/RAM/Uptime & dịch vụ định kỳ (`LabGuard.Agent` — A-004: 18 tests PASS, đối soát Windows Reference 10/10 PASS).
> - Máy chủ TCP Server (`LabGuard.Central` — B-001 qua PR #7: `CentralServer.cs`, 9 tests PASS).
> - TCP Echo Server (`tools/TcpEcho/` — D-002 qua PR #8: `Program.cs`, đa client, shutdown an toàn).
> - Bộ công cụ giả lập kiểm thử độc lập Mock Harness (`tools/mock_harness/` qua PR #9: `FakeAgent`, `FakeCentral`, `LabGuard.MockHarness.Tests` 15 tests PASS).
>
> **Đang làm / Nghiệm thu từng phần:**
> - CSDL SQL Server (`database/schema.sql` — B-005 DDL đã merge qua PR #5; phần C# persistence và SQL Server chưa nghiệm thu).
> - Giao diện WinForms (`LabGuard.Admin` — C-001 layout 0.1 đã merge qua PR #4; nghiệm thu toàn diện UI đang chờ thực hiện).
>
> **Ký hiệu hiện trạng:**
> - ✅ **ĐÃ XONG & NGHIỆM THU (`DONE / ACCEPTED / CLOSED`):** Mã nguồn chính thức đã hoàn thành và nghiệm thu (A-001..A-004, B-001, D-002; Mock Harness).
> - 🟡 **ĐANG LÀM / NGHIỆM THU TỪNG PHẦN:** B-005 (`schema.sql` DDL đã có), C-001 (khung UI 0.1 đã có).
> - ⚪ **CHƯA LÀM:** Các nhiệm vụ tiếp theo (A-005+, B-002+, C-002+, D-001, D-003+).

---

## MỤC LỤC

1. [Phần 1: Thư viện giao thức dùng chung (`src/LabGuard.Protocol/`) — ĐÃ HOÀN THÀNH (A-001 & A-002)](#phần-1-thư-viện-giao-thức-dùng-chung-srclabguardprotocol--đã-hoàn-thành-a-001--a-002)
2. [Phần 2: Ứng dụng máy trạm (`src/LabGuard.Agent/`) — Sinh viên A (A-001 đến A-004 ĐÃ XONG; A-005 là việc tiếp theo)](#phần-2-ứng-dụng-máy-trạm-srclabguardagent--sinh-viên-a-a-001-đến-a-004-đã-xong--nghiệm-thu)
3. [Phần 3: Máy chủ trung tâm (`src/LabGuard.Central/`) — Sinh viên B (B-001 ĐÃ XONG; B-002 là việc tiếp theo)](#phần-3-máy-chủ-trung-tâm-srclabguardcentral--sinh-viên-b-b-001-đã-xong-b-002-là-việc-tiếp-theo)
4. [Phần 4: Giao diện quản trị (`src/LabGuard.Admin/`) — Sinh viên C (C-001 ĐANG LÀM; C-002 là việc tiếp theo)](#phần-4-giao-diện-quản-trị-srclabguardadmin--sinh-viên-c-c-001-đang-làm--layout-01-c-002-là-việc-tiếp-theo)
5. [Phần 5: Công cụ giả lập tải (`src/LabGuard.Simulator/`)](#phần-5-công-cụ-giả-lập-tải-srclabguardsimulator)
6. [Phần 6: Cơ sở dữ liệu SQL Server (`database/`) — Sinh viên B (B-005 ĐANG LÀM / schema.sql đã merge)](#phần-6-cơ-sở-dữ-liệu-sql-server-database--sinh-viên-b-task-b-005)
7. [Phần 7: Các dự án kiểm thử tự động (`tests/`) & Công cụ (`tools/`) — D-002 & Mock Harness](#phần-7-các-dự-án-kiểm-thử-tự-động-tests--công-cụ-tools)
8. [Phần 8: Tài liệu đặc tả kỹ thuật & Hướng dẫn (`docs/`)](#phần-8-tài-liệu-đặc-tả-kỹ-thuật--hướng-dẫn-docs)

---

## PHẦN 1: THƯ VIỆN GIAO THỨC DÙNG CHUNG (`src/LabGuard.Protocol/`) — ĐÃ HOÀN THÀNH (A-001 & A-002)

Toàn bộ thư viện này là giao ước mạng chuẩn của LabGuard, toàn bộ Protocol tests PASS và đóng băng để cả 4 thành viên sử dụng chung.

| File | Nói đơn giản nó làm gì | Ai cần file này | Khi nào nó chạy | Nó nhận gì | Nó đưa gì ra |
| :--- | :--- | :--- | :--- | :--- | :--- |
| [`FrameCodec.cs`](src/LabGuard.Protocol/FrameCodec.cs) | Ghi thêm 4 byte độ dài vào đầu gói tin khi gửi, và gom đủ byte khi đọc về từ luồng TCP để không bị dính/rách gói | Cả 4 người (A, B, C, D) | Mỗi lần gửi/nhận bất kỳ gói tin nào qua mạng | Luồng Stream TCP hoặc mảng byte | Gói tin nguyên vẹn hoặc mảng byte có 4 byte tiền tố độ dài |
| [`WireEnvelope.cs`](src/LabGuard.Protocol/WireEnvelope.cs) | Định nghĩa phong bì thư 6 trường chuẩn: `version`, `messageType`, `requestId`, `deviceId`, `sentAtUtc`, `payload` | Cả 4 người | Khi tạo mới bức thư gửi đi hoặc đọc bức thư vừa nhận | 6 thông tin cơ bản | Đối tượng phong bì thư hoàn chỉnh trong bộ nhớ |
| [`EnvelopeValidator.cs`](src/LabGuard.Protocol/EnvelopeValidator.cs) | Kiểm tra nghiêm ngặt tính hợp lệ của phong bì (đúng 6 trường, không có trường lạ, đúng giờ UTC, đúng version 1) | A, B, C | Ngay sau khi đọc được một chuỗi JSON từ mạng | Chuỗi JSON thô | Báo hợp lệ kèm phong bì thư, hoặc ném lỗi nếu vi phạm |
| [`MessageTypes.cs`](src/LabGuard.Protocol/MessageTypes.cs) | Danh mục 69 loại thông điệp chuẩn của LabGuard (`HEARTBEAT`, `METRIC_BATCH`, `LOGIN`...). *(Lưu ý: 16 tệp JSON trong thư mục `Fixtures/` là bộ fixture quy chuẩn của nhiệm vụ A-001)* | Cả 4 người | Khi cần so khớp loại tin để xử lý | Tên loại tin | Hằng số chuỗi chuẩn xác |
| [`ErrorCodes.cs`](src/LabGuard.Protocol/ErrorCodes.cs) | Danh mục hằng số các mã lỗi canonical (như `ERR_INVALID_VERSION`, `ERR_MALFORMED_JSON`...) | Cả 4 người | Khi báo lỗi về cho đối tác mạng | Tên mã lỗi | Hằng số mã lỗi chuẩn |
| [`Dtos/TelemetryDtos.cs`](src/LabGuard.Protocol/Dtos/TelemetryDtos.cs) | Khuôn mẫu dữ liệu đo đạc tài nguyên (`MetricBatchPayload`, `BatchAckPayload`, `HeartbeatPayload`) | A, B | Khi đóng gói gửi số liệu và khi máy chủ nhận số liệu | Dữ liệu CPU, RAM, Uptime | Đối tượng C# chuẩn để đưa vào payload |
| [`Dtos/HandshakeDtos.cs`](src/LabGuard.Protocol/Dtos/HandshakeDtos.cs) | Khuôn mẫu dữ liệu bắt tay kết nối (`HelloAgentPayload`, `AuthAgentPayload`, `AuthResultPayload`...) | A, B | Khi máy con mới mở socket kết nối vào máy chủ | Thông tin máy, mật khẩu mã hóa | Đối tượng C# chuẩn cho quá trình bắt tay |
| [`Dtos/AdminDtos.cs`](src/LabGuard.Protocol/Dtos/AdminDtos.cs) | Khuôn mẫu dữ liệu giao tiếp quản trị (`LoginPayload`, `LoginResultPayload`, `IncidentAckPayload`...) | B, C | Khi Admin đăng nhập hoặc gửi lệnh thao tác sự cố | Tài khoản, mật khẩu, mã sự cố | Đối tượng C# chuẩn giao tiếp Admin-Central |
| [`Dtos/EventDtos.cs`](src/LabGuard.Protocol/Dtos/EventDtos.cs) | Khuôn mẫu sự kiện cảnh báo sự cố gửi tới Admin | B, C | Khi Central phát hiện sự cố và gửi thông báo | Dữ liệu cảnh báo sự cố | Đối tượng sự kiện C# |
| [`Serialization/ProtocolSerializer.cs`](src/LabGuard.Protocol/Serialization/ProtocolSerializer.cs) | Bộ tuần tự hóa/giải tuần tự hóa JSON UTF-8 tuân thủ quy tắc phân biệt hoa thường của Wire Contract | Cả 4 người | Khi chuyển đổi giữa đối tượng C# và chuỗi JSON byte | Đối tượng C# hoặc mảng byte JSON | Mảng byte JSON hoặc đối tượng C# |
| [`LabGuard.Protocol.csproj`](src/LabGuard.Protocol/LabGuard.Protocol.csproj) | Cấu hình dự án thư viện giao thức (.NET 9.0) | .NET SDK | Khi biên dịch project | Cấu hình SDK | Thư viện .dll dùng chung |

---

## PHẦN 2: ỨNG DỤNG MÁY TRẠM (`src/LabGuard.Agent/`) — SINH VIÊN A (A-001 ĐẾN A-004 ĐÃ XONG / NGHIỆM THU)

Chương trình chạy ngầm trên máy con sinh viên. Toàn bộ các nhiệm vụ A-001, A-002, A-003 và A-004 đã được nghiệm thu chính thức (18/18 xUnit cases PASS, đối soát tham chiếu Windows Reference 10/10 PASS). Các tính năng reconnect, ReplayBuffer, ServiceMonitor... do **Sinh viên A tự tay viết** từ A-005 trở đi.

| File | Nói đơn giản nó làm gì | Ai cần file này | Hiện trạng thực tế |
| :--- | :--- | :--- | :--- |
| [`AgentSocketClient.cs`](src/LabGuard.Agent/AgentSocketClient.cs) | Quản lý TcpClient + hàng đợi Bounded Channel 256 gửi tuần tự; Single-Writer Loop; xử lý lỗi ghi mạng; DisposeAsync đúng | Sinh viên A | ✅ **A-003 ĐÃ XONG:** 16/16 tests PASS. Public ctor cố định 256; `CreateOutbox` factory nội bộ. |
| [`MetricsCollector.cs`](src/LabGuard.Agent/MetricsCollector.cs) | Thu thập CPU (GetSystemTimes delta), RAM (GlobalMemoryStatusEx), Uptime (GetTickCount64) qua P/Invoke Win32 API | Sinh viên A | ✅ **A-004 ĐÃ XONG:** Nghiệm thu PASS — 10/10 xUnit cases PASS, đạt yêu cầu WBS < 100 ms (phiên validation 10 mẫu max = 0.616 ms; mẫu warm-up trước đó ghi nhận 1.137 ms). |
| [`MetricsService.cs`](src/LabGuard.Agent/MetricsService.cs) | Hẹn giờ 5s (PeriodicTimer) lấy số liệu đóng gói METRIC_BATCH gửi qua AgentSocketClient | Sinh viên A | ✅ **A-004 ĐÃ XONG:** Nghiệm thu PASS — BootId ổn định, BatchId duy nhất, 4/4 xUnit cases PASS. |
| [`HeartbeatService.cs`](src/LabGuard.Agent/HeartbeatService.cs) | Hẹn giờ 5s (PeriodicTimer) phát tín hiệu nhịp tim HEARTBEAT với SequenceNumber tăng dần | Sinh viên A | ✅ **A-004 ĐÃ XONG:** Nghiệm thu PASS — Gửi qua AgentSocketClient (kênh outbox nội bộ), 4/4 xUnit cases PASS. |
| [`Program.cs`](src/LabGuard.Agent/Program.cs) | Điểm khởi đầu chương trình Console (khung hiển thị trạng thái hiện tại của Agent) | Sinh viên A | ⚪ **KHUNG SƯỜN TÍCH HỢP:** Sẽ kết nối Central khi B-001+ hoàn tất. |
| [`LabGuard.Agent.csproj`](src/LabGuard.Agent/LabGuard.Agent.csproj) | Cấu hình dự án máy trạm Agent, tham chiếu tới `LabGuard.Protocol` | .NET SDK | Giữ nguyên để toàn bộ Solution biên dịch thành công. |

> **Các file Sinh viên A sẽ tự tay tạo mới khi làm việc tiếp theo:**
> - `AgentSocketClient.cs` mở rộng (Task A-005 [CHƯA LÀM — TIẾP THEO]: Kết nối lại Exponential Backoff + Jitter)
> - `ReplayBuffer.cs` (Task A-006: Bộ đệm giữ 50 gói tin khi mất mạng)
> - `ServiceMonitor.cs` (Task A-007: Kiểm tra cổng dịch vụ 80 và 8080)

---

## PHẦN 3: MÁY CHỦ TRUNG TÂM (`src/LabGuard.Central/`) — SINH VIÊN B (B-001 ĐÃ XONG; B-002 LÀ VIỆC TIẾP THEO)

Máy chủ trung tâm điều hành toàn bộ hệ thống. Nhiệm vụ B-001 đã hoàn thành và nghiệm thu qua PR #7. Sinh viên B sẽ tiếp tục phát triển các tính năng tiếp theo.

| File | Nói đơn giản nó làm gì | Ai cần file này | Hiện trạng thực tế |
| :--- | :--- | :--- | :--- |
| [`CentralServer.cs`](src/LabGuard.Central/CentralServer.cs) | Khởi tạo TcpListener cổng 5000, đón nhận đa client bất đồng bộ, đọc gói tin qua FrameCodec, xử lý lỗi/ngắt kết nối an toàn | Sinh viên B | ✅ **B-001 ĐÃ XONG (PR #7):** 9/9 tests PASS trong `Central.Tests`. |
| [`Program.cs`](src/LabGuard.Central/Program.cs) | Điểm khởi đầu chương trình Console máy chủ Central, tích hợp CentralServer và xử lý Ctrl+C | Sinh viên B | ✅ **ĐÃ CẬP NHẬT (PR #7):** Chạy máy chủ lắng nghe cổng 5000. |
| [`LabGuard.Central.csproj`](src/LabGuard.Central/LabGuard.Central.csproj) | Cấu hình dự án Central Server, tham chiếu `LabGuard.Protocol` và `Microsoft.Data.SqlClient` | .NET SDK | Giữ nguyên để toàn bộ Solution biên dịch thành công. |

> **Các file Sinh viên B sẽ tự tay tạo mới / hoàn thiện khi làm việc tiếp theo:**
> - `ClientConnection.cs` (Task B-002 [CHƯA LÀM — TIẾP THEO]: Bắt tay HELLO/AUTH; Task B-004: Phân quyền RBAC)
> - `LivenessMonitor.cs` & `MonotonicLivenessRegistry.cs` (Task B-003: Theo dõi nhịp tim và báo OFFLINE sau 20s)
> - `DatabaseRepository.cs` (Task B-005 [ĐANG LÀM]: Lưu số liệu máy vào SQL Server; Task B-007: Vòng đời sự cố và chống sửa đè OCC)
> - `RuleEngine.cs` (Task B-006: Bộ quy tắc phát hiện quá tải CPU/RAM)

---

## PHẦN 4: GIAO DIỆN QUẢN TRỊ (`src/LabGuard.Admin/`) — SINH VIÊN C (C-001 ĐANG LÀM; C-002 LÀ VIỆC TIẾP THEO)

Ứng dụng Windows Forms dành cho người trực phòng máy. Nhiệm vụ C-001 layout 0.1 đã merge qua PR #4. Sinh viên C sẽ tiếp tục hoàn thiện giao diện và kết nối mạng.

| File | Nói đơn giản nó làm gì | Ai cần file này | Hiện trạng thực tế |
| :--- | :--- | :--- | :--- |
| [`DashboardForm.cs`](src/LabGuard.Admin/DashboardForm.cs) | Khung giao diện WinForms 0.1: 2 bảng DataGridView (danh sách máy, sự cố), cụm nút bấm tác vụ, DoubleBuffered | Sinh viên C | 🟡 **C-001 ĐANG LÀM (PR #4):** Layout 0.1 đã merge (`docs/evidence/admin_dashboard_c001.png`), đang chờ nghiệm thu toàn diện UI & data binding. |
| [`Program.cs`](src/LabGuard.Admin/Program.cs) | Điểm khởi đầu chuẩn của Windows Forms: cấu hình DPI, ApplicationConfiguration và mở `DashboardForm` | Sinh viên C | ✅ **ĐÃ CẬP NHẬT (PR #4):** Khởi chạy DashboardForm chuẩn mực. |
| [`LabGuard.Admin.csproj`](src/LabGuard.Admin/LabGuard.Admin.csproj) | Cấu hình dự án Windows Forms, tham chiếu `LabGuard.Protocol` | .NET SDK | Giữ nguyên để toàn bộ Solution biên dịch thành công. |

> **Các file Sinh viên C sẽ tự tay tạo mới khi làm việc tiếp theo:**
> - `AdminClient.cs` (Task C-002 [CHƯA LÀM — TIẾP THEO]: Kết nối ngầm tới máy chủ Central, đăng nhập và nhận thông báo)

---

## PHẦN 5: CÔNG CỤ GIẢ LẬP TẢI (`src/LabGuard.Simulator/`)

| File | Nói đơn giản nó làm gì | Hiện trạng thực tế |
| :--- | :--- | :--- |
| [`Program.cs`](src/LabGuard.Simulator/Program.cs) | Khung Console mẫu in thông báo chưa hiện thực | ⚪ **KHUNG SƯỜN TỐI THIỂU:** Chưa có logic sinh tải (kế hoạch D-006 dự kiến). |
| [`LabGuard.Simulator.csproj`](src/LabGuard.Simulator/LabGuard.Simulator.csproj) | Cấu hình dự án giả lập | Giữ nguyên để Solution biên dịch. |

---

## PHẦN 6: CƠ SỞ DỮ LIỆU SQL SERVER (`database/`) — SINH VIÊN B (B-005 ĐANG LÀM / SCHEMA.SQL ĐÃ MERGE)

| File | Nói đơn giản nó làm gì | Hiện trạng thực tế |
| :--- | :--- | :--- |
| [`schema.sql`](database/schema.sql) | Kịch bản DDL tạo CSDL và các bảng ProcessedBatches, DeviceSessions, MetricSamples | 🟡 **B-005 ĐANG LÀM (PR #5):** Kịch bản DDL đã merge vào repository; phần code C# persistence và kiểm thử SQL Server chưa nghiệm thu. |
| [`seed.sql`](database/seed.sql) | Kịch bản nạp tài khoản mẫu ban đầu | ⚪ **KHUNG CHỜ (PLACEHOLDER):** Sinh viên B sẽ hoàn thiện khi làm phần xác thực người dùng. |

---

## PHẦN 7: CÁC DỰ ÁN KIỂM THỬ TỰ ĐỘNG (`tests/`) & CÔNG CỤ (`tools/`)

| Thư mục / File | Nói đơn giản nó làm gì | Hiện trạng thực tế |
| :--- | :--- | :--- |
| `tests/LabGuard.Protocol.Tests/` | Các bài kiểm tra tự động xác thực việc cắt khung tin 4 byte (`FrameCodecTests.cs`) và 16 bản tin mẫu canonical (`WireContractAndFixturesTests.cs`, `ProtocolSpecificationTests.cs`). 16 tệp JSON trong `Fixtures/` là bộ fixture quy chuẩn của nhiệm vụ A-001. | ✅ **Protocol tests PASS:** 107/107 tests đạt 100% (A-001 & A-002). |
| `tests/LabGuard.Agent.Tests/` | Bộ kiểm thử Agent: `AgentSocketClientTests.cs` (16 tests A-003), `MetricsCollectorTests.cs`, `MetricsServiceTests.cs`, `HeartbeatServiceTests.cs` (18 tests A-004), và `FakeTcpServer.cs`. | ✅ **Agent tests PASS:** 34/34 tests đạt 100% (A-003 & A-004; đối soát Windows Reference 10/10 PASS). |
| `tests/LabGuard.Central.Tests/` | Bộ kiểm thử máy chủ Central: kiểm tra mở cổng 5000, nhiều client kết nối đồng thời, phân mảnh/ghép khung tin, đóng socket an toàn (`CentralServerTests.cs`). | ✅ **Central tests PASS:** 9/9 tests đạt 100% (PR #7, B-001). |
| `tests/LabGuard.MockHarness.Tests/` | Bộ kiểm thử tích hợp công cụ giả lập: kiểm tra FakeAgent gửi heartbeat/batch, FakeCentral phản hồi ACK chuẩn và bắt lỗi gói tin sai (`FakeAgentIntegrationTests.cs`, `FakeCentralIntegrationTests.cs`). | ✅ **MockHarness tests PASS:** 15/15 tests đạt 100% (PR #9). |
| `tools/TcpEcho/` | Chương trình Console TCP Echo Server đơn giản thử nghiệm socket TCP (Task D-002: đa client, shutdown an toàn). | ✅ **D-002 ĐÃ XONG (PR #8):** Đã nghiệm thu chính thức (`docs/evidence/D002_TCP_ECHO_VERIFICATION.md`). |
| `tools/mock_harness/` | Bộ công cụ giả lập kiểm thử độc lập gồm `FakeAgent` (máy con giả lập) và `FakeCentral` (máy chủ giả lập phản hồi ACK). | ✅ **ĐÃ CÓ (PR #9):** Công cụ bổ trợ kiểm thử giao thức độc lập, không thay thế nhiệm vụ D-003. |
| `tools/validation/test_frame_codec/` | Công cụ console độc lập giúp kiểm tra nhanh khung tin TCP của A-002 | ✅ **ĐÃ CÓ:** Phục vụ kiểm tra độc lập việc đóng/mở khung tin 4 byte (10/10 scenarios passed). |

---

## PHẦN 8: TÀI LIỆU ĐẶC TẢ KỸ THUẬT & HƯỚNG DẪN (`docs/`)

| Thư mục / File | Nói đơn giản nó làm gì | Hiện trạng thực tế |
| :--- | :--- | :--- |
| `docs/pdf/` | Thư mục chứa 5 cuốn cẩm nang PDF chuyên biệt kèm mã nguồn LaTeX và kịch bản `build.ps1` | ✅ **ĐÃ CÓ (87 trang):** Cẩm nang tổng thể và 4 cẩm nang riêng cho A, B, C, D. |
| [`docs/spec/PROTOCOL_SPEC.md`](docs/spec/PROTOCOL_SPEC.md) | Đặc tả kỹ thuật wire framing và danh mục 69 loại bản tin chuẩn | ✅ **ĐÃ CÓ:** Nền tảng giao thức đã đóng băng của A-001/A-002. |
| [`docs/spec/CONTRACT_MATRIX.md`](docs/spec/CONTRACT_MATRIX.md) | Ma trận đối soát giao ước kỹ thuật và 16 canonical fixtures | ✅ **ĐÃ CÓ:** Phục vụ nghiệm thu kiểm thử. |
| [`docs/spec/ARCHITECTURE_DECISIONS.md`](docs/spec/ARCHITECTURE_DECISIONS.md) | Sổ ghi nhận 19 quyết định kiến trúc kỹ thuật chuẩn mực (ADR) | ✅ **ĐÃ CÓ:** Tham số hàng đợi 256/1000/100, heartbeat 5s, timeout 20s, OCC... |
| `docs/internal/` | Tài liệu quản trị nội bộ nhóm (`WBS_TASKS.csv`, `DEPENDENCY_MAP.md`, `OWNERSHIP_MATRIX.md`, `README.md`) | ✅ **ĐÃ CÓ:** Điều phối tiến độ và quyền sở hữu mã nguồn. |

