# Integration Platform - Architecture Document

## 1. Kiến trúc tổng thể (Overall Architecture)

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CLIENTS                                     │
│         (Mobile App, Web App, Internal Services)                    │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      API GATEWAY (Ocelot / YARP)                    │
│  - Rate Limiting, Auth (JWT/OAuth2), Routing, Load Balancing        │
└──────┬──────────────┬───────────────┬───────────────────────────────┘
       │              │               │
       ▼              ▼               ▼
┌────────────┐ ┌─────────────┐ ┌──────────────┐
│ Integration│ │   Config    │ │   Business   │
│  Service   │ │   Service   │ │   Service    │
│            │ │             │ │              │
│ - Routing  │ │ - CRUD cfg  │ │ - Payment    │
│ - Connector│ │ - Versioning│ │ - Transfer   │
│   Loading  │ │ - Rollout   │ │ - Inquiry    │
│ - Mapping  │ │ - Cache     │ │              │
│ - Retry    │ │   (Redis)   │ │              │
└─────┬──────┘ └──────┬──────┘ └──────┬───────┘
      │               │               │
      │    ┌──────────┴────────┐      │
      │    │   Message Broker  │      │
      │    │ (RabbitMQ/Kafka)  │◄─────┘
      │    └───────────────────┘
      │
      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     CONNECTOR LAYER                                  │
│                                                                      │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │  Generic HTTP    │  │  Custom DLL      │  │  Custom DLL      │  │
│  │  Connector       │  │  Connector       │  │  Connector       │  │
│  │  (Config-driven) │  │  (Bank ABC)      │  │  (Wallet XYZ)    │  │
│  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘  │
│           │                     │                      │            │
└───────────┼─────────────────────┼──────────────────────┼────────────┘
            │                     │                      │
            ▼                     ▼                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    THIRD-PARTY SYSTEMS                               │
│     Bank APIs    |    E-Wallets    |    Internal Apps    |   ...     │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                     OBSERVABILITY                                    │
│  OpenTelemetry  |  Prometheus + Grafana  |  ELK/Seq Logging         │
└─────────────────────────────────────────────────────────────────────┘
```

## 2. Module Responsibilities

### API Gateway (`gateway/`)
- Entry point duy nhất cho tất cả request
- Authentication & Authorization (JWT/OAuth2)
- Rate limiting & throttling
- Request routing tới downstream services
- Load balancing
- SSL termination

### Integration Service (`services/integration-service/`)
- Core engine xử lý tích hợp
- Load connector (Generic HTTP hoặc Custom DLL) dựa trên config
- Thực hiện request/response mapping
- Retry, Circuit Breaker, Timeout
- Gọi third-party API thông qua connector

### Config Service (`services/config-service/`)
- CRUD cấu hình connector
- Versioning config (rollback khi cần)
- Cache config bằng Redis
- API cho admin quản lý cấu hình
- Hỗ trợ rollout (canary, blue-green)

### Business Service (`services/business-service/`)
- Xử lý nghiệp vụ cụ thể (payment, transfer, inquiry...)
- Gọi Integration Service khi cần tích hợp
- Tách biệt logic nghiệp vụ khỏi logic tích hợp

### Building Blocks (`building-blocks/`)
- Shared libraries dùng chung giữa các service
- Base classes, interfaces, utilities
- Common middleware, exception handling

### Connectors (`connectors/`)
- Chứa các custom connector DLL
- Mỗi connector implement IConnector interface
- Build độc lập, deploy dưới dạng plugin

## 3. Communication Flow

### Synchronous Flow (Request → Third-party)
```
Client → Gateway → Business Service → Integration Service
    → ConnectorFactory.Create(connectorId)
    → Load config từ Config Service (cached)
    → MappingEngine.MapRequest(config, request)
    → Connector.ExecuteAsync(mappedRequest)
    → MappingEngine.MapResponse(config, rawResponse)
    → Return to Business Service → Return to Client
```

### Asynchronous Flow (Event-driven)
```
Business Service → Publish Event to Message Broker
    → Integration Service subscribes
    → Process integration asynchronously
    → Publish result event
    → Business Service receives result
```

## 4. Deployment Architecture

```
┌─────────────────────────────────────────────┐
│              Kubernetes Cluster              │
│                                              │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐       │
│  │ Gateway │ │ Gateway │ │ Gateway │       │
│  │ Pod (1) │ │ Pod (2) │ │ Pod (n) │       │
│  └─────────┘ └─────────┘ └─────────┘       │
│                                              │
│  ┌──────────────┐  ┌──────────────┐         │
│  │ Integration  │  │ Integration  │         │
│  │ Service (1)  │  │ Service (n)  │         │
│  └──────────────┘  └──────────────┘         │
│                                              │
│  ┌──────────────┐  ┌──────────────┐         │
│  │   Config     │  │  Business    │         │
│  │  Service     │  │  Service     │         │
│  └──────────────┘  └──────────────┘         │
│                                              │
│  ┌──────────────┐  ┌──────────────┐         │
│  │   Redis      │  │  RabbitMQ    │         │
│  │   Cluster    │  │  Cluster     │         │
│  └──────────────┘  └──────────────┘         │
│                                              │
│  ┌──────────────┐  ┌──────────────┐         │
│  │  PostgreSQL  │  │  Prometheus  │         │
│  │              │  │  + Grafana   │         │
│  └──────────────┘  └──────────────┘         │
└─────────────────────────────────────────────┘
```

## 5. CI/CD Pipeline

```
Code Push → Build (.NET) → Run Tests → Build Docker Image
    → Push to Registry → Deploy to K8s (Helm)
    → Health Check → Rollback if failed
```

## 6. Security

- JWT/OAuth2 tại Gateway
- mTLS giữa các service nội bộ
- Secret management (K8s Secrets / Vault)
- Input validation tại mỗi service
- Audit logging cho mọi integration call
