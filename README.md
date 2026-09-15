# LABGUARD — Hệ thống giám sát phòng máy tính tập trung
**Đồ án môn học:** NT106 - Lập trình mạng căn bản | Trường ĐH Công nghệ Thông tin (UIT)

👉 **DÀNH CHO THÀNH VIÊN MỚI / BẮT ĐẦU TẠI ĐÂY:** [**TEAM_START_HERE.md**](TEAM_START_HERE.md)

---

## 📌 Giới thiệu tổng quan
LabGuard là hệ thống giám sát và quản lý phòng máy tính tập trung viết bằng **C# (.NET 9.0)**. Hệ thống hoạt động theo mô hình Client - Server thời gian thực:
- **Agent** chạy ngầm trên máy trạm (Windows), liên tục đọc chỉ số phần cứng (CPU qua `GetSystemTimes`, RAM qua `GlobalMemoryStatusEx`, Uptime qua `TickCount64`) và trạng thái dịch vụ mạng, sau đó đóng gói gửi qua kết nối TCP về máy chủ theo giao thức framing chuẩn: 4-byte unsigned big-endian payload length + N bytes UTF-8 JSON (độ dài tối đa 262,144 bytes / 256 KiB).
- **Central Server** tiếp nhận kết nối, xác thực, duy trì cơ chế nhịp tim (Heartbeat 5s / Liveness timeout > 20s), ghi dữ liệu vào CSDL SQL Server (`dbo.MetricSamples`, `dbo.Incidents`, `dbo.IncidentHistory`) và tự động phân tích phát hiện sự cố bất thường qua bộ quy tắc (Rule Engine với hysteresis 3 mẫu liên tiếp).
- **Admin Dashboard (WinForms)** hiển thị danh sách thiết bị trực tuyến/ngoại tuyến, các thẻ thống kê KPI, bảng sự cố và thanh công cụ thao tác (`INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`) với cơ chế kiểm soát tranh chấp dữ liệu (Optimistic Concurrency Control bằng `RowVersion` 8-byte hex).

---

## 🚀 Khởi động nhanh (Quick Start)

### Yêu cầu môi trường
- .NET SDK 9.0 trở lên.
- Microsoft SQL Server (LocalDB hoặc SQL Server Express).
- Hệ điều hành: Windows 10/11 (để đọc chỉ số phần cứng qua Win32 API).

### Trình tự chạy hệ thống
1. **Khởi tạo CSDL**:
   Chạy file [database/schema.sql](database/schema.sql) và [database/seed.sql](database/seed.sql) trên SQL Server để tạo database `LabGuardDb`.
2. **Khởi động Central Server**:
   ```powershell
   dotnet run --project src/LabGuard.Central/LabGuard.Central.csproj
   ```
3. **Khởi động Agent máy trạm**:
   ```powershell
   dotnet run --project src/LabGuard.Agent/LabGuard.Agent.csproj
   ```
4. **Khởi động Admin Dashboard**:
   ```powershell
   dotnet run --project src/LabGuard.Admin/LabGuard.Admin.csproj
   ```

---

## 📂 Cấu trúc Solution

```text
LABGUARD_ROOT/
├── LabGuard.sln                    # Solution chính (.NET 9.0)
├── TEAM_START_HERE.md              # Hướng dẫn chi tiết cho 3 thành viên
├── PROJECT_STATE.md                # Tình trạng kỹ thuật và kết quả test hiện tại
├── TODO.md                         # Bảng phân công công việc tiếp theo
├── CLEANUP_MANIFEST.md             # Danh mục dọn dẹp không gian làm việc
│
├── src/                            # Mã nguồn ứng dụng
│   ├── LabGuard.Protocol/          # Thư viện giao thức (Length-Prefix Codec, DTO, Enums)
│   ├── LabGuard.Central/           # Máy chủ trung tâm (TCP Server, RuleEngine, DB)
│   ├── LabGuard.Agent/             # Agent thu thập chỉ số (Win32, ReplayBuffer)
│   ├── LabGuard.Admin/             # Giao diện quản trị viên Windows Forms
│   └── LabGuard.Simulator/         # Công cụ sinh tải giả lập nhiều máy trạm
│
├── tests/                          # Bộ kiểm thử tự động (xUnit)
│   ├── LabGuard.Protocol.Tests/    # Kiểm thử đóng gói / giải mã FrameCodec, DTO
│   ├── LabGuard.Agent.Tests/       # Kiểm thử ReplayBuffer, Metrics
│   └── LabGuard.Central.Tests/     # Kiểm thử RuleEngine, State Machine, Concurrency
│
├── database/                       # Kịch bản cơ sở dữ liệu SQL Server
│   ├── schema.sql                  # Định nghĩa bảng (Devices, Incidents, IncidentHistory,...)
│   └── seed.sql                    # Dữ liệu khởi tạo mẫu
│
├── tools/                          # Công cụ hỗ trợ demo và kiểm định hợp đồng
│   ├── demo/                       # Kịch bản PowerShell chạy demo tự động
│   └── validation/                 # Kịch bản Python kiểm tra hợp đồng giao thức
│
├── docs/                           # Tài liệu kỹ thuật và lịch sử phát triển
│   ├── history/                    # Tài liệu lưu trữ các mốc phát triển trước
│   └── screenshots/                # Ảnh chụp màn hình giao diện Dashboard
│
└── main.tex, main.pdf, sections/   # Báo cáo đề cương đồ án LaTeX
```

---

## 👥 Phân công trách nhiệm nhóm

| Thành viên | Phụ trách chính | Các file mã nguồn trọng tâm |
| :--- | :--- | :--- |
| **Sinh viên A** | **Agent & Simulator** | `AgentSocketClient.cs`, `MetricsService.cs`, `ReplayBuffer.cs`, `ServiceMonitor.cs` |
| **Sinh viên B** | **Central Server & Database** | `CentralServer.cs`, `ClientConnection.cs`, `RuleEngine.cs`, `DatabaseRepository.cs`, `schema.sql` |
| **Sinh viên C** | **Admin WinForms** | `DashboardForm.cs`, `AdminClient.cs`, `Program.cs` |

Chi tiết công việc và hướng dẫn từng bước xem tại [TEAM_START_HERE.md](TEAM_START_HERE.md).

---

## 📜 Tài liệu đặc tả kỹ thuật (Frozen Contracts)

Các tài liệu hợp đồng giao thức và quyết định kiến trúc đã được đóng băng để đảm bảo tính tương thích giữa các module:
- [03_PROTOCOL_SPEC.md](03_PROTOCOL_SPEC.md): Đặc tả cấu trúc gói tin (Length-Prefix 4-byte UInt32 Big-Endian + UTF-8 JSON Payload, Wire Message Types).
- [CONTRACT_MATRIX.md](CONTRACT_MATRIX.md): Ma trận hợp đồng giữa Agent, Central và Admin.
- [CP2_DECISIONS_REV2.md](CP2_DECISIONS_REV2.md): Quyết định kỹ thuật về cơ chế Reconnect Backoff, Replay Buffer và OCC Concurrency.

---

## 📄 Báo cáo đề cương đồ án (LaTeX)

Thư mục này đồng thời chứa mã nguồn tài liệu báo cáo đề cương môn học theo chuẩn LaTeX của trường UIT:
- `main.tex`: Cấu hình chính và dàn trang.
- `sections/`: Nội dung các chương (Tổng quan, Mục tiêu, Phương pháp, Nội dung, Kế hoạch).
- `appendices/`: Phụ lục (Thuật ngữ, Bản đồ công việc, Câu hỏi vấn đáp).
- `template/nt106.cls`: Định dạng văn bản đồ án môn học NT106.

### Trình tự biên dịch LaTeX chuẩn:
- **Chuỗi công cụ khuyến nghị:** MiKTeX (Windows x64) với `pdfTeX 1.40.26` và `Biber 2.21`.
```bash
pdflatex -interaction=nonstopmode main.tex
biber main
pdflatex -interaction=nonstopmode main.tex
```
*(Báo cáo đã được cân chỉnh chặt chẽ đúng giới hạn 30 trang A4 theo yêu cầu đề cương).*
