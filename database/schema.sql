-- LabGuard SQL Server 2022 Express - canonical B-005 schema baseline.
-- Contract sources:
--   docs/spec/CONTRACT_MATRIX.md (ProcessedBatches)
--   docs/spec/ARCHITECTURE_DECISIONS.md (CP2-009, CP2-013, CP2-017)
--   src/LabGuard.Protocol/Dtos/TelemetryDtos.cs and AdminDtos.cs
--
-- NEW DATABASES ONLY. This is CREATE TABLE DDL, NOT an in-place migration.
-- If the previous schema was executed, preserve its data and prepare a
-- reviewed migration before changing existing column types/primary keys.

CREATE TABLE dbo.ProcessedBatches (
    DeviceId NVARCHAR(64) NOT NULL,
    BootId UNIQUEIDENTIFIER NOT NULL,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    ReceivedUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_ProcessedBatches_ReceivedUtc DEFAULT SYSUTCDATETIME(),
    Status TINYINT NOT NULL,
    CONSTRAINT PK_ProcessedBatches PRIMARY KEY (DeviceId, BootId, BatchId)
);

-- Supports TTL 24-hour cleanup (the cleanup job itself is future B-005 work).
CREATE NONCLUSTERED INDEX IX_ProcessedBatches_ReceivedUtc
    ON dbo.ProcessedBatches (ReceivedUtc);

CREATE TABLE dbo.MetricSamples (
    SampleId BIGINT IDENTITY(1,1) NOT NULL,
    DeviceId NVARCHAR(64) NOT NULL,
    BootId UNIQUEIDENTIFIER NOT NULL,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    CpuPercent FLOAT NOT NULL,
    MemoryPercent FLOAT NOT NULL,
    UptimeSeconds BIGINT NOT NULL,
    RecordedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_MetricSamples_RecordedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_MetricSamples PRIMARY KEY (SampleId)
);

CREATE NONCLUSTERED INDEX IX_MetricSamples_Device_Time
    ON dbo.MetricSamples (DeviceId, RecordedAtUtc DESC);

-- IncidentId is bigint in the frozen Admin/Event DTOs and command digest,
-- not NVARCHAR. SQL Server rowversion is an opaque 8-byte OCC token.
CREATE TABLE dbo.Incidents (
    IncidentId BIGINT IDENTITY(1,1) NOT NULL,
    DeviceId NVARCHAR(64) NOT NULL,
    RuleName NVARCHAR(50) NOT NULL,
    ConditionState NVARCHAR(20) NOT NULL,
    WorkflowState NVARCHAR(20) NOT NULL,
    Severity NVARCHAR(20) NOT NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_Incidents_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2(7) NOT NULL
        CONSTRAINT DF_Incidents_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    AssigneeId NVARCHAR(64) NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_Incidents PRIMARY KEY (IncidentId)
);

-- Intentionally NOT implemented in this focused contract correction:
--   data migrations, FK/reference policy, ingestion transactions,
--   duplicate BATCH_ACK logic, TTL cleanup job, IncidentHistory, seed rows.
-- Those require B-005/B-007/B-009 implementation and SQL Server tests.
