# BẢN ĐỒ TOÀN BỘ TẬP TIN DỰ ÁN (FILE MAP)

> **Tài liệu tra cứu cấu trúc mã nguồn thực tế của dự án LabGuard.**  
> Hiện tại, dự án đang ở giai đoạn bắt đầu phân công cho 4 sinh viên thực hiện.  
> **Đã nghiệm thu hoàn thành: thư viện giao thức (`LabGuard.Protocol` — A-001, A-002) và kết nối TCP Agent (`LabGuard.Agent` — A-003).**  
> Các phân hệ Central, Admin, Database hiện tại chỉ giữ lại khung sườn tối thiểu (skeleton).  
>
> **Ký hiệu hiện trạng:**  
> - ✅ **ĐÃ XONG & NGHIỆM THU:** Mã nguồn chính thức đã hoàn thành và kiểm thử 100% pass (A-001, A-002, A-003).  
> - ⚪ **CHƯA LÀM (KHUNG SƯỜN TỐI THIỂU):** Khung dự án chờ sinh viên tự tay lập trình theo nhiệm vụ được giao.

---

## MỤC LỤC

1. [Phần 1: Thư viện giao thức dùng chung (`src/LabGuard.Protocol/`) — ĐÃ HOÀN THÀNH (A-001 & A-002)](#phần-1-thư-viện-giao-thức-dùng-chung-srclabguardprotocol--đã-hoàn-thành-a-001--a-002)
2. [Phần 2: Ứng dụng máy trạm (`src/LabGuard.Agent/`) — Sinh viên A (A-003 ĐÃ XONG, A-004+ chưa làm)](#phần-2-ứng-dụng-máy-trạm-srclabguardagent--sinh-viên-a-a-003-đã-xong-a-004-chưa-làm)
3. [Phần 3: Máy chủ trung tâm (`src/LabGuard.Central/`) — Sinh viên B (Bắt đầu từ B-001)](#phần-3-máy-chủ-trung-tâm-srclabguardcentral--sinh-viên-b-bắt-đầu-từ-b-001)
4. [Phần 4: Giao diện quản trị (`src/LabGuard.Admin/`) — Sinh viên C (Bắt đầu từ C-001)](#phần-4-giao-diện-quản-trị-srclabguardadmin--sinh-viên-c-bắt-đầu-từ-c-001)
5. [Phần 5: Công cụ giả lập tải (`src/LabGuard.Simulator/`)](#phần-5-công-cụ-giả-lập-tải-srclabguardsimulator)
6. [Phần 6: Cơ sở dữ liệu SQL Server (`database/`) — Sinh viên B (Task B-005)](#phần-6-cơ-sở-dữ-liệu-sql-server-database--sinh-viên-b-task-b-005)
7. [Phần 7: Các dự án kiểm thử tự động (`tests/`) & Công cụ (`tools/`)](#phần-7-các-dự-án-kiểm-thử-tự-động-tests--công-cụ-tools)
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

## PHẦN 2: ỨNG DỤNG MÁY TRẠM (`src/LabGuard.Agent/`) — SINH VIÊN A (A-003 ĐÃ XONG)

Chương trình chạy ngầm trên máy con sinh viên. A-003 đã hoàn thành. Các tính năng thu thập metrics, reconnect, ReplayBuffer... do **Sinh viên A tự tay viết** từ A-004 trở đi.

| File | Nói đơn giản nó làm gì | Ai cần file này | Hiện trạng thực tế |
| :--- | :--- | :--- | :--- |
| [`AgentSocketClient.cs`](src/LabGuard.Agent/AgentSocketClient.cs) | Quản lý TcpClient + hàng đợi Bounded Channel 256 gửi tuần tự; Single-Writer Loop; xử lý lỗi ghi mạng; DisposeAsync đúng | Sinh viên A | ✅ **A-003 ĐÃ XONG:** 16/16 tests PASS. Public ctor cố định 256; `CreateOutbox` factory nội bộ. |
| [`Program.cs`](src/LabGuard.Agent/Program.cs) | Khung chương trình Console tối thiểu | Sinh viên A | ⚪ **KHUNG SƯỜN:** A sẽ bổ sung logic khởi động các service sau này. |
| [`LabGuard.Agent.csproj`](src/LabGuard.Agent/LabGuard.Agent.csproj) | Cấu hình dự án máy trạm Agent, tham chiếu tới `LabGuard.Protocol` | .NET SDK | Giữ nguyên để toàn bộ Solution biên dịch thành công. |

> **Các file Sinh viên A sẽ tự tay tạo mới khi làm việc tiếp theo:**  
> - `MetricsCollector.cs` & `MetricsService.cs` (Task A-004 [CHƯA LÀM — TIẾP THEO]: Gọi Win32 API đo CPU, RAM, Uptime)  
> - `AgentSocketClient.cs` mở rộng (Task A-005: Kết nối lại Exponential Backoff)  
> - `ReplayBuffer.cs` (Task A-006: Bộ đệm giữ 50 gói tin khi mất mạng)  
> - `ServiceMonitor.cs` (Task A-007: Kiểm tra cổng dịch vụ 80 và 8080)  

---

## PHẦN 3: MÁY CHỦ TRUNG TÂM (`src/LabGuard.Central/`) — SINH VIÊN B (BẮT ĐẦU TỪ B-001)

Máy chủ trung tâm điều hành toàn bộ hệ thống. Toàn bộ logic mở socket lắng nghe, phân quyền, xử lý CSDL và quy tắc sự cố sẽ do **Sinh viên B tự tay viết**.

| File | Nói đơn giản nó làm gì | Ai cần file này | Hiện trạng thực tế |
| :--- | :--- | :--- | :--- |
| [`Program.cs`](src/LabGuard.Central/Program.cs) | Khung chương trình Console tối thiểu: in thông báo chờ Sinh viên B bắt đầu thực hiện nhiệm vụ B-001 | Sinh viên B | ⚪ **KHUNG SƯỜN TỐI THIỂU:** B sẽ bắt đầu từ file này để mở cổng TCP Socket lắng nghe. |
| [`LabGuard.Central.csproj`](src/LabGuard.Central/LabGuard.Central.csproj) | Cấu hình dự án Central Server, tham chiếu `LabGuard.Protocol` và `Microsoft.Data.SqlClient` | .NET SDK | Giữ nguyên để toàn bộ Solution biên dịch thành công. |

> **Các file Sinh viên B sẽ tự tay tạo mới khi làm việc:**  
> - `CentralServer.cs` (Task B-001: Mở cổng TCP 5000 lắng nghe kết nối)  
> - `ClientConnection.cs` (Task B-002: Bắt tay HELLO/AUTH; Task B-004: Phân quyền RBAC)  
> - `LivenessMonitor.cs` & `MonotonicLivenessRegistry.cs` (Task B-003: Theo dõi nhịp tim và báo OFFLINE sau 20s)  
> - `DatabaseRepository.cs` (Task B-005: Lưu số liệu máy; Task B-007: Vòng đời sự cố và chống sửa đè OCC)  
> - `RuleEngine.cs` (Task B-006: Bộ quy tắc phát hiện quá tải CPU/RAM)  

---

## PHẦN 4: GIAO DIỆN QUẢN TRỊ (`src/LabGuard.Admin/`) — SINH VIÊN C (BẮT ĐẦU TỪ C-001)

Ứng dụng Windows Forms dành cho người trực phòng máy. Toàn bộ giao diện và kết nối mạng sẽ do **Sinh viên C tự tay xây dựng**.

| File | Nói đơn giản nó làm gì | Ai cần file này | Hiện trạng thực tế |
| :--- | :--- | :--- | :--- |
| [`Program.cs`](src/LabGuard.Admin/Program.cs) | Điểm khởi đầu chuẩn của Windows Forms: khởi tạo và mở `DashboardForm` | Sinh viên C | ⚪ **KHUNG SƯỜN TỐI THIỂU** |
| [`DashboardForm.cs`](src/LabGuard.Admin/DashboardForm.cs) | Cửa sổ Windows Forms trống chứa nhãn thông báo đang chờ Sinh viên C bắt đầu thiết kế | Sinh viên C | ⚪ **KHUNG SƯỜN TỐI THIỂU:** C sẽ tự tay thiết kế bảng thiết bị, bảng sự cố và các nút bấm trong C-001. |
| [`LabGuard.Admin.csproj`](src/LabGuard.Admin/LabGuard.Admin.csproj) | Cấu hình dự án Windows Forms, tham chiếu `LabGuard.Protocol` | .NET SDK | Giữ nguyên để toàn bộ Solution biên dịch thành công. |

> **Các file Sinh viên C sẽ tự tay tạo mới khi làm việc:**  
> - `AdminClient.cs` (Task C-002: Kết nối ngầm tới máy chủ Central, đăng nhập và nhận thông báo)  

---

## PHẦN 5: CÔNG CỤ GIẢ LẬP TẢI (`src/LabGuard.Simulator/`)

| File | Nói đơn giản nó làm gì | Hiện trạng thực tế |
| :--- | :--- | :--- |
| [`Program.cs`](src/LabGuard.Simulator/Program.cs) | Khung Console mẫu in thông báo chưa hiện thực | ⚪ **KHUNG SƯỜN TỐI THIỂU:** Chưa có logic sinh tải. |
| [`LabGuard.Simulator.csproj`](src/LabGuard.Simulator/LabGuard.Simulator.csproj) | Cấu hình dự án giả lập | Giữ nguyên để Solution biên dịch. |

---

## PHẦN 6: CƠ SỞ DỮ LIỆU SQL SERVER (`database/`) — SINH VIÊN B (TASK B-005)

| File | Nói đơn giản nó làm gì | Hiện trạng thực tế |
| :--- | :--- | :--- |
| [`schema.sql`](database/schema.sql) | Kịch bản tạo CSDL và các bảng trong SQL Server | ⚪ **KHUNG CHỜ (PLACEHOLDER):** Sinh viên B sẽ tự tay viết kịch bản này khi thực hiện Task B-005. |
| [`seed.sql`](database/seed.sql) | Kịch bản nạp tài khoản mẫu ban đầu | ⚪ **KHUNG CHỜ (PLACEHOLDER):** Sinh viên B sẽ tự tay viết kịch bản này khi thực hiện Task B-005. |

---

## PHẦN 7: CÁC DỰ ÁN KIỂM THỬ TỰ ĐỘNG (`tests/`) & CÔNG CỤ (`tools/`)

| Thư mục / File | Nói đơn giản nó làm gì | Hiện trạng thực tế |
| :--- | :--- | :--- |
| `tests/LabGuard.Protocol.Tests/` | Các bài kiểm tra tự động xác thực việc cắt khung tin 4 byte (`FrameCodecTests.cs`) và 16 bản tin mẫu canonical (`WireContractAndFixturesTests.cs`, `ProtocolSpecificationTests.cs`). 16 tệp JSON trong `Fixtures/` là bộ fixture quy chuẩn của nhiệm vụ A-001. | ✅ **Protocol tests PASS:** Kiểm thử chính thức của 2 nhiệm vụ đã hoàn thành (A-001 & A-002). |
| `tests/LabGuard.Agent.Tests/` | Bộ kiểm thử A-003: `AgentSocketClientTests.cs` (16/16 PASS) và `FakeTcpServer.cs` (máy chủ TCP giả lập loopback dùng `FrameCodec`). | ✅ **A-003 tests PASS:** 16/16 kiểm thử kết nối TCP, hàng đợi, backpressure, writer failure, DisposeAsync. |
| `tests/LabGuard.Central.Tests/` | Dự án kiểm thử cho Central | ⚪ **KHUNG CHỜ (SKELETON):** Sinh viên B sẽ viết các bài test khi thực hiện B-001+. |
| `tools/validation/test_frame_codec/` | Công cụ console độc lập giúp kiểm tra nhanh khung tin TCP của A-002 | ✅ **ĐÃ CÓ:** Phục vụ kiểm tra độc lập việc đóng/mở khung tin 4 byte. |

---

## PHẦN 8: TÀI LIỆU ĐẶC TẢ KỸ THUẬT & HƯỚNG DẪN (`docs/`)

| Thư mục / File | Nói đơn giản nó làm gì | Hiện trạng thực tế |
| :--- | :--- | :--- |
| `docs/pdf/` | Thư mục chứa 5 cuốn cẩm nang PDF chuyên biệt kèm mã nguồn LaTeX và kịch bản `build.ps1` | ✅ **ĐÃ CÓ (87 trang):** Cẩm nang tổng thể và 4 cẩm nang riêng cho A, B, C, D. |
| [`docs/spec/PROTOCOL_SPEC.md`](docs/spec/PROTOCOL_SPEC.md) | Đặc tả kỹ thuật wire framing và danh mục 69 loại bản tin chuẩn | ✅ **ĐÃ CÓ:** Nền tảng giao thức đã đóng băng của A-001/A-002. |
| [`docs/spec/CONTRACT_MATRIX.md`](docs/spec/CONTRACT_MATRIX.md) | Ma trận đối soát giao ước kỹ thuật và 16 canonical fixtures | ✅ **ĐÃ CÓ:** Phục vụ nghiệm thu kiểm thử. |
| [`docs/spec/ARCHITECTURE_DECISIONS.md`](docs/spec/ARCHITECTURE_DECISIONS.md) | Sổ ghi nhận 19 quyết định kiến trúc kỹ thuật chuẩn mực (ADR) | ✅ **ĐÃ CÓ:** Tham số hàng đợi 256/1000/100, heartbeat 5s, timeout 20s, OCC... |
| `docs/internal/` | Tài liệu quản trị nội bộ nhóm (`WBS_TASKS.csv`, `DEPENDENCY_MAP.md`, `OWNERSHIP_MATRIX.md`, `README.md`) | ✅ **ĐÃ CÓ:** Điều phối tiến độ và quyền sở hữu mã nguồn. |

