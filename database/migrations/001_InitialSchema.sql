-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  Integration Platform — SQL Server Schema                        ║
-- ║  Version: 1.0.0                                                  ║
-- ╚══════════════════════════════════════════════════════════════════╝

-- ═══ Tenants (Multi-tenant support) ═══
CREATE TABLE Tenants (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Code            NVARCHAR(50) NOT NULL UNIQUE,
    Name            NVARCHAR(255) NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ═══ Users ═══
CREATE TABLE Users (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES Tenants(Id),
    Username        NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(255) NOT NULL,
    PasswordHash    NVARCHAR(500) NOT NULL,
    Role            NVARCHAR(50) NOT NULL DEFAULT 'viewer', -- admin, editor, viewer
    IsActive        BIT NOT NULL DEFAULT 1,
    LastLoginAt     DATETIME2,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UNIQUE(TenantId, Username),
    UNIQUE(TenantId, Email)
);

-- ═══ Integration Configs (Main table) ═══
CREATE TABLE IntegrationConfigs (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES Tenants(Id),
    ConfigKey       NVARCHAR(100) NOT NULL,     -- "bank-b-transfer"
    Name            NVARCHAR(255) NOT NULL,
    Description     NVARCHAR(MAX),
    ConnectorType   NVARCHAR(50) NOT NULL DEFAULT 'generic-http',
    Status          NVARCHAR(20) NOT NULL DEFAULT 'draft', -- draft, active, inactive, archived
    Version         INT NOT NULL DEFAULT 1,
    Tags            NVARCHAR(500),               -- comma-separated
    
    -- Endpoint
    BaseUrl         NVARCHAR(500) NOT NULL,
    DefaultHeaders  NVARCHAR(MAX),               -- JSON
    
    -- Auth
    AuthType        NVARCHAR(50) NOT NULL DEFAULT 'none',
    AuthParams      NVARCHAR(MAX),               -- JSON (encrypted in production)
    
    -- Resilience
    TimeoutMs       INT NOT NULL DEFAULT 30000,
    RetryConfig     NVARCHAR(MAX),               -- JSON
    CircuitBreaker  NVARCHAR(MAX),               -- JSON
    
    -- Metadata
    Metadata        NVARCHAR(MAX),               -- JSON
    
    -- Audit
    CreatedBy       NVARCHAR(100) NOT NULL,
    UpdatedBy       NVARCHAR(100),
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2,
    DeletedBy       NVARCHAR(100),
    
    UNIQUE(TenantId, ConfigKey)
);

-- ═══ Operations ═══
CREATE TABLE IntegrationOperations (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ConfigId        UNIQUEIDENTIFIER NOT NULL REFERENCES IntegrationConfigs(Id) ON DELETE CASCADE,
    Name            NVARCHAR(100) NOT NULL,
    HttpMethod      NVARCHAR(10) NOT NULL DEFAULT 'POST',
    Path            NVARCHAR(500) NOT NULL,
    ContentType     NVARCHAR(100) DEFAULT 'application/json',
    Description     NVARCHAR(500),
    IsEnabled       BIT NOT NULL DEFAULT 1,
    SortOrder       INT DEFAULT 0,
    
    UNIQUE(ConfigId, Name)
);

-- ═══ Request Mappings ═══
CREATE TABLE RequestMappings (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OperationId     UNIQUEIDENTIFIER NOT NULL REFERENCES IntegrationOperations(Id) ON DELETE CASCADE,
    SourceField     NVARCHAR(200) NOT NULL,
    TargetField     NVARCHAR(200) NOT NULL,
    DefaultValue    NVARCHAR(500),
    Transform       NVARCHAR(100),
    IsRequired      BIT NOT NULL DEFAULT 0,
    SortOrder       INT DEFAULT 0
);

-- ═══ Response Mappings ═══
CREATE TABLE ResponseMappings (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OperationId     UNIQUEIDENTIFIER NOT NULL REFERENCES IntegrationOperations(Id) ON DELETE CASCADE,
    SourceField     NVARCHAR(200) NOT NULL,
    TargetField     NVARCHAR(200) NOT NULL,
    DefaultValue    NVARCHAR(500),
    Transform       NVARCHAR(100),
    SortOrder       INT DEFAULT 0
);

-- ═══ Validation Rules ═══
CREATE TABLE ValidationRules (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OperationId     UNIQUEIDENTIFIER NOT NULL REFERENCES IntegrationOperations(Id) ON DELETE CASCADE,
    Field           NVARCHAR(200) NOT NULL,
    Rule            NVARCHAR(200) NOT NULL,
    ErrorMessage    NVARCHAR(500),
    SortOrder       INT DEFAULT 0
);

-- ═══ Config History (Versioning) ═══
CREATE TABLE ConfigHistory (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ConfigId        UNIQUEIDENTIFIER NOT NULL REFERENCES IntegrationConfigs(Id),
    Version         INT NOT NULL,
    Snapshot        NVARCHAR(MAX) NOT NULL,      -- Full JSON snapshot
    ChangeType      NVARCHAR(20) NOT NULL,        -- created, updated, activated, deactivated, deleted
    ChangedBy       NVARCHAR(100) NOT NULL,
    ChangedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ChangeNote      NVARCHAR(500)
);

-- ═══ API Execution History (Audit) ═══
CREATE TABLE ApiExecutionHistory (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES Tenants(Id),
    CorrelationId   NVARCHAR(100) NOT NULL,
    TraceId         NVARCHAR(100),
    ConfigKey       NVARCHAR(100) NOT NULL,
    Operation       NVARCHAR(100) NOT NULL,
    
    -- Request
    RequestBody     NVARCHAR(MAX),               -- masked sensitive fields
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
    CallerService   NVARCHAR(100)
);

-- ═══ Retry History ═══
CREATE TABLE RetryHistory (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExecutionId     UNIQUEIDENTIFIER NOT NULL REFERENCES ApiExecutionHistory(Id),
    AttemptNumber   INT NOT NULL,
    AttemptedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    StatusCode      INT,
    ErrorMessage    NVARCHAR(500),
    DelayMs         INT
);

-- ═══ Audit Logs ═══
CREATE TABLE AuditLogs (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER NOT NULL REFERENCES Tenants(Id),
    EntityType      NVARCHAR(50) NOT NULL,
    EntityId        UNIQUEIDENTIFIER NOT NULL,
    Action          NVARCHAR(50) NOT NULL,
    PerformedBy     NVARCHAR(100) NOT NULL,
    OldValues       NVARCHAR(MAX),               -- JSON
    NewValues       NVARCHAR(MAX),               -- JSON
    IpAddress       NVARCHAR(50),
    UserAgent       NVARCHAR(500),
    PerformedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ═══ Indexes ═══
CREATE INDEX IX_IntegrationConfigs_TenantId_ConfigKey ON IntegrationConfigs(TenantId, ConfigKey) WHERE IsDeleted = 0;
CREATE INDEX IX_IntegrationConfigs_Status ON IntegrationConfigs(Status) WHERE IsDeleted = 0;
CREATE INDEX IX_IntegrationOperations_ConfigId ON IntegrationOperations(ConfigId);
CREATE INDEX IX_ApiExecutionHistory_CorrelationId ON ApiExecutionHistory(CorrelationId);
CREATE INDEX IX_ApiExecutionHistory_ConfigKey ON ApiExecutionHistory(ConfigKey);
CREATE INDEX IX_ApiExecutionHistory_StartedAt ON ApiExecutionHistory(StartedAt DESC);
CREATE INDEX IX_ConfigHistory_ConfigId ON ConfigHistory(ConfigId, Version DESC);
CREATE INDEX IX_AuditLogs_EntityId ON AuditLogs(EntityType, EntityId);
CREATE INDEX IX_AuditLogs_PerformedAt ON AuditLogs(PerformedAt DESC);

-- ═══ Seed default tenant ═══
INSERT INTO Tenants (Id, Code, Name) VALUES (NEWID(), 'default', 'Default Tenant');
