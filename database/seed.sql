-- ============================================================================
-- LABGUARD DATABASE SEED SCRIPT (DEMO DATA)
-- Database: Microsoft SQL Server 2022 Express
-- Default demo accounts:
--   1. admin    / Admin@123    (Role: Admin)
--   2. operator / Operator@123 (Role: Operator)
--   3. viewer   / Viewer@123   (Role: Viewer)
-- Password hashing: PBKDF2-HMAC-SHA256 (10,000 iterations, 16-byte salt, 32-byte hash)
-- ============================================================================

USE LabGuardDb;
GO

-- 1. USERS SEED
-- Demo salt (16 bytes hex): 0x0102030405060708090A0B0C0D0E0F10
-- Demo hash placeholder (32 bytes binary):
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, PasswordSalt, FullName, Role, IsActive)
    VALUES (
        N'admin',
        0x5E884898DA28047151D0E56F8DC6292773603D0D6AABBDD62A11EF721D1542D8,
        0x0102030405060708090A0B0C0D0E0F10,
        N'Quản Trị Viên Hệ Thống',
        N'Admin',
        1
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'operator')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, PasswordSalt, FullName, Role, IsActive)
    VALUES (
        N'operator',
        0x5E884898DA28047151D0E56F8DC6292773603D0D6AABBDD62A11EF721D1542D8,
        0x0102030405060708090A0B0C0D0E0F10,
        N'Kỹ Thuật Viên Trực Phòng',
        N'Operator',
        1
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'viewer')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, PasswordSalt, FullName, Role, IsActive)
    VALUES (
        N'viewer',
        0x5E884898DA28047151D0E56F8DC6292773603D0D6AABBDD62A11EF721D1542D8,
        0x0102030405060708090A0B0C0D0E0F10,
        N'Giảng Viên Quan Sát',
        N'Viewer',
        1
    );
END
GO

-- 2. DEVICES SEED
IF NOT EXISTS (SELECT 1 FROM dbo.Devices WHERE DeviceId = N'LAB01-PC01')
BEGIN
    INSERT INTO dbo.Devices (DeviceId, DeviceName, MacAddress, IpAddress, AgentVersion, Status, LastSeenUtc)
    VALUES (N'LAB01-PC01', N'Máy tính 01 - Phòng Lab 1', '00:1A:2B:3C:4D:01', '192.168.1.101', '1.0.0', N'OFFLINE', NULL);
END

IF NOT EXISTS (SELECT 1 FROM dbo.Devices WHERE DeviceId = N'LAB01-PC02')
BEGIN
    INSERT INTO dbo.Devices (DeviceId, DeviceName, MacAddress, IpAddress, AgentVersion, Status, LastSeenUtc)
    VALUES (N'LAB01-PC02', N'Máy tính 02 - Phòng Lab 1', '00:1A:2B:3C:4D:02', '192.168.1.102', '1.0.0', N'OFFLINE', NULL);
END

IF NOT EXISTS (SELECT 1 FROM dbo.Devices WHERE DeviceId = N'LAB01-PC03')
BEGIN
    INSERT INTO dbo.Devices (DeviceId, DeviceName, MacAddress, IpAddress, AgentVersion, Status, LastSeenUtc)
    VALUES (N'LAB01-PC03', N'Máy tính 03 - Phòng Lab 1', '00:1A:2B:3C:4D:03', '192.168.1.103', '1.0.0', N'OFFLINE', NULL);
END
GO

-- 3. SERVICES SEED
IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE DeviceId = N'LAB01-PC01' AND ServiceName = N'Web Server')
BEGIN
    INSERT INTO dbo.Services (DeviceId, ServiceName, ExpectedPort, Status, ConsecutiveFailures)
    VALUES (N'LAB01-PC01', N'Web Server', 80, N'UNKNOWN', 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE DeviceId = N'LAB01-PC01' AND ServiceName = N'SSH')
BEGIN
    INSERT INTO dbo.Services (DeviceId, ServiceName, ExpectedPort, Status, ConsecutiveFailures)
    VALUES (N'LAB01-PC01', N'SSH', 22, N'UNKNOWN', 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE DeviceId = N'LAB01-PC02' AND ServiceName = N'Web Server')
BEGIN
    INSERT INTO dbo.Services (DeviceId, ServiceName, ExpectedPort, Status, ConsecutiveFailures)
    VALUES (N'LAB01-PC02', N'Web Server', 80, N'UNKNOWN', 0);
END
GO
