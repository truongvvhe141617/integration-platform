-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  Database: BusinessService                                        ║
-- ║  Chạy: USE BusinessService; GO; rồi execute file này             ║
-- ╚══════════════════════════════════════════════════════════════════╝

USE BusinessService;
GO

-- ═══ Payment Transactions ═══
CREATE TABLE biz_payment_transaction (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TenantId        NVARCHAR(100) NOT NULL DEFAULT 'default',
    OrderId         NVARCHAR(100) NOT NULL,
    PaymentMethod   NVARCHAR(50) NOT NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    Currency        NVARCHAR(10) NOT NULL DEFAULT 'VND',
    Status          NVARCHAR(20) NOT NULL DEFAULT 'pending',
    TransactionId   NVARCHAR(200),
    PaymentUrl      NVARCHAR(500),
    ErrorMessage    NVARCHAR(500),
    CorrelationId   NVARCHAR(100),
    CompletedAt     DATETIME2,

    -- Audit
    CreatedBy       NVARCHAR(100),
    CreatedDate     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedBy       NVARCHAR(100),
    UpdatedDate     DATETIME2
);

CREATE INDEX IX_biz_payment_order ON biz_payment_transaction(OrderId);
CREATE INDEX IX_biz_payment_correlation ON biz_payment_transaction(CorrelationId);
CREATE INDEX IX_biz_payment_status ON biz_payment_transaction(Status, CreatedDate DESC);

PRINT 'BusinessService tables created successfully.';
GO
