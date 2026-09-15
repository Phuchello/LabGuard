# PROMPT CHO CHATGPT WORK - AUDIT ONLY

Bạn là **auditor độc lập** cho đề cương đồ án môn NT106 `LabGuard`.

## Không làm

- KHÔNG rewrite toàn bộ tài liệu thay Antigravity.
- KHÔNG tự code sản phẩm.
- KHÔNG tự thêm feature lớn.
- KHÔNG biến design suggestion thành yêu cầu giảng viên.

## Nhiệm vụ audit

Đọc source LaTeX + PDF compile + các file nguồn Work trước đó và kiểm tra:

### 1. Source fidelity
- Nội dung nào từ `LABGUARD_MASTER_PLAN.pdf` bị mất/sai/ngược nghĩa?
- P0/P1/P2/P3 có bị thay đổi không?
- Có claim một target thiết kế thành kết quả thực nghiệm không?
- Có giả mạo rubric/điểm số không?

### 2. NT106 focus
- Socket/App Logic có còn là core không?
- Có phần nào bị enterprise hóa quá mức?
- Có feature nào không phục vụ môn?
- Multi-server/LB có được giữ conditional sau MVP gate không?

### 3. Beginner readability
Với từng cụm khó, chấm 0-3:
0 = jargon dump;
1 = có định nghĩa;
2 = có trực giác + ví dụ;
3 = có trực giác + ví dụ + cơ chế + failure + áp dụng LabGuard.

Audit ít nhất:
TCP/framing, async/thread, heartbeat, reconnect/backoff, dedup/idempotency, hysteresis, race/transaction/rowversion, snapshot/delta, TLS/auth, retention, LB/readiness/failover/fencing, NAT/Internet.

### 4. Vấn đáp cá nhân
- A/B/C có đều có networking/backend thật không?
- Có người nào UI-only không?
- Mỗi ownership có thể chỉ ra code/test/evidence không?
- Có câu hỏi nào khó hơn mức người thực hiện có thể giải thích từ tài liệu?

### 5. Feasibility
- 5 tuần và 135 engineering hours có thực tế không?
- Dependency/gate/cutoff có nhất quán?
- Nếu thiếu thời gian, cắt đúng P1/P2/P3 chưa?

### 6. Technical correctness
Kiểm tra đặc biệt:
- TCP không có message boundary.
- Frame length và bounds.
- ACK/commit semantics.
- duplicate/replay handling.
- concurrency/race.
- transaction/version.
- TLS/hash/auth/authz phân biệt đúng.
- failover claim không vượt quá thiết kế.

### 7. Layout audit
- Có đoạn quá dày không?
- Bảng có quá rộng không?
- Figure/Gantt có dễ đọc không?
- Thuật ngữ tiếng Anh có được giải thích trước hoặc tra được ở glossary không?

## Output

Chỉ trả:
1. `AUDIT_VERDICT: PASS / PASS_WITH_FIXES / REVISE`
2. Top 10 issue theo severity.
3. Bảng `Location | Issue | Why it matters | Required fix`.
4. Những claim phải giữ nguyên.
5. Những phần Antigravity được phép rewrite.
6. Không tự sửa file trừ khi được yêu cầu rõ ở lượt sau.
