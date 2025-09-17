# Notification Service Microservice

A comprehensive .NET 8 microservice for handling multi-channel notifications (Email, SMS, Push, In-App) with real-time WebSocket communication via SignalR, enterprise-grade features including message queuing, caching, monitoring, and containerization.

## 🚀 Features

- **Multi-Channel Notifications**: Email (SendGrid), SMS (Twilio), Push (Firebase), **In-App (SignalR)**
- **Real-time Communication**: WebSocket connections via SignalR for instant notification delivery
- **Connection Management**: Redis-backed and in-memory connection tracking with user presence
- **Persistent Notifications**: MongoDB storage for notification history and user preferences
- **Message Queuing**: RabbitMQ with exponential backoff retry mechanism
- **Caching**: Redis for template, user preference, and notification caching
- **Database**: MongoDB for notification templates, history, and in-app notifications
- **Monitoring**: Prometheus metrics, Elasticsearch logging, Grafana dashboards
- **Containerization**: Docker support with development environment
- **Architecture**: Clean layered architecture with DI and SOLID principles

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  API Layer      │    │  Worker Service │    │  SignalR Hub    │    │  External APIs  │
│  (REST API)     │    │  (Background)   │    │  (WebSocket)    │    │  SendGrid/Twilio│
└─────────┬───────┘    └─────────┬───────┘    └─────────┬───────┘    └─────────────────┘
          │                      │                      │
          ▼                      ▼                      ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           Application Layer                                         │
│                     (Business Logic & Interfaces)                                   │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│  │  Notification   │ │    Real-time    │ │   Connection    │ │   User Prefs    │  │
│  │    Service      │ │    Service      │ │   Manager       │ │    Service      │  │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘  │
└─────────────────────┬───────────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                        Infrastructure Layer                                         │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────┐ ┌─────────────────┐  │
│  │  MongoDB    │ │   Redis     │ │  RabbitMQ   │ │ Logging │ │    SignalR      │  │
│  │ Repository  │ │   Cache     │ │ Messaging   │ │ Metrics │ │  Backplane      │  │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────┘ └─────────────────┘  │
└─────────────────────┬───────────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           Domain Layer                                              │
│                     (Entities & Business Rules)                                     │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│  │   Notification  │ │  InApp          │ │   Notification  │ │     User        │  │
│  │   Template      │ │  Notification   │ │   History       │ │  Preferences    │  │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

## 🛠️ Tech Stack

### Core Technologies
- **.NET 8.0**: Latest .NET framework
- **ASP.NET Core**: Web API framework with SignalR support
- **SignalR**: Real-time WebSocket communication
- **C# 12**: Latest language features

### Data & Messaging
- **MongoDB 8.0**: Document database for notifications and preferences
- **Redis 7.4**: In-memory cache and SignalR backplane
- **RabbitMQ 3.13**: Message broker

### External Services
- **SendGrid**: Email delivery service
- **Twilio**: SMS service
- **Firebase**: Push notifications

### Monitoring & Logging
- **Serilog**: Structured logging
- **Elasticsearch 8.11**: Log aggregation
- **Prometheus**: Metrics collection
- **Grafana**: Monitoring dashboards
- **Kibana**: Log visualization

### Containerization
- **Docker**: Containerization platform
- **Docker Compose**: Multi-container orchestration

## 🚦 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/downloads)

### 1. Clone the Repository

```bash
git clone <repository-url>
cd isra-notification-service
```

### 2. Run with Docker Compose (Recommended)

```bash
# Windows
./deploy.bat

# Linux/macOS
chmod +x deploy.sh
./deploy.sh
```

This will:
- Build the .NET solution
- Create Docker images
- Start all services (API, Worker, MongoDB, Redis, RabbitMQ, Elasticsearch, etc.)
- Perform health checks

### 3. Manual Setup (Development)

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Start infrastructure services
docker-compose up -d mongodb redis rabbitmq elasticsearch

# Run API (Terminal 1)
cd src/NotificationService.Api
dotnet run

# Run Worker (Terminal 2)
cd src/NotificationService.Worker
dotnet run
```

## 📊 Service URLs

After running `deploy.bat` or `deploy.sh`, access these services:

| Service | URL | Credentials |
|---------|-----|-------------|
| **API Swagger** | http://localhost:8080/swagger | - |
| **SignalR Hub** | ws://localhost:8080/notificationHub | JWT Token |
| **API Health** | http://localhost:8080/health | - |
| **API Metrics** | http://localhost:8080/metrics | - |
| **RabbitMQ Management** | http://localhost:15672 | guest/guest |
| **MongoDB Express** | http://localhost:8082 | admin/admin123 |
| **Redis Commander** | http://localhost:8083 | - |
| **Kibana** | http://localhost:5601 | - |
| **Grafana** | http://localhost:3000 | admin/admin123 |
| **Prometheus** | http://localhost:9090 | - |

## 📚 API Documentation

### Send Notification

Send a notification request that will be processed asynchronously.

```http
POST /api/notifications
Content-Type: application/json

{
  "templateId": "welcome-email",
  "channel": "email",
  "recipient": {
    "email": "user@example.com",
    "phone": "+1234567890",
    "deviceToken": "firebase-device-token",
    "userId": "user123",
    "language": "en",
    "timeZone": "UTC"
  },
  "variables": {
    "UserName": "John Doe",
    "CompanyName": "Example Corp"
  },
  "scheduledFor": null,
  "priority": "normal"
}
```

**Response:**
```json
{
  "id": "63f8a1b2c4d5e6f7a8b9c0d1",
  "status": "queued",
  "message": "Notification queued successfully"
}
```

### Get Notification Status

Check the status of a notification.

```http
GET /api/notifications/{id}
```

**Response:**
```json
{
  "id": "63f8a1b2c4d5e6f7a8b9c0d1",
  "status": "delivered",
  "channel": "email",
  "recipientEmail": "user@example.com",
  "sentAt": "2024-01-15T10:30:00Z",
  "retryCount": 0,
  "lastError": null
}
```

### Notification Channels

| Channel | Description | Required Fields |
|---------|-------------|-----------------|
| `email` | Email notifications via SendGrid | `recipient.email` |
| `sms` | SMS notifications via Twilio | `recipient.phone` |
| `push` | Push notifications via Firebase | `recipient.deviceToken` |

### Notification Status

| Status | Description |
|--------|-------------|
| `queued` | Notification is queued for processing |
| `processing` | Notification is being processed |
| `delivered` | Notification delivered successfully |
| `failed` | Notification delivery failed (after retries) |

## 📡 SignalR Real-Time Notifications

### SignalR Hub Connection

Connect to the SignalR hub for real-time notifications:

```javascript
// JavaScript Client
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
    .withUrl('/notificationHub', {
        accessTokenFactory: () => 'your-jwt-token'
    })
    .withAutomaticReconnect()
    .build();

// Start connection
await connection.start();

// Listen for notifications
connection.on('ReceiveNotification', (notification) => {
    console.log('New notification:', notification);
    // Handle notification display
});

// Listen for unread count updates
connection.on('UnreadCountUpdate', (count) => {
    console.log('Unread count:', count);
    // Update badge/counter
});
```

### SignalR Hub Methods

| Method | Description | Parameters |
|--------|-------------|------------|
| `JoinUserGroup` | Join user-specific notification group | `userId` |
| `LeaveUserGroup` | Leave user-specific notification group | `userId` |
| `MarkAsRead` | Mark notification as read | `notificationId` |
| `GetNotifications` | Get user notifications with pagination | `userId`, `unreadOnly`, `skip`, `take` |
| `StartTyping` | Indicate user is typing | `groupId` |
| `StopTyping` | Stop typing indication | `groupId` |

### In-App Notification API Endpoints

#### Create In-App Notification
```http
POST /api/v1/inappnotifications
Content-Type: application/json
Authorization: Bearer {jwt-token}

{
  "userId": "user123",
  "title": "New Message",
  "message": "You have received a new message",
  "data": {
    "messageId": "msg456",
    "senderId": "user789"
  },
  "expiresAt": "2024-12-31T23:59:59Z"
}
```

#### Get User Notifications
```http
GET /api/v1/inappnotifications/user/{userId}?page=1&pageSize=20&unreadOnly=true
Authorization: Bearer {jwt-token}
```

#### Mark as Read
```http
PUT /api/v1/inappnotifications/{notificationId}/read
Authorization: Bearer {jwt-token}
```

#### Get Unread Count
```http
GET /api/v1/inappnotifications/user/{userId}/unread-count
Authorization: Bearer {jwt-token}
```

#### Send Real-time Notification
```http
POST /api/v1/inappnotifications/realtime/user
Content-Type: application/json
Authorization: Bearer {jwt-token}

{
  "userId": "user123",
  "title": "Instant Alert",
  "message": "This is sent in real-time",
  "data": {
    "type": "alert",
    "priority": "high"
  }
}
```

#### Broadcast to All Users
```http
POST /api/v1/inappnotifications/broadcast
Content-Type: application/json
Authorization: Bearer {jwt-token}

{
  "title": "System Maintenance",
  "message": "System will be down for maintenance",
  "data": {
    "maintenanceWindow": "2024-01-15T02:00:00Z"
  }
}
```

### SignalR Events

| Event | Description | Payload |
|-------|-------------|---------|
| `ReceiveNotification` | New notification received | `InAppNotification` object |
| `NotificationRead` | Notification marked as read | `{ notificationId, readAt }` |
| `UnreadCountUpdate` | Unread count changed | `{ userId, count }` |
| `UserOnline` | User connected | `{ userId, connectionId }` |
| `UserOffline` | User disconnected | `{ userId }` |
| `UserTyping` | User started typing | `{ userId, groupId }` |
| `UserStoppedTyping` | User stopped typing | `{ userId, groupId }` |

### SignalR Integration Example

```javascript
// Complete SignalR integration example
class NotificationManager {
    constructor(jwtToken) {
        this.connection = new HubConnectionBuilder()
            .withUrl('/notificationHub', {
                accessTokenFactory: () => jwtToken
            })
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .build();
        
        this.setupEventHandlers();
    }

    async connect() {
        try {
            await this.connection.start();
            console.log('SignalR Connected');
            
            // Join user group
            await this.connection.invoke('JoinUserGroup', this.userId);
        } catch (err) {
            console.error('SignalR Connection Error:', err);
        }
    }

    setupEventHandlers() {
        // Handle new notifications
        this.connection.on('ReceiveNotification', (notification) => {
            this.showNotification(notification);
            this.updateUnreadCount();
        });

        // Handle unread count updates
        this.connection.on('UnreadCountUpdate', (count) => {
            this.updateBadge(count);
        });

        // Handle connection events
        this.connection.onreconnected(() => {
            console.log('SignalR Reconnected');
            this.connection.invoke('JoinUserGroup', this.userId);
        });
    }

    async markAsRead(notificationId) {
        try {
            await this.connection.invoke('MarkAsRead', notificationId);
        } catch (err) {
            console.error('Failed to mark as read:', err);
        }
    }

    showNotification(notification) {
        // Display notification in UI
        if (notification.showToast) {
            this.showToast(notification.title, notification.message);
        }
        
        if (notification.playSound) {
            this.playNotificationSound();
        }
    }
}

## ⚙️ Configuration

### Environment Variables

Set these environment variables for external service integrations:

```bash
# Email (SendGrid)
Email__SendGrid__ApiKey=your-sendgrid-api-key
Email__SendGrid__FromEmail=noreply@yourcompany.com
Email__SendGrid__FromName=Your Company

# SMS (Twilio)
Sms__Twilio__AccountSid=your-twilio-account-sid
Sms__Twilio__AuthToken=your-twilio-auth-token
Sms__Twilio__FromPhoneNumber=+1234567890

# Push (Firebase)
Push__Firebase__ProjectId=your-firebase-project-id
Push__Firebase__PrivateKeyPath=/path/to/firebase-key.json

# SignalR & Redis Configuration
Redis__ConnectionString=localhost:6379
Redis__Database=0
Redis__UseRedisBackplane=true

# In-App Notifications
InAppNotifications__CacheExpiration=00:30:00
InAppNotifications__MaxRetryAttempts=3
InAppNotifications__CleanupInterval=24:00:00

# Elasticsearch (optional)
Logging__Elasticsearch__Enabled=true
Logging__Elasticsearch__Url=http://localhost:9200
```

### Connection Strings

Default connection strings (override in production):

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "NotificationService"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "RabbitMQ": {
    "ConnectionString": "amqp://guest:guest@localhost:5672/"
  }
}
```

## 🧪 Testing

### Unit Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Integration Tests

```bash
# Start test environment
docker-compose -f docker-compose.test.yml up -d

# Run integration tests
dotnet test --filter Category=Integration
```

### Manual Testing

Use the provided Postman collection or test via Swagger UI:

1. Start the services: `./deploy.bat` or `./deploy.sh`
2. Open Swagger UI: http://localhost:8080/swagger
3. Test the `/api/notifications` endpoints

## 🔍 Monitoring & Observability

### Logs

View application logs:

```bash
# API logs
docker-compose logs -f notification-api

# Worker logs
docker-compose logs -f notification-worker

# All logs
docker-compose logs -f
```

### Metrics

Access metrics and monitoring:

- **Prometheus Metrics**: http://localhost:9090
- **Grafana Dashboards**: http://localhost:3000
- **Application Metrics**: http://localhost:8080/metrics

### Health Checks

Check service health:

```bash
# API health
curl http://localhost:8080/health

# Individual service health
curl http://localhost:8080/health/mongodb
curl http://localhost:8080/health/redis
curl http://localhost:8080/health/rabbitmq
```

## 🚀 Deployment

### Docker Production

```bash
# Build production images
docker build -f src/NotificationService.Api/Dockerfile -t notification-service-api:latest .
docker build -f src/NotificationService.Worker/Dockerfile -t notification-service-worker:latest .

# Run production compose
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Kubernetes

Kubernetes manifests are available in the `k8s/` directory:

```bash
# Deploy to Kubernetes
kubectl apply -f k8s/
```

### CI/CD

The project includes GitHub Actions workflows for:
- Building and testing
- Docker image creation
- Deployment to staging/production

## 🔧 Development

### Project Structure

```
src/
├── NotificationService.Api/          # REST API endpoints
├── NotificationService.Application/  # Business logic & interfaces
├── NotificationService.Domain/       # Domain entities & rules
├── NotificationService.Infrastructure/ # External service implementations
└── NotificationService.Worker/       # Background message processing

tests/
├── NotificationService.UnitTests/    # Unit tests
└── NotificationService.IntegrationTests/ # Integration tests

scripts/
├── mongodb-init.js                   # MongoDB initialization
└── setup.sql                         # Additional setup scripts

monitoring/
├── prometheus.yml                    # Prometheus configuration
└── grafana/                          # Grafana dashboards
```

### Adding New Notification Channels

1. Create a new service implementing `INotificationService`
2. Register in `ServiceCollectionExtensions`
3. Add configuration settings
4. Update strategy selector logic
5. Add tests

### Database Migrations

MongoDB is schema-less, but for structural changes:

1. Update entity models in `NotificationService.Domain`
2. Create migration script in `scripts/`
3. Update indexes in MongoDB initialization

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

### Code Standards

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- Write unit tests for new features
- Update documentation for API changes
- Ensure Docker builds pass

## 📋 Troubleshooting

### Common Issues

**Port conflicts:**
```bash
# Check if ports are in use
netstat -an | findstr ":8080"  # Windows
lsof -i :8080                  # Linux/macOS

# Change ports in docker-compose.yml if needed
```

**MongoDB connection issues:**
```bash
# Check MongoDB logs
docker-compose logs mongodb

# Verify connection string in configuration
```

**RabbitMQ not starting:**
```bash
# Clear RabbitMQ data
docker-compose down -v
docker-compose up -d rabbitmq
```

**SignalR connection issues:**
```bash
# Check Redis connection
docker-compose logs redis

# Verify JWT token in client
# Ensure proper CORS configuration for WebSocket
```

### Performance Tuning

- Adjust RabbitMQ prefetch count for worker performance
- Configure Redis memory policies for SignalR backplane
- Monitor MongoDB query performance with proper indexing
- Scale worker instances for high throughput
- Use Redis cluster for SignalR in production
- Implement connection limits and rate limiting for SignalR

## 🚀 SignalR Module Features

### ✅ Implemented Features

- **Real-time Communication**: WebSocket connections with automatic reconnection
- **User Management**: Join/leave user-specific notification groups
- **Connection Tracking**: Redis-backed and in-memory connection management
- **Authentication**: JWT-based authentication for SignalR connections
- **Notification Delivery**: Instant push notifications to connected users
- **Read Status**: Real-time read/unread status synchronization
- **User Presence**: Online/offline status tracking
- **Broadcasting**: Send notifications to all connected users
- **Typing Indicators**: Real-time typing status for group communications
- **Caching**: Redis caching for notifications and user preferences
- **Persistence**: MongoDB storage for notification history
- **Error Handling**: Comprehensive error handling and logging
- **Scalability**: Redis backplane for multi-instance deployments

### 🎯 Usage Scenarios

- **Real-time Alerts**: System alerts, security notifications
- **Chat Applications**: Instant messaging with typing indicators
- **Live Updates**: Status changes, progress updates
- **User Engagement**: Welcome messages, feature announcements
- **Collaborative Features**: Real-time collaboration notifications
- **Push Notifications**: Alternative to mobile push notifications

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [MongoDB .NET Driver](https://mongodb.github.io/mongo-csharp-driver/)
- [RabbitMQ .NET Client](https://rabbitmq.github.io/rabbitmq-dotnet-client/)
- [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)
- [Serilog Documentation](https://serilog.net/)

---

**📞 Support**: For questions or issues, please open a GitHub issue or contact the development team.

**🔄 Last Updated**: September 2025 - Added SignalR Real-time Notification Module