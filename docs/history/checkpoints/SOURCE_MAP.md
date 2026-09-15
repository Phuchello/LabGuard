# LABGUARD — SƠ ĐỒ NGUỒN TÀI LIỆU & PHÂN HẠNG TIN CẬY (SOURCE MAP)
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (UIT)  

---

## 1. PHÂN CẤP NGUỒN TÀI LIỆU (EVIDENCE TIERS)

### TIER 1: Nguồn chuẩn mực & Tài liệu chính thức (Highest Authority)
1. **Tài liệu học tập NT106 của Trường ĐH Công nghệ Thông tin (UIT)**:
   - Giáo trình chính thức: *Tài liệu hướng dẫn Thực hành Lập trình Mạng căn bản* — ThS. Đỗ Thị Hương Lan & ThS. Trần Hồng Nghi, NXB ĐHQG-HCM, 2022.
   - Slide bài giảng Lý thuyết NT106 (TL1 đến TL8) — Khoa Mạng máy tính và Truyền thông, UIT.
   - Bộ đề cương đáp án vấn đáp môn Lập trình mạng UIT (`van-dap-mon-lap-trinh-mang-1.pdf`).
2. **IETF Request for Comments (RFC)**:
   - `RFC 9293`: Transmission Control Protocol (TCP) Specification (2022).
   - `RFC 1700` / `STD 2`: Network Byte Order (Big-Endian).
   - `RFC 8446`: The Transport Layer Security (TLS) Protocol Version 1.3.
   - `RFC 5246`: The Transport Layer Security (TLS) Protocol Version 1.2.
   - `RFC 2898` / `RFC 8018`: PKCS #5: Password-Based Cryptography Specification Version 2.1 (PBKDF2).
   - `RFC 6598`: IANA-Reserved IPv4 Prefix for Shared Address Space (CGNAT 100.64.0.0/10).
3. **Microsoft Learn Official Documentation**:
   - `System.Net.Sockets.Socket` & `TcpClient` / `TcpListener`.
   - `System.Net.Security.SslStream` (TLS Stream handling).
   - `System.IO.Stream.ReadExactlyAsync` (.NET 7+ API).
   - `System.Threading.Channels.Channel<T>` (High-performance async queues).
   - `rowversion (Transact-SQL)` & Concurrency Control in SQL Server.
4. **OWASP (Open Web Application Security Project)**:
   - OWASP Password Storage Cheat Sheet.
   - OWASP Denial of Service Cheat Sheet.

### TIER 2: Tài liệu đặc tả kỹ thuật dự án (Project Specifications)
- `LABGUARD_MASTER_PLAN.pdf` (Phiên bản 1.0 ngày 13/09/2026 do ChatGPT Work tạo): Bản thiết kế gốc gồm 26 mục, 60 task, 3 handbook cá nhân và 33 kịch bản kiểm thử.

### TIER 3: Báo cáo & Đồ án sinh viên khóa trước (Student References)
- Các bài tập Assignment 1–4, Lab 1–6 và Báo cáo tiến độ môn học NT106 các khóa trước trong thư mục `c:\Users\lyle3\OneDrive\Documents\Môn học\Lập trình mạng\LTMCB\`.

### TIER 4: Tài nguyên tham khảo cộng đồng (Secondary References)
- StackOverflow, blog kỹ thuật (chỉ dùng để tham khảo giải pháp fix lỗi cú pháp, tuyệt đối không dùng làm căn cứ kiến trúc).

---

## 2. ÁNH XẠ NGUỒN VÀO CÁC MỤC THIẾT KẾ CỦA LABGUARD

| Khối tính năng | Nguồn chính (Tier 1) | Nguồn thiết kế dự án (Tier 2) | Trạng thái xác thực |
|:---|:---|:---|:---:|
| TCP Socket & Framing | RFC 9293, RFC 1700, MS Learn `Socket` | Master Plan Mục I | ĐÃ XÁC THỰC 100% |
| Heartbeat & Liveness | UIT Viva Q2/Q17, RFC 9293 | Master Plan Mục C, G | ĐÃ XÁC THỰC 100% |
| Bảo mật TLS | RFC 8446, MS Learn `SslStream` | Master Plan Mục K | ĐÃ XÁC THỰC 100% |
| Băm mật khẩu | RFC 8018, OWASP Cheat Sheet | Master Plan Mục K | ĐÃ XÁC THỰC 100% |
| Concurrency (Channel/Thread) | MS Learn `System.Threading.Channels` | Master Plan Mục R | ĐÃ XÁC THỰC 100% |
| Database & `rowversion` | MS Learn SQL Server `rowversion` | Master Plan Mục J | ĐÃ XÁC THỰC 100% |
| Gateway & LB (P1) | NGINX docs, RFC Layer 4 Proxy | Master Plan Mục H, P1 | ĐÃ XÁC THỰC (P1 CONDITIONAL) |
| Target 50 Agent | UIT Course Scope | Master Plan Mục G | CHỈ TIÊU THIẾT KẾ (CHƯA ĐO THỰC TẾ) |
