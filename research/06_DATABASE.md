# LABGUARD — CHUYÊN ĐỀ CƠ SỞ DỮ LIỆU, TRANSACTION & KHÓA LẠC QUAN (OCC)
**Mã tài liệu:** `research/06_DATABASE.md`  
**Chuyên đề:** Thiết kế Lược đồ CSDL Chuẩn (T-SQL DDL), Ranh giới Transaction (ACID Boundaries), Khóa Lạc Quan (`rowversion` OCC) & Chế độ Suy giảm khi DB Lỗi (Degraded Mode)  
**Môn học:** NT106 — Lập trình mạng căn bản (Khoa MMT&TT, UIT)  
**Hệ quản trị CSDL:** Microsoft SQL Server 2022 Express (Baseline chính thức ADR-005)  
**Trạng thái:** CHECKPOINT 2 REV2 — FREEZE CANDIDATE (READY FOR FINAL INDEPENDENT AUDIT)  
**Đối tượng thụ hưởng:** Thành viên B (Database & Server Architect), Thành viên C (Admin Client)  

---

## 1. TỔNG QUAN & NGUYÊN TẮC THIẾT KẾ CSDL CHO NT106

Trong đồ án môn Lập trình mạng căn bản, cơ sở dữ liệu đóng vai trò là **tầng lưu trữ dữ liệu bền vững (Durable Persistence Layer)**:
- Lược đồ CSDL được tinh gọn tối đa để phục vụ đúng phạm vi đồ án mạng: Lưu trữ thiết bị, thông số hiệu năng MVP (CPU, RAM, Uptime; Disk đã loại khỏi MVP theo Checkpoint 1), giám sát trạng thái dịch vụ mạng, quản lý vòng đời sự cố (Incident FSM), nhật ký kiểm toán (Audit Logs) và **2 bảng khử trùng lặp kỹ thuật (`ProcessedBatches` và `ProcessedCommands`)**.
- Thao tác dữ liệu được thực hiện thông qua **Dapper ORM kết hợp T-SQL tham số hóa (`SqlCommand` với Parameters)** để loại bỏ nguy cơ SQL Injection và tối ưu hiệu năng đọc/ghi.

---

## 2. KỊCH BẢN DDL T-SQL HOÀN CHỈNH CHO 10 BẢNG CỐT LÕI

```sql
-- ============================================================================
-- LABGUARD DATABASE DDL SCRIPT (CHECKPOINT 2 FREEZE CANDIDATE)
-- Database: Microsoft SQL Server 2022 Express
-- Scope: P0-MVP Telemetry (CPU, RAM, Uptime, Services - Disk excluded)
-- ============================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'LabGuardDb')
BEGIN
    CREATE DATABASE LabGuardDb COLLATE Latin1_General_100_CI_AS_SC_UTF8;
END
GO

USE LabGuardDb;
GO

-- 1. BẢNG NGƯỜI DÙNG & TÀI KHOẢN HỆ THỐNG (Users)
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserId INT IDENTITY(1,1) NOT NULL,
        Username NVARCHAR(64) NOT NULL,
        PasswordHash VARBINARY(32) NOT NULL,      -- PBKDF2-HMAC-SHA256 (32 bytes)
        PasswordSalt VARBINARY(16) NOT NULL,      -- 128-bit Cryptographic Salt
        FullName NVARCHAR(128) NOT NULL,
        Role NVARCHAR(32) NOT NULL,               -- 'Admin', 'Operator', 'Viewer'
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
        CONSTRAINT UQ_Users_Username UNIQUE (Username)
    );
END
GO

-- 2. BẢNG DANH MỤC THIẾT BỊ MÁY TRẠM (Devices)
IF OBJECT_ID(N'dbo.Devices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Devices (
        DeviceId NVARCHAR(64) NOT NULL,           -- Hardware/Hostname ID (e.g. 'LAB01-PC05')
        DeviceName NVARCHAR(128) NOT NULL,
        MacAddress VARCHAR(32) NOT NULL,
        IpAddress VARCHAR(45) NOT NULL,           -- Hỗ trợ IPv4 và IPv6
        AgentVersion VARCHAR(32) NOT NULL,
        Status NVARCHAR(32) NOT NULL DEFAULT N'OFFLINE', -- 'ONLINE', 'OFFLINE', 'DEGRADED'
        LastSeenUtc DATETIME2(3) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Devices PRIMARY KEY CLUSTERED (DeviceId)
    );
    CREATE NONCLUSTERED INDEX IX_Devices_Status_LastSeen ON dbo.Devices (Status, LastSeenUtc);
END
GO

-- 3. BẢNG PHIÊN KẾT NỐI AGENT (AgentSessions)
IF OBJECT_ID(N'dbo.AgentSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentSessions (
        SessionId UNIQUEIDENTIFIER NOT NULL,
        DeviceId NVARCHAR(64) NOT NULL,
        BootId UNIQUEIDENTIFIER NOT NULL,
        ConnectionGeneration INT NOT NULL,
        ConnectedAtUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        DisconnectedAtUtc DATETIME2(3) NULL,
        DisconnectReason NVARCHAR(256) NULL,
        CONSTRAINT PK_AgentSessions PRIMARY KEY CLUSTERED (SessionId),
        CONSTRAINT FK_AgentSessions_Devices FOREIGN KEY (DeviceId) REFERENCES dbo.Devices (DeviceId) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_AgentSessions_DeviceId ON dbo.AgentSessions (DeviceId, DisconnectedAtUtc);
END
GO

-- 4. BẢNG GIÁM SÁT DỊCH VỤ MẠNG (Services)
IF OBJECT_ID(N'dbo.Services', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Services (
        ServiceId INT IDENTITY(1,1) NOT NULL,
        DeviceId NVARCHAR(64) NOT NULL,
        ServiceName NVARCHAR(128) NOT NULL,
        ExpectedPort INT NOT NULL,
        Status NVARCHAR(32) NOT NULL DEFAULT N'UNKNOWN', -- 'HEALTHY', 'DOWN', 'UNKNOWN'
        ConsecutiveFailures INT NOT NULL DEFAULT 0,
        LastCheckedUtc DATETIME2(3) NULL,
        CONSTRAINT PK_Services PRIMARY KEY CLUSTERED (ServiceId),
        CONSTRAINT FK_Services_Devices FOREIGN KEY (DeviceId) REFERENCES dbo.Devices (DeviceId) ON DELETE CASCADE,
        CONSTRAINT UQ_Services_Device_Service UNIQUE (DeviceId, ServiceName)
    );
END
GO

-- 5. BẢNG LƯU MẪU ĐO HIỆU NĂNG THEO CHU KỲ (MetricSamples)
-- Lưu ý: Chỉ số ổ đĩa (Disk) đã được loại khỏi MVP P0 theo Checkpoint 1 baseline
IF OBJECT_ID(N'dbo.MetricSamples', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MetricSamples (
        SampleId BIGINT IDENTITY(1,1) NOT NULL,
        DeviceId NVARCHAR(64) NOT NULL,
        BatchId UNIQUEIDENTIFIER NOT NULL,
        CpuPercent DECIMAL(5,2) NOT NULL,         -- 0.00% to 100.00%
        MemoryPercent DECIMAL(5,2) NOT NULL,      -- 0.00% to 100.00%
        UptimeSeconds BIGINT NOT NULL,            -- System Uptime in seconds
        RecordedAtUtc DATETIME2(3) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_MetricSamples PRIMARY KEY CLUSTERED (SampleId),
        CONSTRAINT FK_MetricSamples_Devices FOREIGN KEY (DeviceId) REFERENCES dbo.Devices (DeviceId)
    );
    CREATE NONCLUSTERED INDEX IX_MetricSamples_DeviceId_RecordedAt 
        ON dbo.MetricSamples (DeviceId, RecordedAtUtc DESC);
END
GO

-- 6. BẢNG QUẢN LÝ SỰ CỐ PHÒNG MÁY (Incidents)
IF OBJECT_ID(N'dbo.Incidents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Incidents (
        IncidentId INT IDENTITY(1,1) NOT NULL,
        DeviceId NVARCHAR(64) NOT NULL,
        RuleType NVARCHAR(64) NOT NULL,           -- 'DEVICE_OFFLINE', 'HIGH_CPU', 'HIGH_MEMORY', 'SERVICE_DOWN'
        Severity NVARCHAR(32) NOT NULL,           -- 'CRITICAL', 'HIGH', 'WARNING', 'INFO'
        ConditionState NVARCHAR(32) NOT NULL,     -- 'ACTIVE', 'RECOVERED', 'UNKNOWN' (Vật lý)
        WorkflowState NVARCHAR(32) NOT NULL,      -- 'OPEN', 'ACKNOWLEDGED', 'IN_PROGRESS', 'RECOVERED', 'RESOLVED' (Vận hành)
        TriggerValue DECIMAL(10,2) NOT NULL,
        OpenedAtUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        RecoveredAtUtc DATETIME2(3) NULL,
        ResolvedAtUtc DATETIME2(3) NULL,
        OccurrenceCount INT NOT NULL DEFAULT 1,
        RowVersion ROWVERSION NOT NULL,           -- SQL Server rowversion data type (8-byte auto-generated)
        CONSTRAINT PK_Incidents PRIMARY KEY CLUSTERED (IncidentId),
        CONSTRAINT FK_Incidents_Devices FOREIGN KEY (DeviceId) REFERENCES dbo.Devices (DeviceId)
    );
    CREATE NONCLUSTERED INDEX IX_Incidents_WorkflowState_OpenedAt 
        ON dbo.Incidents (WorkflowState, OpenedAtUtc DESC);
END
GO

-- 7. BẢNG NHẬT KÝ LỊCH SỬ CHUYỂN TRẠNG THÁI SỰ CỐ (IncidentHistory)
IF OBJECT_ID(N'dbo.IncidentHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IncidentHistory (
        HistoryId BIGINT IDENTITY(1,1) NOT NULL,
        IncidentId INT NOT NULL,
        ChangedByUserId INT NULL,                 -- NULL nếu do hệ thống Rule Engine tự động cập nhật
        OldConditionState NVARCHAR(32) NULL,
        NewConditionState NVARCHAR(32) NULL,
        OldWorkflowState NVARCHAR(32) NULL,
        NewWorkflowState NVARCHAR(32) NULL,
        Note NVARCHAR(512) NOT NULL,
        ChangedAtUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_IncidentHistory PRIMARY KEY CLUSTERED (HistoryId),
        CONSTRAINT FK_IncidentHistory_Incidents FOREIGN KEY (IncidentId) REFERENCES dbo.Incidents (IncidentId) ON DELETE CASCADE,
        CONSTRAINT FK_IncidentHistory_Users FOREIGN KEY (ChangedByUserId) REFERENCES dbo.Users (UserId)
    );
    CREATE NONCLUSTERED INDEX IX_IncidentHistory_IncidentId ON dbo.IncidentHistory (IncidentId, ChangedAtUtc ASC);
END
GO

-- 8. BẢNG KHỬ TRÙNG LẶP TELEMETRY BATCHES (ProcessedBatches)
IF OBJECT_ID(N'dbo.ProcessedBatches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessedBatches (
        DeviceId NVARCHAR(64) NOT NULL,
        BootId UNIQUEIDENTIFIER NOT NULL,
        BatchId UNIQUEIDENTIFIER NOT NULL,
        ProcessedAtUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ProcessedBatches PRIMARY KEY CLUSTERED (DeviceId, BootId, BatchId)
    );
    CREATE NONCLUSTERED INDEX IX_ProcessedBatches_ProcessedAtUtc ON dbo.ProcessedBatches (ProcessedAtUtc);
END
GO

-- 9. BẢNG KHỬ TRÙNG LẶP LỆNH ADMIN (ProcessedCommands)
IF OBJECT_ID(N'dbo.ProcessedCommands', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessedCommands (
        UserId INT NOT NULL,
        CommandId UNIQUEIDENTIFIER NOT NULL,
        RequestDigest CHAR(64) NOT NULL,          -- SHA-256 Hex chuỗi 64 ký tự
        ResponsePayloadJson NVARCHAR(MAX) NOT NULL,
        ProcessedAtUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ProcessedCommands PRIMARY KEY CLUSTERED (UserId, CommandId)
    );
    CREATE NONCLUSTERED INDEX IX_ProcessedCommands_ProcessedAtUtc ON dbo.ProcessedCommands (ProcessedAtUtc);
END
GO

-- 10. BẢNG NHẬT KÝ KIỂM TOÁN VẬN HÀNH (AuditLogs)
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        LogId BIGINT IDENTITY(1,1) NOT NULL,
        UserId INT NULL,                          -- NULL nếu là tác vụ tự động
        Action NVARCHAR(64) NOT NULL,             -- 'LOGIN', 'LOGOUT', 'INCIDENT_ACK', 'INCIDENT_RESOLVE'
        EntityName NVARCHAR(64) NOT NULL,         -- 'Devices', 'Incidents', 'Users'
        EntityId NVARCHAR(64) NOT NULL,
        DetailsJson NVARCHAR(MAX) NULL,
        ClientIp VARCHAR(45) NULL,
        TimestampUtc DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (LogId),
        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
    );
    CREATE NONCLUSTERED INDEX IX_AuditLogs_TimestampUtc ON dbo.AuditLogs (TimestampUtc DESC);
END
GO
```

---

## 3. BẢN CHẤT KỸ THUẬT CỦA KIỂU DỮ LIỆU `rowversion`

Một nội dung quan trọng cần nắm rõ về mặt CSDL:
- **`rowversion` là một kiểu dữ liệu (data type) độc lập của Microsoft SQL Server.**
- Khi một cột được khai báo với kiểu dữ liệu `rowversion`, SQL Server Engine **tự động sinh ra một giá trị nhị phân 8 byte tăng dần đơn điệu** cho bản ghi đó mỗi khi có thao tác `INSERT` hoặc `UPDATE`.
- **Phân biệt kỹ thuật:**
  - `rowversion` **KHÔNG PHẢI** là việc khai báo một cột `binary(8)` thông thường. Cột `binary(8)` thông thường chỉ lưu mảng byte tĩnh do ứng dụng gán vào, không có cơ chế tự động tăng số phiên bản của SQL Server Engine.
  - `rowversion` **HOÀN TOÀN KHÔNG PHẢI** là kiểu dữ liệu ngày giờ (không phải timestamp hay datetime), dù trong các phiên bản SQL Server cũ nó từng mang synonym là `timestamp`.

### Kịch bản Thực thi OCC trong T-SQL:
```sql
UPDATE dbo.Incidents
SET WorkflowState = @NewWorkflowState,
    ResolvedAtUtc = SYSUTCDATETIME()
WHERE IncidentId = @IncidentId 
  AND RowVersion = @ExpectedRowVersion;

SELECT @@ROWCOUNT;
```
Nếu `@@ROWCOUNT == 0`, chứng tỏ bản ghi đã bị thay đổi bởi tác vụ khác. Server trả về lỗi `ERR_CONCURRENCY_VIOLATION` (409 Conflict).

---

## 4. RANH GIỚI TRANSACTION VÀ TÍNH NGUYÊN TỬ (ACID BOUNDARIES)

### Ranh giới Transaction Ingestion
Khi nạp một `METRIC_BATCH` hợp lệ, chuỗi thao tác được bảo đảm trong 1 `SqlTransaction`:
1. Kiểm tra khóa `(DeviceId, BootId, BatchId)` trong `ProcessedBatches`. Nếu đã tồn tại: rollback và trả về cờ trùng lặp để gửi lại `BATCH_ACK`.
2. Chèn bản ghi số liệu hiệu năng vào `MetricSamples`.
3. Đánh giá Rule Engine và cập nhật/tạo mới sự cố trong `Incidents`.
4. Chèn bản ghi đánh dấu đã xử lý vào `ProcessedBatches`.
5. Gọi `tx.CommitAsync()` hoàn tất trước khi gửi `BATCH_ACK` ra socket.

---

## 5. CHẾ ĐỘ HOẠT ĐỘNG KHI CSDL MẤT KẾT NỐI (DEGRADED DATABASE MODE)

Để đảm bảo tính chịu lỗi (Fault-tolerance) mà không gây rủi ro lưu trữ trùng lặp hay tràn bộ nhớ:

### Kiến trúc Lưu trữ Thử lại Do Agent Quản lý (Agent-Owned Retry Model):
Khi Central Server phát hiện kết nối tới SQL Server bị ngắt (`SqlException` hoặc timeout):

1. **Duy trì Socket TCP:** Central Server không tắt tiến trình, giữ nguyên toàn bộ các kết nối socket đang mở với Agent và WinForms Admin.
2. **Duy trì Liveness trên RAM:** Các bản tin `HEARTBEAT` vẫn được tiếp nhận bình thường và cập nhật vào từ điển `MemoryLivenessRegistry` trên RAM dựa trên Monotonic Clock (`Stopwatch.GetTimestamp()`).
3. **Từ chối Gửi ACK cho Telemetry:** Central Server **không lưu trữ và không gửi `BATCH_ACK`** cho các bản tin `METRIC_BATCH`.
   - **Agent là bên sở hữu duy nhất của số liệu chưa commit:** Số liệu này được lưu trữ an toàn trong `ReplayBuffer` của chính Agent.
   - Tránh việc Central Server phải duy trì một hàng đợi RAM thứ hai (vốn tiềm ẩn nguy cơ trùng lặp bản tin retry và tràn bộ nhớ khi mất điện).
4. **Từ chối có Kiểm soát các Lệnh Quản trị (Admin Fail-Fast):**
   - Mọi lệnh thay đổi trạng thái từ WinForms Admin (`INCIDENT_ACK`, `INCIDENT_RESOLVE`) đòi hỏi ghi CSDL sẽ nhận ngay bản tin `ERROR`:
     ```json
     {
       "version": 1,
       "messageType": "ERROR",
       "requestId": "770e8400-e29b-41d4-a716-446655440002",
       "deviceId": "SERVER",
       "sentAtUtc": "2026-09-14T08:05:00.0000000Z",
       "payload": {
         "errorCode": "ERR_DATABASE_UNAVAILABLE",
         "message": "Cơ sở dữ liệu đang tạm thời mất kết nối. Hệ thống đang hoạt động ở chế độ giám sát suy giảm."
       }
     }
     ```
5. **Cảnh báo Trạng thái Suy giảm:** Central Server phát sóng `HEALTH_UPDATE` (nếu tính năng sức khỏe tùy chọn được kích hoạt) thông báo trạng thái `DEGRADED`.
6. **Tự động Phục hồi:** Khi SQL Server hoạt động trở lại, Central Server nối lại CSDL. Các Agent sau khi timeout sẽ tự động gửi lại các mẻ đo từ `ReplayBuffer`, Server ghi CSDL thành công và phản hồi `BATCH_ACK` tuần tự.
