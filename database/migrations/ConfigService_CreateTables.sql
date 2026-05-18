-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  Database: ConfigService                                          ║
-- ║  Chạy: USE ConfigService; GO; rồi execute file này               ║
-- ╚══════════════════════════════════════════════════════════════════╝

USE ConfigService;
GO

-- ═══ Tenants ═══
CREATE TABLE cfg_tenant (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Code            NVARCHAR(50) NOT NULL UNIQUE,
    Name            NVARCHAR(255) NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ═══ Users ═══
CREATE TABLE cfg_user (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TenantId        NVARCHAR(100) NOT NULL DEFAULT 'default',
    Username        NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(255) NOT NULL,
    PasswordHash    NVARCHAR(500) NOT NULL,
    Role            NVARCHAR(50) NOT NULL DEFAULT 'viewer',
    IsActive        BIT NOT NULL DEFAULT 1,
    LastLoginAt     DATETIME2,
    CreatedBy       NVARCHAR(100),
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy       NVARCHAR(100),
    UpdatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ═══ Integration Config (Main) ═══
CREATE TABLE cfg_integration_config (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TenantId        NVARCHAR(100) NOT NULL DEFAULT 'default',
    ConfigKey       NVARCHAR(100) NOT NULL,
    Name            NVARCHAR(255) NOT NULL,
    Description     NVARCHAR(MAX),
    ConnectorType   NVARCHAR(50) NOT NULL DEFAULT 'generic-http',
    Status          NVARCHAR(20) NOT NULL DEFAULT 'draft',
    Version         INT NOT NULL DEFAULT 1,
    Tags            NVARCHAR(500),
    BaseUrl         NVARCHAR(500) NOT NULL,
    DefaultHeaders  NVARCHAR(MAX),
    AuthType        NVARCHAR(50) NOT NULL DEFAULT 'none',
    AuthParams      NVARCHAR(MAX),
    TimeoutMs       INT NOT NULL DEFAULT 30000,
    RetryConfig     NVARCHAR(MAX),
    CircuitBreaker  NVARCHAR(MAX),
    Metadata        NVARCHAR(MAX),
    IsDeleted       BIT NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2,
    DeletedBy       NVARCHAR(100),
    CreatedBy       NVARCHAR(100),
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy       NVARCHAR(100),
    UpdatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE UNIQUE INDEX IX_cfg_config_tenant_key ON cfg_integration_config(TenantId, ConfigKey) WHERE IsDeleted = 0;
CREATE INDEX IX_cfg_config_status ON cfg_integration_config(Status) WHERE IsDeleted = 0;

-- ═══ Operations ═══
CREATE TABLE cfg_integration_operation (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ConfigId        UNIQUEIDENTIFIER NOT NULL REFERENCES cfg_integration_config(Id) ON DELETE CASCADE,
    Name            NVARCHAR(100) NOT NULL,
    HttpMethod      NVARCHAR(10) NOT NULL DEFAULT 'POST',
    Path            NVARCHAR(500) NOT NULL,
    ContentType     NVARCHAR(100) DEFAULT 'application/json',
    Description     NVARCHAR(500),
    IsEnabled       BIT NOT NULL DEFAULT 1,
    SortOrder       INT DEFAULT 0,
    CreatedBy       NVARCHAR(100),
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy       NVARCHAR(100),
    UpdatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE UNIQUE INDEX IX_cfg_operation_config_name ON cfg_integration_operation(ConfigId, Name);

-- ═══ Request Mappings ═══
CREATE TABLE cfg_request_mapping (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OperationId     UNIQUEIDENTIFIER NOT NULL REFERENCES cfg_integration_operation(Id) ON DELETE CASCADE,
    SourceField     NVARCHAR(200) NOT NULL,
    TargetField     NVARCHAR(200) NOT NULL,
    DefaultValue    NVARCHAR(500),
    Transform       NVARCHAR(100),
    IsRequired      BIT NOT NULL DEFAULT 0,
    SortOrder       INT DEFAULT 0,
    CreatedBy       NVARCHAR(100),
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy       NVARCHAR(100),
    UpdatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ═══ Response Mappings ═══
CREATE TABLE cfg_response_mapping (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OperationId     UNIQUEIDENTIFIER NOT NULL REFERENCES cfg_integration_operation(Id) ON DELETE CASCADE,
    SourceField     NVARCHAR(200) NOT NULL,
    TargetField     NVARCHAR(200) NOT NULL,
    DefaultValue    NVARCHAR(500),
    Transform       NVARCHAR(100),
    SortOrder       INT DEFAULT 0,
    CreatedBy       NVARCHAR(100),
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy       NVARCHAR(100),
    UpdatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ═══ Validation Rules ═══
CREATE TABLE cfg_validation_rule (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OperationId     UNIQUEIDENTIFIER NOT NULL REFERENCES cfg_integration_operation(Id) ON DELETE CASCADE,
    Field           NVARCHAR(200) NOT NULL,
    Rule            NVARCHAR(200) NOT NULL,
    ErrorMessage    NVARCHAR(500),
    SortOrder       INT DEFAULT 0,
    CreatedBy       NVARCHAR(100),
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy       NVARCHAR(100),
    UpdatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ═══ Config History (Versioning) ═══
CREATE TABLE cfg_config_history (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ConfigId        UNIQUEIDENTIFIER NOT NULL REFERENCES cfg_integration_config(Id),
    Version         INT NOT NULL,
    Snapshot        NVARCHAR(MAX) NOT NULL,
    ChangeType      NVARCHAR(20) NOT NULL,
    ChangedBy       NVARCHAR(100) NOT NULL,
    ChangedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ChangeNote      NVARCHAR(500)
);

CREATE INDEX IX_cfg_history_config ON cfg_config_history(ConfigId, Version DESC);

-- ═══ Audit Log ═══
CREATE TABLE cfg_audit_log (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TenantId        NVARCHAR(100) NOT NULL DEFAULT 'default',
    EntityType      NVARCHAR(50) NOT NULL,
    EntityId        UNIQUEIDENTIFIER NOT NULL,
    Action          NVARCHAR(50) NOT NULL,
    PerformedBy     NVARCHAR(100) NOT NULL,
    OldValues       NVARCHAR(MAX),
    NewValues       NVARCHAR(MAX),
    IpAddress       NVARCHAR(50),
    UserAgent       NVARCHAR(500),
    PerformedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_cfg_audit_entity ON cfg_audit_log(EntityType, EntityId);
CREATE INDEX IX_cfg_audit_time ON cfg_audit_log(PerformedAt DESC);

-- ═══ Seed default tenant ═══
INSERT INTO cfg_tenant (Id, Code, Name) VALUES (NEWID(), 'default', 'Default Tenant');

PRINT 'ConfigService tables created successfully.';
GO
