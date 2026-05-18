# Integration Flows - Ví dụ chi tiết

## Flow 1: Case đơn giản - Thêm API ví điện tử MoMo (CHỈ CẦN CONFIG)

### Bước thực hiện:
1. Tạo config JSON mô tả MoMo API (xem `config-examples/simple-api-config.json`)
2. POST config lên Config Service
3. Done! Hệ thống tự động sử dụng Generic HTTP Connector

### Sequence Diagram:
```
Client                Gateway          Business Svc       Integration Svc      Config Svc        MoMo API
  │                     │                  │                    │                  │                 │
  │ POST /payment       │                  │                    │                  │                 │
  │────────────────────>│                  │                    │                  │                 │
  │                     │ route + auth     │                    │                  │                 │
  │                     │─────────────────>│                    │                  │                 │
  │                     │                  │ POST /execute      │                  │                 │
  │                     │                  │ {configId:         │                  │                 │
  │                     │                  │  "ewallet-momo",   │                  │                 │
  │                     │                  │  operation:        │                  │                 │
  │                     │                  │  "payment",        │                  │                 │
  │                     │                  │  data: {...}}      │                  │                 │
  │                     │                  │───────────────────>│                  │                 │
  │                     │                  │                    │                  │                 │
  │                     │                  │                    │ GET config       │                 │
  │                     │                  │                    │ (check Redis     │                 │
  │                     │                  │                    │  cache first)    │                 │
  │                     │                  │                    │─────────────────>│                 │
  │                     │                  │                    │ ConnectorConfig  │                 │
  │                     │                  │                    │<─────────────────│                 │
  │                     │                  │                    │                  │                 │
  │                     │                  │                    │ 1. Validate request               │
  │                     │                  │                    │ 2. Map request (config-driven)    │
  │                     │                  │                    │ 3. Create GenericHttpConnector     │
  │                     │                  │                    │ 4. Apply auth (ApiKey)            │
  │                     │                  │                    │                  │                 │
  │                     │                  │                    │ POST /api/v2/payment              │
  │                     │                  │                    │ (mapped payload) │                 │
  │                     │                  │                    │────────────────────────────────── >│
  │                     │                  │                    │                  │                 │
  │                     │                  │                    │ Response         │                 │
  │                     │                  │                    │<──────────────────────────────────│
  │                     │                  │                    │                  │                 │
  │                     │                  │                    │ 5. Map response (config-driven)   │
  │                     │                  │                    │                  │                 │
  │                     │                  │ Mapped response    │                  │                 │
  │                     │                  │<───────────────────│                  │                 │
  │                     │ Response         │                    │                  │                 │
  │                     │<─────────────────│                    │                  │                 │
  │ Response            │                  │                    │                  │                 │
  │<────────────────────│                  │                    │                  │                 │
```

### Ví dụ request từ client:
```json
POST /api/v1/integration/execute
{
  "connectorConfigId": "ewallet-momo",
  "operation": "payment",
  "data": {
    "orderId": "ORD-2024-001",
    "amount": 150000,
    "currency": "VND",
    "customer": {
      "phone": "0901234567"
    },
    "description": "Thanh toán đơn hàng #001"
  }
}
```

### Sau khi mapping (gửi tới MoMo):
```json
POST https://api.momo.vn/api/v2/payment
Headers: X-API-Key: {{MOMO_API_KEY}}
{
  "partnerRefId": "ORD-2024-001",
  "amount": 150000,
  "currency": "VND",
  "customerPhone": "0901234567",
  "orderInfo": "Thanh toán đơn hàng #001"
}
```

### Response mapping (trả về client):
```json
{
  "success": true,
  "data": {
    "transactionId": "MOMO-TXN-12345",
    "statusCode": "0",
    "statusMessage": "Success",
    "redirectUrl": "https://payment.momo.vn/pay/MOMO-TXN-12345"
  },
  "correlationId": "abc-123-def",
  "executionTimeMs": 245.5
}
```

---

## Flow 2: Case phức tạp - Thêm Bank ABC (CẦN CUSTOM DLL)

### Tại sao cần custom connector?
- Bank ABC yêu cầu HMAC-SHA256 signature trên mỗi request
- Cần mTLS (mutual TLS) với certificate
- Response format đặc biệt cần parse riêng

### Bước thực hiện:
1. Tạo project `Connector.SampleBank` implement `IConnector`
2. Build DLL: `dotnet build -c Release`
3. Copy DLL vào thư mục `plugins/` của Integration Service
4. Tạo config JSON (xem `config-examples/complex-bank-config.json`)
5. POST config lên Config Service
6. Done! Integration Service tự động load DLL và sử dụng

### Sequence Diagram:
```
Client          Gateway       Business Svc    Integration Svc     ConnectorFactory    SampleBank DLL    Bank ABC API
  │               │               │                │                    │                  │                │
  │ POST          │               │                │                    │                  │                │
  │ /transfer     │               │                │                    │                  │                │
  │──────────────>│               │                │                    │                  │                │
  │               │──────────────>│                │                    │                  │                │
  │               │               │ POST /execute  │                    │                  │                │
  │               │               │───────────────>│                    │                  │                │
  │               │               │                │                    │                  │                │
  │               │               │                │ Load config        │                  │                │
  │               │               │                │ connectorType =    │                  │                │
  │               │               │                │ "sample-bank"      │                  │                │
  │               │               │                │                    │                  │                │
  │               │               │                │ Create connector   │                  │                │
  │               │               │                │───────────────────>│                  │                │
  │               │               │                │                    │                  │                │
  │               │               │                │                    │ Load DLL         │                │
  │               │               │                │                    │ (if not loaded)  │                │
  │               │               │                │                    │─────────────────>│                │
  │               │               │                │                    │                  │                │
  │               │               │                │ SampleBankConnector│                  │                │
  │               │               │                │<───────────────────│                  │                │
  │               │               │                │                    │                  │                │
  │               │               │                │ ExecuteAsync()     │                  │                │
  │               │               │                │ 1. Build signed request               │                │
  │               │               │                │ 2. Generate HMAC signature            │                │
  │               │               │                │ 3. Apply mTLS cert │                  │                │
  │               │               │                │                    │                  │                │
  │               │               │                │ POST /partner/v1/fund-transfer        │                │
  │               │               │                │ + X-Bank-Signature │                  │                │
  │               │               │                │─────────────────────────────────────────────────────── >│
  │               │               │                │                    │                  │                │
  │               │               │                │ Bank response      │                  │                │
  │               │               │                │<───────────────────────────────────────────────────────│
  │               │               │                │                    │                  │                │
  │               │               │                │ 4. Verify response signature          │                │
  │               │               │                │ 5. Parse bank-specific format         │                │
  │               │               │                │                    │                  │                │
  │               │               │ Response       │                    │                  │                │
  │               │               │<───────────────│                    │                  │                │
  │               │ Response      │                │                    │                  │                │
  │               │<──────────────│                │                    │                  │                │
  │ Response      │               │                │                    │                  │                │
  │<──────────────│               │                │                    │                  │                │
```

---

## So sánh 2 case:

| Tiêu chí              | Case đơn giản (MoMo)     | Case phức tạp (Bank ABC)     |
|------------------------|--------------------------|------------------------------|
| Cần viết code?         | ❌ Không                  | ✅ Viết DLL connector         |
| Connector type         | generic-http             | sample-bank (custom)         |
| Thời gian tích hợp     | ~30 phút (chỉ config)   | ~1-2 ngày (code + test)     |
| Sửa core system?       | ❌ Không                  | ❌ Không                      |
| Deploy lại core?       | ❌ Không                  | ❌ Không (chỉ copy DLL)      |
| Auth handling          | Config-driven            | Custom logic trong DLL       |
| Request mapping        | Config-driven            | Custom + Config              |
