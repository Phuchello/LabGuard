# LABGUARD NT106 — BẢN ĐỒ PHỤ THUỘC & TIẾN ĐỘ SONG SONG (DEPENDENCY MAP)

> **Dự án:** LABGUARD — Hệ thống giám sát phòng máy tính tập trung thời gian thực
> **Mục đích:** Chỉ rõ đường găng công việc, các luồng chạy song song an toàn, các điểm chốt chặn kiểm soát chất lượng, và cách giải phóng sự phụ thuộc giữa 4 thành viên theo kế hoạch 34 nhiệm vụ chuẩn hóa.

---

## 1. SƠ ĐỒ DÒNG CHẢY CÔNG VIỆC TỔNG THỂ CỦA 4 THÀNH VIÊN

```
TUẦN W1: KHỞI TẠO NỀN TẢNG (FOUNDATION & PROTOCOL FREEZE)
========================================================================================
[A] LG-A-001 (Wire Contract) [XONG] ---> LG-A-002 (FrameCodec) [XONG] ---> LG-A-003 (TCP Queue) [XONG]
    |                                                |
    +-----------------------+------------------------+
    |                       |                        |
    v                       v                        v
[B] LG-B-001 (Mở cổng TCP) [XONG]  [C] LG-C-001 (WinForms) [ĐANG LÀM] [D] LG-D-001 (Hiểu Protocol)
    LG-B-002 (Bắt tay AUTH)            LG-C-002 (AdminClient)             LG-D-002 (TCP Echo) [XONG]
                                                                          * Mock Harness (PR #9) [XONG]

TUẦN W2: NGHIỆP VỤ ĐO ĐẠC & QUẢN TRỊ (METRICS & INCIDENTS)
========================================================================================
[A] LG-A-004 (Đo CPU/RAM) [XONG]   [B] LG-B-003 (Liveness 20s)   [C] LG-C-003 (Bảng thiết bị Grid)
    LG-A-005 (Reconnect 30s)           LG-B-004 (Phân quyền RBAC)    LG-C-004 (Bảng sự cố Grid)
    LG-A-006 (ReplayBuffer 50)         LG-B-005 (SQL DDL) [ĐANG LÀM]
                                  |
                                  v
[D] LG-D-003 (TCP Relay & BoundedChannel 100 Wait trong tools/)

TUẦN W3: XỬ LÝ SỰ CỐ & ĐỒNG BỘ OCC (STATE MACHINE & CONCURRENCY)
========================================================================================
[A] LG-A-007 (Service 80/8080) [B] LG-B-006 (RuleEngine)     [C] LG-C-005 (Nút bấm ACK)
    LG-A-008 (Ngắt kết nối)        LG-B-007 (Vòng đời & OCC)     LG-C-006 (Nút bấm Start)
                                                                 LG-C-007 (Nút bấm Resolve)

TUẦN W4: KIỂM THỬ THỰC TẾ & MỞ RỘNG TẢI (INTEGRATION & SCALE)
========================================================================================
[A] LG-A-009 (Test máy ảo)     [B] LG-B-008 (LabGuardTestDb) [C] LG-C-008 (Phân công Assign)
                                   LG-B-009 (UserId Sự cố)       LG-C-010 (Khóa Viewer)
                                                                 LG-C-011 (Báo lỗi OCC 409)
                               |
                               v
[D] LG-D-004 (Gateway dự kiến) ---> LG-D-005 (Collector dự kiến) ---> LG-D-006 (Simulator dự kiến)
```

---

## 2. PHÂN TÍCH ĐƯỜNG GĂNG (CRITICAL PATH)

1. **Chặng 1 (ĐÃ XONG & NGHIỆM THU):**
   - Thành viên A: Giao thức mạng phong bì 6 trường (`LG-A-001`), đóng khung TCP 4-byte (`LG-A-002`), socket client và hàng đợi single-writer (`LG-A-003`), đo CPU/RAM/Uptime Windows (`LG-A-004`).
   - Thành viên B: Khởi tạo CentralServer mở cổng TCP 5000 lắng nghe đa client (`LG-B-001`, PR #7).
   - Thành viên D: TCP Echo Server trong `tools/TcpEcho/` (`LG-D-002`, PR #8).
   - Mock Harness: Bộ công cụ giả lập kiểm thử giao thức độc lập FakeAgent & FakeCentral (`tools/mock_harness/`, PR #9).
   - Tổng cộng **165/165 tests PASS** cục bộ trên toàn bộ solution.
2. **Chặng 2 (ĐANG LÀM):**
   - Thành viên A: Cơ chế tự động kết nối lại khi rớt mạng Exponential Backoff + Jitter (`LG-A-005`).
   - Thành viên B: Bắt tay xác thực (`LG-B-002`) và hoàn thiện C# persistence cho CSDL SQL Server (`LG-B-005`, DDL `database/schema.sql` đã merge qua PR #5).
   - Thành viên C: Hoàn tất nghiệm thu giao diện WinForms (`LG-C-001`, layout 0.1 đã merge qua PR #4) và kết nối socket ngầm tới Central (`LG-C-002`).
   - Thành viên D: Thử nghiệm chuyển tiếp gói tin TCP Relay trong `tools/` (`LG-D-003`).
3. **Chặng 3:** Tích hợp số liệu đo đạc thật và CSDL SQL Server (A-006, B-003..B-005, C-003..C-004).
4. **Chặng 4:** Quy tắc phát hiện sự cố và kiểm soát tương tranh OCC (B-006..B-007, C-005..C-011).
5. **Chặng 5:** Thử nghiệm thực tế mạng LAN và chuẩn bị tầng mở rộng quy mô (A-008..A-009, B-008..B-009, D-004..D-006).
