# Hướng dẫn chạy Integration Platform (Microservices)

## Cấu trúc hiện tại

```
integration-platform/
├── gateway/                    ← API Gateway (YARP) — port 5000
├── config-service/             ← Config Service — port 5002
├── integration-service/        ← Integration Service — port 5001
├── business-service/           ← Business Service — port 5003
├── websocket-service/          ← WebSocket Service — port 5004
├── admin-ui/                   ← Frontend React — port 3000
├── building-blocks/            ← Shared libraries
├── connectors/                 ← Custom DLL connectors
├── database/                   ← SQL migrations
└── infrastructure/             ← Docker, K8s, Observability
```

Mỗi service là 1 solution độc lập, có `.sln` riêng, chạy riêng, deploy riêng.

---

## Bước 1: Triển khai SQL Server

### Cách A: Docker (khuyến nghị cho dev)

```bash
docker run -d \
  --name ip-sqlserver \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=Dev@Password123!" \
  -e "MSSQL_PID=Developer" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

Đợi ~30 giây, kiểm tra:
```bash
docker logs ip-sqlserver | tail -5
# Phải thấy: "SQL Server is now ready for client connections"
```

### Cách B: SQL Server local (đã cài sẵn)

Nếu đã có SQL Server trên máy (SSMS, SQL Express...), dùng connection string:
```
Server=localhost;Database=master;User Id=sa;Password=YourPassword;TrustServerCertificate=True
```

### Cách C: Docker Compose (tất cả infrastructure)

```bash
cd infrastructure/docker
docker compose -f docker-compose.infra.yml up -d
```

---

## Bước 2: Tạo Database

Kết nối SQL Server bằng SSMS / Azure Data Studio / DBeaver:
- Server: `localhost,1433`
- User: `sa`
- Password: `Dev@Password123!`

Chạy:
```sql
-- Tạo DB cho mỗi service (mỗi service có DB riêng)
CREATE DATABASE ConfigServiceDb;
CREATE DATABASE IntegrationServiceDb;
CREATE DATABASE BusinessServiceDb;
GO
```

Sau đó chạy migration:
```sql
USE ConfigServiceDb;
GO
-- Chạy file: database/migrations/001_InitialSchema.sql
```

Hoặc dùng EF Core migrations (nếu đã setup):
```bash
cd config-service
dotnet ef database update --project ConfigService.Infrastructure --startup-project ConfigService.Api
```

---

## Bước 3: Chạy từng service

Mỗi service chạy trong 1 terminal riêng:

```bash
# Terminal 1: Config Service (port 5002)
cd config-service/ConfigService.Api
dotnet run

# Terminal 2: Integration Service (port 5001)
cd integration-service/IntegrationService.Api
dotnet run

# Terminal 3: Business Service (port 5003)
cd business-service/BusinessService.Api
dotnet run

# Terminal 4: WebSocket Service (port 5004) — cần RabbitMQ
cd websocket-service/WebSocketService.Api
dotnet run

# Terminal 5: Gateway (port 5000) — optional
cd gateway/Gateway.Api
dotnet run

# Terminal 6: Admin UI (port 3000)
cd admin-ui
npm run dev
```

### Thứ tự khởi động:
1. SQL Server (Docker hoặc local)
2. Redis (nếu dùng) + RabbitMQ (nếu dùng WebSocket)
3. Config Service
4. Integration Service
5. Business Service / WebSocket Service
6. Gateway
7. Admin UI

---

## Bước 4: Kiểm tra

| Service | URL | Swagger |
|---------|-----|---------|
| Config Service | http://localhost:5002/health | http://localhost:5002/swagger |
| Integration Service | http://localhost:5001/health | http://localhost:5001/swagger |
| Business Service | http://localhost:5003/health | http://localhost:5003/swagger |
| WebSocket Service | http://localhost:5004/health | — |
| Gateway | http://localhost:5000/health | — |
| Admin UI | http://localhost:3000 | — |

---

## Bước 5: Test API

```bash
# Test Config Service
curl http://localhost:5002/api/v1/integrations

# Test Integration Service (sync)
curl -X POST http://localhost:5001/api/v1/integration/execute \
  -H "Content-Type: application/json" \
  -d '{"connectorConfigId":"bank-b-transfer","operation":"transfer","data":{"fromAccount":"012","toAccount":"987","amount":5000000,"referenceId":"REF-001"}}'
```

---

## Connection Strings

Mỗi service có `appsettings.json` riêng:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost,1433;Database=ConfigServiceDb;User Id=sa;Password=Dev@Password123!;TrustServerCertificate=True;MultipleActiveResultSets=True",
    "Redis": "localhost:6380"
  }
}
```

Đổi `Database=ConfigServiceDb` thành tên DB tương ứng cho mỗi service.

---

## Lưu ý quan trọng

1. **Mỗi service có DB riêng** — không share DB giữa các services
2. **Services giao tiếp qua HTTP** — Integration Service gọi Config Service qua HTTP
3. **Gateway là optional** — có thể gọi trực tiếp từng service khi dev
4. **RabbitMQ chỉ cần khi dùng async** — nếu chỉ test sync thì không cần
5. **Redis chỉ cần khi dùng cache** — fallback InMemory khi không có Redis

---

## Troubleshooting

| Lỗi | Nguyên nhân | Fix |
|-----|-------------|-----|
| Connection refused 1433 | SQL Server chưa chạy | `docker start ip-sqlserver` |
| Port already allocated | Port đã bị chiếm | Đổi port trong docker hoặc tắt process cũ |
| Config not found | Config Service chưa chạy | Khởi động Config Service trước |
| Build failed (file locked) | Service đang chạy | Tắt service trước khi build |
| Black screen (Admin UI) | Import lỗi hoặc API crash | Check browser console (F12) |
