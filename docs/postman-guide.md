# Hướng dẫn chạy và test bằng Postman

## 1. Khởi động hệ thống

### Cách 1: Docker Compose
```bash
cd integration-platform/infrastructure/docker
docker-compose up -d
```

### Cách 2: Chạy từng service (development — không cần Docker/Redis)

Mở 4 terminal riêng biệt, mỗi terminal chạy 1 service:

```bash
# Terminal 1: Config Service (port 5002)
cd integration-platform/services/config-service/ConfigService.Api
dotnet run

# Terminal 2: Integration Service (port 5001)
cd integration-platform/services/integration-service/IntegrationService.Api
dotnet run

# Terminal 3: Business Service (port 5003)
cd integration-platform/services/business-service/BusinessService.Api
dotnet run

# Terminal 4: Gateway (port 5000) — optional, có thể test trực tiếp từng service
cd integration-platform/gateway/Gateway.Api
dotnet run
```

Hoặc build toàn bộ trước:
```bash
cd integration-platform
dotnet build integration-platform.sln
```

### Ports

| Service              | URL                        | Swagger UI                            |
|----------------------|----------------------------|---------------------------------------|
| API Gateway          | http://localhost:5000       | (không có Swagger)                    |
| Integration Service  | http://localhost:5001       | http://localhost:5001/swagger          |
| Config Service       | http://localhost:5002       | http://localhost:5002/swagger          |
| Business Service     | http://localhost:5003       | http://localhost:5003/swagger          |

> Chế độ dev: Integration Service dùng InMemory cache thay Redis,
> Config Service dùng InMemory storage thay PostgreSQL.
> Không cần cài thêm gì ngoài .NET 8 SDK.

---

## 2. Import Postman Collection

File: `docs/postman-collection.json` — import vào Postman (File → Import).

Collection đã có sẵn variables:
- `config_url` = `http://localhost:5002`
- `integration_url` = `http://localhost:5001`
- `business_url` = `http://localhost:5003`
- `gateway_url` = `http://localhost:5000`

---

## 3. Test Flow: Thêm API mới (Case đơn giản — chỉ config)

### Step 1: Tạo connector config

```
POST http://localhost:5002/api/v1/configs
Content-Type: application/json
X-User-Id: admin@company.com
```

```json
{
  "id": "ewallet-momo",
  "name": "MoMo E-Wallet Integration",
  "connectorType": "generic-http",
  "endpoint": {
    "baseUrl": "https://test-payment.momo.vn",
    "defaultHeaders": {
      "Content-Type": "application/json"
    }
  },
  "authentication": {
    "type": "ApiKey",
    "parameters": {
      "HeaderName": "X-API-Key",
      "ApiKey": "test-api-key-123"
    }
  },
  "operations": [
    {
      "name": "payment",
      "httpMethod": "POST",
      "path": "/api/v2/payment",
      "contentType": "application/json",
      "requestMappings": [
        { "source": "orderId", "target": "partnerRefId", "required": true },
        { "source": "amount", "target": "amount", "required": true },
        { "source": "customer.phone", "target": "customerPhone", "required": true }
      ],
      "responseMappings": [
        { "source": "transId", "target": "transactionId" },
        { "source": "resultCode", "target": "statusCode" }
      ],
      "validations": [
        { "field": "orderId", "rule": "Required", "errorMessage": "Order ID is required" },
        { "field": "amount", "rule": "Required", "errorMessage": "Amount is required" }
      ]
    }
  ],
  "retry": {
    "maxRetries": 3,
    "initialDelayMs": 1000,
    "backoffStrategy": "Exponential",
    "retryOnStatusCodes": [408, 429, 500, 502, 503, 504]
  },
  "circuitBreaker": {
    "failureThreshold": 5,
    "durationOfBreakSeconds": 30,
    "samplingDurationSeconds": 60
  },
  "timeoutMs": 15000
}
```

Expected: `201 Created`, status = `"Draft"`

### Step 2: Governance flow (Draft → Review → Approved → Active)

```
POST http://localhost:5002/api/v1/configs/ewallet-momo/transition?status=Review
X-User-Id: admin@company.com

POST http://localhost:5002/api/v1/configs/ewallet-momo/transition?status=Approved
X-User-Id: reviewer@company.com

POST http://localhost:5002/api/v1/configs/ewallet-momo/transition?status=Active
X-User-Id: deployer@company.com
```

### Step 3: Verify config đã Active

```
GET http://localhost:5002/api/v1/configs/ewallet-momo
```

### Step 4: Gọi Integration Service

```
POST http://localhost:5001/api/v1/integration/execute
Content-Type: application/json
X-Correlation-Id: test-001
```

```json
{
  "connectorConfigId": "ewallet-momo",
  "operation": "payment",
  "idempotencyKey": "PAY-001",
  "data": {
    "orderId": "ORD-001",
    "amount": 150000,
    "customer": {
      "phone": "0901234567"
    }
  }
}
```

> Lưu ý: Vì MoMo API thật không chạy, response sẽ là connection error.
> Điều quan trọng là flow hoạt động: config loaded → mapping applied → connector called.

### Step 5: Xem audit log

```
GET http://localhost:5002/api/v1/configs/ewallet-momo/audit
```

---

## 4. Test Validation

Gửi request thiếu required field:

```
POST http://localhost:5001/api/v1/integration/execute
Content-Type: application/json
```

```json
{
  "connectorConfigId": "ewallet-momo",
  "operation": "payment",
  "data": {
    "amount": 100000
  }
}
```

Expected: `400 Bad Request` với validation errors.

---

## 5. Test Idempotency

Gửi cùng request 2 lần với cùng `idempotencyKey`:

```json
{
  "connectorConfigId": "ewallet-momo",
  "operation": "payment",
  "idempotencyKey": "IDEMP-001",
  "data": {
    "orderId": "ORD-IDEMP",
    "amount": 100000,
    "customer": { "phone": "0901234567" }
  }
}
```

Lần 2 trả cached response, không gọi lại third-party.

---

## 6. Test Config Versioning & Rollback

```
# Update config (tạo v2)
PUT http://localhost:5002/api/v1/configs/ewallet-momo
X-User-Id: admin@company.com
Content-Type: application/json
Body: (config JSON đã sửa)

# Xem tất cả versions
GET http://localhost:5002/api/v1/configs/ewallet-momo/versions

# Rollback về v1
POST http://localhost:5002/api/v1/configs/ewallet-momo/rollback/1
X-User-Id: admin@company.com
```

---

## 7. Test Health Check

```
GET http://localhost:5001/api/v1/integration/health/ewallet-momo
GET http://localhost:5001/health
GET http://localhost:5002/health
GET http://localhost:5003/health
```

---

## 8. Thứ tự khởi động khuyến nghị

1. Config Service (port 5002) — khởi động trước vì các service khác phụ thuộc
2. Integration Service (port 5001) — cần Config Service
3. Business Service (port 5003) — cần Integration Service
4. Gateway (port 5000) — optional, proxy tới 3 service trên
