# LABGUARD — HỆ THỐNG GIÁM SÁT PHÒNG MÁY TẬP TRUNG

> **Đồ án môn học:** NT106 — Lập trình mạng căn bản
> **Khoa Mạng máy tính & Truyền thông — Trường ĐH Công nghệ Thông tin (UIT)**

---

## 📚 BỘ TÀI LIỆU NÊN ĐỌC (5 TẬP PDF CHÍNH THỨC)

Bộ sách hướng dẫn được biên soạn chuyên biệt, chuẩn hóa đồ họa và mục lục siêu liên kết:
* 📖 **Dành cho cả nhóm:** [LabGuard_Tong_Quan_A-Z.pdf](docs/pdf/LabGuard_Tong_Quan_A-Z.pdf) — Sổ tay toàn diện đồ án (Master Book), lý thuyết mạng, luồng dữ liệu, phân công RACI và 30 câu hỏi vấn đáp.
* 📕 **Thành viên A (Agent & Protocol):** [LabGuard_Huong_Dan_Thanh_Vien_A.pdf](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_A.pdf) — Giao ước mạng, FrameCodec, Socket Client, Replay Buffer, đo CPU/RAM và 20 câu hỏi vấn đáp.
* 📗 **Thành viên B (Central Server & Database):** [LabGuard_Huong_Dan_Thanh_Vien_B.pdf](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_B.pdf) — TCP Server đa luồng, SQL Server, Rule Engine, Dead Man's Switch, OCC và 20 câu hỏi vấn đáp.
* 📘 **Thành viên C (Admin Client & UI):** [LabGuard_Huong_Dan_Thanh_Vien_C.pdf](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_C.pdf) — Giao diện WinForms Dashboard, an toàn luồng Cross-Thread UI (`BeginInvoke`), tương tác sự cố OCC, RBAC và 20 câu hỏi vấn đáp.
* 📙 **Thành viên D (Gateway & Collector):** [LabGuard_Huong_Dan_Thanh_Vien_D.pdf](docs/pdf/LabGuard_Huong_Dan_Thanh_Vien_D.pdf) — Gateway, Collector (Scale Layer), hàng đợi chống tràn bộ nhớ, kiểm thử Wireshark và 20 câu hỏi vấn đáp.

---

## 1. LABGUARD LÀ GÌ?

LabGuard là hệ thống tự động theo dõi tình trạng máy tính trong phòng thực hành mạng (đo CPU, RAM, thời gian máy bật, và kiểm tra xem máy còn liên lạc hay không). Khi một máy tính bị quá tải hoặc rớt mạng, hệ thống phát cảnh báo tức thì lên màn hình của người quản trị phòng máy.

```
       [ Máy trạm sinh viên ]                         [ Máy giáo viên / Kỹ thuật viên ]
       +--------------------+                         +-------------------------------+
       |   LabGuard.Agent   | ---------\              |        LabGuard.Admin         |
       | (Thu thập số liệu) |           \             |   (Màn hình xem máy & sự cố)  |
       +--------------------+            \            +-------------------------------+
                                          v                           ^
                                +-----------------------+             |
                                |   LabGuard.Central    | ------------/
                                | (Máy chủ xử lý & CSDL)|
                                +-----------------------+
```

Hệ thống phân chia thành 4 phân hệ chính cho 4 sinh viên:
1. **`LabGuard.Agent` (Máy trạm — Sinh viên A phụ trách):** Chạy ngầm trên từng máy con, đo mức CPU thật, RAM thật, thời gian chạy, giữ hàng đợi gửi tuần tự và tự kết nối lại khi mất mạng.
2. **`LabGuard.Central` (Máy chủ trung tâm — Sinh viên B phụ trách):** Lắng nghe kết nối mạng (port 5000), nhận số liệu máy, phân tích cảnh báo sự cố, và lưu vào CSDL SQL Server.
3. **`LabGuard.Admin` (Giao diện quản trị — Sinh viên C phụ trách):** Ứng dụng Windows Forms hiển thị bảng danh sách máy và bảng danh sách sự cố theo thời gian thực để người trực tiếp nhận và xử lý.
4. **`LabGuard.Gateway` & `LabGuard.Collector` (Phần mở rộng — Sinh viên D phụ trách):** Tầng cân bằng tải L4 TCP và trạm gom dữ liệu phân tán khi hệ thống mở rộng phục vụ hàng trăm máy trạm.

---

## 2. HIỆN TRẠNG MÃ NGUỒN THỰC TẾ

> [!IMPORTANT]
> **Hiện trạng chính thức của nhóm (sau khi merge PR #4, #5, #7, #8, #9):**
> - ✅ **ĐÃ HOÀN THÀNH & NGHIỆM THU (DONE / ACCEPTED):**
>   - **A-001, A-002, A-003, A-004 (Agent & Protocol):** Đã nghiệm thu chính thức (`Protocol.Tests` 107/107 PASS, `Agent.Tests` 34/34 PASS trong đó A-004 có 18/18 xUnit cases và đối soát Windows Reference 10/10 PASS).
>   - **B-001 (Central Server):** Đã nghiệm thu qua PR #7 (`CentralServer.cs` mở cổng Socket TCP 5000, hỗ trợ đa client, `Central.Tests` 9/9 PASS).
>   - **D-002 (TCP Echo):** Đã nghiệm thu qua PR #8 (`tools/TcpEcho/Program.cs` hỗ trợ đa client, shutdown an toàn).
> - 🟡 **ĐANG LÀM / NGHIỆM THU TỪNG PHẦN:**
>   - **B-005 (Database SQL Server):** PR #5 đã merge `database/schema.sql` (DDL tạo bảng `ProcessedBatches`, `MetricSamples`, `Incidents`). Phần C# persistence trong `DatabaseRepository.cs` và kiểm thử tương tác SQL Server chưa nghiệm thu.
>   - **C-001 (Admin WinForms UI):** PR #4 đã merge khung giao diện UI 0.1 (`DashboardForm.cs`, ảnh bằng chứng `docs/evidence/admin_dashboard_c001.png`). Nghiệm thu toàn diện UI và data binding đang chờ thực hiện.
> - ⚪ **CHƯA LÀM / TIẾP THEO:**
>   - **Sinh viên A:** Tiếp theo là `A-005` (Tự động kết nối lại khi rớt mạng).
>   - **Sinh viên B:** Tiếp theo là `B-002` (Bắt tay HELLO/AUTH) và hoàn thiện `B-005` persistence.
>   - **Sinh viên C:** Tiếp theo là `C-002` (AdminClient kết nối Socket tới Central).
>   - **Sinh viên D:** `D-001` (chưa có báo cáo); tiếp theo là `D-003` (TCP Relay). Các module `Gateway`, `Collector`, `Simulator` (`D-004..D-006`) là dự kiến mở rộng sau.
> - 🛠️ **CÔNG CỤ GIẢ LẬP BỔ TRỢ (MOCK HARNESS):** PR #9 đã merge `tools/mock_harness/FakeAgent`, `FakeCentral` và `tests/LabGuard.MockHarness.Tests` (15/15 tests PASS). Đây là công cụ phục vụ kiểm thử giao thức độc lập, không thay thế cho nhiệm vụ `D-003`.

### Các lệnh kiểm tra hệ thống:

#### 1. Chạy bộ kiểm thử tự động (165/165 tests PASS)
```powershell
dotnet test
```
*Kết quả:* `Passed! - Failed: 0` — Tổng cộng **165 tests PASS** trên môi trường cục bộ:
- `LabGuard.Protocol.Tests`: 107/107 PASS
- `LabGuard.Central.Tests`: 9/9 PASS
- `LabGuard.Agent.Tests`: 34/34 PASS
- `LabGuard.MockHarness.Tests`: 15/15 PASS
*(Lưu ý: Đây là số liệu kiểm thử cục bộ trước khi merge; hệ thống CI tự động trên GitHub chưa được thiết lập).*

#### 2. Chạy công cụ độc lập kiểm tra đóng khung tin TCP 4 byte (A-002)
```powershell
dotnet run --project tools/validation/test_frame_codec/test_frame_codec.csproj
```
*Kết quả:* Toàn bộ 10 kịch bản kiểm tra phân mảnh và ghép khung tin TCP đều đạt chuẩn (`10/10 scenarios passed`).

#### 3. Chạy thử các phân hệ dự án
* Máy trạm Agent: `dotnet run --project src/LabGuard.Agent/LabGuard.Agent.csproj`
* Máy chủ Central (B-001 lắng nghe port 5000): `dotnet run --project src/LabGuard.Central/LabGuard.Central.csproj`
* Giao diện Admin (C-001 WinForms Dashboard): `dotnet run --project src/LabGuard.Admin/LabGuard.Admin.csproj`
* Máy chủ TCP Echo (D-002): `dotnet run --project tools/TcpEcho/TcpEcho.csproj`
* Giả lập Mock Harness: `dotnet run --project tools/mock_harness/FakeCentral/FakeCentral.csproj` / `FakeAgent.csproj`

---

## 3. BẠN LÀ THÀNH VIÊN TRONG NHÓM? BẮT ĐẦU TẠI ĐÂY!

Để bắt tay vào thực hiện nhiệm vụ của mình, hãy đọc theo đúng thứ tự 2 tài liệu sau:

👉 **[START_HERE.md](START_HERE.md)**: **Bắt đầu tại đây!** Hướng dẫn nhanh cho từng bạn (A, B, C, D) — Đọc để biết bạn phụ trách việc gì, bắt đầu từ file nào, cách thử và làm sao biết mình chạy đúng.
👉 **[FILE_MAP.md](FILE_MAP.md)**: Bản đồ toàn bộ file trong project — Giải thích vai trò của từng file trong mã nguồn hiện tại.
