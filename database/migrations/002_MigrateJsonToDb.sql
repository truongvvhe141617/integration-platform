-- Migration 002: Seed data from JSON configs
-- Run after 001_InitialSchema.sql
-- This migrates existing JSON config files into the database

-- ═══ Bank B Transfer ═══
DECLARE @bankBId UNIQUEIDENTIFIER = NEWID();
DECLARE @bankBOp1 UNIQUEIDENTIFIER = NEWID();
DECLARE @bankBOp2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO IntegrationConfigs (Id, ConfigId, Name, ConnectorType, Status, Version, BaseUrl, DefaultHeaders, AuthType, AuthParams, TimeoutMs, RetryConfig, CircuitBreakerConfig, Tags, CreatedBy)
VALUES (@bankBId, 'bank-b-transfer', 'Bank B - Fund Transfer', 'generic-http', 'active', 1,
    'https://api.bankb.vn',
    '{"X-Channel":"PARTNER","X-Bank-Code":"BANKB"}',
    'apikey', '{"HeaderName":"X-Api-Key","ApiKey":"bank-b-secret-key"}',
    30000,
    '{"maxRetries":2,"initialDelayMs":1000,"backoffStrategy":"exponential"}',
    '{"failureThreshold":3,"durationOfBreakSeconds":60,"samplingDurationSeconds":120}',
    'banking,transfer', 'migration-script');

INSERT INTO IntegrationOperations (Id, ConfigId, Name, HttpMethod, Path, ContentType, SortOrder)
VALUES (@bankBOp1, @bankBId, 'transfer', 'POST', '/v1/fund-transfer', 'application/json', 0);

INSERT INTO RequestMappings (OperationId, SourceField, TargetField, IsRequired, SortOrder) VALUES
(@bankBOp1, 'fromAccount', 'src_acct_no', 1, 0),
(@bankBOp1, 'toAccount', 'dest_acct_no', 1, 1),
(@bankBOp1, 'amount', 'txn_amount', 1, 2),
(@bankBOp1, 'currency', 'txn_ccy', 0, 3),
(@bankBOp1, 'description', 'txn_desc', 0, 4),
(@bankBOp1, 'referenceId', 'partner_ref_no', 1, 5);

INSERT INTO ResponseMappings (OperationId, SourceField, TargetField, SortOrder) VALUES
(@bankBOp1, 'result_code', 'statusCode', 0),
(@bankBOp1, 'result_msg', 'statusMessage', 1),
(@bankBOp1, 'data.bank_ref_no', 'bankTransactionId', 2),
(@bankBOp1, 'data.txn_date', 'transactionDate', 3),
(@bankBOp1, 'data.fee', 'transactionFee', 4);

INSERT INTO ValidationRules (OperationId, Field, Rule, ErrorMessage, SortOrder) VALUES
(@bankBOp1, 'fromAccount', 'Required', 'Source account is required', 0),
(@bankBOp1, 'toAccount', 'Required', 'Destination account is required', 1),
(@bankBOp1, 'amount', 'Required', 'Amount is required', 2),
(@bankBOp1, 'referenceId', 'Required', 'Reference ID is required', 3);

-- Balance inquiry operation
INSERT INTO IntegrationOperations (Id, ConfigId, Name, HttpMethod, Path, ContentType, SortOrder)
VALUES (@bankBOp2, @bankBId, 'balance-inquiry', 'POST', '/v1/balance', 'application/json', 1);

INSERT INTO RequestMappings (OperationId, SourceField, TargetField, IsRequired, SortOrder) VALUES
(@bankBOp2, 'accountNumber', 'acct_no', 1, 0);

INSERT INTO ResponseMappings (OperationId, SourceField, TargetField, SortOrder) VALUES
(@bankBOp2, 'data.avail_bal', 'availableBalance', 0),
(@bankBOp2, 'data.current_bal', 'currentBalance', 1),
(@bankBOp2, 'data.currency', 'currency', 2);

-- ═══ E-Wallet Payment ═══
DECLARE @walletId UNIQUEIDENTIFIER = NEWID();
DECLARE @walletOp UNIQUEIDENTIFIER = NEWID();

INSERT INTO IntegrationConfigs (Id, ConfigId, Name, ConnectorType, Status, Version, BaseUrl, AuthType, AuthParams, TimeoutMs, Tags, CreatedBy)
VALUES (@walletId, 'ewallet-payment', 'E-Wallet Payment', 'generic-http', 'active', 1,
    'https://api.ewallet.vn', 'apikey', '{"HeaderName":"X-Api-Key","ApiKey":"wallet-key"}',
    15000, 'payment,ewallet', 'migration-script');

INSERT INTO IntegrationOperations (Id, ConfigId, Name, HttpMethod, Path, SortOrder)
VALUES (@walletOp, @walletId, 'payment', 'POST', '/api/v2/payment', 0);

INSERT INTO RequestMappings (OperationId, SourceField, TargetField, IsRequired, Transform, SortOrder) VALUES
(@walletOp, 'orderId', 'partner_ref_id', 1, NULL, 0),
(@walletOp, 'amount', 'amount', 1, NULL, 1),
(@walletOp, 'customer.phone', 'customer_phone', 1, 'Trim', 2),
(@walletOp, 'customer.name', 'customer_name', 0, NULL, 3),
(@walletOp, 'description', 'order_info', 0, NULL, 4);

INSERT INTO ResponseMappings (OperationId, SourceField, TargetField, SortOrder) VALUES
(@walletOp, 'resultCode', 'statusCode', 0),
(@walletOp, 'message', 'statusMessage', 1),
(@walletOp, 'transId', 'transactionId', 2),
(@walletOp, 'payUrl', 'paymentUrl', 3);

-- ═══ Record history ═══
INSERT INTO ConfigHistory (ConfigId, Version, Snapshot, ChangeType, ChangedBy, ChangeNote)
VALUES (@bankBId, 1, '{}', 'migrated', 'migration-script', 'Migrated from JSON config');

INSERT INTO ConfigHistory (ConfigId, Version, Snapshot, ChangeType, ChangedBy, ChangeNote)
VALUES (@walletId, 1, '{}', 'migrated', 'migration-script', 'Migrated from JSON config');

PRINT 'Migration complete: 2 configs migrated from JSON to DB';
GO
