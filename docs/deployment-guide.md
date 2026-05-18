# Deployment Guide — Tách riêng từng service

## Kiến trúc deploy

```
Server A (hoặc K8s Pod A)          Server B                    Server C
┌─────────────────────┐    HTTP    ┌──────────────────┐  HTTP  ┌──────────────────┐
│   Gateway :5000     │───────────>│ Integration Svc  │───────>│  Config Service  │
│                     │            │     :5001         │        │     :5002        │
└─────────────────────┘            └────────┬─────────┘        └──────────────────┘
                                            │ HTTP
                                            ▼
                                   ┌──────────────────┐
                                   │  Bank B / Bank C  │
                                   │  (third-party)    │
                                   └──────────────────┘
```

Mỗi service là 1 Docker container riêng, deploy ở đâu cũng được.
Chỉ cần đổi URL trỏ tới nhau.

## Cách 1: Docker Compose (1 máy, nhiều container)

```bash
cd infrastructure/docker
docker-compose up -d
```

## Cách 2: Deploy riêng từng server

### Server A — Integration Service
```bash
docker build -f services/integration-service/Dockerfile -t integration-service .
docker run -d -p 5001:8080 \
  -e Services__ConfigService=http://server-b:5002 \
  -e Connectors__ConfigMode=file \
  -v /path/to/configs:/app/configs \
  integration-service
```

### Server B — Config Service
```bash
docker build -f services/config-service/Dockerfile -t config-service .
docker run -d -p 5002:8080 config-service
```

### Server C — Gateway
```bash
docker build -f gateway/Dockerfile -t gateway .
docker run -d -p 5000:8080 \
  -e ReverseProxy__Clusters__integration-cluster__Destinations__d1__Address=http://server-a:5001 \
  -e ReverseProxy__Clusters__config-cluster__Destinations__d1__Address=http://server-b:5002 \
  gateway
```

## Điều duy nhất cần đổi khi tách server

Chỉ đổi URL trong environment variables:

| Service             | Biến cần đổi                        | Trỏ tới                |
|---------------------|--------------------------------------|------------------------|
| Integration Service | `Services__ConfigService`            | URL của Config Service |
| Business Service    | `Services__IntegrationService`       | URL của Integration    |
| Gateway             | `ReverseProxy__Clusters__*__Address` | URL của từng service   |

Không sửa code, không sửa config file. Chỉ đổi environment variable.
