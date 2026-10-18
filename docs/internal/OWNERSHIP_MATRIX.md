# LABGUARD NT106 — MA TRẬN PHÂN QUYỀN SỞ HỮU MÃ NGUỒN (OWNERSHIP MATRIX)

> **Dự án:** LABGUARD — Hệ thống giám sát phòng máy tính tập trung thời gian thực  
> **Phiên bản:** Phân công chính thức theo quyết định trực tiếp của người điều phối ngày 17/10/2026; ánh xạ A/B/C/D tại [TEAM_ASSIGNMENTS.md](TEAM_ASSIGNMENTS.md)
> **Nguyên tắc cốt lõi:** Phân chia ranh giới rõ ràng theo phân hệ (Module Ownership), bảo vệ hợp đồng dùng chung, chống sửa chéo gây conflict mã nguồn trong quá trình làm việc song song.

---

## 1. BẢNG PHÂN QUYỀN SỞ HỮU CHI TIẾT THEO THƯ MỤC VÀ FILE

| Đường dẫn thư mục / Tập tin | Chủ sở hữu chính (Primary Owner) | Người dự phòng (Backup / Reviewer) | Quyền hạn của Owner | Quyền hạn của thành viên khác | Mục đích & Trách nhiệm |
| :--- | :---: | :---: | :---: | :---: | :--- |
| `src/LabGuard.Protocol/**` | **Thành viên A** | **B, C, D** | Đọc / Đề xuất sửa *(Chế độ Đóng Băng)* | Chỉ đọc *(Read-only)* | Cấu trúc thông điệp `WireEnvelope`, `FrameCodec`, DTO. Hợp đồng đóng băng. |
| `docs/spec/PROTOCOL_SPEC.md` | **Thành viên A** | **B, C, D** | Đọc / Cập nhật tài liệu | Chỉ đọc *(Read-only)* | Đặc tả giao thức mạng và danh mục 69 bản tin. |
| `docs/spec/CONTRACT_MATRIX.md` | **Thành viên A** | **B, C, D** | Đọc / Cập nhật bảng đối soát | Chỉ đọc *(Read-only)* | Ma trận hợp đồng 69 bản tin và 16 canonical fixtures. |
| `docs/spec/ARCHITECTURE_DECISIONS.md` | **Cả 4 thành viên** | **Nhóm trưởng** | Đọc / Tra cứu thông số | Chỉ đọc *(Read-only)* | Sổ ghi nhận 19 quyết định kiến trúc chuẩn (ADR). |
| `src/LabGuard.Agent/**` | **Thành viên A** | **Thành viên B** | Toàn quyền chỉnh sửa *(Full write)* | Chỉ đọc *(Read-only)* | Thu thập CPU, RAM, Uptime, Service Probe, ReplayBuffer, TCP client. |
| `src/LabGuard.Simulator/**` | **Thành viên D** | **Thành viên A** | Toàn quyền chỉnh sửa *(Full write)* | Có quyền chạy test tải | P1 tùy chọn; hiện chỉ placeholder Simulator, chưa có logic sinh tải. |
| `tests/LabGuard.Agent.Tests/**` | **Thành viên A** | **Thành viên B** | Toàn quyền chỉnh sửa *(Full write)* | Chỉ đọc *(Read-only)* | Unit test kiểm thử ReplayBuffer và logic Agent. |
| `src/LabGuard.Central/**` | **Thành viên B** | **Thành viên C** | Toàn quyền chỉnh sửa *(Full write)* | Chỉ đọc *(Read-only)* | Ingress socket, SessionStore, AuthService, RuleEngine, IngestProcessor. |
| `database/**` | **Thành viên B** | **Thành viên C** | Toàn quyền chỉnh sửa *(Full write)* | Chỉ đọc *(Read-only)* | Script CSDL `schema.sql`, `seed.sql`, migrations, chỉ mục SQL. |
| `tests/LabGuard.Central.Tests/**` | **Thành viên B** | **Thành viên A** | Toàn quyền chỉnh sửa *(Full write)* | Chỉ đọc *(Read-only)* | Unit test cho RuleEngine, DB repo, OCC 409 conflict, ClientConnection. |
| `src/LabGuard.Admin/**` | **Thành viên C** | **Thành viên B** | Toàn quyền chỉnh sửa *(Full write)* | Chỉ đọc *(Read-only)* | Giao diện WinForms Dashboard, Lab View, Incident View, UiDispatcher, RPC client. |
| `src/LabGuard.Gateway/**` | **Thành viên D** | **Thành viên A** | Toàn quyền chỉnh sửa *(Full write)* | Chỉ đọc *(Read-only)* | P1 tùy chọn, chưa có project. Kiến thức L4 Least-Connections/Duplex Relay được giữ để tham khảo. |
| `src/LabGuard.Collector/**` | **Thành viên D** | **Thành viên B** | Toàn quyền chỉnh sửa *(Full write)* | Chỉ đọc *(Read-only)* | P1 tùy chọn, chưa có project. Kiến thức L7 gom mẻ/upstream/router chỉ để tham khảo. |
| `docs/pdf/**` | **Cả 4 thành viên** | **Nhóm trưởng** | Tra cứu cẩm nang của mình | Review chéo | Bộ 5 cuốn cẩm nang PDF chuyên biệt và mã nguồn LaTeX. |
| `docs/internal/**` | **Cả 4 thành viên** | **Nhóm trưởng** | Cập nhật tiến độ nhiệm vụ | Quản trị dự án | Bảng WBS, sơ đồ phụ thuộc và ma trận quyền sở hữu. |
| `tools/**` | **Thành viên A, D** | **Thành viên B** | Cập nhật công cụ kiểm tra | Chạy thử nghiệm | Công cụ kiểm tra độc lập test_frame_codec và bài tập socket của D. |


### Bổ sung phân công chính thức ngày 17/10/2026

- A — Võ Trọng Phúc (25521452): Protocol & Agent.
- B — Nguyễn Bá Quang (25521524): Central Server & SQL Server.
- C — Lý Tuyết Vy (25522107): Admin WinForms.
- D — Đỗ Minh Ngọc (25521203): Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố.

Nguồn: quyết định trực tiếp của người điều phối dự án, ghi tại TEAM_ASSIGNMENTS.md; không phải chữ ký hoặc biên bản đồng thuận tập thể được tạo thêm. Assistant là công việc chính thức trong kế hoạch phát triển, chưa phải chức năng đã được lập trình.

| Phần bổ sung | Chủ trì | Phối hợp | Ranh giới |
|---|---|---|---|
| `src/LabGuard.Troubleshooting/**` (đường dẫn thư viện dự kiến, chưa có mã) | D / Ngọc | B, C | Logic evidence-to-recommendation C# được Admin gọi, không executable chẩn đoán độc lập |
| Context thiết bị/sự cố qua Central | B / Quang | D, C | B-010 ghép dữ liệu được phép khi core sẵn sàng, không Admin→SQL/Agent |
| Panel chi tiết sự cố và handler UI | C / Vy | D, B | C-012 nhúng logic D và giữ workflow/quyền hiện có |
| Sáu ca A–F trong luồng Admin thật sau này | D / Ngọc | B, C | D-010 chỉ có bằng chứng sau khi thực hiện; GK trình bày ý tưởng/kế hoạch |

D-001/002/003 giữ nguyên. Gateway/Collector/Simulator vẫn P1 có điều kiện; dòng Simulator đồng bộ owner chính D với WBS D-006, A hỗ trợ. Không thay điều kiện triển khai thành cam kết bắt buộc. B-010 (2 giờ) / C-012 (3 giờ) là effort ghép bổ sung, không tính toàn bộ core còn thiếu; readiness và công sức core phải rà riêng.

Không sửa schema/protocol trong quyết định phân công này. Giai đoạn 1 dùng dữ liệu Central được phép và hiện thiếu bằng chứng. Giai đoạn 2 lịch sử SQL qua GET_METRICS/METRICS_RESULT tùy chọn cần được duyệt và kiểm tra riêng.

---

## 2. NGUYÊN TẮC LÀM VIỆC VÀ CHỐNG XUNG ĐỘT MÃ NGUỒN (GIT WORKFLOW)

1. **Quy tắc 1: Không ai được sửa code trong phân hệ của người khác mà không xin phép.**  
   - Ví dụ: Sinh viên B không bao giờ được tự ý mở file `src/LabGuard.Agent/...` để sửa, dù phát hiện ra lỗi. B phải thông báo cho A, giải thích lỗi và để A tự sửa trên nhánh của A.
2. **Quy tắc 2: Không sửa trực tiếp trên nhánh `main`.**  
   - Mỗi thành viên tạo nhánh làm việc riêng theo mã nhiệm vụ:  
     - Thành viên A: `feature/lane-a-agent` hoặc `task/LG-A-003-writer-queue`  
     - Thành viên B: `feature/lane-b-central` hoặc `task/LG-B-001-sql-schema`  
     - Thành viên C: `feature/lane-c-admin` hoặc `task/LG-C-001-winforms-shell`  
     - Thành viên D: `feature/lane-d-tools-assistant` hoặc `task/LG-D-003-tcp-relay`
3. **Quy tắc 3: Kiểm thử tự động phải pass 100% trước khi tạo Pull Request.**  
   - Trước khi merge code vào nhánh chung, bắt buộc phải chạy:
     ```powershell
     dotnet test
     ```
   - Toàn bộ 107 bài test tự động giao thức phải hoàn toàn xanh lá (PASS). Nếu có bài test bị FAIL, người làm hỏng phải tự sửa trước khi merge.
4. **Quy tắc 4: Người duyệt (Reviewer) bắt buộc phải kiểm tra trước khi bấm Merge.**  
   - Code của A phải được B duyệt.  
   - Code của B phải được C duyệt.  
   - Code của C phải được B duyệt.  
   - Code của D phải được A hoặc B duyệt.

---

## 3. QUY TRÌNH KIỂM SOÁT THAY ĐỔI HỢP ĐỒNG DÙNG CHUNG (FREEZE & CHANGE CONTROL)

Các thành phần thuộc nhóm **Hợp đồng dùng chung (Shared Contract)** bao gồm:
- `src/LabGuard.Protocol/**` (Mã nguồn Outer Envelope, DTO, FrameCodec)
- `database/schema.sql` (Cấu trúc các bảng CSDL, tên cột, kiểu dữ liệu)
- `docs/spec/PROTOCOL_SPEC.md`, `docs/spec/CONTRACT_MATRIX.md` và `docs/spec/ARCHITECTURE_DECISIONS.md`

### Quy định nghiêm ngặt:
1. **Trạng thái hiện tại:** **ĐÓNG BĂNG HOÀN TOÀN (FROZEN)**. Không ai được phép đơn phương thay đổi tên trường JSON (ví dụ đổi `sentAtUtc` thành `timestamp`), không được đổi kiểu dữ liệu, không được thêm bớt cột CSDL.
2. **Nếu phát sinh nhu cầu thay đổi cấp bách (Blocker):**
   - Bước 1: Người đề xuất phải lập một phiếu yêu cầu thay đổi (RFC) ngắn nêu rõ: *Thay đổi cái gì? Tại sao phải thay đổi? Nếu không thay đổi thì hệ thống bị nghẽn ở đâu?*
   - Bước 2: Tổ chức cuộc họp nhanh 15 phút với **đầy đủ 4 thành viên (A, B, C, D)**.
   - Bước 3: **Nguyên tắc đồng thuận 100% (Unanimous Approval)**: Cả 4 thành viên cùng đồng ý và ký nhận vào biên bản họp thì mới được phép mở khóa để sửa.
   - Bước 4: Thành viên A thực hiện cập nhật mã nguồn trong `LabGuard.Protocol`, sau đó cả 4 thành viên cùng cập nhật lại code phân hệ của mình để đảm bảo tính tương thích tuyệt đối.
