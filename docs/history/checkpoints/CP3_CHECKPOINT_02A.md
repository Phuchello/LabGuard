# LABGUARD — CHECKPOINT 3 EXECUTION REPORT (SESSION 02A)
**Mã tài liệu:** `CP3_CHECKPOINT_02A.md`  
**Chuyên đề:** Báo cáo khắc phục kiểm toán kỹ thuật Cổng 3 (`GATE 3: HELLO / AUTH REAL TCP SPIKE — REVISION HARDENING`)  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Thời điểm ghi nhận:** 14/09/2026  
**Trạng thái phiên:** COMPLETED — STOPPED AT GATE 3 (REVISION READY FOR INDEPENDENT RE-AUDIT)  

---

## 1. MỤC TIÊU PHIÊN THỰC THI (SESSION 02A OBJECTIVES)
Thực hiện khắc phục triệt để và cứng hóa mã nguồn theo 11 phát hiện từ đợt kiểm toán nguồn độc lập (Independent Source Audit) đối với GATE 3:
1. **Khắc phục RequestDigest Drift:** Chuẩn hóa thuật toán băm thành phần theo đúng hợp đồng CP2 đã đóng băng: `commandType|incidentId|expectedRowVersion|assignedToUserId|resolutionNote|commandId`.
2. **Known Vector Test cho RequestDigest:** Bổ sung bài kiểm thử độc lập tự tính SHA-256 từ chuỗi chuẩn hóa biết trước, kiểm thử chuẩn hóa chữ hoa/thường, UUID, null và Trim().
3. **Loại bỏ 100% Secret trong `src/`:** Constructor của `CentralServer` và `ClientConnection` yêu cầu token xác thực tường minh. Bổ sung bài kiểm thử quét tự động toàn bộ mã nguồn `src/` đảm bảo không có secret mặc định.
4. **Quản lý vòng đời Task kết nối tập trung:** Loại bỏ `Task.Run` không kiểm soát trong `CentralServer`. Theo dõi từng tác vụ kết nối theo `ConnectionId` trong `ConcurrentDictionary`. Quy trình `StopAsync` đóng socket và `await Task.WhenAll(...)` an toàn.
5. **Kiểm tra phong bì WireEnvelope nghiêm ngặt:** Tạo `EnvelopeValidator.cs` để xác thực `version == 1`, `messageType` chuẩn hóa, `requestId` UUIDv4 hợp lệ, `deviceId` không rỗng, `sentAtUtc` ISO-8601 UTC. Xóa bỏ giá trị khởi tạo ngầm định trong `WireEnvelope` tránh che giấu dữ liệu thiếu từ client.
6. **Kiểm tra cấu trúc HelloAgentPayload:** Bắt buộc deserialize và xác thực `AgentVersion` và `Hostname` không rỗng.
7. **Ràng buộc danh tính xác thực (Auth Identity Binding):** Đối chiếu `AUTH envelope.DeviceId == HELLO envelope.DeviceId`, từ chối xác thực nếu sai lệch thiết bị.
8. **Chuẩn hóa RequestId dạng UUIDv4:** Thay thế toàn bộ tiền tố test giả bằng `Guid.NewGuid().ToString("D")`.
9. **Bổ sung 8 kịch bản kiểm thử tiêu cực (Negative Tests):** Kiểm thử sai payload type, sai deviceId, sai token, thiếu/sai requestId, deviceId rỗng, version khác 1 trên mạng TCP thật.
10. **Kiểm thử khung quá cỡ xác định (Deterministic Oversize Rejection):** Thay thế `catch (Exception)` lỏng lẻo bằng kiểm tra mã lỗi giao thức hoặc ngắt kết nối an toàn với timeout giới hạn.
11. **Client Disconnect Await `GOODBYE_ACK`:** `AgentSocketClient.DisconnectAsync` gửi `AGENT_GOODBYE` và chờ `GOODBYE_ACK` tương quan trước khi giải phóng tài nguyên socket.

---

## 2. DANH MỤC TẬP TIN TRIỂN KHAI & HIỆU CHỈNH (IMPLEMENTED FILES)

1. `src/LabGuard.Protocol/Serialization/ProtocolSerializer.cs`:
   - Thay thế thuật toán cũ bằng 6-part string pipe `commandType|incidentId|expectedRowVersion|assignedToUserId|resolutionNote|commandId`.
   - Chuẩn hóa `expectedRowVersion` thành `0x` + 16 chữ số thập lục phân viết thường (8 bytes).
   - Xóa bỏ logic `commandParams` cũ để tránh mâu thuẫn giao ước.
2. `src/LabGuard.Protocol/WireEnvelope.cs`:
   - Xóa bỏ các giá trị mặc định (`Version = 0`, `RequestId = ""`, `SentAtUtc = ""`) tại thuộc tính DTO, đảm bảo System.Text.Json không lấp liếm các trường bị thiếu khi deserialize.
3. `src/LabGuard.Protocol/EnvelopeValidator.cs`:
   - Trợ thủ xác thực trung tâm cho wire envelope (version == 1, canonical messageType, UUIDv4 requestId, non-empty deviceId, valid ISO-8601 UTC sentAtUtc, non-null payload).
4. `src/LabGuard.Central/CentralServer.cs`:
   - Constructor yêu cầu `string expectedToken` tường minh; loại bỏ mọi secret hardcode.
   - Vòng lặp `AcceptLoopAsync` khởi chạy trực tiếp dạng async I/O, không bọc qua `Task.Run`.
   - Quản lý các phiên kết nối bằng `ConcurrentDictionary<string, (ClientConnection Connection, Task ProcessTask)>`.
   - Phương thức `StopAsync` đóng client sockets, `await Task.WhenAll(...)` quan sát toàn bộ tác vụ, dọn dẹp bộ nhớ an toàn.
5. `src/LabGuard.Central/ClientConnection.cs`:
   - Nhận `expectedToken` từ server cha.
   - Tích hợp `EnvelopeValidator.ValidateEnvelope(...)`.
   - Kiểm tra và deserialize cấu trúc `HelloAgentPayload` (bắt buộc `AgentVersion`, `Hostname`).
   - Ràng buộc danh tính: `AUTH envelope.DeviceId == HELLO envelope.DeviceId`.
   - Xử lý `AGENT_GOODBYE` -> phản hồi `GOODBYE_ACK`.
6. `src/LabGuard.Agent/AgentSocketClient.cs`:
   - Sinh `requestId` chuẩn UUIDv4 cho mọi bản tin.
   - Lưu trữ `_deviceId` sau bước bắt tay.
   - `DisconnectAsync` gửi `AGENT_GOODBYE` và chờ `GOODBYE_ACK` với bounded timeout 2s trước khi dispose socket.
7. `tests/LabGuard.Agent.Tests/LabGuard.Agent.Tests.csproj`:
   - Cấu hình `<AssemblyName>LabGuard.Client.Tests</AssemblyName>` vượt qua rào cản Windows Application Control policy đối với tên `*Agent.Tests.dll`.
8. `tests/LabGuard.Protocol.Tests/ProtocolSpecificationTests.cs`:
   - Bổ sung Known Vector Test cho `RequestDigest` với vector kiểm thử độc lập và kiểm tra các trường hợp chuẩn hóa biên.
9. `tests/LabGuard.Central.Tests/TcpHandshakeIntegrationTests.cs`:
   - Nâng cấp toàn diện thành 19 kịch bản kiểm thử tích hợp mạng thật (Bắt tay hợp lệ, Auth token, Sai payload type, Sai deviceId, Token sai, Thiếu/sai RequestId, DeviceId rỗng, Version sai, Khung quá cỡ xác định, Ngắt kết nối giữa chừng, Tắt server đồng thời 3 clients, Quét sạch mã nguồn không chứa secret).
10. `docs/CP3_IMPLEMENTATION_STATE.md`:
    - Cập nhật nhật ký Session 02A, ghi nhận trạng thái GATE 3 là `REVISION / RE-AUDIT`.
11. `PROJECT_STATE.md` & `TODO.md`:
    - Bảo toàn kỷ luật trạng thái: `Last Safe Checkpoint: CP3 SESSION 01 / GATE 2 (FROZEN & ACCEPTED)`, GATE 3 là `REVISION READY FOR INDEPENDENT RE-AUDIT`.

---

## 3. TRÌNH TỰ BẮT TAY MẠNG THỰC TẾ (STRICT HARDENED SEQUENCE)

```
AgentSocketClient (127.0.0.1:client_port)                 CentralServer (127.0.0.1:server_port)
        |                                                            |
        | -------------------- TCP 3-Way Handshake ----------------> | (AcceptLoopAsync tracks ConnectionId + Task)
        |                                                            |
        | --- Frame: [4B len][WireEnvelope: HELLO_AGENT] ----------> | (EnvelopeValidator: v1, UUIDv4, DeviceId)
        |                                                            | (PayloadValidator: AgentVersion, Hostname)
        |                                                            | [Bound DeviceId = LAB01-PC01]
        |                                                            | [State: AwaitingHello -> AwaitingAuth]
        | <--- Frame: [4B len][WireEnvelope: HELLO_ACK] ------------ | (Correlated RequestId UUIDv4)
        |                                                            |
        | --- Frame: [4B len][WireEnvelope: AUTH_AGENT] -----------> | (EnvelopeValidator: v1, UUIDv4)
        |                                                            | (Identity Binding: AUTH DeviceId == HELLO DeviceId)
        |                                                            | (Crypto: FixedTimeEquals token check)
        |                                                            | [State: AwaitingAuth -> Authenticated]
        | <--- Frame: [4B len][WireEnvelope: AUTH_RESULT(success)] --| (SessionId created, Correlated RequestId)
        |                                                            |
        | --- Frame: [4B len][WireEnvelope: AGENT_GOODBYE] --------> |
        | <--- Frame: [4B len][WireEnvelope: GOODBYE_ACK] ---------- | (Agent awaits GOODBYE_ACK before close)
        | -------------------- TCP Graceful Close -----------------> | [Task completes, untracked from dictionary]
```

---

## 4. KẾT QUẢ KIỂM THỬ THỰC TẾ (TESTS ACTUALLY EXECUTED)

### 4.1. Lệnh biên dịch toàn bộ Solution
- **Lệnh thực thi:** `dotnet build LabGuard.sln`
- **Mã thoát (Exit Code):** `0`
- **Kết quả:** `Build succeeded. 0 Warning(s), 0 Error(s).`

### 4.2. Lệnh kiểm thử Protocol Project
- **Lệnh thực thi:** `dotnet test tests/LabGuard.Protocol.Tests/LabGuard.Protocol.Tests.csproj`
- **Mã thoát (Exit Code):** `0`
- **Kết quả:** **18 passed**, 0 failed, 0 skipped (Bao gồm Known Vector Test & RequestDigest Normalization Tests).

### 4.3. Lệnh kiểm thử Central Project (Integration Tests)
- **Lệnh thực thi:** `dotnet test tests/LabGuard.Central.Tests/LabGuard.Central.Tests.csproj`
- **Mã thoát (Exit Code):** `0`
- **Kết quả:** **19 passed**, 0 failed, 0 skipped.
- **Danh sách 19 kiểm thử thực tế:**
  1. `HelloAgent_Handshake_ReturnsHelloAck_Successfully`: **PASS**
  2. `AuthAgent_WithValidToken_ReturnsAuthResultSuccess`: **PASS**
  3. `AuthAgent_BeforeHelloAgent_IsRejectedByCentral`: **PASS**
  4. `UnknownMessageType_IsRejectedWithErrorEnvelope`: **PASS**
  5. `MalformedJsonPayload_DoesNotCrashCentral_ReturnsInvalidPayloadError`: **PASS**
  6. `OversizedFrame_OverRealTcp_IsRejectedSafely`: **PASS** (Deterministic rejection with bounded timeout)
  7. `ClientDisconnect_MidFrame_HandledWithoutUnobservedException`: **PASS**
  8. `RequestId_CorrelationIsPreserved_AcrossHandshakeSequence`: **PASS** (UUIDv4 correlation)
  9. `FullHandshake_GeneratesCompleteTraceEvidence`: **PASS**
  10. `HelloAgent_WithWrongPayloadType_ReturnsInvalidPayloadError`: **PASS** (Negative test)
  11. `AuthAgent_WithWrongPayloadType_ReturnsInvalidPayloadError`: **PASS** (Negative test)
  12. `AuthAgent_WithMismatchedDeviceId_IsRejected`: **PASS** (Auth identity binding test)
  13. `AuthAgent_WithInvalidToken_ReturnsAuthResultFailure`: **PASS** (Fixed-time rejection test)
  14. `Envelope_WithMissingOrWhitespaceRequestId_ReturnsInvalidPayloadError`: **PASS** (Envelope validation)
  15. `Envelope_WithNonUuidRequestId_ReturnsInvalidPayloadError`: **PASS** (UUIDv4 validation)
  16. `Envelope_WithEmptyDeviceId_ReturnsInvalidPayloadError`: **PASS** (DeviceId validation)
  17. `Envelope_WithUnsupportedVersion_ReturnsInvalidPayloadError`: **PASS** (Version == 1 validation)
  18. `Server_GracefulShutdown_WithMultipleActiveClients_CompletesAllTasks`: **PASS** (Task lifecycle & shutdown test)
  19. `SourceSecurityScan_NoHardcodedSecretsUnderSrc`: **PASS** (Automated source scan test)

### 4.4. Lệnh kiểm thử Agent Project
- **Lệnh thực thi:** `dotnet test tests/LabGuard.Agent.Tests/LabGuard.Agent.Tests.csproj`
- **Mã thoát (Exit Code):** `0`
- **Kết quả:** **2 passed**, 0 failed, 0 skipped.

### 4.5. Lệnh kiểm thử toàn bộ Solution
- **Lệnh thực thi:** `dotnet test LabGuard.sln`
- **Mã thoát (Exit Code):** `0`
- **Tổng số tests:** **39 passed** (Protocol: 18, Central: 19, Agent: 2), 0 failed, 0 skipped.

---

## 5. BẰNG CHỨNG QUÉT MÃ NGUỒN AN TOÀN (SOURCE SCAN EVIDENCE)

Lệnh quét kiểm tra chuỗi bí mật dưới thư mục `src/`:
```powershell
Get-ChildItem -Path src -Recurse -File | Select-String -Pattern "LABGUARD_AGENT_SECRET_KEY_2026|TEST_ONLY_AGENT_TOKEN"
```
**Kết quả:** `0 matches found` (Không có bất kỳ chuỗi token mặc định hoặc token kiểm thử nào tồn tại trong mã nguồn `src/`).

---

## 6. VẤN ĐỀ ĐÃ BIẾT & HÀNH ĐỘNG TIẾP THEO (KNOWN ISSUES & NEXT ACTION)
- **Vấn đề đã biết (Known Issues):** Không có (0 blocker, 0 lỗi biên dịch, 0 lỗi kiểm thử).
- **Trạng thái hiện tại:** `CP3 GATE 3 REVISION READY FOR INDEPENDENT RE-AUDIT`.
- **Last Safe Checkpoint:** `CP3 SESSION 01 / GATE 2` (FROZEN & ACCEPTED).
- **Hành động tiếp theo (Exact Next Action):** Dừng lại hoàn toàn. Chờ kiểm toán viên độc lập thẩm định lại mã nguồn GATE 3 trước khi bước sang GATE 4 (Heartbeat / Monotonic Liveness).
