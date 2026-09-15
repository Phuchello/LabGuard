# LABGUARD — CHECKPOINT 3 EXECUTION REPORT (SESSION 02B)
**Mã tài liệu:** `CP3_CHECKPOINT_02B.md`  
**Chuyên đề:** Báo cáo khắc phục vi điểm kiểm toán Cổng 3 (`GATE 3: HELLO / AUTH REAL TCP SPIKE — FINAL MICRO-HARDENING`)  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Thời điểm ghi nhận:** 14/09/2026  
**Trạng thái phiên:** COMPLETED — STOPPED AT GATE 3 (READY FOR FINAL INDEPENDENT RE-AUDIT)  

---

## 1. MỤC TIÊU PHIÊN THỰC THI (SESSION 02B OBJECTIVES)
Khắc phục triệt để và dứt điểm 5 phát hiện vi mô cuối cùng từ Independent Source Re-Audit đối với GATE 3:
1. **Xác thực ngữ nghĩa UUIDv4 (Semantic RFC 4122 Validation):**
   - Triển khai phương thức kiểm tra `EnvelopeValidator.IsCanonicalUuidV4(string? value)`.
   - UUIDv4 được xác thực theo ngữ nghĩa chuẩn RFC 4122 (độ dài 36 ký tự, định dạng "D", vị trí version `value[14] == '4'`, vị trí variant `value[19]` thuộc `[8, 9, a, b, A, B]`), không chỉ dừng lại ở cú pháp GUID chung.
   - Bổ sung kiểm thử từ chối UUIDv1 (`6ba7b810-9dad-11d1-80b4-00c04fd430c8`) và chuỗi sai định dạng trên cả Protocol unit test lẫn Central TCP integration test.
2. **Bắt buộc mốc thời gian `sentAtUtc` là UTC thực sự (Zero Offset):**
   - Sau khi parse ISO-8601, bắt buộc `parsed.Offset == TimeSpan.Zero`.
   - Chấp nhận đuôi `Z` và `+00:00`; từ chối dứt khoát các múi giờ khác (ví dụ: `+07:00` bị trả về `ERR_INVALID_PAYLOAD`).
   - `sentAtUtc` chỉ dùng làm metadata kiểm tra phong bì và ghi log, không dùng cho tính toán liveness máy trạm.
3. **Xóa bỏ 100% giá trị mặc định phía Server trong Wire Input DTOs:**
   - Trong `HelloAgentPayload`, xóa bỏ `AgentVersion = "1.0.0"`, `Hostname = Environment.MachineName`, `OsVersion = Environment.OSVersion.ToString()`. Khởi tạo tất cả bằng `string.Empty`.
   - Trong `HelloAdminPayload`, xóa bỏ các giá trị môi trường server, khởi tạo `string.Empty`.
   - Loại bỏ rủi ro payload rỗng `{}` từ client vô tình thừa hưởng giá trị của server và vượt qua bước kiểm tra.
   - Bổ sung kiểm thử mạng TCP từ chối payload rỗng `{}`, thiếu `agentVersion`, thiếu `hostname`.
4. **Xóa bỏ triệt để Race Condition trong đăng ký kết nối (`ConnectionRuntime`):**
   - Tách cấu trúc `ConnectionRuntime` gồm `ClientConnection` và `Task? ProcessTask`.
   - Thêm runtime vào `_activeConnections` TRƯỚC KHI kích hoạt `ProcessConnectionAsync`, loại bỏ hoàn toàn khả năng tác vụ kết thúc trước khi kịp lưu vào dictionary.
   - Khối `finally` đảm bảo gọi `TryRemove(connectionId, out _)`.
   - `StopAsync` snapshot danh sách runtime, đóng socket để unblock read loop, await toàn bộ `ProcessTask` hợp lệ, dọn sạch bộ nhớ.
   - Bổ sung kiểm thử kết nối/ngắt kết nối dồn dập 50 lần, sau thời gian xả hữu hạn `ActiveConnectionCount == 0`, sau đó client chuẩn kết nối HELLO thành công 100%.
5. **Đối soát tương quan `GOODBYE_ACK` (Correlation Enforcement):**
   - `AgentSocketClient.DisconnectAsync` bắt buộc kiểm tra đồng thời: `ackEnvelope.MessageType == MessageTypes.GoodbyeAck` VÀ `ackEnvelope.RequestId == goodbye requestId`.
   - Bổ sung kiểm thử xác minh `requestId` của `GOODBYE_ACK` trùng khớp chính xác với `AGENT_GOODBYE`.
6. **Kỷ luật câu chữ kỹ thuật (Report Wording Discipline):**
   - Đã loại bỏ các giá trị mặc định tự sinh requestId/timestamp có nguy cơ che giấu trường dữ liệu thiếu từ mạng; các trường rỗng/thiếu đều bị `EnvelopeValidator` từ chối dứt khoát.
   - UUIDv4 được xác thực ngữ nghĩa theo chuẩn RFC 4122, không đơn thuần là định dạng chuỗi GUID "D".

---

## 2. DANH MỤC TẬP TIN TRIỂN KHAI & HIỆU CHỈNH (IMPLEMENTED FILES)

1. `src/LabGuard.Protocol/EnvelopeValidator.cs`:
   - Bổ sung `IsCanonicalUuidV4(string? value)`.
   - Bổ sung kiểm tra `parsedDto.Offset == TimeSpan.Zero` cho `sentAtUtc`.
2. `src/LabGuard.Protocol/Dtos/HandshakeDtos.cs`:
   - Chuyển toàn bộ khởi tạo `HelloAgentPayload` và `HelloAdminPayload` thành `string.Empty`.
3. `src/LabGuard.Central/CentralServer.cs`:
   - Hiện thực `ConnectionRuntime`, đăng ký phiên vào `_activeConnections` trước khi gọi `ProcessConnectionAsync`.
   - Khắc phục triệt để race condition khi client ngắt kết nối cực nhanh.
4. `src/LabGuard.Central/ClientConnection.cs`:
   - Cập nhật fallback `corrId` sử dụng `EnvelopeValidator.IsCanonicalUuidV4`.
5. `src/LabGuard.Agent/AgentSocketClient.cs`:
   - Thêm thuộc tính `public bool IsAuthenticated => !string.IsNullOrEmpty(SessionId);`.
   - Thắt chặt kiểm tra tương quan `ackEnvelope.RequestId == requestId` trong `DisconnectAsync`.
6. `tests/LabGuard.Protocol.Tests/ProtocolSpecificationTests.cs`:
   - Bổ sung kiểm thử `EnvelopeValidator_IsCanonicalUuidV4_EnforcesStrictRfc4122V4` (thử nghiệm UUIDv4, UUIDv1, nil GUID, malformed).
   - Bổ sung kiểm thử `EnvelopeValidator_SentAtUtc_EnforcesZeroOffset` (Z pass, +00:00 pass, +07:00 reject).
7. `tests/LabGuard.Central.Tests/TcpHandshakeIntegrationTests.cs`:
   - Cập nhật các bài kiểm thử cũ để điền tường minh `AgentVersion` và `Hostname` khi gửi `HELLO_AGENT`.
   - Thêm Test 20: `Envelope_UuidV1RequestId_IsRejectedOverTcp`.
   - Thêm Test 21: `Envelope_SentAtUtc_EnforcesZeroOffsetOverTcp`.
   - Thêm Test 22: `HelloAgentPayload_MissingRequiredFields_IsRejectedOverTcp`.
   - Thêm Test 23: `ConnectionRace_RapidConnectDisconnect_LeavesZeroActiveConnections`.
   - Thêm Test 24: `AgentGoodbye_AwaitsCorrelatedGoodbyeAck`.
8. `docs/CP3_IMPLEMENTATION_STATE.md`:
   - Cập nhật nhật ký Session 02B, chuyển trạng thái GATE 3 thành `READY FOR FINAL RE-AUDIT`.
9. `PROJECT_STATE.md` & `TODO.md`:
   - Giữ vững `Last Safe Checkpoint: CP3 SESSION 01 / GATE 2 (FROZEN & ACCEPTED)`.
   - Cập nhật Gate 3 thành `READY FOR FINAL INDEPENDENT RE-AUDIT`.

---

## 3. TRÌNH TỰ BẮT TAY MẠNG THỰC TẾ (STRICT HARDENED SEQUENCE)

```
AgentSocketClient (127.0.0.1:client_port)                 CentralServer (127.0.0.1:server_port)
        |                                                            |
        | -------------------- TCP 3-Way Handshake ----------------> | (AcceptLoopAsync registers ConnectionRuntime to dict)
        |                                                            | (runtime.ProcessTask = ProcessConnectionAsync)
        |                                                            |
        | --- Frame: [4B len][WireEnvelope: HELLO_AGENT] ----------> | (EnvelopeValidator: v1, IsCanonicalUuidV4, ZeroOffset UTC)
        |                                                            | (PayloadValidator: Client-supplied AgentVersion, Hostname)
        |                                                            | [Bound DeviceId = LAB01-PC01]
        |                                                            | [State: AwaitingHello -> AwaitingAuth]
        | <--- Frame: [4B len][WireEnvelope: HELLO_ACK] ------------ | (Correlated RequestId UUIDv4)
        |                                                            |
        | --- Frame: [4B len][WireEnvelope: AUTH_AGENT] -----------> | (EnvelopeValidator: v1, IsCanonicalUuidV4)
        |                                                            | (Identity Binding: AUTH DeviceId == HELLO DeviceId)
        |                                                            | (Crypto: FixedTimeEquals token check)
        |                                                            | [State: AwaitingAuth -> Authenticated]
        | <--- Frame: [4B len][WireEnvelope: AUTH_RESULT(success)] --| (SessionId created, Correlated RequestId)
        |                                                            |
        | --- Frame: [4B len][WireEnvelope: AGENT_GOODBYE (Req X)]-> |
        | <--- Frame: [4B len][WireEnvelope: GOODBYE_ACK (Req X)] -- | (Agent asserts MessageType==GOODBYE_ACK && RequestId==X)
        | -------------------- TCP Graceful Close -----------------> | [Task completes, TryRemove removes entry safely]
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
- **Kết quả:** **20 passed**, 0 failed, 0 skipped.

### 4.3. Lệnh kiểm thử Central Project (Integration Tests)
- **Lệnh thực thi:** `dotnet test tests/LabGuard.Central.Tests/LabGuard.Central.Tests.csproj`
- **Mã thoát (Exit Code):** `0`
- **Kết quả:** **24 passed**, 0 failed, 0 skipped.
- **Danh sách 24 kiểm thử tích hợp mạng thật:**
  1. `HelloAgent_Handshake_ReturnsHelloAck_Successfully`: **PASS**
  2. `AuthAgent_WithValidToken_ReturnsAuthResultSuccess`: **PASS**
  3. `AuthAgent_BeforeHelloAgent_IsRejectedByCentral`: **PASS**
  4. `UnknownMessageType_IsRejectedWithErrorEnvelope`: **PASS**
  5. `MalformedJsonPayload_DoesNotCrashCentral_ReturnsInvalidPayloadError`: **PASS**
  6. `OversizedFrame_OverRealTcp_IsRejectedSafely`: **PASS**
  7. `ClientDisconnect_MidFrame_HandledWithoutUnobservedException`: **PASS**
  8. `RequestId_CorrelationIsPreserved_WithValidUuid`: **PASS**
  9. `FullHandshake_GeneratesCompleteTraceEvidence`: **PASS**
  10. `HelloAgent_WithWrongPayloadType_ReturnsInvalidPayloadError`: **PASS**
  11. `AuthAgent_WithWrongPayloadType_ReturnsInvalidPayloadError`: **PASS**
  12. `AuthAgent_WithInvalidPayloadType_IsRejected`: **PASS**
  13. `AuthAgent_DeviceIdMismatch_IsRejected`: **PASS**
  14. `AuthAgent_WithInvalidToken_ReturnsAuthResultFailure`: **PASS**
  15. `Envelope_MissingRequestId_IsRejected`: **PASS**
  16. `Envelope_NonUuidRequestId_IsRejected`: **PASS**
  17. `Envelope_EmptyDeviceId_IsRejected`: **PASS**
  18. `Envelope_UnsupportedVersion_IsRejected`: **PASS**
  19. `SourceSecurityScan_NoHardcodedSecretsUnderSrc`: **PASS**
  20. `Envelope_UuidV1RequestId_IsRejectedOverTcp`: **PASS**
  21. `Envelope_SentAtUtc_EnforcesZeroOffsetOverTcp`: **PASS**
  22. `HelloAgentPayload_MissingRequiredFields_IsRejectedOverTcp`: **PASS**
  23. `ConnectionRace_RapidConnectDisconnect_LeavesZeroActiveConnections`: **PASS**
  24. `AgentGoodbye_AwaitsCorrelatedGoodbyeAck`: **PASS**

### 4.4. Lệnh kiểm thử Agent Project
- **Lệnh thực thi:** `dotnet test tests/LabGuard.Agent.Tests/LabGuard.Agent.Tests.csproj`
- **Mã thoát (Exit Code):** `0`
- **Kết quả:** **2 passed**, 0 failed, 0 skipped.

### 4.5. Lệnh kiểm thử toàn bộ Solution
- **Lệnh thực thi:** `dotnet test LabGuard.sln`
- **Mã thoát (Exit Code):** `0`
- **Tổng số tests:** **46 passed** (Protocol: 20, Central: 24, Agent: 2), 0 failed, 0 skipped.

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
- **Trạng thái hiện tại:** `CP3 GATE 3 READY FOR FINAL INDEPENDENT RE-AUDIT`.
- **Last Safe Checkpoint:** `CP3 SESSION 01 / GATE 2` (FROZEN & ACCEPTED).
- **Hành động tiếp theo (Exact Next Action):** Dừng lại hoàn toàn. Chờ kiểm toán viên độc lập thẩm định lại toàn diện GATE 3 trước khi bước sang GATE 4 (Heartbeat / Monotonic Liveness).
