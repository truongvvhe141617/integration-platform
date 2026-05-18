-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  Database: IntegrationService                                     ║
-- ║  Chạy: USE IntegrationService; GO; rồi execute file này          ║
-- ╚══════════════════════════════════════════════════════════════════╝

USE IntegrationService;
GO

-- ═══ Execution Log (mỗi row = 1 lần gọi third-party) ═══
CREATE TABLE int_execution_log (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TenantId        NVARCHAR(100) NOT NULL DEFAULT 'default',
    CorrelationId   NVARCHAR(100) NOT NULL,
    TraceId         NVARCHAR(100),
    ConfigKey       NVARCHAR(100) NOT NULL,
    Operation       NVARCHAR(100) NOT NULL,

    -- Request
    RequestBody     NVARCHAR(MAX),
    RequestHeaders  NVARCHAR(MAX),

    -- Response
    ResponseBody    NVARCHAR(MAX),
    ResponseStatus  INT,

    -- Timing
    StartedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt     DATETIME2,
    DurationMs      INT,

    -- Result
    IsSuccess       BIT NOT NULL DEFAULT 0,
    ErrorCode       NVARCHAR(50),
    ErrorMessage    NVARCHAR(500),

    -- Retry
    RetryCount      INT NOT NULL DEFAULT 0,

    -- Source
    CallerIp        NVARCHAR(50),
    CallerService   NVARCHAR(100),

    -- Audit
    CreatedBy       NVARCHAR(100),
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy       NVARCHAR(100),
    UpdatedDate     DATETIME2
);

CREATE INDEX IX_int_exec_correlation ON int_execution_log(CorrelationId);
CREATE INDEX IX_int_exec_configkey ON int_execution_log(ConfigKey);
CREATE INDEX IX_int_exec_started ON int_execution_log(StartedAt DESC);
CREATE INDEX IX_int_exec_success ON int_execution_log(IsSuccess, StartedAt DESC);

-- ═══ Retry Log (chi tiết từng lần retry) ═══
CREATE TABLE int_retry_log (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExecutionLogId  UNIQUEIDENTIFIER NOT NULL REFERENCES int_execution_log(Id) ON DELETE CASCADE,
    AttemptNumber   INT NOT NULL,
    AttemptedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    StatusCode      INT,
    ErrorMessage    NVARCHAR(500),
    DelayMs         INT NOT NULL DEFAULT 0,

    CreatedBy       NVARCHAR(100),
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy       NVARCHAR(100),
    UpdatedDate     DATETIME2
);

CREATE INDEX IX_int_retry_exec ON int_retry_log(ExecutionLogId);

PRINT 'IntegrationService tables created successfully.';
GO
