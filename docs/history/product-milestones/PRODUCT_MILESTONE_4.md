# BÁO CÁO NGHIỆM THU: PRODUCT MILESTONE 4 (LABGUARD — NT106)
## VÒNG ĐỜI XỬ LÝ SỰ CỐ, CHUYỂN TRẠNG THÁI VẬN HÀNH & KIỂM SOÁT ĐỒNG THỜI LẠC QUAN (OCC)

---

### 1. TỔNG QUAN KẾT QUẢ MILESTONE 4
Milestone 4 đã hoàn thành toàn diện 100% các tiêu chí nghiệm thu được đặt ra theo tôn chỉ **PRODUCT-FIRST**, tuân thủ nghiêm ngặt đặc tả giao thức Checkpoint 1 & 2 đã đóng băng (`03_PROTOCOL_SPEC.md`, `CONTRACT_MATRIX.md`), lược đồ cơ sở dữ liệu 10 bảng của `LabGuardDb`, và nguyên tắc bất biến Single Writer / Single Reader.

| Hạng mục nghiệm thu | Yêu cầu kỹ thuật | Trạng thái | Chi tiết kiểm chứng |
| :--- | :--- | :---: | :--- |
| **A. Bộ lệnh quản lý sự cố Admin** | Hỗ trợ 4 lệnh: `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START`, `INCIDENT_RESOLVE`. Trả về `COMMAND_RESULT` tương ứng qua socket. | **PASS** | `ClientConnection.cs` & `AdminClient.cs` định tuyến chính xác, tương quan `RequestId`/`CommandId`. |
| **B. Kiểm soát đồng thời lạc quan (OCC)** | Sử dụng SQL Server `rowversion` 8-byte, chuẩn hóa canonical hex `0x` + 16 ký tự thường. Nếu token không khớp: từ chối với mã lỗi `409 Conflict` (`ERR_CONCURRENCY_CONFLICT`), không làm thay đổi CSDL. | **PASS** | Verified qua unit tests và integration tests `IncidentCommand_WithMismatchedRowVersion_RejectsWith409Conflict_WithoutMutatingDatabase`. |
| **C. Khử trùng lặp & Idempotency** | Central Server kiểm tra `dbo.ProcessedCommands` theo `(UserId, CommandId)`. Nếu trùng lệnh + trùng `RequestDigest` (SHA-256): replay kết quả 200 OK. Nếu trùng `CommandId` nhưng khác digest: từ chối 409 (`ERR_IDEMPOTENCY_KEY_REUSE`). | **PASS** | Verified qua `IncidentCommand_DuplicateWithSameDigest_ReturnsIdempotentSuccess_WithoutReexecuting` và `IncidentCommand_DuplicateWithDifferentDigest_RejectsWith409IdempotencyKeyReuse`. |
| **D. FSM chuyển đổi trạng thái** | `OPEN` $\rightarrow$ `ACKNOWLEDGED` (qua `INCIDENT_ACK`).<br>`OPEN`/`ACKNOWLEDGED` $\rightarrow$ `IN_PROGRESS` (qua `INCIDENT_ASSIGN`/`INCIDENT_START`).<br>`ACKNOWLEDGED`/`IN_PROGRESS`/`RECOVERED` $\rightarrow$ `RESOLVED` (qua `INCIDENT_RESOLVE`). | **PASS** | Triển khai trong `DatabaseRepository.ExecuteIncidentCommandAsync` và ghi log vào `dbo.IncidentHistory`. |
| **E. Ràng buộc bảo vệ đóng sự cố (Resolve Guard)** | Không thể đóng sự cố khi điều kiện vật lý đang `ACTIVE` (`400 ERR_CONDITION_STILL_ACTIVE`). Bắt buộc phải có `resolutionNote` không rỗng (`400 ERR_MISSING_RESOLUTION_NOTE`). | **PASS** | Verified qua `IncidentResolve_WhenConditionActive_IsRejectedWith400` và `IncidentResolve_WhenConditionRecoveredAndNoteProvided_TransitionsToResolved`. |
| **F. Giao diện Admin Dashboard** | Bổ sung thanh công cụ thao tác 5 nút: `Tiếp nhận (ACK)`, `Phân công (Assign)`, `Bắt đầu (Start)`, `Đóng sự cố (Resolve)`, `Làm mới`. Tự động bật/tắt nút theo trạng thái FSM. Hộp thoại nhập liệu User ID & Ghi chú xử lý. | **PASS** | Verified qua ảnh chụp giao diện thực tế `docs/screenshots/dashboard_milestone4.png`. |
| **G. Đồng bộ thời gian thực** | Central Server phát sóng `INCIDENT_UPDATE` đến tất cả Admin Client đang kết nối khi trạng thái sự cố thay đổi. | **PASS** | Verified qua test TCP `IncidentCommand_OverTcp_SucceedsAndReceivesCommandResult`. |

---

### 2. KẾT QUẢ AUTOMATED TEST SUITE (100% PASS)
Hệ thống kiểm thử tự động đạt **77/77 test PASS** (0 test fail, 0 test bị bỏ qua):
- **`LabGuard.Protocol.Tests`**: 20/20 test passed.
- **`LabGuard.Agent.Tests`**: 5/5 test passed.
- **`LabGuard.Central.Tests`**: 52/52 test passed (+9 test mới trong `IncidentWorkflowAndOccTests.cs`).

```text
Passed!  - Failed: 0, Passed: 20, Skipped: 0, Total: 20, Duration: 53 ms - LabGuard.Protocol.Tests.dll
Passed!  - Failed: 0, Passed:  5, Skipped: 0, Total:  5, Duration: 25 ms - LabGuard.Agent.Tests.dll
Passed!  - Failed: 0, Passed: 52, Skipped: 0, Total: 52, Duration: 1 s  - LabGuard.Central.Tests.dll
```

---

### 3. HÌNH ẢNH GIAO DIỆN ADMIN THỰC TẾ (DEMO)
Ảnh chụp màn hình thực tế của ứng dụng WinForms Admin (`LabGuard.Admin.exe`) kết nối với Central Server thực tế qua TCP và trực quan hóa toàn bộ vòng đời xử lý sự cố:

![Admin Dashboard Milestone 4](docs/screenshots/dashboard_milestone4.png)

#### Điểm nổi bật trên Dashboard:
1. **Thanh thao tác sự cố thông minh (Action Toolbar)**:
   - Các nút chức năng được tô màu trực quan:
     - `Tiếp nhận (ACK)`: Màu Xanh dương (Slate/Blue).
     - `Phân công (Assign)`: Màu Tím Indigo.
     - `Bắt đầu xử lý (Start)`: Màu Cam Hổ Phách (Amber).
     - `Đóng sự cố (Resolve)`: Màu Xanh Ngọc (Emerald).
     - `Làm mới`: Màu Xám Slate.
   - Khi chọn sự cố `TEST-PC-IDEMP` (đang ở trạng thái `ACKNOWLEDGED` và điều kiện `ACTIVE`):
     - Nút `Tiếp nhận (ACK)` tự động chuyển sang màu xám mờ (Disabled) vì sự cố đã được tiếp nhận trước đó.
     - Nút `Phân công (Assign)` và `Bắt đầu (Start)` sáng rõ (Enabled) cho phép người trực phòng phân công hoặc tiến hành xử lý.
     - Nút `Đóng sự cố (Resolve)` tự động chuyển sang xám mờ (Disabled) nhằm ngăn chặn việc đóng sự cố khi lỗi vật lý vẫn đang diễn ra (`ACTIVE`).
2. **Cột hiển thị trạng thái vận hành (`TIẾN TRÌNH`)**:
   - Bổ sung cột `TIẾN TRÌNH` (`WorkflowState`) song song với cột `ĐIỀU KIỆN` (`ConditionState`), hiển thị rõ ràng nhãn `OPEN`, `ACKNOWLEDGED`, `IN_PROGRESS`, `RECOVERED`, `RESOLVED` với màu sắc nhận diện chuẩn UI/UX hiện đại.
3. **Cơ chế xử lý xung đột OCC 409**:
   - Khi có hai quản trị viên cùng thao tác trên một sự cố với token `RowVersion` cũ, ứng dụng hiển thị hộp thoại cảnh báo thân thiện và tự động kích hoạt làm mới danh sách dữ liệu.

---

### 4. BẢNG GHI DỮ LIỆU THỰC TẾ TRONG SQL SERVER (`LabGuardDb`)
```text
--- dbo.Incidents ---
  Incident #41 | Device: TEST-PC-IDEMP | Rule: HIGH_MEMORY | State: ACTIVE    | WF: ACKNOWLEDGED | RV: 0x0000000000000827
  Incident #40 | Device: TEST-TCP-OCC   | Rule: HIGH_CPU    | State: ACTIVE    | WF: ACKNOWLEDGED | RV: 0x0000000000000823
  Incident #39 | Device: TEST-PC-RES-OK | Rule: OFFLINE     | State: RECOVERED | WF: RESOLVED     | RV: 0x0000000000000821
  Incident #38 | Device: TEST-PC-OCC    | Rule: HIGH_CPU    | State: ACTIVE    | WF: OPEN         | RV: 0x000000000000081d

--- dbo.IncidentHistory ---
  History #25 | IncId: 41 | ChangedBy: 1 | OldWF: OPEN         | NewWF: ACKNOWLEDGED | Note: Sự cố đã được tiếp nhận xử lý
  History #24 | IncId: 40 | ChangedBy: 1 | OldWF: OPEN         | NewWF: ACKNOWLEDGED | Note: Sự cố đã được tiếp nhận xử lý
  History #23 | IncId: 39 | ChangedBy: 1 | OldWF: RECOVERED    | NewWF: RESOLVED     | Note: Da kiem tra lai ket noi cap mang phong may.

--- dbo.ProcessedCommands ---
  UserId: 1 | CmdId: 98a5813d-5192-4bb3-8b74-4b5333f02179 | Digest: e4948a8798993fafe39906666a7b1b365f57... | Replay: 200 OK
```
