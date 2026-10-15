# HƯỚNG DẪN BẮT ĐẦU CHO THÀNH VIÊN NHÓM (START HERE)

> **Chào mừng bạn đến với đồ án LabGuard!**
> Tài liệu này được viết riêng cho 4 bạn sinh viên năm 2. Bạn không cần đọc thuật ngữ phức tạp hay hàng chục mã công việc. Chỉ cần tìm đúng tên mình (A, B, C hoặc D) dưới đây!

---

## 📚 BỘ TÀI LIỆU PDF CHUYÊN BIỆT CHO 4 THÀNH VIÊN

Mỗi bạn có riêng một cuốn cẩm nang chi tiết kèm hình ảnh minh họa, hướng dẫn từng bước và bộ câu hỏi vấn đáp:
* 📖 **Cả nhóm:** [`docs/pdf/LabGuard_Tong_Quan_A-Z.pdf`](docs/pdf/LabGuard_Tong_Quan_A-Z.pdf) — Sổ tay toàn diện (Master Book): Luồng mạng, giao ước 6 trường, ma trận RACI và 30 câu hỏi bảo vệ đồ án.
* 📕 **Sinh viên A:** [`docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_A.pdf`](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_A.pdf) — Sổ tay Agent & Protocol: Đo CPU/RAM thật, hàng đợi BoundedChannel, ReplayBuffer và 20 câu hỏi vấn đáp.
* 📗 **Sinh viên B:** [`docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_B.pdf`](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_B.pdf) — Sổ tay Central & Database: TCP Server đa luồng, SQL Server, RuleEngine, OCC và 20 câu hỏi vấn đáp.
* 📘 **Sinh viên C:** [`docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_C.pdf`](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_C.pdf) — Sổ tay Admin WinForms: Thiết kế DataGridView, an toàn luồng UI `BeginInvoke`, RBAC và 20 câu hỏi vấn đáp.
* 📙 **Sinh viên D:** [`docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_D.pdf`](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_D.pdf) — Sổ tay Gateway & Collector: Tầng mở rộng Scale Layer, kiểm thử Wireshark và 20 câu hỏi vấn đáp.

---

> [!IMPORTANT]
> **NGUYÊN TẮC QUAN TRỌNG: MÃ NGUỒN PHẢI PHẢN ÁNH ĐÚNG TIẾN ĐỘ THẬT CỦA NHÓM!**
>
> Toàn bộ mã nguồn làm sẵn từ trước đã được dọn sạch khỏi các phân hệ chưa bắt đầu. Các dự án phản ánh chính xác tiến độ thực tế đã nghiệm thu và merge vào nhánh `main`.
>
> **Hiện trạng chính thức toàn nhóm lúc này (sau khi merge PR #4, #5, #7, #8, #9):**
> * **Sinh viên A:** Đã nghiệm thu chính thức (`DONE / ACCEPTED / CLOSED`) 4 việc đầu tiên (`A-001`, `A-002`, `A-003`, `A-004`). Bộ test `Agent.Tests` đạt 34/34 PASS (A-004 có 18/18 xUnit cases + đối soát Windows Reference 10/10 PASS). Tiếp theo: `A-005` (Reconnect).
> * **Sinh viên B:** Đã hoàn thành và nghiệm thu `B-001` qua PR #7 (`CentralServer.cs` mở cổng TCP 5000, 9/9 `Central.Tests` PASS). Nhiệm vụ `B-005` đang làm (PR #5 đã merge `database/schema.sql` DDL; phần C# persistence và SQL Server chưa nghiệm thu). Tiếp theo: `B-002` (Bắt tay HELLO/AUTH) và hoàn thiện `B-005`.
> * **Sinh viên C:** Nhiệm vụ `C-001` đang làm (PR #4 đã merge layout UI 0.1 trên `DashboardForm.cs`; nghiệm thu toàn diện UI & data binding đang chờ thực hiện). Tiếp theo: `C-002` (AdminClient kết nối Socket Central).
> * **Sinh viên D:** Đã hoàn thành và nghiệm thu `D-002` qua PR #8 (`tools/TcpEcho/Program.cs`). Đã merge Mock Harness bổ trợ (`tools/mock_harness/`, PR #9). Nhiệm vụ `D-001` chưa có báo cáo nghiệm thu; tiếp theo: `D-003` (TCP Relay).

---

## NẾU MỚI TẢI MÃ NGUỒN VỀ, BẠN CHỈ CẦN HIỂU 4 CÂU NÀY:

1. **Sinh viên A** phụ trách chương trình chạy ngầm trên máy con (`LabGuard.Agent`) và thư viện giao thức (`LabGuard.Protocol`).
2. **Sinh viên B** phụ trách máy chủ trung tâm (`LabGuard.Central`) và CSDL SQL Server (`database/`).
3. **Sinh viên C** phụ trách màn hình giao diện cho giáo viên và người trực phòng máy (`LabGuard.Admin`).
4. **Sinh viên D** phụ trách phần mở rộng sau này khi phòng máy có hàng trăm máy tính (`Gateway & Collector`).

---

## 👤 SINH VIÊN A — MÁY TRẠM CẦN GIÁM SÁT (AGENT)

### 1. Mục đích của phân hệ:
Chương trình chạy ngầm trên từng máy tính trong phòng máy, định kỳ đo CPU thật, RAM thật, thời gian máy bật, gửi nhịp tim báo máy còn sống (mỗi 5 giây) và tự động kết nối lại khi mạng bị rớt.

### 2. Thư mục của bạn:
* `src/LabGuard.Agent/` (mã nguồn máy trạm)
* `src/LabGuard.Protocol/` (thư viện giao thức đã hoàn thành — bạn dùng chung)
* `tests/LabGuard.Agent.Tests/` (nơi viết bài kiểm thử cho Agent)

### 3. Hiện trạng mã nguồn trong thư mục của bạn:
* Hiện tại `src/LabGuard.Agent/` đã có:
  - [`AgentSocketClient.cs`](src/LabGuard.Agent/AgentSocketClient.cs) (Task A-003: Đã nghiệm thu `DONE / ACCEPTED`)
  - [`MetricsCollector.cs`](src/LabGuard.Agent/MetricsCollector.cs), [`MetricsService.cs`](src/LabGuard.Agent/MetricsService.cs), [`HeartbeatService.cs`](src/LabGuard.Agent/HeartbeatService.cs) (Task A-004: **Đã nghiệm thu chính thức `DONE / ACCEPTED`**)
  - [`Program.cs`](src/LabGuard.Agent/Program.cs) (Khung sườn hiển thị trạng thái hiện tại)
* Bạn sẽ **tự tay lập trình** các tính năng tiếp theo:
  1. `AgentSocketClient.cs` mở rộng (Task A-005: Tự động kết nối lại với Exponential Backoff + Jitter)
  2. `ReplayBuffer.cs` (Task A-006: Bộ đệm giữ 50 gói tin khi mất mạng)
  3. `ServiceMonitor.cs` (Task A-007: Thử bắt tay TCP kiểm tra cổng dịch vụ 80 và 8080)

### 4. Tình trạng công việc thực tế của bạn:

| Việc của bạn | Trạng thái | Bạn cần làm gì? |
| :--- | :---: | :--- |
| **Đóng khung gói tin TCP 4 byte (A-002)** | **XONG** | Đã viết xong `FrameCodec.cs`, test tự động 100% pass, đã nghiệm thu. |
| **16 bản tin JSON mẫu chuẩn (A-001)** | **XONG** | Đã đóng băng giao ước `WireEnvelope.cs` và 16 fixtures, đã nghiệm thu. |
| **Làm kết nối TCP & hàng đợi gửi dữ liệu (A-003)** | **XONG** | Đã tạo `AgentSocketClient.cs`, BoundedChannel 256, Single Writer Loop, `CreateOutbox` factory, 16/16 tests pass. |
| **Đo CPU, RAM và Uptime thật (A-004)** | **XONG** | Đã tạo `MetricsCollector.cs`, `MetricsService.cs`, `HeartbeatService.cs` (Đã nghiệm thu chính thức: 18/18 xUnit tests PASS + đối soát Windows Reference 10/10 PASS). |
| **Tự động kết nối lại khi rớt mạng (A-005)** | **CHƯA LÀM — TIẾP THEO** | Việc tiếp theo của A: Thêm thuật toán Exponential Backoff (1s, 2s, 4s... 30s) vào `AgentSocketClient.cs`. |
| **Bộ nhớ giữ dữ liệu chưa gửi được (A-006)** | **CHƯA LÀM** | Sẽ làm sau A-005: Tạo `ReplayBuffer.cs` lưu tối đa 50 gói khi mất mạng. |
| **Kiểm tra cổng dịch vụ mạng (A-007)** | **CHƯA LÀM** | Sẽ làm sau A-006: Tạo `ServiceMonitor.cs` thử cổng 80 và 8080. |
| **Xử lý ngắt kết nối đột ngột (A-008)** | **CHƯA LÀM** | Kịch bản thử nghiệm tắt server đột ngột. |
| **Thử nghiệm trên máy ảo Ubuntu (A-009)** | **CHƯA LÀM** | Kịch bản kiểm thử tải trên môi trường thực tế. |

---

## 👤 SINH VIÊN B — MÁY CHỦ TRUNG TÂM & CƠ SỞ DỮ LIỆU (CENTRAL)

### 1. Mục đích của phân hệ:
Máy chủ nhận kết nối từ tất cả máy con, kiểm tra tài khoản, lưu số liệu máy vào CSDL SQL Server, và tự động phát hiện máy nào bị quá tải CPU/RAM hoặc tắt máy đột ngột để tạo cảnh báo sự cố.

### 2. Thư mục của bạn:
* `src/LabGuard.Central/` (mã nguồn máy chủ)
* `database/` (kịch bản CSDL SQL Server)
* `tests/LabGuard.Central.Tests/` (nơi viết bài kiểm thử cho Central)

### 3. Hiện trạng mã nguồn trong thư mục của bạn:
* `src/LabGuard.Central/` đã có [`Program.cs`](src/LabGuard.Central/Program.cs) và [`CentralServer.cs`](src/LabGuard.Central/CentralServer.cs) (Task B-001: Đã nghiệm thu qua PR #7, mở Socket TCP lắng nghe cổng 5000, hỗ trợ đa client, `Central.Tests` 9/9 PASS).
* Thư mục `database/` đã có [`schema.sql`](database/schema.sql) (Task B-005 DDL: Đã merge qua PR #5 tạo bảng `ProcessedBatches`, `MetricSamples`, `Incidents`).
* Bạn sẽ **tự tay tạo mới và hoàn thiện** các file tính năng tiếp theo:
  1. `ClientConnection.cs` (Task B-002: Bắt tay HELLO/AUTH; Task B-004: Phân quyền RBAC)
  2. `LivenessMonitor.cs` & `MonotonicLivenessRegistry.cs` (Task B-003: Nhận nhịp tim và báo OFFLINE sau 20s)
  3. `DatabaseRepository.cs` (Task B-005: Hoàn thiện logic C# lưu trữ telemetry vào SQL Server; Task B-007: Vòng đời sự cố và chống sửa đè OCC)
  4. `RuleEngine.cs` (Task B-006: Bộ quy tắc phát hiện quá tải CPU >= 90%, RAM >= 85%)

### 4. Tình trạng công việc thực tế của bạn:

| Việc của bạn | Trạng thái | Bạn cần làm gì? |
| :--- | :---: | :--- |
| **Mở cổng Socket TCP nhận kết nối (B-001)** | **XONG** | Đã tạo `CentralServer.cs`, mở `TcpListener` port 5000, xử lý đa client, 9/9 tests PASS (PR #7). |
| **Bắt tay và xác thực máy con (B-002)** | **CHƯA LÀM — TIẾP THEO** | Việc tiếp theo của B: Tạo `ClientConnection.cs` tiếp nhận gói tin `HELLO` và `AUTH` từ máy trạm. |
| **Nhận nhịp tim và theo dõi máy tắt (B-003)** | **CHƯA LÀM** | Nhận `HEARTBEAT` 5s, hẹn giờ quét nếu quá 20s không thấy tin thì chuyển sang `OFFLINE`. |
| **Phân quyền tài khoản máy chủ RBAC (B-004)** | **CHƯA LÀM** | Chặn Operator không được gán người sửa, Viewer chỉ được xem. |
| **Lập trình CSDL & Lưu số liệu máy (B-005)** | **ĐANG LÀM** | `schema.sql` đã merge (PR #5). Tiếp tục tạo `DatabaseRepository.cs` lưu số liệu vào SQL Server và kiểm thử. |
| **Bộ quy tắc phát hiện sự cố (RuleEngine) (B-006)** | **CHƯA LÀM** | Tạo `RuleEngine.cs` phát hiện chuỗi mẫu vi phạm CPU/RAM/Service. |
| **Vòng đời sự cố và chống đè dữ liệu OCC (B-007)** | **CHƯA LÀM** | Xử lý trạng thái OPEN -> ACK -> START -> RESOLVE qua `RowVersion`. |
| **Tách CSDL kiểm thử LabGuardTestDb (B-008)** | **CHƯA LÀM** | Tách cấu hình connection string để test không đè vào CSDL chính. |
| **Ghi nhận UserId kỹ thuật viên xử lý (B-009)** | **CHƯA LÀM** | Lưu thông tin người sửa vào bảng sự cố và bảng lịch sử. |

---

## 👤 SINH VIÊN C — MÀN HÌNH QUẢN TRỊ (ADMIN WINFORMS)

### 1. Mục đích của phân hệ:
Màn hình giao diện máy tính dành cho giáo viên và người trực phòng máy, hiển thị bảng danh sách các máy tính trong phòng và bảng các sự cố đang diễn ra, cung cấp các nút bấm để tiếp nhận và xử lý sự cố.

### 2. Thư mục của bạn:
* `src/LabGuard.Admin/`

### 3. Hiện trạng mã nguồn trong thư mục của bạn:
* `src/LabGuard.Admin/` đã có file [`Program.cs`](src/LabGuard.Admin/Program.cs) và [`DashboardForm.cs`](src/LabGuard.Admin/DashboardForm.cs) (Task C-001: Khung giao diện UI 0.1 với 2 bảng DataGridView danh sách máy và sự cố, cụm nút bấm, DoubleBuffered đã merge qua PR #4; ảnh bằng chứng `docs/evidence/admin_dashboard_c001.png`).
* Bạn sẽ **tiếp tục hoàn thiện và lập trình**:
  1. Hoàn tất nghiệm thu toàn diện giao diện C-001
  2. Tạo mới `AdminClient.cs` (Task C-002: Kết nối ngầm Socket TCP tới Central, đăng nhập tài khoản và nhận thông báo mới)

### 4. Tình trạng công việc thực tế của bạn:

> *Lưu ý về danh mục nhiệm vụ:* Phân hệ Admin gồm đúng **10 nhiệm vụ**: `C-001` đến `C-008`, `C-010`, `C-011` (nhiệm vụ `C-009` được thu hồi/dự phòng).

| Việc của bạn | Trạng thái | Bạn cần làm gì? |
| :--- | :---: | :--- |
| **Thiết kế màn hình chính WinForms (C-001)** | **ĐANG LÀM** | Layout 0.1 đã merge (PR #4, `docs/evidence/admin_dashboard_c001.png`); đang chờ nghiệm thu toàn diện UI. |
| **Kết nối ngầm tới máy chủ Central (C-002)** | **CHƯA LÀM — TIẾP THEO** | **Việc tiếp theo của C:** Tạo mới `AdminClient.cs`, mở socket kết nối tới cổng 5000 và gửi lệnh đăng nhập `LOGIN`. |
| **Bảng danh sách máy trạm DataGridView (C-003)** | **CHƯA LÀM** | Đổ dữ liệu thiết bị (DeviceId, Status ONLINE/OFFLINE, CPU, RAM) lên bảng. |
| **Bảng danh sách sự cố DataGridView (C-004)** | **CHƯA LÀM** | Đổ dữ liệu sự cố do Central gửi về lên bảng. |
| **Nút bấm tiếp nhận sự cố ACK (C-005)** | **CHƯA LÀM** | Lập trình sự kiện bấm nút ACK gửi gói tin `INCIDENT_ACK` lên Central. |
| **Nút bấm bắt đầu sửa Start (C-006)** | **CHƯA LÀM** | Lập trình sự kiện bấm nút Start đổi trạng thái sự cố sang IN_PROGRESS. |
| **Nút bấm hoàn tất sửa Resolve (C-007)** | **CHƯA LÀM** | Lập trình sự kiện bấm nút Resolve đóng sự cố. |
| **Thêm cột và nút phân công người sửa (C-008)** | **CHƯA LÀM** | Thêm cột "Người xử lý" và hộp thoại popup chọn kỹ thuật viên khi bấm `Assign`. |
| **Khóa nút bấm theo phân quyền (C-010)** | **CHƯA LÀM** | Khi đăng nhập tài khoản chỉ xem (Viewer), làm mờ các nút thao tác sự cố. |
| **Hộp thoại báo lỗi thân thiện (C-011)** | **CHƯA LÀM** | Hiện popup tiếng Việt khi server từ chối thao tác (xung đột dữ liệu hoặc không đủ quyền). |

---

## 👤 SINH VIÊN D — PHẦN MỞ RỘNG SAU NÀY (GATEWAY & COLLECTOR)

### 1. Mục đích của phân hệ:
Nghiên cứu và chuẩn bị giải pháp mở rộng quy mô khi phòng máy tăng lên hàng trăm máy tính: bộ cân bằng tải phân phối kết nối (`Gateway`) và trạm trung chuyển gom dữ liệu (`Collector`).

### 2. Thư mục của bạn:
* `tools/TcpEcho/` (mã nguồn TCP Echo Server — Task D-002 đã hoàn thành)
* `tools/mock_harness/` (công cụ giả lập `FakeAgent` và `FakeCentral` phục vụ kiểm thử — PR #9)
* `tools/` (nơi viết các chương trình console thử nghiệm socket như TCP Relay D-003)
* `tests/LabGuard.MockHarness.Tests/` (kiểm thử tự động cho mock harness, 15/15 tests PASS)
* `tests/LabGuard.Protocol.Tests/Fixtures/` (xem 16 file mẫu gói tin JSON)
* [FILE_MAP.md](FILE_MAP.md) (đọc để hiểu luồng hệ thống)

### 3. Tình trạng công việc thực tế của bạn:

| Việc của bạn | Trạng thái | Bạn cần làm gì? |
| :--- | :---: | :--- |
| **Chạy thử và hiểu giao thức TCP (D-001)** | **CHƯA LÀM** | Chưa có báo cáo tìm hiểu giao thức (chưa nghiệm thu; đọc hiểu `FrameCodec.cs`, 16 tệp mẫu JSON trong fixtures và wire contract). |
| **Viết thử chương trình TCP Echo đơn giản (D-002)** | **XONG** | Đã hoàn thành `tools/TcpEcho/Program.cs`, xử lý đa client, shutdown an toàn (PR #8). |
| **Thử nghiệm chuyển tiếp gói tin TCP Relay (D-003)** | **CHƯA LÀM — TIẾP THEO** | **Việc tiếp theo của D:** Viết code nhận tin từ client, cho vào BoundedChannel (100) và chuyển tiếp sang một server đích khác. *(Lưu ý: Mock Harness trong `tools/mock_harness/` qua PR #9 là công cụ bổ trợ, không phải D-003).* |
| **Tạo dự án LabGuard.Gateway (D-004 / Dự kiến)** | **CHƯA LÀM** | Bắt tay triển khai bộ cân bằng tải L4 TCP sau khi hệ thống cốt lõi của A, B, C đạt chuẩn. |
| **Tạo dự án LabGuard.Collector (D-005 / Dự kiến)** | **CHƯA LÀM** | Bắt tay triển khai trạm gom dữ liệu phân tán dồn luồng về Central. |
| **Viết logic sinh tải cho Simulator (D-006 / Dự kiến)** | **CHƯA LÀM** | Viết code giả lập 20–50 máy trạm gửi số liệu đồng thời để thử tải hệ thống. |

---

## 2. BẢNG THEO DÕI CÔNG VIỆC TRÊN FILE EXCEL

Cả nhóm mở file **[`LabGuard_Task_Tracker.xlsx`](LabGuard_Task_Tracker.xlsx)** để xem việc hàng ngày:
* **Sheet `BẮT ĐẦU`**: Mở đầu ngày, xem hôm nay mình và các bạn cần làm gì.
* **Sheet `A - MÁY TRẠM`**: Danh sách toàn bộ việc của A (A-001, A-002, A-003, A-004 đã Xong / Nghiệm thu; A-005 là Chưa làm — Tiếp theo; A-006+ là Chưa làm).
* **Sheet `B - MÁY CHỦ`**: Danh sách toàn bộ việc của B (B-001 đã Xong; B-005 đang làm; B-002 là Chưa làm — Tiếp theo).
* **Sheet `C - GIAO DIỆN`**: Danh sách toàn bộ việc của C (C-001 đang làm — Layout 0.1; C-002 là Chưa làm — Tiếp theo).
* **Sheet `D - MỞ RỘNG`**: Danh sách toàn bộ việc của D (D-002 đã Xong; Mock Harness đã merge; D-001 chưa nghiệm thu; D-003 là Chưa làm — Tiếp theo).


---

## 3. QUY TRÌNH 8 BƯỚC LÀM VIỆC VỚI GIT (ĐỂ KHÔNG BỊ XUNG ĐỘT CODE)

1. **Trước khi bắt đầu code mỗi ngày:** Lấy code mới nhất về:
   ```powershell
   git checkout main
   git pull origin main
   ```
2. **Tạo nhánh riêng cho công việc của mình:**
   ```powershell
   git checkout -b feat/ten-minh-ten-viec
   ```
   *(Ví dụ: `feat/a-writer-queue`, `feat/b-server-socket`, `feat/c-dashboard-ui`)*
3. **Chỉ sửa file trong phân hệ của mình:**
   * A chỉ viết trong `src/LabGuard.Agent/`
   * B chỉ viết trong `src/LabGuard.Central/` hoặc `database/`
   * C chỉ viết trong `src/LabGuard.Admin/`
   * D viết bài tập trong thư mục riêng `tools/`
4. **Kiểm tra xem mình đã sửa những file nào:**
   ```powershell
   git status
   ```
   *Nếu thấy có file của bạn khác bị sửa nhầm, hãy khôi phục lại ngay.*
5. **Chạy thử toàn bộ kiểm thử để chắc chắn không làm hỏng code:**
   ```powershell
   dotnet test
   ```
   *Chỉ khi màn hình báo `Passed! - Failed: 0` thì mới sang bước tiếp theo.*
6. **Lưu (commit) công việc kèm lời nhắn rõ ràng:**
   ```powershell
   git add .
   git commit -m "Mo ta viec minh vua lam bang tieng Viet ro rang"
   ```
7. **Đẩy nhánh lên GitHub:**
   ```powershell
   git push origin feat/ten-minh-ten-viec
   ```
8. **Báo cho cả nhóm:** Nhắn tin lên nhóm chat để các bạn khác cùng thử và duyệt code.

---

## 4. DANH SÁCH TÀI LIỆU CHUYÊN SÂU & ĐẶC TẢ KỸ THUẬT (THAM KHẢO KHI CẦN)

Các tài liệu kỹ thuật chi tiết, ma trận nhiệm vụ và đặc tả giao thức được tổ chức gọn gàng trong `docs/spec/` và `docs/internal/`. **Khi mới bắt đầu, bạn không cần đọc hết các tài liệu này:**

| Tài liệu tra cứu | Nội dung | Khi nào cần xem? |
| :--- | :--- | :--- |
| [`docs/spec/PROTOCOL_SPEC.md`](docs/spec/PROTOCOL_SPEC.md) | Đặc tả chi tiết giao thức nhị phân và danh mục 69 bản tin chuẩn | Khi cần tra cứu chi tiết cấu trúc payload hoặc quy chuẩn wire framing |
| [`docs/spec/CONTRACT_MATRIX.md`](docs/spec/CONTRACT_MATRIX.md) | Ma trận giao ước kỹ thuật và 16 canonical fixtures | Khi nghiệm thu hoặc đối soát dữ liệu kiểm thử giữa các phân hệ |
| [`docs/spec/ARCHITECTURE_DECISIONS.md`](docs/spec/ARCHITECTURE_DECISIONS.md) | Sổ ghi nhận quyết định kiến trúc và tham số chuẩn (ADR) | Khi cần tra cứu tham số kỹ thuật (timeout 20s, hàng đợi 256/1000/100, OCC...) |
| [`docs/internal/WBS_TASKS.csv`](docs/internal/WBS_TASKS.csv) | Danh mục 34 nhiệm vụ kỹ thuật chi tiết | Phục vụ theo dõi tiến độ, phân công workload và tiêu chí nghiệm thu |
| [`docs/internal/DEPENDENCY_MAP.md`](docs/internal/DEPENDENCY_MAP.md) | Sơ đồ luồng dữ liệu và phụ thuộc kỹ thuật | Khi chuẩn bị ghép nối tích hợp giữa Agent, Central, Admin |
| [`docs/internal/OWNERSHIP_MATRIX.md`](docs/internal/OWNERSHIP_MATRIX.md) | Ma trận phân quyền sở hữu mã nguồn | Khi tạo nhánh Git và phân định ranh giới commit của từng người |
