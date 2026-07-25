# Notification & Audit Service API

A comprehensive .NET 8 Web API for managing notifications and audit logging with SQLite persistence.

## Documentation

| Document | What it covers |
|----------|----------------|
| [README.md](README.md) | This file — features, API, schema, and how to run the service. |
| [PR_DESCRIPTION.md](docs/PR_DESCRIPTION.md) | Change summary, AI-tool disclosure, service integration, testing, risks, self-review checklist. |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Service relationship & integration contract, layered data flow, and key design decisions. |
| [SPEC.md](docs/SPEC.md) | The ordered prompt chain — feature, technique, and rationale per prompt, plus post-generation corrections. |
| [PROMPTS.md](docs/PROMPTS.md) | Narrative log of how AI pair-programming built the service and where human judgment was needed. |
| [PROMPT_ENGINEERING.md](docs/PROMPT_ENGINEERING.md) | Per-prompt table: Copilot mode (Ask/Edit/Agent), features used, technique applied, and rationale. |
| [FEATURE_USAGE_LOG.md](docs/FEATURE_USAGE_LOG.md) | Which Copilot feature was used where, why that feature over another, and what happened. |
| [TOOL_STRATEGY.md](docs/TOOL_STRATEGY.md) | Which AI features fit which tasks, accept-vs-override heuristics, guardrails, and risks. |
| [REVIEW.md](docs/REVIEW.md) | Structured code review of the Project service (found the cross-tenant IDOR). |
| [LIMITATIONS.md](docs/LIMITATIONS.md) | Real cases where Copilot output was wrong/incomplete — detection, fix, and lessons. |
| [IMPACT_ANALYSIS.md](docs/IMPACT_ANALYSIS.md) | Impact of capturing caller IP addresses — affected components, compliance risks, rollout sequencing. |

## Features

### Notification Service
- Create notifications for users
- Retrieve notifications (filtered by user, read status)
- Mark notifications as read (individually or in bulk)
- Delete notifications
- Automatic cleanup of old notifications
- Support for notification types (Info, Warning, Error, Success)
- Link notifications to related entities

### Audit Service
- Log entity changes (Create, Update, Delete operations)
- Track user actions with context (IP address, user agent)
- Query audit logs by:
  - Entity name and ID
  - Action type
  - User ID
  - Date range
- Automatic cleanup of old audit logs
- JSON serialization of old and new values for change tracking

## Database Schema

### AuditLogs Table
| Column | Type | Description |
|--------|------|-------------|
| Id | INTEGER | Primary Key |
| EntityName | TEXT | Name of the entity being audited |
| EntityId | INTEGER | ID of the entity |
| Action | TEXT | Operation type (Create, Update, Delete) |
| OldValues | TEXT | JSON serialized old values |
| NewValues | TEXT | JSON serialized new values |
| UserId | TEXT | ID of the user performing the action |
| UserName | TEXT | Name of the user |
| Timestamp | DATETIME | When the change occurred |
| IpAddress | TEXT | IP address of the requester |
| UserAgent | TEXT | Browser/client user agent |

**Indexes:**
- EntityName
- (EntityName, EntityId)
- Timestamp

### Notifications Table
| Column | Type | Description |
|--------|------|-------------|
| Id | INTEGER | Primary Key |
| Title | TEXT | Notification title |
| Message | TEXT | Notification message |
| Type | TEXT | Notification type (Info, Warning, Error, Success) |
| RecipientId | TEXT | ID of the recipient user |
| RecipientEmail | TEXT | Email of the recipient |
| IsRead | BOOLEAN | Whether the notification has been read |
| CreatedAt | DATETIME | When the notification was created |
| ReadAt | DATETIME | When the notification was marked as read |
| RelatedEntityType | TEXT | Type of related entity |
| RelatedEntityId | INTEGER | ID of the related entity |

**Indexes:**
- RecipientId
- CreatedAt
- (RecipientId, IsRead)

## API Endpoints

### Notifications

#### Create Notification
```
POST /api/notifications
Content-Type: application/json

{
  "title": "New Order",
  "message": "Your order #123 has been confirmed",
  "type": "Info",
  "recipientId": "user123",
  "recipientEmail": "user@example.com",
  "relatedEntityType": "Order",
  "relatedEntityId": 123
}
```

#### Get Notification
```
GET /api/notifications/{id}
```

#### Get User Notifications
```
GET /api/notifications/user/{userId}?isRead=false
```

#### Mark as Read
```
PUT /api/notifications/{id}/read
```

#### Mark All as Read
```
PUT /api/notifications/user/{userId}/read-all
```

#### Delete Notification
```
DELETE /api/notifications/{id}
```

#### Delete Old Notifications
```
DELETE /api/notifications/cleanup/old?daysOld=30
```

### Audit Logs

#### Log Change
```
POST /api/audit
Content-Type: application/json

{
  "entityName": "User",
  "entityId": 1,
  "action": "Update",
  "oldValues": "{\"email\":\"old@example.com\"}",
  "newValues": "{\"email\":\"new@example.com\"}",
  "userId": "admin1",
  "userName": "Admin User",
  "ipAddress": "192.168.1.1",
  "userAgent": "Mozilla/5.0..."
}
```

#### Get Audit Log
```
GET /api/audit/{id}
```

#### Get Entity Audit Logs
```
GET /api/audit/entity/{entityName}?entityId=1&action=Update&days=30
```

#### Get User Audit Logs
```
GET /api/audit/user/{userId}?days=30
```

#### Get Audit Logs by Date Range
```
GET /api/audit/range?startDate=2024-01-01&endDate=2024-12-31
```

#### Delete Old Audit Logs
```
DELETE /api/audit/cleanup/old?daysOld=90
```

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=audit.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## Running the Application

```bash
# Restore packages
dotnet restore

# Apply migrations and create database
dotnet ef database update

# Run the application
dotnet run

# Access Swagger UI
http://localhost:5000/swagger
```

## Dependencies

- .NET 8.0
- Entity Framework Core 8.0
- SQLite
- Swashbuckle (Swagger/OpenAPI)

## License

MIT
