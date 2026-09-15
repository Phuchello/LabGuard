# LABGUARD — CHECKPOINT 3 EXECUTION REPORT (SESSION 02)
**Mã tài liệu:** `CP3_CHECKPOINT_02.md`  
**Chuyên đề:** Báo cáo nghiệm thu kỹ thuật Cổng 3 (`GATE 3: HELLO / AUTH REAL TCP SPIKE`)  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Thời điểm ghi nhận:** 14/09/2026  
**Trạng thái phiên:** COMPLETED — STOPPED AT GATE 3 FOR INDEPENDENT REVIEW  

---

## 1. MỤC TIÊU PHIÊN THỰC THI (SESSION 02 OBJECTIVES)
- **Hiệu chỉnh giải thích kỹ thuật:** Đính chính cách diễn đạt gây hiểu lầm về "atomic TCP write" trong `FrameCodec.cs` và tài liệu; làm rõ bản chất dòng byte của TCP, hiện tượng phân tách/dính gói (segment splitting/coalescing), vai trò của `FrameCodec` trong tái cấu trúc bản tin, và kỷ luật Single Writer để chống xen kẽ byte giữa các luồng ứng dụng.
- **Hiện thực máy chủ Central Server tối thiểu:**
  - `src/LabGuard.Central/CentralServer.cs`: Quản lý `TcpListener`, bind cổng động/loopback `127.0.0.1`, vòng lặp chấp nhận kết nối `AcceptTcpClientAsync`, quản lý vòng đời `ClientConnection`.
  - `src/LabGuard.Central/ClientConnection.cs`: Đọc khung byte bằng `FrameCodec`, tuần tự hóa `WireEnvelope`, máy trạng thái bắt tay (`AwaitingHello` $\rightarrow$ `AwaitingAuth` $\rightarrow$ `Authenticated`), kiểm tra token bằng `CryptographicOperations.FixedTimeEquals`, thực thi Single Writer qua `SemaphoreSlim(1,1)`.
- **Hiện thực máy trạm Agent tối thiểu:**
  - `src/LabGuard.Agent/AgentSocketClient.cs`: Kết nối `TcpClient` thực tới server, gửi `HELLO_AGENT` $\rightarrow$ nhận `HELLO_ACK`, gửi `AUTH_AGENT` $\rightarrow$ nhận `AUTH_RESULT`, bảo toàn và đối soát `requestId` tương quan.
- **Bộ kiểm thử tích hợp mạng thật (Real TCP Socket Tests):**
  - Xây dựng 9 kịch bản kiểm thử tích hợp mạng thật qua loopback socket tại `tests/LabGuard.Central.Tests/TcpHandshakeIntegrationTests.cs`.
- **Điều kiện dừng (Stop Condition):** Dừng ngay sau khi hoàn thành GATE 3. Không tự ý triển khai Heartbeat timer, Liveness scanner, SQL Server Ingestion, Reconnect loop hay WinForms.

---

## 2. DANH MỤC TẬP TIN TRIỂN KHAI & HIỆU CHỈNH (IMPLEMENTED FILES)

1. `src/LabGuard.Protocol/FrameCodec.cs`: Cập nhật chú thích kỹ thuật chính xác về dòng byte TCP và kỷ luật Single Writer.
2. `CP3_CHECKPOINT_01.md`: Cập nhật câu chữ mô tả cơ chế ghi khung và phân đoạn TCP.
3. `src/LabGuard.Central/ClientConnection.cs`: Lớp xử lý kết nối máy trạm, máy trạng thái bắt tay, xác thực thời gian cố định, xử lý lỗi giao thức (vi phạm thứ tự, gói tin lạ, JSON hỏng, khung quá cỡ, ngắt kết nối đột ngột).
4. `src/LabGuard.Central/CentralServer.cs`: Máy chủ TCP tối thiểu quản lý `TcpListener` và các phiên kết nối đồng thời.
5. `src/LabGuard.Agent/AgentSocketClient.cs`: Client kết nối TCP, gửi HELLO và AUTH, kiểm tra requestId tương quan.
6. `tests/LabGuard.Central.Tests/TcpHandshakeIntegrationTests.cs`: 9 kịch bản kiểm thử mạng TCP thực tế.
7. `tests/LabGuard.Agent.Tests/AgentClientTests.cs`: Kiểm thử khởi tạo và trạng thái ngắt kết nối của `AgentSocketClient`.
8. `docs/CP3_IMPLEMENTATION_STATE.md`: Cập nhật GATE 3 chuyển sang trạng thái `PASS`.
9. `PROJECT_STATE.md` & `TODO.md`: Cập nhật `Last Safe Checkpoint: CP3 SESSION 02 / GATE 3`.

---

## 3. TRÌNH TỰ BẮT TAY MẠNG THỰC TẾ (REAL NETWORK SEQUENCE)

```
AgentSocketClient (127.0.0.1:client_port)                 CentralServer (127.0.0.1:server_port)
        |                                                            |
        | -------------------- TCP 3-Way Handshake ----------------> |
        |                                                            |
        | --- Frame: [4B len][WireEnvelope: HELLO_AGENT] ----------> | (AcceptTcpClientAsync -> ClientConnection)
        |                                                            | [State: AwaitingHello -> AwaitingAuth]
        | <--- Frame: [4B len][WireEnvelope: HELLO_ACK] ------------ | (RequestId correlated)
        |                                                            |
        | --- Frame: [4B len][WireEnvelope: AUTH_AGENT] -----------> | (FixedTimeEquals token check)
        |                                                            | [State: AwaitingAuth -> Authenticated]
        | <--- Frame: [4B len][WireEnvelope: AUTH_RESULT(success)] --| (SessionId created, RequestId correlated)
        |                                                            |
        | --- Frame: [4B len][WireEnvelope: AGENT_GOODBYE] --------> |
        | <--- Frame: [4B len][WireEnvelope: GOODBYE_ACK] ---------- |
        | -------------------- TCP Graceful Close -----------------> | [State: Closed, Socket disposed]
```

---

## 4. KẾT QUẢ KIỂM THỬ THỰC TẾ (TESTS ACTUALLY EXECUTED)

### 4.1. Lệnh biên dịch toàn bộ Solution
- **Lệnh thực thi:** `dotnet build LabGuard.sln`
- **Mã thoát (Exit Code):** `0`
- **Kết quả:** `Build succeeded. 0 Warning(s), 0 Error(s).`

### 4.2. Lệnh kiểm thử tích hợp mạng thật (Central.Tests)
- **Lệnh thực thi:** `dotnet test tests/LabGuard.Central.Tests/LabGuard.Central.Tests.csproj --logger "console;verbosity=detailed"`
- **Mã thoát (Exit Code):** `0`
- **Số lượng tests:** **9 tests passed**, 0 failed, 0 skipped.
- **Danh sách kiểm thử chi tiết:**
  1. `HelloAgent_Handshake_ReturnsHelloAck_Successfully`: **PASS** (6 ms)
  2. `AuthAgent_WithValidToken_ReturnsAuthResultSuccess`: **PASS** (22 ms)
  3. `AuthAgent_BeforeHelloAgent_IsRejectedByCentral`: **PASS** (37 ms)
  4. `UnknownMessageType_IsRejectedWithErrorEnvelope`: **PASS** (5 ms)
  5. `MalformedJsonPayload_DoesNotCrashCentral_ReturnsInvalidPayloadError`: **PASS** (3 ms)
  6. `OversizedFrame_OverRealTcp_IsRejectedSafely`: **PASS** (3 ms)
  7. `ClientDisconnect_MidFrame_HandledWithoutUnobservedException`: **PASS** (112 ms)
  8. `RequestId_CorrelationIsPreserved_AcrossHandshakeSequence`: **PASS** (2 ms)
  9. `FullHandshake_GeneratesCompleteTraceEvidence`: **PASS** (5 ms)

### 4.3. Lệnh kiểm thử toàn bộ Solution
- **Lệnh thực thi:** `dotnet test LabGuard.sln`
- **Mã thoát (Exit Code):** `0`
- **Tổng số tests toàn hệ thống:** **28 tests passed** (Protocol: 17, Central: 9, Agent: 2), 0 failed, 0 skipped.

---

## 5. BẰNG CHỨNG GÓI TIN & NHẬT KÝ MẠNG THẬT (REAL TCP EVIDENCE)

Dưới đây là đoạn nhật ký bắt tay thực tế thu thập trực tiếp từ bài kiểm thử `FullHandshake_GeneratesCompleteTraceEvidence` qua loopback socket TCP (`127.0.0.1`):

```text
[2026-09-14T01:56:56.5216080Z] [CentralServer:57188] Central Server listening on 127.0.0.1:57188.
[2026-09-14T01:56:56.5216423Z] [AgentSocketClient] Connecting to 127.0.0.1:57188...
[2026-09-14T01:56:56.5221725Z] [AgentSocketClient] Connected to 127.0.0.1:57188.
[2026-09-14T01:56:56.5222178Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] Client connected.
[2026-09-14T01:56:56.5224308Z] [AgentSocketClient] Sending HELLO_AGENT requestId=063e198a-993d-42fc-97cd-b4c116065c89 deviceId=LAB01-PC09
[2026-09-14T01:56:56.5226040Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] Received messageType=HELLO_AGENT requestId=063e198a-993d-42fc-97cd-b4c116065c89 payloadSize=304b
[2026-09-14T01:56:56.5226202Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] HELLO_AGENT accepted for deviceId=LAB01-PC09. Sending HELLO_ACK.
[2026-09-14T01:56:56.5226869Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] Sent messageType=HELLO_ACK requestId=063e198a-993d-42fc-97cd-b4c116065c89 payloadSize=317b
[2026-09-14T01:56:56.5227449Z] [AgentSocketClient] Read response messageType=HELLO_ACK requestId=063e198a-993d-42fc-97cd-b4c116065c89 size=317b
[2026-09-14T01:56:56.5227717Z] [AgentSocketClient] Received HELLO_ACK from serverVersion=1.0.0 serverTimeUtc=2026-09-14T01:56:56.5226197Z
[2026-09-14T01:56:56.5228023Z] [AgentSocketClient] Sending AUTH_AGENT requestId=e4ed41f5-b936-4239-b43c-195d7c664999 deviceId=LAB01-PC09
[2026-09-14T01:56:56.5229205Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] Received messageType=AUTH_AGENT requestId=e4ed41f5-b936-4239-b43c-195d7c664999 payloadSize=269b
[2026-09-14T01:56:56.5229553Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] AUTH_AGENT successful for deviceId=LAB01-PC09. SessionId=c6e590b5-4af7-4885-9342-a9892851bbf4. Sending AUTH_RESULT(success=true).
[2026-09-14T01:56:56.5230297Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] Sent messageType=AUTH_RESULT requestId=e4ed41f5-b936-4239-b43c-195d7c664999 payloadSize=273b
[2026-09-14T01:56:56.5230596Z] [AgentSocketClient] Read response messageType=AUTH_RESULT requestId=e4ed41f5-b936-4239-b43c-195d7c664999 size=273b
[2026-09-14T01:56:56.5230763Z] [AgentSocketClient] Received AUTH_RESULT: SUCCESS, sessionId=c6e590b5-4af7-4885-9342-a9892851bbf4
[2026-09-14T01:56:56.5256085Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] Received messageType=AGENT_GOODBYE requestId=5905e0b9-24e9-4d19-ae16-8f5d3616f917 payloadSize=215b
[2026-09-14T01:56:56.5265384Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] Received AGENT_GOODBYE. Sending GOODBYE_ACK and closing.
[2026-09-14T01:56:56.5267231Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] Sent messageType=GOODBYE_ACK requestId=5905e0b9-24e9-4d19-ae16-8f5d3616f917 payloadSize=206b
[2026-09-14T01:56:56.5267812Z] [Conn:c45f0bb3-645d-4d82-89b6-ef7b7fb6151e] [127.0.0.1:57189] Connection closed.
```

- Không chứa thông tin bí mật (pre-shared key không xuất hiện trong nhật ký).
- Bảo toàn đầy đủ mốc thời gian ISO-8601 UTC, địa chỉ remote endpoint, `requestId` tương quan, tên bản tin và kích thước byte khung dữ liệu.

---

## 6. VẤN ĐỀ ĐÃ BIẾT & HÀNH ĐỘNG TIẾP THEO (KNOWN ISSUES & NEXT ACTION)
- **Vấn đề đã biết (Known Issues):** Không có (0 blocker, 0 cảnh báo biên dịch, 0 lỗi kiểm thử).
- **Hành động tiếp theo (Exact Next Action):** Dừng lại chờ kiểm toán độc lập nghiệm thu GATE 3 trước khi bước sang GATE 4 (Heartbeat / Monotonic Liveness Scanner).
