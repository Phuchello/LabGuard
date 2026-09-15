# LABGUARD — CHECKPOINT 3 EXECUTION REPORT (SESSION 01)
**Mã tài liệu:** `CP3_CHECKPOINT_01.md`  
**Chuyên đề:** Báo cáo nghiệm thu kỹ thuật Cổng 1 (Solution Build) & Cổng 2 (`FRAMECODEC_GATE`)  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Thời điểm ghi nhận:** 14/09/2026  
**Trạng thái phiên:** COMPLETED — STOPPED AT FRAMECODEC_GATE FOR INDEPENDENT REVIEW  

---

## 1. MỤC TIÊU PHIÊN THỰC THI (SESSION 01 OBJECTIVES)
- Thiết lập toàn bộ cấu trúc dự án .NET 9 chuẩn hóa cho LabGuard (`LabGuard.sln`, 5 dự án mã nguồn `src/`, 3 dự án kiểm thử `tests/`, và thư mục cơ sở dữ liệu `database/`).
- Triển khai thư viện dùng chung `LabGuard.Protocol` bao gồm:
  - `WireEnvelope<T>` (chuẩn hóa đúng 6 trường JSON Envelope theo RFC 8259).
  - `FrameCodec` (chuẩn hóa Length-Prefix 4-byte UInt32 Big-Endian, ngưỡng $0 < N \le 262,144$ bytes).
  - Danh mục 69 bản tin chuẩn (`MessageTypes.cs`).
  - Danh mục mã lỗi chuẩn (`ErrorCodes.cs`).
  - Thuật toán băm xác định `RequestDigest` và tuần tự hóa JSON chuẩn mực (`ProtocolSerializer.cs`).
  - Toàn bộ DTOs cho các bản tin cốt lõi P0 (`HandshakeDtos`, `TelemetryDtos`, `AdminDtos`, `EventDtos`).
- Xây dựng bộ kiểm thử đơn vị `tests/LabGuard.Protocol.Tests` mô phỏng đầy đủ 7 kịch bản dòng byte TCP theo đặc tả `CP2-TEST-001`.
- **Điều kiện dừng (Stop Condition):** Dừng ngay sau khi `FRAMECODEC_GATE` đạt PASS. Không tự ý mở rộng sang GATE 3 (Sockets/Agent/Central).

---

## 2. DANH MỤC THÀNH PHẦN ĐÃ THỰC THI (IMPLEMENTED ARTIFACTS)

### 2.1. Cấu trúc Solution & Projects
1. `LabGuard.sln`
2. `src/LabGuard.Protocol/LabGuard.Protocol.csproj` (Target: `net9.0`)
3. `src/LabGuard.Agent/LabGuard.Agent.csproj` (Target: `net9.0`)
4. `src/LabGuard.Central/LabGuard.Central.csproj` (Target: `net9.0`)
5. `src/LabGuard.Admin/LabGuard.Admin.csproj` (Target: `net9.0-windows`)
6. `src/LabGuard.Simulator/LabGuard.Simulator.csproj` (Target: `net9.0`)
7. `tests/LabGuard.Protocol.Tests/LabGuard.Protocol.Tests.csproj` (Target: `net9.0`)
8. `tests/LabGuard.Agent.Tests/LabGuard.Agent.Tests.csproj` (Target: `net9.0`)
9. `tests/LabGuard.Central.Tests/LabGuard.Central.Tests.csproj` (Target: `net9.0`)

### 2.2. Cơ sở dữ liệu T-SQL
1. `database/schema.sql`: 10 bảng CSDL theo đặc tả `research/06_DATABASE.md`, hỗ trợ OCC qua `rowversion`, trường `UptimeSeconds BIGINT NOT NULL`, bảng `ProcessedBatches` và `ProcessedCommands`.
2. `database/seed.sql`: Dữ liệu mẫu ban đầu gồm 3 tài khoản người dùng (`admin`, `operator`, `viewer`) với mật khẩu băm PBKDF2-HMAC-SHA256 (600,000 vòng), 3 máy trạm mẫu, 3 dịch vụ mẫu.

### 2.3. Mã nguồn Protocol Core
1. `src/LabGuard.Protocol/FrameCodec.cs`:
   - Header 4-byte UInt32 Big-Endian (Network Byte Order).
   - Kiểm tra ngưỡng an toàn: từ chối $N == 0$ và $N > 262,144$ bytes bằng `InvalidDataException`.
   - Cơ chế ghi khung tập trung dữ liệu vào buffer; TCP là dòng byte liên tục (byte stream) nên một lần `WriteAsync` không đồng nghĩa với một phân đoạn TCP duy nhất. Khung ứng dụng được tái cấu trúc hoàn toàn nhờ tiền tố độ dài + vòng lặp đọc chính xác; quy tắc Single Writer bảo đảm dữ liệu giữa các luồng ứng dụng không bị xen kẽ byte (interleaving).
2. `src/LabGuard.Protocol/WireEnvelope.cs`: 6 trường cố định (`version`, `messageType`, `requestId`, `deviceId`, `sentAtUtc`, `payload`).
3. `src/LabGuard.Protocol/MessageTypes.cs`: Khóa cứng toàn bộ 69 bản tin chuẩn hóa (34 P0-MVP REQUIRED, 25 P0 OPTIONAL, 10 P1 CONDITIONAL).
4. `src/LabGuard.Protocol/ErrorCodes.cs`: Các hằng số mã lỗi chuẩn hóa (`ERR_CONDITION_STILL_ACTIVE`, `ERR_CONCURRENCY_VIOLATION`, `ERR_IDEMPOTENCY_KEY_REUSE`, `ERR_DATABASE_UNAVAILABLE`, `ERR_UNAUTHORIZED`,...).
5. `src/LabGuard.Protocol/Serialization/ProtocolSerializer.cs`: Đóng gói `System.Text.Json` camelCase, case-insensitive, bỏ qua null; giải thuật tính `RequestDigest` chuẩn xác định (Deterministic SHA-256).
6. `src/LabGuard.Protocol/Dtos/`: Định nghĩa các DTO trao đổi cho Handshake, Telemetry, Admin và Sự kiện Realtime.

---

## 3. BẰNG CHỨNG KIỂM THỬ TỰ ĐỘNG (AUTOMATED TEST EVIDENCE)

### 3.1. Kết quả thực thi `dotnet test tests/LabGuard.Protocol.Tests`
Tổng số tests: **17 tests**. Kết quả: **17/17 PASS**, 0 Failed, 0 Skipped. Thời gian: 5.6s.

| STT | Tên Phương thức Kiểm thử | Nhóm Kịch bản | Kết quả | Thời gian |
|:---:|:---|:---|:---:|:---:|
| 1 | `WriteAndRead_NormalFrame_ReturnsExactPayload` | Kịch bản 1: Khung bình thường | **PASS** | 1 ms |
| 2 | `ReadFrameAsync_PartialHeader_AssemblesCorrectly` | Kịch bản 2: Partial Header (1b, 1b, 2b) | **PASS** | < 1 ms |
| 3 | `ReadFrameAsync_PartialPayload_CP2_TEST_001_AssemblesCorrectly` | Kịch bản 3: Partial Payload (500b qua 100b, 200b, 200b) | **PASS** | 1 ms |
| 4 | `ReadFrameAsync_MultipleCoalescedFrames_SeparatesIndependently` | Kịch bản 4: Dính gói tin (Packet Coalescing) | **PASS** | < 1 ms |
| 5 | `ReadFrameAsync_OversizeFrame_ThrowsInvalidDataException` (262,145b) | Kịch bản 5: Vượt ngưỡng MaxPayloadSize (256 KiB) | **PASS** | < 1 ms |
| 6 | `ReadFrameAsync_OversizeFrame_ThrowsInvalidDataException` (500,000b) | Kịch bản 5: Vượt ngưỡng MaxPayloadSize | **PASS** | < 1 ms |
| 7 | `ReadFrameAsync_OversizeFrame_ThrowsInvalidDataException` (10,000,000b) | Kịch bản 5: Vượt ngưỡng cực đại | **PASS** | < 1 ms |
| 8 | `ReadFrameAsync_ZeroLengthFrame_ThrowsInvalidDataException` | Kịch bản 6: Khung độ dài 0 (N == 0) | **PASS** | 2 s |
| 9 | `ReadFrameAsync_EofMidHeader_ThrowsEndOfStreamException` | Kịch bản 7a: Đóng stream giữa header | **PASS** | < 1 ms |
| 10 | `ReadFrameAsync_EofMidPayload_ThrowsEndOfStreamException` | Kịch bản 7b: Đóng stream giữa payload | **PASS** | < 1 ms |
| 11 | `ReadFrameAsync_EmptyStream_ReturnsNull` | Ngoại lệ: Stream rỗng ở đầu khung (Graceful EOF) | **PASS** | < 1 ms |
| 12 | `WriteFrameAsync_EmptyPayload_ThrowsArgumentException` | Ngoại lệ: Ghi payload rỗng | **PASS** | < 1 ms |
| 13 | `WriteFrameAsync_OversizedPayload_ThrowsArgumentException` | Ngoại lệ: Ghi payload quá kích thước | **PASS** | < 1 ms |
| 14 | `MessageTypes_CatalogHasExactly69CanonicalMessages` | Danh mục: Đủ 69 bản tin chuẩn | **PASS** | < 1 ms |
| 15 | `MessageTypes_P0RequiredMessages_AreAllValid` | Danh mục: 34 bản tin P0 hợp lệ | **PASS** | 2 s |
| 16 | `WireEnvelope_RoundTripSerialization_PreservesAllFields` | Serialization: Đầy đủ 6 trường envelope | **PASS** | 13 ms |
| 17 | `RequestDigest_DeterministicCalculation_MatchesSpecification` | Bảo mật: Tính mã băm xác định OCC/Idempotency | **PASS** | 34 ms |

### 3.2. Trạng thái Build toàn bộ Solution
- Lệnh: `dotnet build LabGuard.sln`
- Kết quả: `Build succeeded. 0 Warning(s), 0 Error(s).`

---

## 4. KẾT LUẬN & ĐIỂM DỪNG (CONCLUSION & STOP GATE)
- **GATE 1 (Solution Skeleton):** ĐẠT (PASS).
- **GATE 2 (`FRAMECODEC_GATE`):** ĐẠT (PASS).
- **Hành động tiếp theo (theo kế hoạch sau khi được duyệt):** Tiến hành GATE 3 (HELLO/AUTH Real TCP Spike giữa Agent Test Client và Central Server).
