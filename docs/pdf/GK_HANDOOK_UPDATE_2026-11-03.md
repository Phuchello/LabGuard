# Nhật ký bổ sung sổ tay PDF — GK 03/11/2026

**Không ghi đè 5 PDF hướng dẫn đang được theo dõi trên main khi chưa rebuild và kiểm tra trình bày.** Nguồn LaTeX hiện phản ánh trạng thái 18/10, còn snapshot QA 03/11 nằm trong [docs/gk/STATUS_2026-11-03.md](../gk/STATUS_2026-11-03.md). Khi tái biên dịch chính thức, nhóm review theo 5 tập:

| Tập | Nội dung bổ sung ngày 03/11 |
|---|---|
| Tổng quan | Hai baseline main/local, 195 vs 276 vs 89, E0/E1 và các approvals còn chặn |
| A Phúc | Agent/Protocol/TLS/ACK/replay, P0 NIC/SERVICE_STATUS, review DNS/TCP/TLS |
| B Quang | SQL COMMIT-before-ACK, dedup, DB-01/C3/C4, authorized Admin, observer-degraded |
| C Vy | Dashboard số liệu thật, stale/UNKNOWN, evidence timeline, Admin auth/session, WinForms |
| D Ngọc | EvidenceEvaluator 89/89 QA, 28 AT, 61 bổ sung; E0 runner & giới hạn thí nghiệm |

Các bản phụ lục PDF ngày 03/11 được tạo để nhóm kiểm tra nội dung ngoài Git. Không gọi các PDF tracked trước đây đã cập nhật nếu chưa thay binary. Giữ nguyên font, mục lục, links và trình tự kiểm tra trong [docs/pdf/README.md](README.md). Khi rebuild: XeLaTeX ba pass, candidate staging, render/QA từng trang, chỉ publish khi nhóm duyệt. Không sửa code/protocol/schema.

**Lưu ý nguồn:** `docs/pdf/source/sync_status.tex` và `sync_status_pr14.tex` vẫn là tài liệu mốc 18/10; không sửa lịch sử hoặc tự rewrite toàn bộ sách từ snapshot 03/11.
