-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  Migration Script: JSON Files → SQL Server                       ║
-- ║  Chạy script này để import configs từ JSON vào DB               ║
-- ╚══════════════════════════════════════════════════════════════════╝

-- Step 1: Ensure default tenant exists
DECLARE @TenantId UNIQUEIDENTIFIER;
SELECT @TenantId = Id FROM Tenants WHERE Code = 'default';
IF @TenantId IS NULL
BEGIN
    SET @TenantId = NEWID();
    INSERT INTO Tenants (Id, Code, Name) VALUES (@TenantId, 'default', 'Default Tenant');
END

-- Step 2: Import bank-b-transfer
DECLARE @ConfigId UNIQUEIDENTIFIER = NEWID();
INSERT INTO IntegrationConfigs (
    Id, TenantId, ConfigKey, Name, ConnectorType, Status, Version,
    BaseUrl, DefaultHeaders, AuthType, AuthParams, TimeoutMs,
    RetryConfig, CircuitBreaker, CreatedBy, CreatedAt, UpdatedAt
) VALUES (
    @ConfigId, @TenantId, 'bank-b-transfer', 'Bank B - Fund Transfer',
    'generic-http', 'active', 1,
    'http://localhost:5002/mock',
    '{"X-Channel":"PARTNER","X-Bank-Code":"BANKB"}',
    'apikey',
    '{"headerName":"X-Api-Key","apiKey":"bank-b-secret-key-demo"}',
    30000,
    '{"maxRetries":2,"initialDelayMs":1000,"backoffStrategy":"Exponential"}',
    '{"failureThreshold":3,"durationOfBreakSeconds":60}',
    'migration-script', GETUTCDATE(), GETUTCDATE()
);

-- Operation: transfer
DECLARE @OpId UNIQUEIDENTIFIER = NEWID();
INSERT INTO IntegrationOperations (Id, ConfigId, Name, HttpMethod, Path, ContentType, IsEnabled)
VALUES (@OpId, @ConfigId, 'transfer', 'POST', '/bank-b/v1/fund-transfer', 'application/json', 1);

-- Request Mappings
INSERT INTO RequestMappings (Id, OperationId, SourceField, TargetField, IsRequired, SortOrder)
VALUES
    (NEWID(), @OpId, 'fromAccount', 'src_acct_no', 1, 1),
    (NEWID(), @OpId, 'toAccount', 'dest_acct_no', 1, 2),
    (NEWID(), @OpId, 'amount', 'txn_amount', 1, 3),
    (NEWID(), @OpId, 'currency', 'txn_ccy', 0, 4),
    (NEWID(), @OpId, 'description', 'txn_desc', 0, 5),
    (NEWID(), @OpId, 'referenceId', 'partner_ref_no', 1, 6);

-- Response Mappings
INSERT INTO ResponseMappings (Id, OperationId, SourceField, TargetField, SortOrder)
VALUES
    (NEWID(), @OpId, 'result_code', 'statusCode', 1),
    (NEWID(), @OpId, 'result_msg', 'statusMessage', 2),
    (NEWID(), @OpId, 'data.bank_ref_no', 'bankTransactionId', 3),
    (NEWID(), @OpId, 'data.txn_date', 'transactionDate', 4),
    (NEWID(), @OpId, 'data.fee', 'transactionFee', 5);

-- Validation Rules
INSERT INTO ValidationRules (Id, OperationId, Field, Rule, ErrorMessage, SortOrder)
VALUES
    (NEWID(), @OpId, 'fromAccount', 'Required', 'Source account is required', 1),
    (NEWID(), @OpId, 'toAccount', 'Required', 'Destination account is required', 2),
    (NEWID(), @OpId, 'amount', 'Required', 'Amount is required', 3),
    (NEWID(), @OpId, 'referenceId', 'Required', 'Reference ID is required', 4);

-- Operation: balance-inquiry
DECLARE @OpId2 UNIQUEIDENTIFIER = NEWID();
INSERT INTO IntegrationOperations (Id, ConfigId, Name, HttpMethod, Path, ContentType, IsEnabled)
VALUES (@OpId2, @ConfigId, 'balance-inquiry', 'POST', '/bank-b/v1/balance', 'application/json', 1);

INSERT INTO RequestMappings (Id, OperationId, SourceField, TargetField, IsRequired)
VALUES (NEWID(), @OpId2, 'accountNumber', 'acct_no', 1);

INSERT INTO ResponseMappings (Id, OperationId, SourceField, TargetField, SortOrder)
VALUES
    (NEWID(), @OpId2, 'result_code', 'statusCode', 1),
    (NEWID(), @OpId2, 'data.avail_bal', 'availableBalance', 2),
    (NEWID(), @OpId2, 'data.current_bal', 'currentBalance', 3),
    (NEWID(), @OpId2, 'data.currency', 'currency', 4);

PRINT 'Migration completed: bank-b-transfer imported';

-- Repeat similar blocks for bank-c-transfer, ewallet-payment, sms-gateway, kyc-service...
-- (Pattern is the same, just different values)
