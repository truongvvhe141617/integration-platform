# Integration Platform — Refactor Plan: JSON → DB + Admin UI

## 1. Architecture Design

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          ADMIN UI (ReactJS + Antd)                       │
│  - CRUD Integration Config                                               │
│  - Test API trực tiếp                                                    │
│  - Enable/Disable                                                        │
│  - Audit log viewer                                                      │
│  - Role-based access                                                     │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │ REST API
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    CONFIG SERVICE (CRUD API)                              │
│  GET/POST/PUT/DELETE /api/v1/integrations                                │
│  POST /api/v1/integrations/:id/test                                      │
│  GET /api/v1/integrations/:id/history                                    │
│                               │                                          │
│                               ▼                                          │
│                    PostgreSQL (integration_configs)                       │
│                    + Redis Cache                                          │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │ HTTP (load config)
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    INTEGRATION SERVICE (Runtime Engine)                   │
│  POST /api/v1/integration/execute                                        │
│  POST /api/v1/integration/execute-async                                  │
│                                                                          │
│  Flow: Load config from DB (cached) → Validate → Map → Execute → Map    │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │ HTTP/Custom
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    EXTERNAL SYSTEMS (Bank, Wallet, SMS...)                │
└─────────────────────────────────────────────────────────────────────────┘
```

## 2. Database Schema (PostgreSQL)

```sql
-- ═══ Main config table ═══
CREATE TABLE integration_configs (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    config_id       VARCHAR(100) NOT NULL UNIQUE,  -- "bank-b-transfer"
    name            VARCHAR(255) NOT NULL,
    description     TEXT,
    connector_type  VARCHAR(50) NOT NULL DEFAULT 'generic-http',
    status          VARCHAR(20) NOT NULL DEFAULT 'draft',  -- draft, active, inactive, archived
    version         INT NOT NULL DEFAULT 1,
    
    -- Endpoint
    base_url        VARCHAR(500) NOT NULL,
    default_headers JSONB DEFAULT '{}',
    
    -- Auth
    auth_type       VARCHAR(50) NOT NULL DEFAULT 'none',  -- none, apikey, basic, bearer, oauth2
    auth_params     JSONB DEFAULT '{}',  -- encrypted in production
    
    -- Resilience
    timeout_ms      INT NOT NULL DEFAULT 30000,
    retry_config    JSONB DEFAULT '{"maxRetries":3,"initialDelayMs":1000,"backoffStrategy":"exponential"}',
    circuit_breaker JSONB DEFAULT '{"failureThreshold":5,"durationOfBreakSeconds":30}',
    
    -- Metadata
    tags            VARCHAR(255)[],
    metadata        JSONB DEFAULT '{}',
    
    -- Audit
    created_by      VARCHAR(100) NOT NULL,
    updated_by      VARCHAR(100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Soft delete
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE
);

-- ═══ Operations (1 config → N operations) ═══
CREATE TABLE integration_operations (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    config_id       UUID NOT NULL REFERENCES integration_configs(id) ON DELETE CASCADE,
    name            VARCHAR(100) NOT NULL,  -- "transfer", "balance-inquiry"
    http_method     VARCHAR(10) NOT NULL DEFAULT 'POST',
    path            VARCHAR(500) NOT NULL,
    content_type    VARCHAR(100) DEFAULT 'application/json',
    description     TEXT,
    is_enabled      BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order      INT DEFAULT 0,
    
    UNIQUE(config_id, name)
);

-- ═══ Request Mappings ═══
CREATE TABLE request_mappings (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    operation_id    UUID NOT NULL REFERENCES integration_operations(id) ON DELETE CASCADE,
    source_field    VARCHAR(200) NOT NULL,  -- "fromAccount"
    target_field    VARCHAR(200) NOT NULL,  -- "src_acct_no"
    default_value   VARCHAR(500),
    transform       VARCHAR(100),  -- "ToUpper", "Trim", "Format:yyyy-MM-dd"
    is_required     BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order      INT DEFAULT 0
);

-- ═══ Response Mappings ═══
CREATE TABLE response_mappings (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    operation_id    UUID NOT NULL REFERENCES integration_operations(id) ON DELETE CASCADE,
    source_field    VARCHAR(200) NOT NULL,  -- "result_code"
    target_field    VARCHAR(200) NOT NULL,  -- "statusCode"
    default_value   VARCHAR(500),
    transform       VARCHAR(100),
    sort_order      INT DEFAULT 0
);

-- ═══ Validations ═══
CREATE TABLE validation_rules (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    operation_id    UUID NOT NULL REFERENCES integration_operations(id) ON DELETE CASCADE,
    field           VARCHAR(200) NOT NULL,
    rule            VARCHAR(200) NOT NULL,  -- "Required", "MaxLength:50", "Regex:^[0-9]+$"
    error_message   VARCHAR(500),
    sort_order      INT DEFAULT 0
);

-- ═══ Config History (versioning) ═══
CREATE TABLE config_history (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    config_id       UUID NOT NULL REFERENCES integration_configs(id),
    version         INT NOT NULL,
    snapshot        JSONB NOT NULL,  -- Full config snapshot at this version
    change_type     VARCHAR(20) NOT NULL,  -- created, updated, activated, deactivated
    changed_by      VARCHAR(100) NOT NULL,
    changed_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    change_note     TEXT
);

-- ═══ Audit Log ═══
CREATE TABLE audit_logs (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_type     VARCHAR(50) NOT NULL,  -- "integration_config", "operation"
    entity_id       UUID NOT NULL,
    action          VARCHAR(50) NOT NULL,  -- "created", "updated", "deleted", "tested", "activated"
    performed_by    VARCHAR(100) NOT NULL,
    details         JSONB,
    ip_address      VARCHAR(50),
    performed_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ═══ Indexes ═══
CREATE INDEX idx_configs_config_id ON integration_configs(config_id) WHERE NOT is_deleted;
CREATE INDEX idx_configs_status ON integration_configs(status) WHERE NOT is_deleted;
CREATE INDEX idx_configs_tags ON integration_configs USING GIN(tags);
CREATE INDEX idx_operations_config ON integration_operations(config_id);
CREATE INDEX idx_history_config ON config_history(config_id, version DESC);
CREATE INDEX idx_audit_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX idx_audit_time ON audit_logs(performed_at DESC);
```

## 3. API Design

### Config CRUD

```
GET    /api/v1/integrations                    → List all (filter, search, pagination)
GET    /api/v1/integrations/:id                → Get by ID (full detail + operations)
POST   /api/v1/integrations                    → Create new config
PUT    /api/v1/integrations/:id                → Update config
DELETE /api/v1/integrations/:id                → Soft delete
PATCH  /api/v1/integrations/:id/status         → Change status (activate/deactivate)
POST   /api/v1/integrations/:id/test           → Test integration (send real request)
POST   /api/v1/integrations/:id/duplicate      → Clone config
GET    /api/v1/integrations/:id/history        → Version history
POST   /api/v1/integrations/:id/rollback/:ver  → Rollback to version
GET    /api/v1/integrations/:id/audit          → Audit log
```

### Query params for GET /integrations

```
?status=active
?search=bank
?tags=banking,payment
?page=1&pageSize=20
?sortBy=updatedAt&sortDir=desc
```

### Request body for POST /integrations

```json
{
  "configId": "bank-d-transfer",
  "name": "Bank D - Fund Transfer",
  "description": "Chuyển tiền qua Bank D API v2",
  "connectorType": "generic-http",
  "baseUrl": "https://api.bankd.vn",
  "defaultHeaders": { "X-Channel": "PARTNER" },
  "authType": "apikey",
  "authParams": { "headerName": "X-Api-Key", "apiKey": "xxx" },
  "timeoutMs": 30000,
  "retryConfig": { "maxRetries": 3, "initialDelayMs": 1000, "backoffStrategy": "exponential" },
  "tags": ["banking", "transfer"],
  "operations": [
    {
      "name": "transfer",
      "httpMethod": "POST",
      "path": "/v2/fund-transfer",
      "requestMappings": [
        { "sourceField": "fromAccount", "targetField": "src_acct", "isRequired": true },
        { "sourceField": "amount", "targetField": "txn_amount", "isRequired": true }
      ],
      "responseMappings": [
        { "sourceField": "result_code", "targetField": "statusCode" },
        { "sourceField": "data.ref_no", "targetField": "bankTransactionId" }
      ],
      "validations": [
        { "field": "fromAccount", "rule": "Required", "errorMessage": "Source account required" }
      ]
    }
  ]
}
```

### Response for POST /integrations/:id/test

```json
{
  "success": true,
  "httpStatus": 200,
  "requestSent": {
    "url": "https://api.bankd.vn/v2/fund-transfer",
    "method": "POST",
    "headers": { "X-Api-Key": "***", "X-Channel": "PARTNER" },
    "body": { "src_acct": "012...", "txn_amount": 5000000 }
  },
  "responseReceived": {
    "statusCode": 200,
    "headers": { "Content-Type": "application/json" },
    "body": { "result_code": "00", "data": { "ref_no": "BANKD-123" } }
  },
  "mappedResponse": {
    "statusCode": "00",
    "bankTransactionId": "BANKD-123"
  },
  "executionTimeMs": 245
}
```

## 4. Runtime Execution Engine

```
Integration Service nhận request
    │
    ▼
ConfigProvider.GetConfigAsync(configId)
    │
    ├── Check Redis cache (TTL 5 min)
    │   └── Hit → return cached
    │
    ├── Miss → Call Config Service API
    │   └── GET /api/v1/integrations/{configId}
    │       └── Config Service query PostgreSQL
    │           └── Return full config (operations, mappings, validations)
    │
    └── Cache result in Redis
    │
    ▼
IntegrationExecutor (unchanged logic)
    → Validate → Map request → Execute connector → Map response
```

**Caching strategy:**
- Redis cache per config_id, TTL 5 minutes
- Config Service publish event khi config thay đổi → Integration Service invalidate cache
- Fallback: InMemory cache nếu Redis down

## 5. Frontend Architecture (ReactJS + Antd)

### Folder Structure

```
admin-ui/
├── public/
├── src/
│   ├── api/                    # API client (Axios)
│   │   ├── client.ts           # Axios instance + interceptors
│   │   ├── integrations.ts     # CRUD API calls
│   │   └── auth.ts             # Login/token
│   │
│   ├── components/             # Reusable components
│   │   ├── Layout/             # App layout (sidebar, header)
│   │   ├── JsonEditor/         # Monaco/CodeMirror JSON editor
│   │   ├── MappingTable/       # Dynamic mapping table (add/remove rows)
│   │   ├── HeadersForm/        # Dynamic key-value form
│   │   └── StatusBadge/        # Active/Inactive badge
│   │
│   ├── pages/                  # Page components
│   │   ├── IntegrationList/    # Table + search + filter
│   │   ├── IntegrationDetail/  # View detail + operations
│   │   ├── IntegrationForm/    # Create/Edit form (multi-step)
│   │   ├── IntegrationTest/    # Test API panel
│   │   ├── AuditLog/           # History viewer
│   │   └── Dashboard/          # Overview stats
│   │
│   ├── hooks/                  # Custom hooks
│   │   ├── useIntegrations.ts  # React Query hooks
│   │   └── useAuth.ts
│   │
│   ├── store/                  # State management (optional)
│   ├── types/                  # TypeScript interfaces
│   ├── utils/                  # Helpers
│   ├── App.tsx
│   └── main.tsx
│
├── package.json
├── vite.config.ts
└── tsconfig.json
```

### UI Flow: Create Integration

```
Step 1: Basic Info
┌─────────────────────────────────────────┐
│ Config ID:  [bank-d-transfer          ] │
│ Name:       [Bank D - Fund Transfer   ] │
│ Description:[Chuyển tiền qua Bank D   ] │
│ Tags:       [banking] [transfer] [+]    │
│ Connector:  [Generic HTTP ▼]            │
└─────────────────────────────────────────┘

Step 2: Endpoint & Auth
┌─────────────────────────────────────────┐
│ Base URL:   [https://api.bankd.vn     ] │
│ Timeout:    [30000] ms                  │
│                                          │
│ Auth Type:  [API Key ▼]                 │
│ Header:     [X-Api-Key               ]  │
│ Value:      [••••••••••••            ]  │
│                                          │
│ Default Headers:                         │
│ ┌──────────────┬──────────────┬───┐     │
│ │ Key          │ Value        │ ✕ │     │
│ ├──────────────┼──────────────┼───┤     │
│ │ X-Channel    │ PARTNER      │ ✕ │     │
│ │ [+ Add Header]              │   │     │
│ └──────────────┴──────────────┴───┘     │
└─────────────────────────────────────────┘

Step 3: Operations
┌─────────────────────────────────────────┐
│ Operations:                              │
│ ┌─────────────────────────────────────┐ │
│ │ ▼ transfer (POST /v2/fund-transfer) │ │
│ │                                      │ │
│ │ Request Mapping:                     │ │
│ │ ┌────────────┬────────────┬────┬──┐ │ │
│ │ │ Source     │ Target     │Req │✕ │ │ │
│ │ ├────────────┼────────────┼────┼──┤ │ │
│ │ │ fromAccount│ src_acct   │ ✓  │✕ │ │ │
│ │ │ amount     │ txn_amount │ ✓  │✕ │ │ │
│ │ │ [+ Add Mapping]         │    │  │ │ │
│ │ └────────────┴────────────┴────┴──┘ │ │
│ │                                      │ │
│ │ Response Mapping:                    │ │
│ │ ┌────────────┬────────────┬──┐      │ │
│ │ │ Source     │ Target     │✕ │      │ │
│ │ ├────────────┼────────────┼──┤      │ │
│ │ │ result_code│ statusCode │✕ │      │ │
│ │ └────────────┴────────────┴──┘      │ │
│ └─────────────────────────────────────┘ │
│ [+ Add Operation]                        │
└─────────────────────────────────────────┘

Step 4: Test & Save
┌─────────────────────────────────────────┐
│ [Test Connection]  [Save as Draft]       │
│                    [Save & Activate]     │
└─────────────────────────────────────────┘
```

### Key Components

```tsx
// MappingTable — dynamic add/remove rows
<MappingTable
  columns={['sourceField', 'targetField', 'defaultValue', 'transform', 'isRequired']}
  data={requestMappings}
  onChange={setRequestMappings}
/>

// HeadersForm — dynamic key-value
<HeadersForm
  value={defaultHeaders}
  onChange={setDefaultHeaders}
/>

// JsonEditor — for advanced users
<JsonEditor
  value={rawConfig}
  onChange={setRawConfig}
  schema={configJsonSchema}
/>
```

## 6. Migration Strategy (JSON → DB)

```
Phase 1: Parallel mode (1-2 weeks)
─────────────────────────────────
- Deploy DB schema
- Build migration script: read JSON files → insert into DB
- Config Service supports both: DB primary, JSON fallback
- Integration Service unchanged (still calls Config Service API)

Phase 2: Admin UI (2-3 weeks)
─────────────────────────────
- Deploy Admin UI
- Team starts managing configs via UI
- JSON files become read-only backup

Phase 3: Remove JSON (1 week)
─────────────────────────────
- Remove FileConfigProvider
- Remove JSON config files
- Config Service = DB only
- Full audit trail active
```

### Migration Script (pseudo)

```csharp
// Read all JSON files
foreach (var file in Directory.GetFiles("configs/", "*.json"))
{
    var config = JsonSerializer.Deserialize<ConnectorConfig>(File.ReadAllText(file));
    
    // Insert into integration_configs
    await db.ExecuteAsync(@"
        INSERT INTO integration_configs (config_id, name, connector_type, status, base_url, ...)
        VALUES (@Id, @Name, @ConnectorType, 'active', @BaseUrl, ...)", config);
    
    // Insert operations + mappings
    foreach (var op in config.Operations)
    {
        var opId = await db.InsertOperation(configId, op);
        await db.InsertRequestMappings(opId, op.RequestMappings);
        await db.InsertResponseMappings(opId, op.ResponseMappings);
        await db.InsertValidations(opId, op.Validations);
    }
}
```

## 7. Bonus Features

### Event-driven (RabbitMQ) — ĐÃ CÓ
- Async integration via message queue
- WebSocket real-time push results

### Multi-tenant
- Add `tenant_id` column to all tables
- Filter by tenant in all queries
- Separate Redis cache namespace per tenant

### Retry/Failure handling — ĐÃ CÓ
- Polly retry + circuit breaker per connector
- Bulkhead isolation
- Dead letter queue for failed messages

### Audit logging — ĐÃ CÓ
- `audit_logs` table tracks all changes
- Who, when, what changed
- IP address tracking
