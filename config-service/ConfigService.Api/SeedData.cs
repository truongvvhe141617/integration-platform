using BuildingBlocks.Abstractions.Connectors;
using ConfigService.Api.Interfaces;

namespace ConfigService.Api;

public static class SeedData
{
    // Mock API chạy trên Config Service (port 5002)
    private const string MockBaseUrl = "http://localhost:5002/mock";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var repo = services.GetRequiredService<IConfigRepository>();
        var audit = services.GetRequiredService<IConfigAuditRepository>();

        await SeedBankB(repo, audit);
        await SeedBankC(repo, audit);
        await SeedEWallet(repo, audit);
        await SeedHttpBin(repo, audit);
    }

    private static async Task SeedBankB(IConfigRepository repo, IConfigAuditRepository audit)
    {
        if (await repo.GetActiveAsync("bank-b-transfer") != null) return;

        await repo.CreateAsync(new ConnectorConfig
        {
            Id = "bank-b-transfer",
            Name = "Bank B - Fund Transfer",
            ConnectorType = "generic-http",
            Version = 1,
            Status = "Active",
            TimeoutMs = 30000,
            Endpoint = new()
            {
                BaseUrl = MockBaseUrl,
                DefaultHeaders = new() { ["X-Channel"] = "PARTNER", ["X-Bank-Code"] = "BANKB" }
            },
            Authentication = new()
            {
                Type = "ApiKey",
                Parameters = new() { ["HeaderName"] = "X-Api-Key", ["ApiKey"] = "bank-b-secret-key-demo" }
            },
            Operations = new()
            {
                new()
                {
                    Name = "transfer",
                    HttpMethod = "POST",
                    Path = "/bank-b/v1/fund-transfer",
                    ContentType = "application/json",
                    RequestMappings = new()
                    {
                        new() { Source = "fromAccount", Target = "src_acct_no", Required = true },
                        new() { Source = "toAccount", Target = "dest_acct_no", Required = true },
                        new() { Source = "amount", Target = "txn_amount", Required = true },
                        new() { Source = "currency", Target = "txn_ccy", DefaultValue = "VND" },
                        new() { Source = "description", Target = "txn_desc", DefaultValue = "Fund Transfer", Transform = "ToUpper" },
                        new() { Source = "referenceId", Target = "partner_ref_no", Required = true }
                    },
                    ResponseMappings = new()
                    {
                        new() { Source = "result_code", Target = "statusCode" },
                        new() { Source = "result_msg", Target = "statusMessage" },
                        new() { Source = "data.bank_ref_no", Target = "bankTransactionId" },
                        new() { Source = "data.txn_date", Target = "transactionDate" },
                        new() { Source = "data.fee", Target = "transactionFee" },
                        new() { Source = "data.status", Target = "transactionStatus" }
                    },
                    Validations = new()
                    {
                        new() { Field = "fromAccount", Rule = "Required", ErrorMessage = "Source account is required" },
                        new() { Field = "toAccount", Rule = "Required", ErrorMessage = "Destination account is required" },
                        new() { Field = "amount", Rule = "Required", ErrorMessage = "Amount is required" },
                        new() { Field = "referenceId", Rule = "Required", ErrorMessage = "Reference ID is required" }
                    }
                },
                new()
                {
                    Name = "balance-inquiry",
                    HttpMethod = "POST",
                    Path = "/bank-b/v1/balance",
                    ContentType = "application/json",
                    RequestMappings = new()
                    {
                        new() { Source = "accountNumber", Target = "acct_no", Required = true }
                    },
                    ResponseMappings = new()
                    {
                        new() { Source = "result_code", Target = "statusCode" },
                        new() { Source = "data.avail_bal", Target = "availableBalance" },
                        new() { Source = "data.current_bal", Target = "currentBalance" },
                        new() { Source = "data.currency", Target = "currency" },
                        new() { Source = "data.acct_name", Target = "accountName" }
                    },
                    Validations = new()
                    {
                        new() { Field = "accountNumber", Rule = "Required", ErrorMessage = "Account number is required" }
                    }
                }
            },
            Retry = new() { MaxRetries = 2, InitialDelayMs = 1000, BackoffStrategy = "Exponential", RetryOnStatusCodes = new() { 500, 502, 503 } },
            CircuitBreaker = new() { FailureThreshold = 3, DurationOfBreakSeconds = 60, SamplingDurationSeconds = 120 },
            Metadata = new() { ["provider"] = "Bank B", ["team"] = "banking-team" }
        });

        await audit.AddAsync(new Models.ConfigAuditEntry { ConfigId = "bank-b-transfer", Version = 1, Action = "Seeded", PerformedBy = "system" });
    }

    private static async Task SeedBankC(IConfigRepository repo, IConfigAuditRepository audit)
    {
        if (await repo.GetActiveAsync("bank-c-transfer") != null) return;

        await repo.CreateAsync(new ConnectorConfig
        {
            Id = "bank-c-transfer",
            Name = "Bank C - Fund Transfer",
            ConnectorType = "generic-http",
            Version = 1,
            Status = "Active",
            TimeoutMs = 20000,
            Endpoint = new()
            {
                BaseUrl = MockBaseUrl,
                DefaultHeaders = new() { ["X-Partner-Id"] = "COMPANY-001" }
            },
            Authentication = new()
            {
                Type = "BearerToken",
                Parameters = new() { ["Token"] = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.demo" }
            },
            Operations = new()
            {
                new()
                {
                    Name = "transfer",
                    HttpMethod = "POST",
                    Path = "/bank-c/api/transfer",
                    ContentType = "application/json",
                    RequestMappings = new()
                    {
                        new() { Source = "fromAccount", Target = "sender_account", Required = true },
                        new() { Source = "toAccount", Target = "receiver_account", Required = true },
                        new() { Source = "amount", Target = "transfer_amount", Required = true },
                        new() { Source = "currency", Target = "ccy", DefaultValue = "VND" },
                        new() { Source = "description", Target = "remark", DefaultValue = "Transfer" },
                        new() { Source = "referenceId", Target = "external_ref", Required = true }
                    },
                    ResponseMappings = new()
                    {
                        new() { Source = "code", Target = "statusCode" },
                        new() { Source = "message", Target = "statusMessage" },
                        new() { Source = "transaction_id", Target = "bankTransactionId" },
                        new() { Source = "processed_at", Target = "transactionDate" },
                        new() { Source = "fee_amount", Target = "transactionFee" }
                    },
                    Validations = new()
                    {
                        new() { Field = "fromAccount", Rule = "Required", ErrorMessage = "Sender account is required" },
                        new() { Field = "toAccount", Rule = "Required", ErrorMessage = "Receiver account is required" },
                        new() { Field = "amount", Rule = "Required", ErrorMessage = "Amount is required" },
                        new() { Field = "referenceId", Rule = "Required", ErrorMessage = "Reference ID is required" }
                    }
                }
            },
            Retry = new() { MaxRetries = 3, InitialDelayMs = 500, BackoffStrategy = "Exponential", RetryOnStatusCodes = new() { 500, 502, 503 } },
            CircuitBreaker = new() { FailureThreshold = 5, DurationOfBreakSeconds = 30, SamplingDurationSeconds = 60 },
            Metadata = new() { ["provider"] = "Bank C", ["team"] = "banking-team" }
        });

        await audit.AddAsync(new Models.ConfigAuditEntry { ConfigId = "bank-c-transfer", Version = 1, Action = "Seeded", PerformedBy = "system" });
    }

    private static async Task SeedEWallet(IConfigRepository repo, IConfigAuditRepository audit)
    {
        if (await repo.GetActiveAsync("ewallet-payment") != null) return;

        await repo.CreateAsync(new ConnectorConfig
        {
            Id = "ewallet-payment",
            Name = "E-Wallet Payment",
            ConnectorType = "generic-http",
            Version = 1,
            Status = "Active",
            TimeoutMs = 15000,
            Endpoint = new()
            {
                BaseUrl = MockBaseUrl,
                DefaultHeaders = new() { ["X-Wallet-Version"] = "v2" }
            },
            Authentication = new()
            {
                Type = "ApiKey",
                Parameters = new() { ["HeaderName"] = "X-Api-Key", ["ApiKey"] = "wallet-key-demo-123" }
            },
            Operations = new()
            {
                new()
                {
                    Name = "payment",
                    HttpMethod = "POST",
                    Path = "/ewallet/api/v2/payment",
                    ContentType = "application/json",
                    RequestMappings = new()
                    {
                        new() { Source = "orderId", Target = "partner_ref_id", Required = true },
                        new() { Source = "amount", Target = "amount", Required = true },
                        new() { Source = "currency", Target = "currency", DefaultValue = "VND" },
                        new() { Source = "customer.phone", Target = "customer_phone", Required = true, Transform = "Trim" },
                        new() { Source = "customer.name", Target = "customer_name" },
                        new() { Source = "description", Target = "order_info", DefaultValue = "Payment" }
                    },
                    ResponseMappings = new()
                    {
                        new() { Source = "resultCode", Target = "statusCode" },
                        new() { Source = "message", Target = "statusMessage" },
                        new() { Source = "transId", Target = "transactionId" },
                        new() { Source = "payUrl", Target = "paymentUrl" },
                        new() { Source = "amount", Target = "amount" }
                    },
                    Validations = new()
                    {
                        new() { Field = "orderId", Rule = "Required", ErrorMessage = "Order ID is required" },
                        new() { Field = "amount", Rule = "Required", ErrorMessage = "Amount is required" },
                        new() { Field = "customer.phone", Rule = "Required", ErrorMessage = "Customer phone is required" }
                    }
                }
            },
            Retry = new() { MaxRetries = 2, InitialDelayMs = 500, BackoffStrategy = "Exponential", RetryOnStatusCodes = new() { 500, 502, 503 } },
            CircuitBreaker = new() { FailureThreshold = 5, DurationOfBreakSeconds = 30, SamplingDurationSeconds = 60 },
            Metadata = new() { ["provider"] = "E-Wallet", ["team"] = "payment-team" }
        });

        await audit.AddAsync(new Models.ConfigAuditEntry { ConfigId = "ewallet-payment", Version = 1, Action = "Seeded", PerformedBy = "system" });
    }

    private static async Task SeedHttpBin(IConfigRepository repo, IConfigAuditRepository audit)
    {
        if (await repo.GetActiveAsync("test-httpbin") != null) return;

        await repo.CreateAsync(new ConnectorConfig
        {
            Id = "test-httpbin",
            Name = "HTTPBin Echo (internet test)",
            ConnectorType = "generic-http",
            Version = 1,
            Status = "Active",
            TimeoutMs = 15000,
            Endpoint = new() { BaseUrl = "https://httpbin.org", DefaultHeaders = new() },
            Authentication = new() { Type = "None", Parameters = new() },
            Operations = new()
            {
                new()
                {
                    Name = "echo",
                    HttpMethod = "POST",
                    Path = "/post",
                    ContentType = "application/json",
                    RequestMappings = new()
                    {
                        new() { Source = "orderId", Target = "order_id", Required = true },
                        new() { Source = "amount", Target = "total_amount", Required = true }
                    },
                    ResponseMappings = new()
                    {
                        new() { Source = "url", Target = "endpoint_called" },
                        new() { Source = "origin", Target = "caller_ip" }
                    },
                    Validations = new() { new() { Field = "orderId", Rule = "Required" } }
                }
            },
            Retry = new() { MaxRetries = 1, InitialDelayMs = 500, BackoffStrategy = "Fixed", RetryOnStatusCodes = new() { 500 } },
            CircuitBreaker = new() { FailureThreshold = 5, DurationOfBreakSeconds = 30, SamplingDurationSeconds = 60 },
            Metadata = new() { ["team"] = "platform-team" }
        });

        await audit.AddAsync(new Models.ConfigAuditEntry { ConfigId = "test-httpbin", Version = 1, Action = "Seeded", PerformedBy = "system" });
    }
}
