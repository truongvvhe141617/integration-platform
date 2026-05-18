# Luồng Integration Service — Chi tiết Request/Response

## Vai trò: Integration Service là "cầu nối thông minh"

```
┌──────────┐         ┌─────────────────────┐         ┌──────────┐
│  App A   │ ──────> │ Integration Service  │ ──────> │  Bank B  │
│ (nội bộ) │ <────── │   (cầu nối giữa)    │ <────── │ (bên thứ │
│          │         │                     │         │    3)    │
└──────────┘         └─────────────────────┘         └──────────┘

App A nói "tiếng Việt"          Dịch sang              Bank B nói "tiếng Anh"
(format nội bộ)              "tiếng" của Bank B         (format riêng)
```

App A không cần biết Bank B dùng API gì, format gì, auth gì.
Integration Service lo hết — dựa trên config.

---

## Ví dụ thực tế: App A chuyển tiền qua Bank B

### BƯỚC 1: App A gửi request (format nội bộ, chuẩn của công ty)

```
POST http://integration-service/api/v1/integration/execute
Content-Type: application/json
X-Correlation-Id: txn-20240101-001
```

```json
{
  "connectorConfigId": "bank-b-transfer",
  "operation": "transfer",
  "idempotencyKey": "TXN-20240101-001",
  "data": {
    "fromAccount": "0123456789",
    "toAccount": "9876543210",
    "amount": 5000000,
    "currency": "VND",
    "description": "Thanh toán hóa đơn #123",
    "referenceId": "REF-2024-001"
  }
}
```

App A chỉ cần biết:
- `connectorConfigId`: gọi tới đâu (Bank B)
- `operation`: làm gì (transfer)
- `data`: dữ liệu theo format NỘI BỘ của công ty

---

### BƯỚC 2: Integration Service xử lý (tự động, dựa trên config)

```
Config "bank-b-transfer" nói rằng:

  Request Mapping:
    fromAccount  →  src_acct_no      (Bank B gọi khác)
    toAccount    →  dest_acct_no
    amount       →  txn_amount
    currency     →  txn_ccy
    description  →  txn_desc         + Transform: ToUpper
    referenceId  →  partner_ref_no

  Auth: ApiKey, header "X-Api-Key"
  Endpoint: https://api.bankb.vn/v1/fund-transfer
  Method: POST
```

Integration Service tự động:
1. Load config từ Config Service
2. Validate request (required fields, format)
3. Map request: format nội bộ → format Bank B
4. Gắn authentication
5. Gọi Bank B API

**Request thực tế gửi tới Bank B:**

```
POST https://api.bankb.vn/v1/fund-transfer
Content-Type: application/json
X-Api-Key: bank-b-secret-key-xxx
X-Correlation-Id: txn-20240101-001
```

```json
{
  "src_acct_no": "0123456789",
  "dest_acct_no": "9876543210",
  "txn_amount": 5000000,
  "txn_ccy": "VND",
  "txn_desc": "THANH TOÁN HÓA ĐƠN #123",
  "partner_ref_no": "REF-2024-001"
}
```

---

### BƯỚC 3: Bank B trả response (format của Bank B)

```json
{
  "result_code": "00",
  "result_msg": "Success",
  "data": {
    "bank_ref_no": "BANKB-TXN-99887766",
    "txn_date": "2024-01-01T10:30:00Z",
    "fee": 11000
  }
}
```

---

### BƯỚC 4: Integration Service map response về format nội bộ

```
Config "bank-b-transfer" nói rằng:

  Response Mapping:
    result_code       →  statusCode
    result_msg        →  statusMessage
    data.bank_ref_no  →  bankTransactionId    (dot notation)
    data.txn_date     →  transactionDate
    data.fee          →  transactionFee
```

**Response trả về cho App A (format nội bộ, chuẩn công ty):**

```json
{
  "success": true,
  "data": {
    "statusCode": "00",
    "statusMessage": "Success",
    "bankTransactionId": "BANKB-TXN-99887766",
    "transactionDate": "2024-01-01T10:30:00Z",
    "transactionFee": 11000
  },
  "correlationId": "txn-20240101-001",
  "traceId": "abc123...",
  "executionTimeMs": 450.5
}
```

App A nhận response theo format NỘI BỘ — không cần biết Bank B trả format gì.

---

## Tổng quan luồng

```
App A                    Integration Service                    Bank B
  │                            │                                  │
  │  ① Request (format nội bộ) │                                  │
  │  {fromAccount, toAccount,  │                                  │
  │   amount, description}     │                                  │
  │ ──────────────────────────>│                                  │
  │                            │                                  │
  │                            │  ② Load config                   │
  │                            │  ③ Validate request              │
  │                            │  ④ Map: nội bộ → Bank B format   │
  │                            │  ⑤ Apply auth (ApiKey)           │
  │                            │                                  │
  │                            │  ⑥ Request (format Bank B)       │
  │                            │  {src_acct_no, dest_acct_no,     │
  │                            │   txn_amount, txn_desc}          │
  │                            │ ────────────────────────────────>│
  │                            │                                  │
  │                            │  ⑦ Response (format Bank B)      │
  │                            │  {result_code, data.bank_ref_no} │
  │                            │ <────────────────────────────────│
  │                            │                                  │
  │                            │  ⑧ Map: Bank B → nội bộ format   │
  │                            │  ⑨ Cache idempotency             │
  │                            │                                  │
  │  ⑩ Response (format nội bộ)│                                  │
  │  {statusCode, statusMessage│                                  │
  │   bankTransactionId}       │                                  │
  │ <──────────────────────────│                                  │
```

---

## Khi thêm Bank C (ngân hàng mới)?

Chỉ cần tạo config mới — KHÔNG SỬA CODE:

```json
{
  "id": "bank-c-transfer",
  "connectorType": "generic-http",
  "endpoint": { "baseUrl": "https://api.bankc.vn" },
  "authentication": { "type": "BearerToken", "parameters": { "Token": "xxx" } },
  "operations": [{
    "name": "transfer",
    "path": "/api/transfer",
    "requestMappings": [
      { "source": "fromAccount", "target": "sender_account" },
      { "source": "toAccount", "target": "receiver_account" },
      { "source": "amount", "target": "transfer_amount" }
    ],
    "responseMappings": [
      { "source": "code", "target": "statusCode" },
      { "source": "transaction_id", "target": "bankTransactionId" }
    ]
  }]
}
```

App A gọi y hệt, chỉ đổi `connectorConfigId`:

```json
{
  "connectorConfigId": "bank-c-transfer",
  "operation": "transfer",
  "data": {
    "fromAccount": "0123456789",
    "toAccount": "5555666677",
    "amount": 1000000
  }
}
```

Response format vẫn giống nhau — App A không cần thay đổi gì.

---

## Tóm tắt

| Ai          | Biết gì                        | Không cần biết                    |
|-------------|--------------------------------|-----------------------------------|
| App A       | connectorConfigId + operation  | Bank B dùng API gì, auth gì      |
| Integration | Config mapping + connector     | Nghiệp vụ của App A              |
| Bank B      | API của chính nó               | App A là ai, format nội bộ gì    |

Integration Service = **translator** giữa 2 bên, cấu hình bằng config.
