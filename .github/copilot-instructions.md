# Copilot Instructions for NotificationAuditService

## Project Overview

The NotificationAuditService is a .NET 8 Web API that provides comprehensive notification and audit logging capabilities with SQLite persistence.

## Architecture

### Directory Structure
- `src/models/` - Data models (Notification, AuditLog)
- `src/services/` - Business logic layer with interfaces and implementations
- `src/data/` - Database context and EF Core configuration
- `tests/` - Unit and integration tests
- `.github/` - GitHub workflows and CI/CD configuration

### Technology Stack
- .NET 8.0
- Entity Framework Core 8.0
- SQLite Database
- Swagger/OpenAPI Documentation

## Key Services

### NotificationService
- Manages user notifications
- Supports multiple notification types (Info, Warning, Error, Success)
- Tracks read/unread status
- Supports bulk operations
- Auto-cleanup of old notifications

### AuditService
- Logs entity changes (CRUD operations)
- Tracks user context (IP, user agent, user info)
- Supports complex queries (by entity, user, date range)
- Auto-cleanup of old audit logs
- JSON serialization for change tracking

## Development Guidelines

1. **Always use async/await** for database operations
2. **Maintain interface contracts** - Keep INotificationService and IAuditService aligned
3. **Use dependency injection** - Services are registered in Program.cs
4. **Database migrations** - Use Entity Framework Core migrations for schema changes
5. **Error handling** - Always check for null/not found cases
6. **Namespace conventions** - Keep namespaces aligned with folder structure

## Common Tasks

### Adding a New Feature to Notifications
1. Add property to `Notification` model
2. Update `AuditDbContext.OnModelCreating()` if needed
3. Add method to `INotificationService` interface
4. Implement in `NotificationService`
5. Add controller endpoint

### Adding a New Audit Query
1. Add method to `IAuditService` interface
2. Implement in `AuditService`
3. Add corresponding controller endpoint
4. Test with date/entity filtering

## Testing
- Place unit tests in `tests/` directory
- Follow naming convention: `[Feature]Tests.cs`
- Use async test methods when testing async code

## API Documentation
Swagger/OpenAPI documentation is available at `/swagger` in development mode.
