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
> **Hiện trạng chính thức của nhóm:**  
> - ✅ **ĐÃ HOÀN THÀNH:** Thư viện giao thức `LabGuard.Protocol` (A-001 & A-002) + Kết nối TCP & hàng đợi gửi Agent (A-003). Tổng cộng **123/123 tests PASS**.
> - ⚪ **KHUNG SƯỜN CHỜ SINH VIÊN THỰC HIỆN:** Các phân hệ `LabGuard.Central`, `LabGuard.Admin`, `LabGuard.Simulator` hiện chỉ giữ khung sườn tối thiểu. Sinh viên tự lập trình theo phân công.

### Các lệnh kiểm tra hệ thống:

#### 1. Chạy bộ kiểm thử tự động (A-001, A-002, A-003)
```powershell
dotnet test
```
*Kết quả:* `Passed! - Failed: 0` — Protocol.Tests 107/107 PASS, Agent.Tests 16/16 PASS.

#### 2. Chạy công cụ độc lập kiểm tra đóng khung tin TCP 4 byte (A-002)
```powershell
dotnet run --project tools/validation/test_frame_codec/test_frame_codec.csproj
```
*Kết quả:* Toàn bộ 10 kịch bản kiểm tra phân mảnh và ghép khung tin TCP đều đạt chuẩn (`10/10 scenarios passed`).

#### 3. Chạy thử các phân hệ dự án
* Máy trạm Agent (đã hoàn thành A-003 kết nối TCP & hàng đợi Single-Writer; A-004+ chưa làm): `dotnet run --project src/LabGuard.Agent/LabGuard.Agent.csproj`
* Khung sườn máy chủ Central: `dotnet run --project src/LabGuard.Central/LabGuard.Central.csproj`
* Khung sườn giao diện Admin: `dotnet run --project src/LabGuard.Admin/LabGuard.Admin.csproj`

---

## 3. BẠN LÀ THÀNH VIÊN TRONG NHÓM? BẮT ĐẦU TẠI ĐÂY!

Để bắt tay vào thực hiện nhiệm vụ của mình, hãy đọc theo đúng thứ tự 2 tài liệu sau:

👉 **[START_HERE.md](START_HERE.md)**: **Bắt đầu tại đây!** Hướng dẫn nhanh cho từng bạn (A, B, C, D) — Đọc để biết bạn phụ trách việc gì, bắt đầu từ file nào, cách thử và làm sao biết mình chạy đúng.  
👉 **[FILE_MAP.md](FILE_MAP.md)**: Bản đồ toàn bộ file trong project — Giải thích vai trò của từng file trong mã nguồn hiện tại.
