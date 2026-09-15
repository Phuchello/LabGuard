using System.Data;
using System.Security.Cryptography;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;
using Microsoft.Data.SqlClient;

namespace LabGuard.Central;

/// <summary>
/// Handles SQL Server persistence for devices, metric samples, telemetry deduplication, and admin queries.
/// Uses standard ADO.NET (Microsoft.Data.SqlClient) for high reliability, clean resource handling,
/// and full alignment with NT106 curriculum.
/// </summary>
public class DatabaseRepository
{
    private readonly string _connectionString;
    private readonly Action<string>? _logger;

    public string ConnectionString => _connectionString;

    public DatabaseRepository(string? connectionString = null, Action<string>? logger = null)
    {
        _logger = logger;
        _connectionString = connectionString
            ?? Environment.GetEnvironmentVariable("LABGUARD_DB_CONNECTION")
            ?? "Server=localhost;Database=LabGuardDb;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    private void Log(string message)
    {
        _logger?.Invoke($"[DatabaseRepository] {message}");
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    /// <summary>
    /// Ensures LabGuardDb database and tables exist by applying schema and seed scripts if not already present.
    /// Returns true if database is accessible and verified ready; false otherwise.
    /// </summary>
    public async Task<bool> EnsureDatabaseInitializedAsync()
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            string originalDb = builder.InitialCatalog;

            // 1. Connect to master to verify database exists
            builder.InitialCatalog = "master";
            await using (var masterConn = new SqlConnection(builder.ConnectionString))
            {
                await masterConn.OpenAsync().ConfigureAwait(false);
                string checkDbSql = "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = @DbName) CREATE DATABASE [" + originalDb + "];";
                await using var checkDbCmd = new SqlCommand(checkDbSql, masterConn);
                checkDbCmd.Parameters.AddWithValue("@DbName", originalDb);
                await checkDbCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            // 2. Connect to target database and check if tables exist
            await using (var targetConn = new SqlConnection(_connectionString))
            {
                await targetConn.OpenAsync().ConfigureAwait(false);
                int tableCount = 0;
                await using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'MetricSamples';", targetConn))
                {
                    var countObj = await countCmd.ExecuteScalarAsync().ConfigureAwait(false);
                    tableCount = Convert.ToInt32(countObj ?? 0);
                }

                if (tableCount == 0)
                {
                    Log("MetricSamples table not found. Executing schema initialization...");
                    string schemaSql = FindSqlScript("schema.sql");
                    if (string.IsNullOrEmpty(schemaSql))
                    {
                        throw new FileNotFoundException("Cannot locate database/schema.sql.");
                    }

                    await ExecuteSqlBatchesAsync(targetConn, schemaSql).ConfigureAwait(false);

                    string seedSql = FindSqlScript("seed.sql");
                    if (!string.IsNullOrEmpty(seedSql))
                    {
                        await ExecuteSqlBatchesAsync(targetConn, seedSql).ConfigureAwait(false);
                    }
                    Log("Database schema and seed initialized successfully.");
                }
                else
                {
                    Log("Database schema is already initialized.");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log($"Database initialization warning: {ex.Message}");
            return false;
        }
    }

    private static string FindSqlScript(string fileName)
    {
        // 1. Search by traversing up from AppContext.BaseDirectory
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "database", fileName);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }
            dir = dir.Parent;
        }

        // 2. Search by traversing up from CurrentDirectory
        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "database", fileName);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }
            dir = dir.Parent;
        }

        return string.Empty;
    }

    private static async Task ExecuteSqlBatchesAsync(SqlConnection conn, string sql)
    {
        string[] batches = System.Text.RegularExpressions.Regex.Split(
            sql,
            @"^\s*GO\s*$",
            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var batch in batches)
        {
            string trimmed = batch.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                if (trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                await using var cmd = new SqlCommand(trimmed, conn);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Ensures device row exists in dbo.Devices.
    /// </summary>
    public async Task EnsureDeviceRegisteredAsync(string deviceId, string? hostname = null, string? ipAddress = null, string? agentVersion = null)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);
            const string sql = @"
MERGE dbo.Devices AS target
USING (SELECT @DeviceId AS DeviceId) AS source
ON (target.DeviceId = source.DeviceId)
WHEN MATCHED THEN
    UPDATE SET LastSeenUtc = SYSUTCDATETIME(), Status = 'ONLINE'
WHEN NOT MATCHED THEN
    INSERT (DeviceId, DeviceName, MacAddress, IpAddress, AgentVersion, Status, LastSeenUtc, CreatedAtUtc)
    VALUES (@DeviceId, COALESCE(@Hostname, @DeviceId), '00:00:00:00:00:00', COALESCE(@IpAddress, '127.0.0.1'), COALESCE(@AgentVersion, '1.0.0'), 'ONLINE', SYSUTCDATETIME(), SYSUTCDATETIME());";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DeviceId", deviceId);
            cmd.Parameters.AddWithValue("@Hostname", (object?)hostname ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? "127.0.0.1");
            cmd.Parameters.AddWithValue("@AgentVersion", (object?)agentVersion ?? "1.0.0");
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log($"Failed to register device '{deviceId}': {ex.Message}");
        }
    }

    /// <summary>
    /// Updates status of a device in dbo.Devices.
    /// </summary>
    public async Task UpdateDeviceStatusAsync(string deviceId, string status)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);
            const string sql = @"
UPDATE dbo.Devices
SET Status = @Status,
    LastSeenUtc = CASE WHEN @Status = 'ONLINE' THEN SYSUTCDATETIME() ELSE LastSeenUtc END
WHERE DeviceId = @DeviceId;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DeviceId", deviceId);
            cmd.Parameters.AddWithValue("@Status", status);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log($"Failed to update device status for '{deviceId}': {ex.Message}");
        }
    }

    /// <summary>
    /// Atomically processes a METRIC_BATCH within a SQL transaction with deduplication check against ProcessedBatches.
    /// Returns (Success, IsDuplicate).
    /// </summary>
    public async Task<(bool Success, bool IsDuplicate)> ProcessMetricBatchAsync(string deviceId, MetricBatchPayload batch, string? clientIp = null)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (!Guid.TryParse(batch.BootId, out var bootIdGuid))
        {
            Log($"Invalid BootId format: {batch.BootId}");
            return (false, false);
        }

        if (!Guid.TryParse(batch.BatchId, out var batchIdGuid))
        {
            Log($"Invalid BatchId format: {batch.BatchId}");
            return (false, false);
        }

        double cpu = batch.Samples.Find(s => string.Equals(s.MetricName, "CPU", StringComparison.OrdinalIgnoreCase))?.MetricValue ?? 0.0;
        double ram = batch.Samples.Find(s => string.Equals(s.MetricName, "RAM", StringComparison.OrdinalIgnoreCase))?.MetricValue ?? 0.0;
        long uptime = batch.UptimeSeconds;

        SqlConnection? conn = null;
        SqlTransaction? tx = null;

        try
        {
            conn = new SqlConnection(_connectionString);
            await conn.OpenAsync().ConfigureAwait(false);
            tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted).ConfigureAwait(false);

            // 1. Check if batch has already been processed (Deduplication)
            const string checkSql = @"
SELECT 1 FROM dbo.ProcessedBatches WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
WHERE DeviceId = @DeviceId AND BootId = @BootId AND BatchId = @BatchId;";

            await using (var checkCmd = new SqlCommand(checkSql, conn, tx))
            {
                checkCmd.Parameters.AddWithValue("@DeviceId", deviceId);
                checkCmd.Parameters.AddWithValue("@BootId", bootIdGuid);
                checkCmd.Parameters.AddWithValue("@BatchId", batchIdGuid);

                var existsObj = await checkCmd.ExecuteScalarAsync().ConfigureAwait(false);
                if (existsObj != null && existsObj != DBNull.Value)
                {
                    await tx.RollbackAsync().ConfigureAwait(false);
                    Log($"Duplicate batch {batch.BatchId} detected for device '{deviceId}'. Skipping insertion and acknowledging.");
                    return (true, true);
                }
            }

            // 2. Ensure device exists in dbo.Devices
            const string ensureDeviceSql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.Devices WITH (UPDLOCK, HOLDLOCK, ROWLOCK) WHERE DeviceId = @DeviceId)
BEGIN
    INSERT INTO dbo.Devices (DeviceId, DeviceName, MacAddress, IpAddress, AgentVersion, Status, LastSeenUtc, CreatedAtUtc)
    VALUES (@DeviceId, @DeviceId, '00:00:00:00:00:00', COALESCE(@IpAddress, '127.0.0.1'), '1.0.0', 'ONLINE', SYSUTCDATETIME(), SYSUTCDATETIME());
END
ELSE
BEGIN
    UPDATE dbo.Devices
    SET Status = 'ONLINE', LastSeenUtc = SYSUTCDATETIME()
    WHERE DeviceId = @DeviceId;
END";

            await using (var ensureCmd = new SqlCommand(ensureDeviceSql, conn, tx))
            {
                ensureCmd.Parameters.AddWithValue("@DeviceId", deviceId);
                ensureCmd.Parameters.AddWithValue("@IpAddress", (object?)clientIp ?? "127.0.0.1");
                await ensureCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            // 3. Insert into MetricSamples
            const string insertSampleSql = @"
INSERT INTO dbo.MetricSamples (DeviceId, BatchId, CpuPercent, MemoryPercent, UptimeSeconds, RecordedAtUtc, CreatedAtUtc)
VALUES (@DeviceId, @BatchId, @Cpu, @Ram, @Uptime, SYSUTCDATETIME(), SYSUTCDATETIME());";

            await using (var insertSampleCmd = new SqlCommand(insertSampleSql, conn, tx))
            {
                insertSampleCmd.Parameters.AddWithValue("@DeviceId", deviceId);
                insertSampleCmd.Parameters.AddWithValue("@BatchId", batchIdGuid);
                insertSampleCmd.Parameters.AddWithValue("@Cpu", (decimal)Math.Round(cpu, 2));
                insertSampleCmd.Parameters.AddWithValue("@Ram", (decimal)Math.Round(ram, 2));
                insertSampleCmd.Parameters.AddWithValue("@Uptime", uptime);
                await insertSampleCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            // 4. Record into ProcessedBatches
            const string insertDedupSql = @"
INSERT INTO dbo.ProcessedBatches (DeviceId, BootId, BatchId, ProcessedAtUtc)
VALUES (@DeviceId, @BootId, @BatchId, SYSUTCDATETIME());";

            await using (var insertDedupCmd = new SqlCommand(insertDedupSql, conn, tx))
            {
                insertDedupCmd.Parameters.AddWithValue("@DeviceId", deviceId);
                insertDedupCmd.Parameters.AddWithValue("@BootId", bootIdGuid);
                insertDedupCmd.Parameters.AddWithValue("@BatchId", batchIdGuid);
                await insertDedupCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            // Commit transaction
            await tx.CommitAsync().ConfigureAwait(false);
            Log($"Successfully persisted batch {batch.BatchId} for device '{deviceId}': CPU={cpu:F1}%, RAM={ram:F1}%, Uptime={uptime}s.");
            return (true, false);
        }
        catch (Exception ex)
        {
            if (tx != null)
            {
                try { await tx.RollbackAsync().ConfigureAwait(false); } catch { }
            }
            Log($"SQL Transaction failed for batch {batch.BatchId}: {ex.Message}");
            return (false, false);
        }
        finally
        {
            if (tx != null) await tx.DisposeAsync().ConfigureAwait(false);
            if (conn != null) await conn.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Retrieves all devices with their latest metric samples for Admin Dashboard.
    /// </summary>
    public async Task<List<DeviceSummaryDto>> GetDeviceSummariesAsync()
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);
            const string sql = @"
SELECT 
    d.DeviceId,
    d.DeviceName AS Hostname,
    d.IpAddress,
    d.MacAddress,
    d.Status,
    CONVERT(VARCHAR(33), d.LastSeenUtc, 126) AS LastSeenUtc,
    CAST(m.CpuPercent AS FLOAT) AS CpuUsage,
    CAST(m.MemoryPercent AS FLOAT) AS RamUsage,
    m.UptimeSeconds
FROM dbo.Devices d
OUTER APPLY (
    SELECT TOP 1 CpuPercent, MemoryPercent, UptimeSeconds
    FROM dbo.MetricSamples s
    WHERE s.DeviceId = d.DeviceId
    ORDER BY s.SampleId DESC
) m
ORDER BY d.DeviceId ASC;";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            var list = new List<DeviceSummaryDto>();
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                list.Add(new DeviceSummaryDto
                {
                    DeviceId = reader.GetString(0),
                    Hostname = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    IpAddress = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    MacAddress = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Status = reader.IsDBNull(4) ? "UNKNOWN" : reader.GetString(4),
                    LastSeenUtc = reader.IsDBNull(5) ? null : reader.GetString(5),
                    CpuUsage = reader.IsDBNull(6) ? null : Convert.ToDouble(reader.GetValue(6)),
                    RamUsage = reader.IsDBNull(7) ? null : Convert.ToDouble(reader.GetValue(7)),
                    UptimeSeconds = reader.IsDBNull(8) ? null : Convert.ToInt64(reader.GetValue(8))
                });
            }
            return list;
        }
        catch (Exception ex)
        {
            Log($"Failed to retrieve device summaries: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Validates Admin user login credentials.
    /// Supports demo accounts from seed.sql (admin/Admin@123, operator/Operator@123, viewer/Viewer@123).
    /// </summary>
    public async Task<(bool Success, string? FullName, string? Role, int? UserId)> AuthenticateUserAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, null, null, null);
        }

        // Fast path for standard development seed credentials
        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "Admin@123")
        {
            return (true, "Quản Trị Viên Hệ Thống", "Admin", 1);
        }
        if (username.Equals("operator", StringComparison.OrdinalIgnoreCase) && password == "Operator@123")
        {
            return (true, "Kỹ Thuật Viên Trực Phòng", "Operator", 2);
        }
        if (username.Equals("viewer", StringComparison.OrdinalIgnoreCase) && password == "Viewer@123")
        {
            return (true, "Giảng Viên Quan Sát", "Viewer", 3);
        }

        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);
            const string sql = @"
SELECT UserId, FullName, Role, PasswordHash, PasswordSalt, IsActive
FROM dbo.Users
WHERE Username = @Username;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await reader.ReadAsync().ConfigureAwait(false))
            {
                int userId = reader.GetInt32(0);
                string fullName = reader.GetString(1);
                string role = reader.GetString(2);
                byte[] passwordHash = (byte[])reader.GetValue(3);
                byte[] passwordSalt = (byte[])reader.GetValue(4);
                bool isActive = reader.GetBoolean(5);

                if (isActive && passwordHash.Length > 0 && passwordSalt.Length > 0)
                {
                    byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                        password,
                        passwordSalt,
                        10000,
                        HashAlgorithmName.SHA256,
                        32);

                    if (CryptographicOperations.FixedTimeEquals(passwordHash, computedHash))
                    {
                        return (true, fullName, role, userId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Authentication DB error: {ex.Message}");
        }

        return (false, null, null, null);
    }

    /// <summary>
    /// Opens a new incident or increments occurrence counter if an ACTIVE incident already exists for (DeviceId, RuleType).
    /// </summary>
    public async Task<IncidentSummaryDto?> OpenOrUpdateIncidentAsync(string deviceId, string ruleType, string severity, double triggerValue)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);

            await EnsureDeviceRegisteredAsync(deviceId).ConfigureAwait(false);

            const string selectSql = @"
SELECT IncidentId, OccurrenceCount, RowVersion, OpenedAtUtc
FROM dbo.Incidents
WHERE DeviceId = @DeviceId AND RuleType = @RuleType AND ConditionState = 'ACTIVE';";

            await using (var selCmd = new SqlCommand(selectSql, conn))
            {
                selCmd.Parameters.AddWithValue("@DeviceId", deviceId);
                selCmd.Parameters.AddWithValue("@RuleType", ruleType);

                await using var reader = await selCmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (await reader.ReadAsync().ConfigureAwait(false))
                {
                    int incidentId = reader.GetInt32(0);
                    int occurrenceCount = reader.GetInt32(1);
                    byte[] rowVersionBytes = (byte[])reader.GetValue(2);
                    DateTime openedAt = reader.GetDateTime(3);
                    await reader.CloseAsync().ConfigureAwait(false);

                    const string updateSql = @"
UPDATE dbo.Incidents
SET OccurrenceCount = OccurrenceCount + 1,
    TriggerValue = @TriggerValue,
    Severity = @Severity
WHERE IncidentId = @IncidentId;
SELECT RowVersion FROM dbo.Incidents WHERE IncidentId = @IncidentId;";

                    await using var updateCmd = new SqlCommand(updateSql, conn);
                    updateCmd.Parameters.AddWithValue("@IncidentId", incidentId);
                    updateCmd.Parameters.AddWithValue("@TriggerValue", (decimal)Math.Round(triggerValue, 2));
                    updateCmd.Parameters.AddWithValue("@Severity", severity);

                    var newRvObj = await updateCmd.ExecuteScalarAsync().ConfigureAwait(false);
                    byte[] newRvBytes = newRvObj is byte[] b ? b : rowVersionBytes;
                    string rvHex = "0x" + Convert.ToHexString(newRvBytes).ToLowerInvariant();

                    Log($"Updated ACTIVE incident #{incidentId} for {deviceId} ({ruleType}, occurrences={occurrenceCount + 1})");
                    return new IncidentSummaryDto
                    {
                        IncidentId = incidentId,
                        DeviceId = deviceId,
                        RuleName = ruleType,
                        ConditionState = "ACTIVE",
                        WorkflowState = "OPEN",
                        OccurrenceCount = occurrenceCount + 1,
                        OpenedAtUtc = openedAt.ToString("O"),
                        RowVersionHex = rvHex
                    };
                }
            }

            const string insertSql = @"
INSERT INTO dbo.Incidents (DeviceId, RuleType, Severity, ConditionState, WorkflowState, TriggerValue, OpenedAtUtc, OccurrenceCount)
OUTPUT INSERTED.IncidentId, INSERTED.RowVersion, INSERTED.OpenedAtUtc
VALUES (@DeviceId, @RuleType, @Severity, 'ACTIVE', 'OPEN', @TriggerValue, SYSUTCDATETIME(), 1);";

            await using var insertCmd = new SqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@DeviceId", deviceId);
            insertCmd.Parameters.AddWithValue("@RuleType", ruleType);
            insertCmd.Parameters.AddWithValue("@Severity", severity);
            insertCmd.Parameters.AddWithValue("@TriggerValue", (decimal)Math.Round(triggerValue, 2));

            await using var insertReader = await insertCmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await insertReader.ReadAsync().ConfigureAwait(false))
            {
                int newId = insertReader.GetInt32(0);
                byte[] rv = (byte[])insertReader.GetValue(1);
                DateTime openedAt = insertReader.GetDateTime(2);
                string rvHex = "0x" + Convert.ToHexString(rv).ToLowerInvariant();

                Log($"Created new ACTIVE incident #{newId} for {deviceId} ({ruleType}, trigger={triggerValue:F1})");
                return new IncidentSummaryDto
                {
                    IncidentId = newId,
                    DeviceId = deviceId,
                    RuleName = ruleType,
                    ConditionState = "ACTIVE",
                    WorkflowState = "OPEN",
                    OccurrenceCount = 1,
                    OpenedAtUtc = openedAt.ToString("O"),
                    RowVersionHex = rvHex
                };
            }
        }
        catch (Exception ex)
        {
            Log($"OpenOrUpdateIncident DB error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Recovers any ACTIVE incident for (DeviceId, RuleType).
    /// Updates ConditionState = 'RECOVERED', WorkflowState = 'RECOVERED', and records in IncidentHistory.
    /// </summary>
    public async Task<IncidentSummaryDto?> RecoverIncidentAsync(string deviceId, string ruleType)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);

            const string selectSql = @"
SELECT IncidentId, OccurrenceCount, OpenedAtUtc, RowVersion
FROM dbo.Incidents
WHERE DeviceId = @DeviceId AND RuleType = @RuleType AND ConditionState = 'ACTIVE';";

            int incidentId = 0;
            int occurrenceCount = 0;
            DateTime openedAt = DateTime.UtcNow;

            await using (var selCmd = new SqlCommand(selectSql, conn))
            {
                selCmd.Parameters.AddWithValue("@DeviceId", deviceId);
                selCmd.Parameters.AddWithValue("@RuleType", ruleType);

                await using var reader = await selCmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (await reader.ReadAsync().ConfigureAwait(false))
                {
                    incidentId = reader.GetInt32(0);
                    occurrenceCount = reader.GetInt32(1);
                    openedAt = reader.GetDateTime(2);
                }
                else
                {
                    return null;
                }
            }

            const string updateSql = @"
UPDATE dbo.Incidents
SET ConditionState = 'RECOVERED',
    WorkflowState = 'RECOVERED',
    RecoveredAtUtc = SYSUTCDATETIME()
OUTPUT INSERTED.RowVersion
WHERE IncidentId = @IncidentId;

INSERT INTO dbo.IncidentHistory (IncidentId, ChangedByUserId, OldConditionState, NewConditionState, OldWorkflowState, NewWorkflowState, Note, ChangedAtUtc)
VALUES (@IncidentId, NULL, 'ACTIVE', 'RECOVERED', 'OPEN', 'RECOVERED', N'Tự động phục hồi bởi Rule Engine', SYSUTCDATETIME());";

            await using var updateCmd = new SqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@IncidentId", incidentId);

            var newRvObj = await updateCmd.ExecuteScalarAsync().ConfigureAwait(false);
            byte[] newRvBytes = newRvObj is byte[] b ? b : new byte[8];
            string rvHex = "0x" + Convert.ToHexString(newRvBytes).ToLowerInvariant();

            Log($"Recovered incident #{incidentId} for {deviceId} ({ruleType})");
            return new IncidentSummaryDto
            {
                IncidentId = incidentId,
                DeviceId = deviceId,
                RuleName = ruleType,
                ConditionState = "RECOVERED",
                WorkflowState = "RECOVERED",
                OccurrenceCount = occurrenceCount,
                OpenedAtUtc = openedAt.ToString("O"),
                RowVersionHex = rvHex
            };
        }
        catch (Exception ex)
        {
            Log($"RecoverIncident DB error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Retrieves all ACTIVE incidents for display on the Admin Dashboard.
    /// </summary>
    /// <summary>
    /// Retrieves all active and unresolved incidents for display on the Admin Dashboard.
    /// </summary>
    public async Task<List<IncidentSummaryDto>> GetActiveIncidentsAsync()
    {
        var list = new List<IncidentSummaryDto>();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);

            const string sql = @"
SELECT IncidentId, DeviceId, RuleType, ConditionState, WorkflowState, OccurrenceCount, OpenedAtUtc, RowVersion
FROM dbo.Incidents
WHERE ConditionState = 'ACTIVE' OR WorkflowState != 'RESOLVED'
ORDER BY OpenedAtUtc DESC;";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                int id = reader.GetInt32(0);
                string deviceId = reader.GetString(1);
                string ruleType = reader.GetString(2);
                string cond = reader.GetString(3);
                string wf = reader.GetString(4);
                int count = reader.GetInt32(5);
                DateTime openedAt = reader.GetDateTime(6);
                byte[] rv = (byte[])reader.GetValue(7);

                list.Add(new IncidentSummaryDto
                {
                    IncidentId = id,
                    DeviceId = deviceId,
                    RuleName = ruleType,
                    ConditionState = cond,
                    WorkflowState = wf,
                    OccurrenceCount = count,
                    OpenedAtUtc = openedAt.ToString("O"),
                    RowVersionHex = "0x" + Convert.ToHexString(rv).ToLowerInvariant()
                });
            }
        }
        catch (Exception ex)
        {
            Log($"GetActiveIncidents DB error: {ex.Message}");
        }

        return list;
    }

    /// <summary>
    /// Retrieves an incident by its ID.
    /// </summary>
    public async Task<IncidentSummaryDto?> GetIncidentByIdAsync(long incidentId)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);

            const string sql = @"
SELECT IncidentId, DeviceId, RuleType, ConditionState, WorkflowState, OccurrenceCount, OpenedAtUtc, RowVersion
FROM dbo.Incidents
WHERE IncidentId = @IncidentId;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidentId", incidentId);
            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await reader.ReadAsync().ConfigureAwait(false))
            {
                int id = reader.GetInt32(0);
                string deviceId = reader.GetString(1);
                string ruleType = reader.GetString(2);
                string cond = reader.GetString(3);
                string wf = reader.GetString(4);
                int count = reader.GetInt32(5);
                DateTime openedAt = reader.GetDateTime(6);
                byte[] rv = (byte[])reader.GetValue(7);

                return new IncidentSummaryDto
                {
                    IncidentId = id,
                    DeviceId = deviceId,
                    RuleName = ruleType,
                    ConditionState = cond,
                    WorkflowState = wf,
                    OccurrenceCount = count,
                    OpenedAtUtc = openedAt.ToString("O"),
                    RowVersionHex = "0x" + Convert.ToHexString(rv).ToLowerInvariant()
                };
            }
        }
        catch (Exception ex)
        {
            Log($"GetIncidentById DB error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Executes an admin incident command with Optimistic Concurrency Control (OCC) and Idempotency deduplication.
    /// Handles INCIDENT_ACK, INCIDENT_ASSIGN, INCIDENT_START, and INCIDENT_RESOLVE.
    /// </summary>
    public async Task<CommandResultPayload> ExecuteIncidentCommandAsync(int userId, IncidentCommandPayload command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.CommandId))
        {
            return new CommandResultPayload
            {
                CommandId = command.CommandId ?? string.Empty,
                Success = false,
                StatusCode = 400,
                ErrorCode = "ERR_INVALID_COMMAND_ID",
                ErrorMessage = "CommandId must be provided."
            };
        }

        string cmdType = command.CommandType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (cmdType != MessageTypes.IncidentAck &&
            cmdType != MessageTypes.IncidentAssign &&
            cmdType != MessageTypes.IncidentStart &&
            cmdType != MessageTypes.IncidentResolve)
        {
            return new CommandResultPayload
            {
                CommandId = command.CommandId,
                Success = false,
                StatusCode = 400,
                ErrorCode = "ERR_UNKNOWN_COMMAND",
                ErrorMessage = $"Unknown incident command type: '{command.CommandType}'."
            };
        }

        // Extract parameters
        int? assignedToUserId = null;
        if (command.CommandParams != null)
        {
            if (command.CommandParams.TryGetValue("assignedToUserId", out var aObj) && aObj != null)
            {
                if (int.TryParse(aObj.ToString(), out int parsedUid))
                {
                    assignedToUserId = parsedUid;
                }
            }
        }

        string? resolutionNote = null;
        if (command.CommandParams != null)
        {
            if (command.CommandParams.TryGetValue("resolutionNote", out var nObj) && nObj != null)
            {
                resolutionNote = nObj.ToString();
            }
            else if (command.CommandParams.TryGetValue("note", out var noteObj) && noteObj != null)
            {
                resolutionNote = noteObj.ToString();
            }
        }

        // Compute canonical SHA-256 RequestDigest per CP2 frozen specification
        string computedDigest = ProtocolSerializer.ComputeRequestDigest(
            cmdType,
            command.IncidentId,
            command.ExpectedRowVersion ?? string.Empty,
            assignedToUserId,
            resolutionNote,
            command.CommandId);

        SqlConnection? conn = null;
        SqlTransaction? tx = null;

        try
        {
            conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);
            tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted).ConfigureAwait(false);

            // 1. Idempotency Check in dbo.ProcessedCommands WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
            const string checkCmdSql = @"
SELECT RequestDigest, ResponsePayloadJson
FROM dbo.ProcessedCommands WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
WHERE UserId = @UserId AND CommandId = @CommandId;";

            await using (var checkCmd = new SqlCommand(checkCmdSql, conn, tx))
            {
                checkCmd.Parameters.AddWithValue("@UserId", userId);
                checkCmd.Parameters.AddWithValue("@CommandId", Guid.Parse(command.CommandId));

                await using var checkReader = await checkCmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (await checkReader.ReadAsync().ConfigureAwait(false))
                {
                    string existingDigest = checkReader.GetString(0).Trim();
                    string existingResponseJson = checkReader.GetString(1);
                    await checkReader.CloseAsync().ConfigureAwait(false);

                    if (string.Equals(existingDigest, computedDigest, StringComparison.OrdinalIgnoreCase))
                    {
                        // Idempotent replay: return previously recorded response
                        Log($"Idempotent replay for CommandId {command.CommandId} (UserId {userId})");
                        var cachedResult = ProtocolSerializer.Deserialize<CommandResultPayload>(existingResponseJson);
                        if (cachedResult != null)
                        {
                            return cachedResult;
                        }
                        return new CommandResultPayload
                        {
                            CommandId = command.CommandId,
                            Success = true,
                            StatusCode = 200
                        };
                    }
                    else
                    {
                        // Idempotency key reuse violation with different payload!
                        Log($"Idempotency key reuse detected for CommandId {command.CommandId} (UserId {userId}): Digest mismatch.");
                        return new CommandResultPayload
                        {
                            CommandId = command.CommandId,
                            Success = false,
                            StatusCode = 409,
                            ErrorCode = "ERR_IDEMPOTENCY_KEY_REUSE",
                            ErrorMessage = "CommandId has already been processed with different request parameters."
                        };
                    }
                }
            }

            // 2. Fetch current incident record with lock
            const string selectIncidentSql = @"
SELECT IncidentId, DeviceId, RuleType, Severity, ConditionState, WorkflowState, RowVersion
FROM dbo.Incidents WITH (UPDLOCK, ROWLOCK)
WHERE IncidentId = @IncidentId;";

            string currentConditionState;
            string currentWorkflowState;
            byte[] currentRowVersionBytes;

            await using (var selCmd = new SqlCommand(selectIncidentSql, conn, tx))
            {
                selCmd.Parameters.AddWithValue("@IncidentId", command.IncidentId);

                await using var reader = await selCmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (!await reader.ReadAsync().ConfigureAwait(false))
                {
                    return new CommandResultPayload
                    {
                        CommandId = command.CommandId,
                        Success = false,
                        StatusCode = 404,
                        ErrorCode = "ERR_INCIDENT_NOT_FOUND",
                        ErrorMessage = $"Incident #{command.IncidentId} does not exist."
                    };
                }

                currentConditionState = reader.GetString(4);
                currentWorkflowState = reader.GetString(5);
                currentRowVersionBytes = (byte[])reader.GetValue(6);
            }

            // 3. Concurrency (OCC) validation
            string currentRvHex = "0x" + Convert.ToHexString(currentRowVersionBytes).ToLowerInvariant();
            string cleanExpected = (command.ExpectedRowVersion ?? string.Empty).Trim().ToLowerInvariant();
            if (cleanExpected.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                cleanExpected = cleanExpected[2..];
            }
            string normExpectedRvHex = "0x" + cleanExpected.PadLeft(16, '0');

            if (!string.Equals(currentRvHex, normExpectedRvHex, StringComparison.OrdinalIgnoreCase))
            {
                Log($"OCC Conflict for incident #{command.IncidentId}: expected {normExpectedRvHex}, but database has {currentRvHex}");
                return new CommandResultPayload
                {
                    CommandId = command.CommandId,
                    Success = false,
                    StatusCode = 409,
                    ErrorCode = "ERR_CONCURRENCY_CONFLICT",
                    ErrorMessage = "Incident was modified by another operator. Please refresh and retry."
                };
            }

            // 4. Validate state machine transition & resolution guards
            string oldWorkflow = currentWorkflowState.ToUpperInvariant();
            string newWorkflow;
            string historyNote;

            switch (cmdType)
            {
                case MessageTypes.IncidentAck:
                    if (oldWorkflow != "OPEN")
                    {
                        return new CommandResultPayload
                        {
                            CommandId = command.CommandId,
                            Success = false,
                            StatusCode = 400,
                            ErrorCode = "ERR_INVALID_TRANSITION",
                            ErrorMessage = $"Cannot ACK incident in '{oldWorkflow}' state. Must be OPEN."
                        };
                    }
                    newWorkflow = "ACKNOWLEDGED";
                    historyNote = "Sự cố đã được tiếp nhận xử lý";
                    break;

                case MessageTypes.IncidentAssign:
                    if (oldWorkflow != "OPEN" && oldWorkflow != "ACKNOWLEDGED" && oldWorkflow != "IN_PROGRESS")
                    {
                        return new CommandResultPayload
                        {
                            CommandId = command.CommandId,
                            Success = false,
                            StatusCode = 400,
                            ErrorCode = "ERR_INVALID_TRANSITION",
                            ErrorMessage = $"Cannot ASSIGN incident in '{oldWorkflow}' state."
                        };
                    }
                    if (!assignedToUserId.HasValue || assignedToUserId.Value <= 0)
                    {
                        return new CommandResultPayload
                        {
                            CommandId = command.CommandId,
                            Success = false,
                            StatusCode = 400,
                            ErrorCode = "ERR_MISSING_ASSIGNEE",
                            ErrorMessage = "AssignedToUserId is required for INCIDENT_ASSIGN."
                        };
                    }
                    newWorkflow = "IN_PROGRESS";
                    historyNote = $"Phân công xử lý cho người dùng ID {assignedToUserId.Value}";
                    break;

                case MessageTypes.IncidentStart:
                    if (oldWorkflow != "OPEN" && oldWorkflow != "ACKNOWLEDGED")
                    {
                        return new CommandResultPayload
                        {
                            CommandId = command.CommandId,
                            Success = false,
                            StatusCode = 400,
                            ErrorCode = "ERR_INVALID_TRANSITION",
                            ErrorMessage = $"Cannot START incident in '{oldWorkflow}' state. Must be OPEN or ACKNOWLEDGED."
                        };
                    }
                    newWorkflow = "IN_PROGRESS";
                    historyNote = "Bắt đầu tiến hành xử lý sự cố";
                    break;

                case MessageTypes.IncidentResolve:
                    if (oldWorkflow == "RESOLVED")
                    {
                        return new CommandResultPayload
                        {
                            CommandId = command.CommandId,
                            Success = false,
                            StatusCode = 400,
                            ErrorCode = "ERR_INVALID_TRANSITION",
                            ErrorMessage = "Incident is already RESOLVED."
                        };
                    }

                    // Guard: Cannot resolve while physical condition is ACTIVE
                    if (string.Equals(currentConditionState, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                    {
                        return new CommandResultPayload
                        {
                            CommandId = command.CommandId,
                            Success = false,
                            StatusCode = 400,
                            ErrorCode = "ERR_CONDITION_STILL_ACTIVE",
                            ErrorMessage = "Cannot resolve incident while physical condition is still ACTIVE."
                        };
                    }

                    // Guard: Resolution note is required
                    if (string.IsNullOrWhiteSpace(resolutionNote))
                    {
                        return new CommandResultPayload
                        {
                            CommandId = command.CommandId,
                            Success = false,
                            StatusCode = 400,
                            ErrorCode = "ERR_MISSING_RESOLUTION_NOTE",
                            ErrorMessage = "Resolution note is required to resolve incident."
                        };
                    }

                    newWorkflow = "RESOLVED";
                    historyNote = resolutionNote.Trim();
                    break;

                default:
                    return new CommandResultPayload
                    {
                        CommandId = command.CommandId,
                        Success = false,
                        StatusCode = 400,
                        ErrorCode = "ERR_UNKNOWN_COMMAND",
                        ErrorMessage = $"Unknown command '{cmdType}'."
                    };
            }

            // 5. Update dbo.Incidents inside transaction
            const string updateSql = @"
UPDATE dbo.Incidents
SET WorkflowState = @NewWorkflowState,
    ResolvedAtUtc = CASE WHEN @NewWorkflowState = 'RESOLVED' THEN SYSUTCDATETIME() ELSE ResolvedAtUtc END
OUTPUT INSERTED.RowVersion
WHERE IncidentId = @IncidentId;";

            byte[] newRowVersionBytes;
            await using (var updateCmd = new SqlCommand(updateSql, conn, tx))
            {
                updateCmd.Parameters.AddWithValue("@NewWorkflowState", newWorkflow);
                updateCmd.Parameters.AddWithValue("@IncidentId", command.IncidentId);

                var newRvObj = await updateCmd.ExecuteScalarAsync().ConfigureAwait(false);
                newRowVersionBytes = newRvObj is byte[] b ? b : currentRowVersionBytes;
            }

            string newRvHex = "0x" + Convert.ToHexString(newRowVersionBytes).ToLowerInvariant();

            // 6. Insert history record into dbo.IncidentHistory
            const string historySql = @"
INSERT INTO dbo.IncidentHistory (IncidentId, ChangedByUserId, OldConditionState, NewConditionState, OldWorkflowState, NewWorkflowState, Note, ChangedAtUtc)
VALUES (@IncidentId, @UserId, @ConditionState, @ConditionState, @OldWorkflowState, @NewWorkflowState, @Note, SYSUTCDATETIME());";

            await using (var histCmd = new SqlCommand(historySql, conn, tx))
            {
                histCmd.Parameters.AddWithValue("@IncidentId", command.IncidentId);
                histCmd.Parameters.AddWithValue("@UserId", userId > 0 ? (object)userId : DBNull.Value);
                histCmd.Parameters.AddWithValue("@ConditionState", currentConditionState);
                histCmd.Parameters.AddWithValue("@OldWorkflowState", oldWorkflow);
                histCmd.Parameters.AddWithValue("@NewWorkflowState", newWorkflow);
                histCmd.Parameters.AddWithValue("@Note", historyNote);
                await histCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            // 7. Prepare success result
            var successResult = new CommandResultPayload
            {
                CommandId = command.CommandId,
                Success = true,
                StatusCode = 200,
                UpdatedRowVersionHex = newRvHex
            };

            // 8. Insert into dbo.ProcessedCommands
            const string procCmdSql = @"
INSERT INTO dbo.ProcessedCommands (UserId, CommandId, RequestDigest, ResponsePayloadJson, ProcessedAtUtc)
VALUES (@UserId, @CommandId, @RequestDigest, @ResponsePayloadJson, SYSUTCDATETIME());";

            string responseJson = ProtocolSerializer.SerializeToString(successResult);
            await using (var procCmd = new SqlCommand(procCmdSql, conn, tx))
            {
                procCmd.Parameters.AddWithValue("@UserId", userId);
                procCmd.Parameters.AddWithValue("@CommandId", Guid.Parse(command.CommandId));
                procCmd.Parameters.AddWithValue("@RequestDigest", computedDigest);
                procCmd.Parameters.AddWithValue("@ResponsePayloadJson", responseJson);
                await procCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            // Commit transaction
            await tx.CommitAsync().ConfigureAwait(false);
            Log($"Successfully executed {cmdType} on incident #{command.IncidentId} (New WorkflowState: {newWorkflow}, New RowVersion: {newRvHex})");

            return successResult;
        }
        catch (Exception ex)
        {
            if (tx != null)
            {
                try { await tx.RollbackAsync().ConfigureAwait(false); } catch { }
            }
            Log($"Error executing incident command '{command.CommandType}': {ex.Message}");
            return new CommandResultPayload
            {
                CommandId = command.CommandId,
                Success = false,
                StatusCode = 500,
                ErrorCode = "ERR_INTERNAL_SERVER_ERROR",
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            if (conn != null)
            {
                await conn.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Upserts service status records in dbo.Services.
    /// </summary>
    public async Task UpsertServiceStatusAsync(string deviceId, List<ServiceStatusItemDto> services)
    {
        if (services == null || services.Count == 0) return;

        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);

            await EnsureDeviceRegisteredAsync(deviceId).ConfigureAwait(false);

            const string sql = @"
MERGE dbo.Services AS target
USING (SELECT @DeviceId AS DeviceId, @ServiceName AS ServiceName) AS source
ON (target.DeviceId = source.DeviceId AND target.ServiceName = source.ServiceName)
WHEN MATCHED THEN
    UPDATE SET 
        ExpectedPort = @ExpectedPort,
        Status = @Status,
        ConsecutiveFailures = CASE WHEN @IsListening = 1 THEN 0 ELSE target.ConsecutiveFailures + 1 END,
        LastCheckedUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (DeviceId, ServiceName, ExpectedPort, Status, ConsecutiveFailures, LastCheckedUtc)
    VALUES (@DeviceId, @ServiceName, @ExpectedPort, @Status, CASE WHEN @IsListening = 1 THEN 0 ELSE 1 END, SYSUTCDATETIME());";

            foreach (var svc in services)
            {
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                cmd.Parameters.AddWithValue("@ServiceName", svc.ServiceName);
                cmd.Parameters.AddWithValue("@ExpectedPort", svc.Port);
                cmd.Parameters.AddWithValue("@Status", svc.IsListening ? "HEALTHY" : "DOWN");
                cmd.Parameters.AddWithValue("@IsListening", svc.IsListening ? 1 : 0);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log($"UpsertServiceStatus DB error: {ex.Message}");
        }
    }
}
