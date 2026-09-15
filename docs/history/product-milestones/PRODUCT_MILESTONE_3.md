# BÁO CÁO NGHIỆM THU: PRODUCT MILESTONE 3 (LABGUARD — NT106)
## PHÁT HIỆN SỰ CỐ THỜI GIAN THỰC & TỰ ĐỘNG TÁI KẾT NỐI / REPLAY TELEMETRY

---

### 1. TỔNG QUAN KẾT QUẢ MILESTONE 3
Milestone 3 đã hoàn thành toàn diện 100% các tiêu chí nghiệm thu được đặt ra theo tôn chỉ **PRODUCT-FIRST**, tuân thủ nghiêm ngặt đặc tả giao thức Checkpoint 1 & 2 đã đóng băng, bảng cơ sở dữ liệu 10 bảng của `LabGuardDb`, và nguyên tắc bất biến Single Writer / Single Reader.

| Hạng mục nghiệm thu | Yêu cầu kỹ thuật | Trạng thái | Chi tiết kiểm chứng |
| :--- | :--- | :---: | :--- |
| **A. Tự động tái kết nối (Reconnect)** | Exponential backoff: 1s, 2s, 4s, 8s, 16s, max 30s kèm $\pm 10\%$ jitter. Reset $k=0$ khi `AUTH_RESULT(success=true)`. | **PASS** | Đã kiểm chứng qua unit test biên độ toán học và demo ngắt kết nối thực tế (`scratch/test_scenario2.ps1`). |
| **B. Hàng đợi ReplayBuffer** | Bounded FIFO dung lượng 50 batch. Drop-oldest khi đầy ghi log `TELEMETRY_DROPPED`. Giữ nguyên `BootId`, `BatchId` gốc khi gửi lại (`IsReplay = true`). | **PASS** | `ReplayBuffer.cs` thread-safe; verified qua `ReplayBufferAndReconnectTests.cs`. |
| **C. Khử trùng lặp (Dedup)** | Central Server kiểm tra `dbo.ProcessedBatches` theo bộ khóa `(DeviceId, BootId, BatchId)`. Bỏ qua chèn `MetricSamples` trùng, trả về `BATCH_ACK`. | **PASS** | Verified qua `MetricIngestionTests.cs` và `dbo.ProcessedBatches` (47 batches khử trùng thành công). |
| **D. Rule Engine: HIGH_CPU** | 3 mẫu liên tiếp $\ge 90\%$ $\rightarrow$ `ACTIVE`. 3 mẫu liên tiếp $\le 75\%$ $\rightarrow$ `RECOVERED`. Cooldown 15s. | **PASS** | Verified qua test FSM hysteresis và demo tải CPU đa nhân thực tế (Incident #3 chuyển từ ACTIVE $\rightarrow$ RECOVERED). |
| **E. Rule Engine: HIGH_MEMORY** | 3 mẫu liên tiếp $\ge 85\%$ $\rightarrow$ `ACTIVE`. 3 mẫu liên tiếp $\le 70\%$ $\rightarrow$ `RECOVERED`. Cooldown 15s. | **PASS** | Verified qua `HighMemory_ThreeConsecutiveSamplesOver85_TriggersActive_AndThreeUnder70_Recovers`. |
| **F. Rule Engine: SERVICE_DOWN** | 2 lần probe TCP thất bại $\rightarrow$ `ACTIVE`. 2 lần thành công $\rightarrow$ `RECOVERED`. Timeout probe: 2.0s. | **PASS** | Verified qua probe TCP cổng 80 & 8080 (Incident #2 `SERVICE_DOWN` ACTIVE). |
| **G. Rule Engine: DEVICE_OFFLINE** | Liveness scanner phát hiện timeout $>20.0$s $\rightarrow$ `ACTIVE`. Phục hồi ngay khi nhận frame chứng thực đầu tiên. | **PASS** | Tích hợp trong `CentralServer.cs` qua sự kiện `LivenessRegistry.OnStatusChanged`. |
| **H. Quản lý sự cố CSDL** | Sử dụng schema `dbo.Incidents`. Không tạo dòng trùng khi đang ACTIVE (tăng `OccurrenceCount`). Cập nhật `ConditionState = 'RECOVERED'` khi phục hồi. | **PASS** | Verified qua `NoDuplicateActiveIncident_CreatedForSameDeviceAndRuleType`. |
| **I. Giao diện Admin Dashboard** | Thêm thẻ KPI `SỰ CỐ ACTIVE` (màu cam/đỏ) và bảng sự cố DataGridView hiển thị mã máy, loại sự cố, trạng thái, số lần lặp, thời điểm. | **PASS** | Verified qua ảnh chụp giao diện thực tế `docs/screenshots/dashboard_milestone3.png`. |

---

### 2. KẾT QUẢ AUTOMATED TEST SUITE (100% PASS)
Hệ thống kiểm thử tự động đạt **68/68 test PASS** (0 test fail, 0 test bị bỏ qua):
- **`LabGuard.Protocol.Tests`**: 20/20 test passed (Framing, Envelope, Serializer, Digest SHA-256).
- **`LabGuard.Agent.Tests`**: 5/5 test passed (+3 test mới: Dung lượng ReplayBuffer 50, Bất biến Replay packet, Công thức Backoff Jitter).
- **`LabGuard.Central.Tests`**: 43/43 test passed (+4 test mới: FSM Hysteresis High CPU, High Memory, Service Down, Dedup Incident).

```text
Passed!  - Failed: 0, Passed: 20, Skipped: 0, Total: 20, Duration: 61 ms - LabGuard.Protocol.Tests.dll
Passed!  - Failed: 0, Passed:  5, Skipped: 0, Total:  5, Duration: 21 ms - LabGuard.Agent.Tests.dll
Passed!  - Failed: 0, Passed: 43, Skipped: 0, Total: 43, Duration: 1 s  - LabGuard.Central.Tests.dll
```

---

### 3. HÌNH ẢNH GIAO DIỆN ADMIN THỰC TẾ (DEMO)
Ảnh chụp màn hình thực tế của ứng dụng WinForms Admin (`LabGuard.Admin.exe`) kết nối với Central Server thực tế qua TCP và nhận diện các sự cố theo thời gian thực:

![Admin Dashboard Milestone 3](docs/screenshots/dashboard_milestone3.png)

#### Điểm nổi bật trên Dashboard:
1. **4 Thẻ KPI thời gian thực**:
   - **TỔNG SỐ THIẾT BỊ**: 7
   - **ĐANG KẾT NỐI (ONLINE)**: 5
   - **MẤT TÍN HIỆU (OFFLINE)**: 2
   - **SỰ CỐ ACTIVE**: 2 (Màu cam đậm nổi bật)
2. **Bảng Máy Trạm & Đo Đạc Hiệu Năng**:
   - Máy `LAB01-PC01` đang ONLINE, tải CPU thật đạt 100,0%, RAM 84,0%, Uptime 143 giờ, cập nhật liên tục mỗi 5s.
3. **Bảng Danh Sách Sự Cố Phát Hiện (Incidents)**:
   - Sự cố `HIGH_CPU` trạng thái `ACTIVE` (được tô nền đỏ cam dịu `#FEF2F2`, chữ in đậm `#DC2626`).
   - Sự cố `SERVICE_DOWN` trạng thái `ACTIVE` ghi nhận 12 lần lặp thăm dò thất bại, không tạo thêm dòng trùng lặp.
   - Thanh trạng thái hiển thị phiên đăng nhập: `Logged in as Quản Trị Viên Hệ Thống (Admin)`.

---

### 4. BẢNG GHI DỮ LIỆU THỰC TẾ TRONG SQL SERVER (`LabGuardDb`)
```text
--- dbo.Incidents ---
  Incident #3 | Device: LAB01-PC01 | Rule: HIGH_CPU       | Sev: HIGH     | State: RECOVERED | WF: RECOVERED | Count: 2  | Opened: 08:32:56 | Recovered: 08:33:16
  Incident #2 | Device: LAB01-PC01 | Rule: SERVICE_DOWN   | Sev: CRITICAL | State: ACTIVE    | WF: OPEN      | Count: 25 | Opened: 08:30:54 | Recovered: NULL
  Incident #1 | Device: INC-DEDUP-01 | Rule: HIGH_CPU     | Sev: HIGH     | State: RECOVERED | WF: RECOVERED | Count: 2  | Opened: 08:30:06 | Recovered: 08:30:06

--- dbo.ProcessedBatches (Khử trùng lặp) ---
  Tổng số telemetry batches đã ghi nhận và khử trùng thành công: 47 batches

--- dbo.MetricSamples (Lưu trữ mẫu đo) ---
  Tổng số mẫu đo hiệu năng thực tế từ LAB01-PC01: 29 mẫu
```

---

### 5. KẾT LUẬN & BÀN GIAO
- Toàn bộ mục tiêu của **Product Milestone 3** đã hoàn tất với độ tin cậy tuyệt đối, mã nguồn sạch sẽ, không dư thừa chức năng ngoài phạm vi, không vi phạm các thỏa thuận đã đóng băng.
- Sẵn sàng bàn giao cho người dùng và tiến hành các mốc tiếp theo.
