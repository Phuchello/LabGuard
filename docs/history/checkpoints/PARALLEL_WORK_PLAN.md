# KẾ HOẠCH PHÁT TRIỂN SONG SONG 3 LÀN (PARALLEL WORK PLAN) — LABGUARD NT106
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Quy mô nhóm:** Đúng 3 sinh viên  
**Khung thời gian:** 5 tuần (Baseline có điều kiện, co giãn theo năng lực thực tế)  
**Tác giả kiến trúc:** Antigravity (Network Software Architect & Lead Researcher)  

---

## I. MÔ HÌNH PHÂN CHIA 3 LÀN ĐỘC LẬP (3-LANE INDEPENDENT ARCHITECTURE)

Khắc phục triệt để tình trạng phụ thuộc chéo (blocking dependencies) bằng cách tách thành 3 mảng độc lập, mỗi thành viên sở hữu trọn vẹn một ứng dụng thực thi kèm bộ test harness giả lập đối tác (Self-test Harness):

```
+---------------------------------------------------------------------------------------+
|                                    SHARED.PROTOCOL                                    |
|             (Envelope, Length-Prefix Codec, DTOs, Error Codes, Wire Enums)            |
|                            Lead: Sinh viên A | Review: B, C                           |
+---------------------------------------------------------------------------------------+
           |                                   |                                   |
           v                                   v                                   v
+-----------------------+           +-----------------------+           +-----------------------+
|    SINH VIÊN A        |           |    SINH VIÊN B        |           |    SINH VIÊN C        |
|  Agent Windows Core   |           |  Central Server Core  |           |  Admin WinForms Client|
|  Simulator 20+ nodes  |           |  SQL Server Ingest/DB |           |  Realtime Dash & Grid |
|  Network Load Harness |  ===TCP==>|  Rule Engine & Eval   |  ===TCP==>|  Client RPC & Cache   |
|  Gateway L4 (P1)      |   TLS     |  Auth Server & RBAC   |   TLS     |  Multi-Admin Conflict |
|  Deploy 2 Collectors  |           |  Snapshot & Fanout    |           |  Shared Collector App |
|                       |           |                       |           |                       |
| Self-test Harness:    |           | Self-test Harness:    |           | Self-test Harness:    |
| Mini Test Server      |           | Simulated Ingest Gen  |           | Mock Central Server   |
+-----------------------+           +-----------------------+           +-----------------------+
```

### 1. Phân định ranh giới trách nhiệm (Ownership Boundaries)
- **Sinh viên A (Transport & Edge):**
  - Sở hữu: `LabGuard.Agent.exe`, `LabGuard.Simulator.exe`, bộ đo tải mạng và script triển khai LAN/Internet.
  - Phụ trách P1: `LabGuard.Gateway.exe` (L4 reverse proxy, least-connections admission, health polling, duplex byte relay) và triển khai/thử nghiệm 2 instance của Collector.
  - Bộ công cụ tự kiểm thử: **Mini Test Server** (giả lập server TCP nhận batch, kiểm tra framing, partial read, retry/backoff).
- **Sinh viên B (Central Engine & Database):**
  - Sở hữu: `LabGuard.Central.exe`, toàn bộ CSDL SQL Server 2022 Express, Rule Engine, Auth/Session Store (PBKDF2-HMAC-SHA256, 600,000 vòng + salt 16B), Snapshot/Fanout Engine.
  - Phụ trách P1: Central internal Collector listener, session generation, fencing và routing phía Central.
  - Bộ công cụ tự kiểm thử: **Simulated Ingest Generator & Headless Client** (bơm mẻ dữ liệu kiểm tra tải ingest CSDL, đo liveness monotonic clock, kiểm tra xung đột `rowversion`).
- **Sinh viên C (Client Experience & Incident Coordination):**
  - Sở hữu: `LabGuard.Admin.exe` (WinForms native), Client RPC Duplex Connection, bộ nhớ đệm cục bộ Local Cache, cơ chế đồng bộ Snapshot + Delta, kiểm thử xung đột đa Admin (Multi-Admin Concurrency).
  - Phụ trách P1: Xây dựng **một ứng dụng Collector dùng chung (`LabGuard.Collector.exe`)**. Khi triển khai, hai node Collector 01 và Collector 02 là hai instance độc lập của cùng một Collector binary (chạy trên các port/máy khác nhau với cấu hình riêng), tuyệt đối không viết hai bản riêng biệt để tránh lệch giao thức.
  - Bộ công cụ tự kiểm thử: **Mock Central Server** (phát sinh snapshot ban đầu, bắn delta event và mô phỏng phản hồi lỗi HTTP/RPC 409 Conflict).

---

## II. MA TRẬN PHÂN BỔ NGÂN SÁCH GIỜ KỸ THUẬT (WORKLOAD BREAKDOWN)

### 1. Bảng phân bổ chi tiết minh bạch (Minh bạch 8 cột, không cộng trùng)

| Thành viên | Ownership chính | P0 Core | Test Harness | Integration | Review / Viva | P1 Mở rộng | Dự phòng (Buffer) | Tổng giờ | Cường độ tuần |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Sinh viên A** | Agent, Simulator, Load, Gateway, Deploy | 24.0 h | 10.0 h | 6.0 h | 5.0 h | 15.0 h | 9.0 h | **69.0 h** | 13.8 h/tuần |
| **Sinh viên B** | Central, SQL DB, Rule, Auth, Fanout | 27.0 h | 9.0 h | 6.0 h | 5.0 h | 16.0 h | 9.0 h | **72.0 h** | 14.4 h/tuần |
| **Sinh viên C** | Admin WinForms, Cache, Multi-Admin, Collector | 25.5 h | 9.0 h | 6.0 h | 5.0 h | 16.0 h | 9.0 h | **70.5 h** | 14.1 h/tuần |
| **TOÀN ĐỘI** | **Hệ thống LABGUARD (P0: 3 App + Shared.Protocol; P1: Gateway + Collector App)** | **76.5 h** | **28.0 h** | **18.0 h** | **15.0 h** | **47.0 h** | **27.0 h** | **211.5 h** | **42.3 h/tuần** |

### 2. Đánh giá tính khả thi và điều kiện áp dụng (Hours Reality Check)
- **Chuẩn đánh giá:** Với định mức tiêu chuẩn 12 giờ/người/tuần $\times$ 3 người $\times$ 5 tuần = 180.0 giờ.
- **Khẳng định tính khả thi:** Kế hoạch 211.5 giờ **chỉ khả thi trọn vẹn khi cả 3 thành viên duy trì cường độ làm việc khoảng 14–15 giờ/người/tuần** (A: 13.8 h/tuần, B: 14.4 h/tuần, C: 14.1 h/tuần; tương đương ~4.5 buổi làm việc tập trung 3 giờ/tuần).
- **So sánh với Blueprint gốc (~135 giờ):** Mức tăng ~76.5 giờ so với ước tính sơ bộ của Master Plan nguồn là hoàn toàn minh bạch, xuất phát từ việc bổ sung các hạng mục kỹ thuật bắt buộc để đảm bảo chất lượng:
  1. Xây dựng 3 bộ Test Harness độc lập để làm việc song song: **+28.0 giờ**.
  2. Tích hợp kiểm thử dồn dập và kiểm thử lỗi mạng (Failure Injection): **+18.0 giờ**.
  3. Diễn tập vấn đáp kỹ thuật 90 câu viva và rà soát tài liệu: **+15.0 giờ**.
  4. Quỹ dự phòng rủi ro gỡ lỗi kỹ thuật mạng và CSDL: **+27.0 giờ**.
  *(Nếu chỉ tính P0 Core: 76.5h + Test Harness: 28.0h + Integration: 18.0h = 122.5 giờ, hoàn toàn nằm trong khuôn khổ ~135 giờ ban đầu).*

### 3. Phương án 12 giờ/người/tuần (Năng lực trần 180 giờ toàn dự án — DEFER P1)
Nếu nhóm duy trì định mức 12 giờ/người/tuần ($3 \times 12 \times 5 = 180.0\text{ giờ}$):
- **Cơ chế kích hoạt:** Tự động quyết định **DEFER P1** ngay từ đầu hoặc tại mốc Gate M4 (cắt giảm toàn bộ 47.0 giờ P1: A giảm 15.0h, B giảm 16.0h, C giảm 16.0h).
- **Khối lượng công việc cam kết thực tế (164.5 giờ):**
  - **Sinh viên A:** P0 Core (24.0h) + Test Harness (10.0h) + Integration (6.0h) + Viva (5.0h) + Buffer (9.0h) = **54.0 giờ** (10.8 h/tuần).
  - **Sinh viên B:** P0 Core (27.0h) + Test Harness (9.0h) + Integration (6.0h) + Viva (5.0h) + Buffer (9.0h) = **56.0 giờ** (11.2 h/tuần).
  - **Sinh viên C:** P0 Core (25.5h) + Test Harness (9.0h) + Integration (6.0h) + Viva (5.0h) + Buffer (9.0h) = **54.5 giờ** (10.9 h/tuần).
  - **Tổng cam kết:** $54.0 + 56.0 + 54.5 = 164.5\text{ giờ}$.
- **Giải trình toán học về Dung lượng dự phòng (Capacity Headroom):**
  - Khối lượng 164.5 giờ này **đã bao gồm đầy đủ 27.0 giờ dự phòng (buffer)** cho cả 3 thành viên (mỗi người 9.0 giờ).
  - Khoảng chênh lệch $180.0 - 164.5 = 15.5\text{ giờ}$ đóng vai trò là **dung lượng năng lực dự phòng (capacity headroom)** ngoài cam kết, giúp giảm áp lực học tập và nghiên cứu. Con số 15.5 giờ này **tuyệt đối không được tính dồn vào khối lượng cam kết và không tính trùng với 27.0 giờ buffer**.
- **Cam kết chất lượng:** Tuyệt đối không cắt giảm bất kỳ tiêu chuẩn nào về bảo mật TLS 1.3, framing nhị phân, tính đúng đắn của máy trạng thái sự cố, kiểm thử mạng LAN trên máy thật và sự chuẩn bị vấn đáp cá nhân.

---

## III. KẾ HOẠCH TRIỂN KHAI 5 TUẦN VÀ CÁC MỐC TÍCH HỢP GIA TĂNG (M0 ĐẾN M6)

### Tuần 1: Protocol Freeze, Skeleton & Thin Vertical Slice
- **Mục tiêu:** Chốt giao ước kỹ thuật tại `CONTRACT_MATRIX.md`, xây dựng khung sườn 3 ứng dụng và chạy thông luồng dọc đầu tiên (Thin Vertical Slice).
- **Công việc chi tiết:**
  - **A:** `LG-A-001` (Wire contract), `LG-A-002` (FrameCodec, test partial reads), `LG-A-003` (Agent TCP connection), `LG-A-004` (TLS client + token enroll), `LG-A-005` (Thu thập CPU/RAM/NIC/Uptime), `LG-A-006` (Heartbeat + Batch sender).
  - **B:** `LG-B-001` (Schema SQL migration + seed an toàn), `LG-B-002` (SQL Repositories + transaction boundary), `LG-B-003` (Central TLS accept + bounded ingress), `LG-B-004` (Session registry, server auth PBKDF2-HMAC-SHA256 600,000 vòng + salt 16B, server-side RBAC).
  - **C:** `LG-C-001` (WinForms UI Shell + navigation), `LG-C-002` (Admin TCP/TLS RPC client connection), `LG-C-003` (Login/Signup WinForms UI + token storage client), `LG-C-004` (Khớp cặp Request/Response correlation ID).
- **Mốc tích hợp M0 (Thứ 3 Tuần 1):** Chốt đóng băng mã nguồn `Shared.Protocol` v1.0.
- **Mốc tích hợp M1 (Thứ 6 Tuần 1):** Thin Vertical Slice hoàn tất: 1 Agent thật gửi `METRIC_BATCH` $\rightarrow$ Central phân khung, lưu CSDL SQL Server $\rightarrow$ Admin WinForms truy vấn và hiển thị trên màn hình.

### Tuần 2: Multi-Agent Ingest, Rule Engine & Realtime Subscription
- **Mục tiêu:** Nâng tải lên 20 simulated agent, kích hoạt rule phát hiện sự cố tự động và cơ chế snapshot phân trang.
- **Công việc chi tiết:**
  - **A:** `LG-A-007` (Local TCP service probe allowlist $\le 5$ cổng, timeout 2s), `LG-A-008` (Agent Simulator 10/20/50 socket độc lập), `LG-A-009` (Reconnect backoff + metric replay/retry), `LG-A-010` (Failure injection).
  - **B:** `LG-B-005` (Metric ingestion, dedup bằng bảng `ProcessedBatches`, commit ACK `BATCH_ACK`), `LG-B-006` (Liveness registry + deadline monotonic clock server), `LG-B-007` (Rule evaluator: HIGH_CPU 3 sample trải 10s, SERVICE_DOWN 2 fail), `LG-B-008` (Incident persistence + constraint duy nhất 1 active incident per key).
  - **C:** `LG-C-005` (Nhận Snapshot phân trang `SNAPSHOT_BEGIN/PAGE/END`), `LG-C-006` (Admin Local Cache merge + UI-thread marshal qua `Control.Invoke`/`BeginInvoke`/`InvokeAsync`), `LG-C-007` (Dashboard realtime, Lab View grid, Device Detail).
- **Mốc tích hợp M2 (Thứ 6 Tuần 2):** Kiểm thử 20 Agent giả lập chạy liên tục 30 phút không crash, Rule Engine mở sự cố chính xác và đẩy realtime event sang WinForms.

### Tuần 3: Incident Collaboration, Multi-Admin Concurrency & Gate M4
- **Mục tiêu:** Hoàn thiện workflow xử lý sự cố, giải quyết xung đột khi nhiều Admin cùng thao tác, chốt cổng kiểm soát MVP.
- **Công việc chi tiết:**
  - **A:** `LG-A-011` (Xoay vòng file log + thu thập evidence LAN), `LG-A-012` (MVP Agent end-to-end trên 2 máy thật trong mạng LAN).
  - **B:** `LG-B-009` (Command handlers `INCIDENT_ACK/START/RESOLVE` với `rowversion` OCC và bảng `ProcessedCommands` TTL 7d), `LG-B-010` (Restart fault handling + Central health), `LG-B-011` (Data retention purge job + báo cáo), `LG-B-012` (MVP concurrency & Incident acceptance).
  - **C:** `LG-C-008` (Incident list/detail + action binding), `LG-C-009` (RBAC UI UX client-side), `LG-C-010` (History/Report/Health views), `LG-C-011` (MVP UI integration & accessibility), `LG-C-012` (Đánh giá nghiệm thu MVP và chủ trì Gate M4).
- **Mốc tích hợp M3 (Thứ 4 Tuần 3):** Hai Admin cùng thao tác 1 Incident: 1 người thành công, 1 người nhận thông báo `409 Conflict` và UI tự động cập nhật lại trạng thái mới nhất.
- **MỐC GATE M4 (Thứ 6 Tuần 3 - CỔNG KIỂM SOÁT QUYẾT ĐỊNH):**
  - Đánh giá toàn bộ tiêu chí P0. Nếu còn bất kỳ lỗi Sev 1/Sev 2 nào chưa giải quyết $\rightarrow$ **KÍCH HOẠT QUY TẮC DEFER P1**, chuyển tuần 4-5 sang ổn định hóa core.
  - Nếu toàn bộ P0 đạt chuẩn $\rightarrow$ **KÍCH HOẠT QUY TRÌNH GO P1**.

### Tuần 4: P1 Load Balancing / Hardening Core
- **Công việc chi tiết (Nếu GO P1):**
  - **A:** `LG-A-013` (Gateway least-connections admission), `LG-A-014` (Gateway duplex relay + health polling).
  - **B:** `LG-B-013` (Internal Collector listener/dispatch), `LG-B-014` (Session generation qua Collector), `LG-B-015` (Collector loss recovery + offline suppression), `LG-B-016` (Tune SQL index), `LG-B-017` (Regression test).
  - **C:** `LG-C-013` (Phát triển ứng dụng Collector dùng chung `LabGuard.Collector.exe`), `LG-C-014` (Collector upstream multiplex + reply router), `LG-C-015` (Collector readiness/health check + bounded buffers), `LG-C-016` (Multi-admin concurrency failure test).
- **Công việc chi tiết (Nếu DEFER P1):**
  - Nhóm tập trung tối ưu hiệu năng CSDL, kiểm thử mạng chập chờn (Flaky network), nén dữ liệu và chuẩn bị báo cáo.
- **Mốc tích hợp M5 (Thứ 6 Tuần 4):** Kiểm thử hạ gục Collector (Collector Kill): Agent tự động ngắt kết nối và failover sang Collector còn lại qua Gateway trong thời gian p95 $\le 45$ giây.

### Tuần 5: Freeze, Regression, Release & Viva Rehearsal
- **Mục tiêu:** Đóng băng mã nguồn, hoàn thiện bộ cài đặt, quay video backup và diễn tập vấn đáp 90 câu hỏi.
- **Công việc chi tiết:**
  - **A:** `LG-A-015` (Tích hợp LB + collector kill test), `LG-A-016` (Triển khai Internet/fallback), `LG-A-017` (Load test 50 nodes + reconnect storm), `LG-A-018` (Thu thập evidence LAN/Internet), `LG-A-019` (Script đóng gói release + SHA-256), `LG-A-020` (Diễn tập Viva A).
  - **B:** `LG-B-018` (DB restore + chứng minh crash consistency), `LG-B-019` (Audit tài liệu kỹ thuật), `LG-B-020` (Diễn tập Viva B).
  - **C:** `LG-C-017` (Kiểm thử hồi quy UI + settings cleanup), `LG-C-018` (Diễn tập kịch bản demo + video backup), `LG-C-019` (Checklist bàn giao + README), `LG-C-020` (Diễn tập Viva C).
- **Mốc tích hợp M6 (Thứ 4 Tuần 5):** ĐÓNG BĂNG TOÀN BỘ CODE (Code Freeze). Không sửa thêm tính năng, chỉ khắc phục lỗi nghiêm trọng nếu có test chứng minh.
