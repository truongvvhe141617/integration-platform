# Hướng dẫn Test — Postman

## Khởi động

```bash
# Terminal 1: Config Service (port 5002) — khởi động TRƯỚC
cd services/config-service/ConfigService.Api
dotnet run

# Terminal 2: Integration Service (port 5001)
cd services/integration-service/IntegrationService.Api
dotnet run
```

Chỉ cần 2 service này. Không cần tạo config — seed data tự tạo 4 configs sẵn.

---

## Test 1: Bank B — Chuyển tiền

App A muốn chuyển 5 triệu từ tài khoản 012... sang 987... qua Bank B.

```
POST http://localhost:5001/api/v1/integration/execute
Content-Type: application/json
X-Correlation-Id: bank-b-test-001
```

```json
{
  "connectorConfigId": "bank-b-transfer",
  "operation": "transfer",
  "idempotencyKey": "TXN-BANKB-001",
  "data": {
    "fromAccount": "0123456789",
    "toAccount": "9876543210",
    "amount": 5000000,
    "currency": "VND",
    "description": "Thanh toan hoa don #123",
    "referenceId": "REF-2024-001"
  }
}
```

Kết quả mong đợi: Integration Service map request sang format Bank B,
gọi httpbin (giả lập Bank B), map response về format nội bộ.

---

## Test 2: Bank C — Cùng nghiệp vụ, khác ngân hàng

Cùng chuyển tiền, nhưng qua Bank C (format API hoàn toàn khác Bank B).
App A chỉ đổi `connectorConfigId`, data format giữ nguyên.

```
POST http://localhost:5001/api/v1/integration/execute
Content-Type: application/json
X-Correlation-Id: bank-c-test-001
```

```json
{
  "connectorConfigId": "bank-c-transfer",
  "operation": "transfer",
  "idempotencyKey": "TXN-BANKC-001",
  "data": {
    "fromAccount": "0123456789",
    "toAccount": "5555666677",
    "amount": 1000000,
    "description": "Chuyen tien Bank C",
    "referenceId": "REF-2024-002"
  }
}
```

So sánh rawResponse của Bank B vs Bank C — cùng data đầu vào nhưng
request gửi đi khác nhau (field names khác, auth khác).

---

## Test 3: E-Wallet — Thanh toán

App A thanh toán qua ví điện tử.

```
POST http://localhost:5001/api/v1/integration/execute
Content-Type: application/json
X-Correlation-Id: wallet-test-001
```

```json
{
  "connectorConfigId": "ewallet-payment",
  "operation": "payment",
  "idempotencyKey": "PAY-WALLET-001",
  "data": {
    "orderId": "ORD-2024-001",
    "amount": 250000,
    "customer": {
      "phone": "  0901234567  ",
      "name": "Nguyen Van A"
    },
    "description": "mua hang online",
    "callbackUrl": "https://myapp.com/callback"
  }
}
```

---

## Test 4: Bank B — Truy vấn số dư

Cùng connector Bank B, nhưng operation khác.

```
POST http://localhost:5001/api/v1/integration/execute
Content-Type: application/json
X-Correlation-Id: balance-test-001
```

```json
{
  "connectorConfigId": "bank-b-transfer",
  "operation": "balance-inquiry",
  "data": {
    "accountNumber": "0123456789"
  }
}
```

---

## Test 5: Validation — Thiếu field bắt buộc

```json
{
  "connectorConfigId": "bank-b-transfer",
  "operation": "transfer",
  "data": {
    "fromAccount": "0123456789",
    "amount": 5000000
  }
}
```

Kết quả: 400 Bad Request — thiếu toAccount và referenceId.

---

## Test 6: Idempotency — Gửi trùng

Gửi Test 1 lần nữa với cùng `idempotencyKey: "TXN-BANKB-001"`.
Lần 2 trả cached response, không gọi lại Bank B (executionTimeMs ≈ 0).

---

## Test 7: Config không tồn tại

```json
{
  "connectorConfigId": "bank-xyz-khong-ton-tai",
  "operation": "transfer",
  "data": { "amount": 100 }
}
```

Kết quả: 404 — Config not found.

---

## Test 8: Xem tất cả configs

```
GET http://localhost:5002/api/v1/configs
```

---

## Test 9: Xem audit log

```
GET http://localhost:5002/api/v1/configs/bank-b-transfer/audit
```

---

## Điểm chính khi test

| Test                  | Chứng minh điều gì                                    |
|-----------------------|--------------------------------------------------------|
| Bank B transfer       | Request/response mapping hoạt động                     |
| Bank C transfer       | Cùng data, khác connector → khác format gửi đi        |
| E-Wallet payment      | Connector khác loại (wallet vs bank), cùng cơ chế     |
| Balance inquiry       | 1 connector có nhiều operations                        |
| Validation            | Config-driven validation                               |
| Idempotency           | Không gọi trùng third-party                            |
| Config not found      | Error handling                                         |
