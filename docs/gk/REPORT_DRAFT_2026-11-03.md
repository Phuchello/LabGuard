# DỰ THẢO BÁO CÁO GIỮA KỲ — LABGUARD

**Ngày soạn:** 03/11/2026 · **Nhóm:** 5 · **Môn học:** NT106 — Lập trình mạng căn bản · **Tình trạng:** CHƯA DUYỆT/NỘP.

> GK trong tài liệu hiện có được xem là buổi trình bày đề xuất và kết quả đến thời điểm báo cáo, không phải deadline toàn bộ sản phẩm.

## 1. Tóm tắt và bài toán

LabGuard hướng đến giám sát 40–50 máy Windows trong phòng thực hành bằng Agent → Central → SQL Server và Admin → Central. Mục tiêu người quản trị biết thiết bị nào có dấu hiệu bất thường, bằng chứng nào hỗ trợ kết luận, bước điều tra/khắc phục nào an toàn và khi nào thực sự phục hồi. Quy mô 40–50 chưa được nghiệm thu.

Giảng viên nhận xét xử lý sự cố còn đơn giản. Vì vậy định hướng mới là **Evidence-based Incident Navigator**: phân biệt dấu hiệu, giả thuyết, UNKNOWN và bằng chứng phục hồi; không chỉ tô màu CPU cao.

## 2. Công nghệ và kiến trúc

C#/.NET 9, WinForms, TCP length-prefix 4-byte Big-Endian + UTF-8 JSON, TLS/HELLO/AUTH theo contract, Central quản lý SQL Server 2022 Express. Admin không kết nối SQL trực tiếp. Replay/lost ACK cần idempotency và COMMIT trước ACK.

## 3. Dữ liệu & tình huống

P0 CPU/RAM/uptime, heartbeat/liveness, NIC, SERVICE_STATUS. Mở rộng E0 có giới hạn: DNS A query từ resolver được khai báo, TCP category, TLS name/cert validation, HTTPS GET nonce/marker. Không thu keylogger, screenshot hoặc dữ liệu cá nhân không cần thiết.

Định nghĩa: observation ≠ signal ≠ operational incident ≠ confirmed security incident. Thiếu/stale không phải số 0 hay máy chết.

## 4. Bằng chứng thực tế

| Nguồn | Đã xác minh | Chưa suy được |
|---|---|---|
| GitHub main `90562c5` | source/README/WBS/195 tests local lịch sử | secure full E2E |
| Local sanitized Checkpoint 4.1 | secure source + 3×276 Passed trong TRX | fresh rerun/capacity |
| Windows QA 03/11 | `LabGuard.Diagnostics` 89/89 Passed, 28 AT + 61 supplemental | E0 hai máy thật |
| E0 plan | bốn kịch bản và acceptance | runner/truy cập DNS/S/SQL/Admin live |

## 5. Demo GK đề xuất (phải ghi rõ mock/đề xuất nếu chưa làm)

- **Healthy:** P1/P2 đều DNS/TCP/TLS/HTTPS OK.
- **DNS lỗi P1:** P1 NXDOMAIN, P2 OK; không khẳng định service chết.
- **Ứng dụng 503:** TCP/TLS OK nhưng nghiệp vụ lỗi ở cả hai điểm.
- **P2 missing:** chỉ hiện P2 UNKNOWN, không tự suy offline.

Bước thực nghiệm sau nhóm duyệt: so sánh single-vantage/full-stack và multi-vantage/full-stack, fault ledger riêng và phép đo thực; không công bố accuracy/p95 khi chưa đo.

## 6. Phân công

A Phúc Agent/Protocol; B Quang Central/SQL; C Vy Admin WinForms; D Ngọc TCP testing và logic hỗ trợ sự cố. WBS 40 nhiệm vụ chính thức hiện ghi 6 XONG, 2 ĐANG LÀM, 32 CHƯA LÀM; local evidence tách riêng.

## 7. Các rủi ro/Gate

Local dirty worktree ≠ main. DB-01/C3/C4/C1/C2 vẫn cần duyệt. Cần P1/P2/S/DNS/certificate/rollback trước live E0. .NET 9 hỗ trợ đến 10/11/2026; việc đánh giá .NET 10 là backlog riêng, không tự nâng tại GK.

## 8. Cần bổ sung trước khi nộp

Điền giảng viên/lớp/lịch, đối chiếu mẫu báo cáo GK, chọn hình/screenshot thật, xác nhận thành viên review, bổ sung phần demo chạy thật nếu có. Không ký xác nhận thay ai.

**Nguồn:** [GK evidence snapshot](STATUS_2026-11-03.md); [GitHub commit](https://github.com/Phuchello/LabGuard/commit/90562c5bbbccac6022cc6658195aa227ee2b6cbe); TRX QA ngày 03/11/2026; Checkpoint 4.3; E0 QA plan.
