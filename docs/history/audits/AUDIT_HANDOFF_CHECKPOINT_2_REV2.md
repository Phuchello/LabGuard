# LABGUARD — BIÊN BẢN BÀN GIAO KIỂM TOÁN CHECKPOINT 2 (REV2)
**Mã tài liệu:** `AUDIT_HANDOFF_CHECKPOINT_2_REV2.md`  
**Chuyên đề:** Biên bản Bàn giao Tái Kiểm toán Độc lập Checkpoint 2 (REV2 Final Consistency Patch)  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Trạng thái đề xuất:** CHECKPOINT 2 REV2 — READY FOR FINAL INDEPENDENT AUDIT  
**Người lập:** Nhóm 3 sinh viên (Người A - Agent, Người B - Server/DB, Người C - Admin WinForms)  
**Kiểm toán viên độc lập:** Independent Auditor (ChatGPT Work)  

---

## 1. TỔNG QUAN BÀN GIAO CHECKPOINT 2 REV2

Thực hiện nghiêm túc chỉ đạo kiểm toán độc lập sau vòng tái thẩm định Checkpoint 2 REV1, nhóm đã hoàn tất gói vá lỗi nhất quán cuối cùng (**REV2 Final Consistency Patch**).

Toàn bộ các mâu thuẫn kỹ thuật biên và những điểm chưa rõ ràng đã được xử lý triệt để, không mở rộng phạm vi, không phát minh giao thức mới, không làm loãng tài liệu và tuân thủ tuyệt đối kỷ luật trạng thái:
- **Tình trạng hiện tại:** `CHECKPOINT 2 REV2 — READY FOR FINAL INDEPENDENT AUDIT` (Chưa đóng băng, chờ phán quyết độc lập của Kiểm toán viên).
- **Văn kiện quyết định duy nhất có hiệu lực ở thư mục gốc:** `CP2_DECISIONS_REV2.md`. Toàn bộ văn bản quyết định và bàn giao cũ (`CP2_DECISIONS.md`, `CP2_DECISIONS_REV1.md`, `AUDIT_HANDOFF_CHECKPOINT_2.md`, `AUDIT_HANDOFF_CHECKPOINT_2_REV1.md`, `CP2_CHO_NHOM_3_NGUOI.md`, `CP2_CHO_NHOM_3_NGUOI_REV1.md`) đã được chuyển vào thư mục `archive/` và gắn tiêu đề `SUPERSEDED / HISTORICAL ARTIFACT`.

---

## 2. BẢNG CHI TIẾT GIẢI TRÌNH 12 HẠNG MỤC PHẢN HỒI KIỂM TOÁN (MỤC A ĐẾN L)

| Hạng mục | Vấn đề Kiểm toán phát hiện | Quyết định Kỹ thuật Chuẩn hóa (REV2) | Vị trí cập nhật trong tài liệu |
|:---|:---|:---|:---|
| **A. `SERVICE_STATUS` Delivery Semantics** | Mâu thuẫn giữa At-Least-Once qua ReplayBuffer và việc không có ACK riêng khiến Agent không biết khi nào xóa khỏi ReplayBuffer. | **Khóa cứng:** `SERVICE_STATUS` là **Latest-State Telemetry**, KHÔNG đưa vào `ReplayBuffer`, không thử lại tầng ứng dụng khi mất kết nối. Lần thăm dò kế tiếp sẽ gửi trạng thái mới nhất. Server thực hiện Idempotent UPSERT trên bảng `Services` theo `(DeviceId, ServiceName)`. | `research/03_CONCURRENCY_RELIABILITY.md` (§7)<br>`research/05_DELIVERY_SEMANTICS.md` (§2, §3.4)<br>`CP2_DECISIONS_REV2.md` (CP2-018) |
| **B. Telemetry Ownership & Dropped Queue** | Chưa làm rõ ranh giới quyền sở hữu mẻ đo khi Server chưa commit và hành vi khi ReplayBuffer bị tràn. | **Khóa cứng:** Agent giữ quyền sở hữu `METRIC_BATCH` trong Unacknowledged Telemetry Queue (`ReplayBuffer`, sức chứa **50 batches**) cho đến khi nhận được `BATCH_ACK` tương ứng. Nếu đầy hàng đợi: áp dụng FIFO Drop-Oldest và **bắt buộc ghi log sự kiện `TELEMETRY_DROPPED`** kèm `batchId`. | `research/03_CONCURRENCY_RELIABILITY.md` (§3.3)<br>`research/05_DELIVERY_SEMANTICS.md` (§3.4)<br>`CP2_DECISIONS_REV2.md` (CP2-003, CP2-016) |
| **C. Central Ingress Pipeline Architecture** | Còn sót lại mâu thuẫn về điều phối worker và concurrent dispatch cho truy vấn chỉ đọc. | **Khóa cứng:** ONE Single Logical Ingress Worker processes the P0 pipeline. Read-only queries are also handled through this controlled worker. No loose Task.Run/fire-and-forget dispatch is required in P0. Partitioning / parallel query workers are FUTURE optimization only. | `research/03_CONCURRENCY_RELIABILITY.md` (§4.2)<br>`CP2_DECISIONS_REV2.md` (CP2-002, ADR-01) |
| **D. Uptime trong CSDL & Telemetry** | Bảng `MetricSamples` trong DDL thiếu cột `UptimeSeconds` dù tài liệu có đề cập đến chỉ số Uptime. | **Khóa cứng:** Bổ sung cột `UptimeSeconds BIGINT NOT NULL` vào bảng `dbo.MetricSamples`. Xác nhận phạm vi dữ liệu thu thập P0 gồm: CPU, RAM, Uptime, Services. Chỉ số Disk hoàn toàn loại khỏi MVP theo Checkpoint 1 baseline. | `research/06_DATABASE.md` (§2 DDL)<br>`CP2_DECISIONS_REV2.md` (CP2-017)<br>`CP2_CHO_NHOM_3_NGUOI_REV2.md` (Phần 2, Phần 3) |
| **E. Định nghĩa Kiểm thử Liveness (`CP2-TEST-006`)** | Diễn đạt dễ gây hiểu nhầm Server phán quyết OFFLINE ngay tại đúng tích tắc 20.0s thay vì tại chu kỳ quét hẹn giờ. | **Khóa cứng:** Chu kỳ quét liveness là 2.5s (`LivenessCheckInterval = 2.5s`). Server đánh giá dựa trên thời gian đơn điệu trôi qua (`elapsed > 20.0s`). Cửa sổ phát hiện xảy ra tại lần quét kế tiếp: `> 20.0s và xấp xỉ <= 22.5s`. Bằng chứng ghi nhận: `lastSeen`, `scanTimestamp`, `elapsed`, `decision = DEVICE_OFFLINE`. | `research/04_MONITORING_DETECTION.md` (§2.2)<br>`research/CP2_TEST_MATRIX.md` (CP2-TEST-006)<br>`CP2_DECISIONS_REV2.md` (CP2-005) |
| **F. Bằng chứng Kiểm thử Backpressure (`CP2-TEST-003`)** | Bắt buộc gói tin TCP Zero Window trong Wireshark là quá khắt khe đối với đồ án lab cục bộ. | **Khóa cứng:** Bằng chứng bắt buộc (Mandatory Evidence) là: số lượng phần tử trong Ingress Channel $\le 1,000$, bộ nhớ RAM Server ổn định trong ngưỡng, không crash. Bằng chứng gói TCP Zero Window / Window Probe qua Wireshark là **tùy chọn bổ trợ (Optional supporting evidence)**. | `research/CP2_TEST_MATRIX.md` (CP2-TEST-003) |
| **G. Hàng đợi Outbox Client (`ClientOutboxChannel`)** | Chưa chuẩn hóa dung lượng và cơ chế hàng đợi outbox của WinForms Client. | **Khóa cứng:** `ClientOutboxChannel` có dung lượng **128 items**, áp dụng mô hình Single Writer Loop, `BoundedChannelFullMode.Wait`. Không yêu cầu bộ lập lịch ưu tiên khắt khe (strict priority scheduler) trong phạm vi MVP P0. | `research/03_CONCURRENCY_RELIABILITY.md` (§4.4)<br>`CP2_DECISIONS_REV2.md` (CP2-010)<br>`CP2_CHO_NHOM_3_NGUOI_REV2.md` (Phần 4) |
| **H. Danh mục Bản tin & Lời văn Bàn giao** | Cần đồng bộ lời văn bảng Delivery Semantics với danh mục 69 bản tin canonical. | **Khóa cứng:** Bảng ngữ nghĩa truyền tải tại `research/05_DELIVERY_SEMANTICS.md` ghi rõ: *"Ma trận delivery semantics cho các bản tin P0 cốt lõi, all message names conform to the canonical 69-message catalog"*. 100% tên bản tin chuẩn theo `03_PROTOCOL_SPEC.md`. | `research/05_DELIVERY_SEMANTICS.md` (§2) |
| **I. Làm sạch Tuyên bố Hiệu năng & Từ ngữ Tuyệt đối hóa** | Rà soát các tuyên bố hiệu năng quá mức, các số liệu suy đoán hoặc giả định chưa kiểm chứng qua đo đạc thực tế. | **Khóa cứng:** Đã rà soát và thay thế bằng các thuật ngữ kỹ thuật khách quan, có thể đo lường được; loại bỏ các số liệu suy đoán chưa chạy thực tế. | Toàn bộ tài liệu `research/*.md` |
| **J. Cấu trúc Tài liệu & Thư mục Lưu trữ (`archive/`)** | Nhiều văn bản quyết định và bàn giao cũ cùng tồn tại ở thư mục gốc gây nhầm lẫn nguồn chân lý. | **Khóa cứng:** Tạo thư mục `archive/` và di chuyển toàn bộ các phiên bản cũ vào lưu trữ lịch sử kèm banner superseded. Thư mục gốc chỉ lưu duy nhất một văn kiện quyết định có hiệu lực: `CP2_DECISIONS_REV2.md`. | `archive/*`<br>`CP2_DECISIONS_REV2.md` |
| **K. Tiêu chí Đo lường Bão Kết nối (`CP2-TEST-019`)** | Tiêu chuẩn nghiệm thu kiểm thử Reconnect Storm chưa đủ định lượng. | **Khóa cứng:** Nhờ Jitter $\pm 10\%$, các lần thử kết nối không bị đồng pha tại cùng một timestamp; toàn bộ 20/20 simulator agents tái kết nối và xác thực thành công trong cửa sổ quan sát (60s); Server không crash, số session xác thực trong bộ nhớ phục hồi đúng 20. | `research/CP2_TEST_MATRIX.md` (CP2-TEST-019) |
| **L. Kiểm tra Tự động & Sẵn sàng Kiểm toán** | Cập nhật kịch bản kiểm tra tự động và tạo báo cáo kiểm thử. | **Khóa cứng:** Kịch bản `validate_cp2_protocol_names.py` và `validate_cp2_decisions.py` được cập nhật kiểm tra danh sách file REV2, xác nhận hợp lệ 100%. Báo cáo kết quả lưu tại `CP2_VALIDATION_REPORT_REV2.md`. | `scratch/*.py`<br>`CP2_VALIDATION_REPORT_REV2.md` |

---

## 3. DANH MỤC TÀI LIỆU CHECKPOINT 2 REV2 HOÀN CHỈNH

1. **Văn bản Quyết định Duy nhất (Authoritative Master Decision):**
   - [`CP2_DECISIONS_REV2.md`](CP2_DECISIONS_REV2.md): 18 quyết định kiến trúc đóng băng chuẩn xác.
2. **Sổ tay Thực chiến Sinh viên (Student Handbook):**
   - [`CP2_CHO_NHOM_3_NGUOI_REV2.md`](CP2_CHO_NHOM_3_NGUOI_REV2.md): Hướng dẫn phân công, class, tham số cụ thể và câu hỏi vấn đáp cho 3 bạn A, B, C.
3. **Bộ Chuyên đề Nghiên cứu Kỹ thuật (Technical Research Topics):**
   - [`research/03_CONCURRENCY_RELIABILITY.md`](research/03_CONCURRENCY_RELIABILITY.md): Single Ingress Worker, Telemetry Ownership, Backpressure, Jitter.
   - [`research/04_MONITORING_DETECTION.md`](research/04_MONITORING_DETECTION.md): Monotonic Clock Liveness (window >20s and <=22.5s), Streak, Hysteresis, Dual-Layer FSM.
   - [`research/05_DELIVERY_SEMANTICS.md`](research/05_DELIVERY_SEMANTICS.md): Latest-state `SERVICE_STATUS`, `ProcessedBatches`, SHA-256 `RequestDigest`, Commit-Then-Respond.
   - [`research/06_DATABASE.md`](research/06_DATABASE.md): T-SQL DDL 10 bảng, `MetricSamples` có `UptimeSeconds BIGINT NOT NULL` (Disk loại khỏi MVP), `rowversion` OCC, Degraded Mode.
   - [`research/CP2_TEST_MATRIX.md`](research/CP2_TEST_MATRIX.md): 20 kịch bản kiểm thử kỹ thuật định lượng.
   - [`research/CP2_OPEN_ISSUES.md`](research/CP2_OPEN_ISSUES.md): Nhật ký rà soát 18 quyết định CP2 REV2.
4. **Báo cáo Kiểm tra Tự động:**
   - [`CP2_VALIDATION_REPORT_REV2.md`](CP2_VALIDATION_REPORT_REV2.md): Bằng chứng chạy tự động mã thoát 0 của 2 kịch bản kiểm tra protocol và decisions.

---

## 4. ĐỀ NGHỊ KIỂM TOÁN VIÊN ĐỘC LẬP THẨM ĐỊNH

Kính đề nghị Kiểm toán viên độc lập thẩm định toàn bộ gói bàn giao **Checkpoint 2 REV2**. Nếu đáp ứng đầy đủ tiêu chuẩn kỹ thuật của môn học NT106, nhóm xin phép được tiếp nhận phê duyệt chính thức để chuyển sang giai đoạn Checkpoint 3 (Viết mã nguồn sản phẩm).
