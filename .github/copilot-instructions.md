# GitHub Copilot Instructions

This document provides comprehensive guidelines for GitHub Copilot to maintain consistency, quality, and security across the Kannan2022 repository ecosystem.

## 1. Technology Stack Declaration

### Core Technologies
- **Runtime:** .NET 8.0 (LTS)
- **Database:** SQLite (for embedded/small-scale), SQL Server (for production)
- **ORM:** Entity Framework Core 8.0.0
- **API Framework:** ASP.NET Core 8.0
- **API Documentation:** Swashbuckle (Swagger/OpenAPI 3.0)

### Package Dependencies
- **Microsoft.EntityFrameworkCore** - v8.0.0+
- **Microsoft.EntityFrameworkCore.Sqlite** - v8.0.0+
- **Microsoft.EntityFrameworkCore.Tools** - v8.0.0+ (build-time only)
- **Microsoft.EntityFrameworkCore.SqlServer** - v8.0.0+ (for SQL Server support)
- **Swashbuckle.AspNetCore** - v6.4.0+
- **Microsoft.AspNetCore.Cors** - Built-in
- **System.Reflection** - Built-in

### Development Tools
- **Build System:** .NET CLI (dotnet)
- **Version Control:** Git
- **CI/CD:** GitHub Actions (when applicable)
- **Testing Framework:** xUnit.net (preferred) or MSTest
- **Code Analysis:** StyleCop Analyzers (SA1600+ rules)

### Supported Platforms
- Windows 10/11+
- Linux (Ubuntu 20.04+)
- macOS 12.0+

---

## 2. Architecture Conventions

### Project Structure
```
ProjectName/
├── Models/                 # Entity models and DTOs
│   ├── AuditLog.cs
│   ├── Notification.cs
│   └── [Domain entities]
├── Services/               # Business logic and interfaces
│   ├── INotificationService.cs
│   ├── NotificationService.cs
│   ├── IAuditService.cs
│   └── AuditService.cs
├── Controllers/            # API endpoints
│   ├── NotificationsController.cs
│   ├── AuditController.cs
│   └── [Domain controllers]
├── Data/                   # Data access layer
│   ├── AuditDbContext.cs
│   └── [Database contexts]
├── Migrations/             # EF Core migrations (auto-generated)
├── Database/               # Database schema scripts
│   └── InitialCreate.sql
├── appsettings.json        # Configuration
├── Program.cs              # Startup and DI configuration
├��─ [ProjectName].csproj    # Project file
├── .gitignore
└── README.md
```

### Architectural Patterns
1. **Layered Architecture**: Models → Services → Controllers
2. **Dependency Injection**: Use constructor injection for all services
3. **Repository Pattern**: Services act as repositories (encapsulate data access)
4. **Async/Await First**: All I/O operations must be async
5. **Interface Segregation**: Define interfaces for all services
6. **Domain-Driven Design**: Organize by domain entities

### Service Layer Pattern
```csharp
// Always define interface first
public interface IMyService 
{
    Task<Result> PerformOperationAsync(Input input);
}

// Implement with dependency injection
public class MyService : IMyService
{
    private readonly ILogger<MyService> _logger;
    private readonly DbContext _context;

    public MyService(ILogger<MyService> logger, DbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<Result> PerformOperationAsync(Input input)
    {
        // Implementation
    }
}
```

### Dependency Injection Registration
In `Program.cs`, register services following this pattern:
```csharp
// Data layer
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlite(connectionString));

// Service layer
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// API layer
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => { /* ... */ });
```

---

## 3. Coding Standards

### Naming Conventions

#### Classes and Records
- **PascalCase** for class names
- **Singular noun** form
- **Suffix conventions:**
  - `Service` - Business logic classes
  - `Controller` - API endpoint classes
  - `Context` - DbContext classes
  - `Dto` - Data transfer objects
  - `Request` - API request models
  - `Response` - API response models

```csharp
// ✅ Correct
public class NotificationService : INotificationService { }
public class AuditController : ControllerBase { }
public class AuditDbContext : DbContext { }
public class CreateNotificationRequest { }
public class NotificationResponse { }

// ❌ Incorrect
public class notification_service { }
public class NotificationSvc { }
public class AuditCtrl { }
```

#### Interfaces
- **PascalCase** with **I prefix**
- Start with verb or adjective when describing behavior

```csharp
// ✅ Correct
public interface INotificationService { }
public interface IRepository { }
public interface IReadable { }

// ❌ Incorrect
public interface NotificationService { }
public interface notification_service { }
public interface Readable { }
```

#### Methods and Properties
- **PascalCase** for public methods and properties
- **camelCase** for private fields and local variables
- **Async method suffix:** `Async` for all async methods

```csharp
// ✅ Correct
public async Task<Notification> CreateNotificationAsync(string title)
public int UnreadCount { get; set; }
private DateTime _createdAt;
private string _userId;

// ❌ Incorrect
public async Task<Notification> create_notification(string title)
public int unread_count { get; set; }
public DateTime CreatedAt;
public string UserId;
```

#### Database Columns
- **Match C# property names** (Entity Framework will handle mapping)
- Use **snake_case in migration comments** for clarity
- **PascalCase in code**

```csharp
// ✅ Correct
public string EntityName { get; set; }    // Maps to EntityName in DB
public DateTime Timestamp { get; set; }   // Maps to Timestamp in DB
public int EntityId { get; set; }         // Maps to EntityId in DB
```

#### Constants and Enums
- **UPPER_SNAKE_CASE** for constants
- **PascalCase** for enum values

```csharp
// ✅ Correct
private const int MAX_RETRY_ATTEMPTS = 3;
private const string DEFAULT_CONNECTION_STRING = "DefaultConnection";

public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success
}

// ❌ Incorrect
private const int max_retry_attempts = 3;
private const int MaxRetryAttempts = 3;
```

### Type Annotations

#### Explicit Type Declarations
- Always declare return types explicitly (no inferred types for public APIs)
- Use **nullable reference types** (enabled by default in .NET 8)
- Mark nullable properties with `?`

```csharp
// ✅ Correct
public class Notification
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }           // Can be null
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }              // Can be null
}

public async Task<Notification?> GetNotificationAsync(int id)
{
    // Returns Notification or null
}

// ❌ Incorrect
public class Notification
{
    public int Id;
    public string Title;                               // Should be required or nullable
    public DateTime? CreatedAt;                        // Should not be nullable
}

public async Task GetNotificationAsync(int id)         // Missing return type
```

#### Type Inference
- Use `var` for local variables only when type is obvious
- Avoid `var` for ambiguous types

```csharp
// ✅ Correct
var notification = await _context.Notifications.FirstOrDefaultAsync();
var count = 5;
var timestamp = DateTime.UtcNow;
IEnumerable<Notification> notifications = await GetNotificationsAsync();

// ❌ Incorrect
INotificationService service = new NotificationService(_context);    // Unnecessary
var service = _context.Notifications.Where(n => n.IsRead);          // Ambiguous
```

### Logging Standards

#### Logging Levels
- **Critical:** Unrecoverable errors requiring immediate action
- **Error:** Recoverable errors (validation failures, missing data)
- **Warning:** Potentially problematic situations
- **Information:** Important milestones (API calls, user actions)
- **Debug:** Detailed diagnostic information
- **Trace:** Most verbose level (rarely used)

#### Logging Format
- Use **structured logging** with named parameters
- Include correlation IDs for request tracing
- Never log sensitive data (passwords, tokens, PII)

```csharp
// ✅ Correct
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public async Task<Notification> CreateNotificationAsync(string title, string message, string type, string recipientId)
    {
        _logger.LogInformation("Creating notification for user {UserId} with type {Type}", 
            recipientId, type);
        
        try
        {
            var notification = new Notification 
            { 
                Title = title, 
                Message = message, 
                Type = type, 
                RecipientId = recipientId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Notification created successfully with Id {NotificationId}", 
                notification.Id);
            
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}", recipientId);
            throw;
        }
    }
}

// ❌ Incorrect
public async Task<Notification> CreateNotificationAsync(string title, string message, string type, string recipientId)
{
    Console.WriteLine("Creating notification");  // Don't use Console
    
    var notification = new Notification { Title = title, Message = message, Type = type, RecipientId = recipientId };
    _context.Notifications.Add(notification);
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Notification created with password: {Password}", password);  // Never log sensitive data
    
    return notification;
}
```

#### Logging in Controllers
```csharp
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, 
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Notification>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        _logger.LogInformation("Incoming POST request to create notification for user {UserId}", 
            request.RecipientId);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for notification creation");
            return BadRequest(ModelState);
        }

        try
        {
            var notification = await _notificationService.CreateNotificationAsync(
                request.Title,
                request.Message,
                request.Type,
                request.RecipientId,
                request.RecipientEmail,
                request.RelatedEntityType,
                request.RelatedEntityId
            );

            _logger.LogInformation("Notification {NotificationId} created and returned successfully", 
                notification.Id);
            
            return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating notification for user {UserId}", 
                request.RecipientId);
            return StatusCode(500, "An internal error occurred");
        }
    }
}
```

---

## 4. Security Rules

### Authentication & Authorization
- **Always use HTTPS** in production (enforce with `app.UseHttpsRedirection()`)
- **Validate all inputs** at controller boundary
- **Use Authorize attributes** for protected endpoints
- **Implement role-based access control (RBAC)** where applicable

```csharp
// ✅ Correct
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    [HttpGet("entity/{entityName}")]
    [Authorize(Roles = "Admin,Auditor")]      // Restrict by role
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogs(string entityName)
    {
        // Only authorized users can access
    }
}

// ❌ Incorrect
public class AuditController : ControllerBase
{
    [HttpGet("entity/{entityName}")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogs(string entityName)
    {
        // No authorization check - anyone can access
    }
}
```

### Input Validation
- **Validate all user inputs** immediately upon receipt
- **Use Model Validation attributes** for declarative validation
- **Implement custom validation** for complex rules
- **Never trust client-side validation**

```csharp
// ✅ Correct
public class CreateNotificationRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, MinimumLength = 3, 
        ErrorMessage = "Title must be between 3 and 255 characters")]
    public required string Title { get; set; }

    [Required(ErrorMessage = "Message is required")]
    [StringLength(2000, MinimumLength = 5,
        ErrorMessage = "Message must be between 5 and 2000 characters")]
    public required string Message { get; set; }

    [Required(ErrorMessage = "Type is required")]
    [RegularExpression("^(Info|Warning|Error|Success)$",
        ErrorMessage = "Invalid notification type")]
    public required string Type { get; set; }

    [Required(ErrorMessage = "RecipientId is required")]
    [RegularExpression("^[a-zA-Z0-9_-]{3,50}$",
        ErrorMessage = "Invalid recipient ID format")]
    public required string RecipientId { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? RecipientEmail { get; set; }
}

// ❌ Incorrect
public class CreateNotificationRequest
{
    public string Title { get; set; }          // No validation
    public string Message { get; set; }        // No validation
    public string Type { get; set; }           // No validation
    public string RecipientId { get; set; }    // No validation
}
```

### Data Protection
- **Never store passwords** in plain text (use hashing with BCrypt/PBKDF2)
- **Never log sensitive data** (passwords, tokens, credit cards, PII)
- **Encrypt sensitive data at rest** (connection strings, API keys)
- **Use environment variables** for secrets, never commit them
- **Implement SQL parameter binding** (EF Core does this automatically)

```csharp
// ✅ Correct - Using environment variables
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Connection string not found");

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlite(connectionString));

// ✅ Correct - Never logging sensitive data
_logger.LogInformation("User {UserId} logged in successfully", userId);
// NOT: _logger.LogInformation("User {UserId} logged in with password {Password}", userId, password);

// ✅ Correct - EF Core uses parameter binding automatically
var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
// This is safe - EF Core uses parameterized queries

// ❌ Incorrect - String concatenation (SQL injection risk)
var query = $"SELECT * FROM Users WHERE Email = '{email}'";

// ❌ Incorrect - Hardcoded secrets
var apiKey = "sk_live_abc123xyz789";
var connectionString = "Server=prod.example.com;Password=MyPassword123";
```

### CORS Policy
- **Define explicit CORS policies** for allowed origins
- **Use the most restrictive policy** necessary
- **Avoid `AllowAnyOrigin` in production** (security risk)

```csharp
// ✅ Correct - Restrictive CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictedCors", policy =>
    {
        policy.WithOrigins(
                "https://app.example.com",
                "https://admin.example.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Total-Count", "X-Page-Count");
    });
});

app.UseCors("RestrictedCors");

// ❌ Incorrect - Overly permissive in production
options.AddPolicy("AllowAll", policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});
```

### Error Handling
- **Never expose stack traces** to clients
- **Log full errors server-side** for debugging
- **Return generic error messages** to clients
- **Implement global exception handling**

```csharp
// ✅ Correct
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Notification>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Validation failed", details = ModelState });

            var notification = await _notificationService.CreateNotificationAsync(
                request.Title, request.Message, request.Type, request.RecipientId);

            return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument provided");
            return BadRequest(new { error = "Invalid input provided" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CreateNotification");
            return StatusCode(500, new { error = "An internal error occurred" });
        }
    }
}

// ❌ Incorrect - Exposing stack trace
catch (Exception ex)
{
    return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
}
```

### Rate Limiting
- Implement rate limiting for sensitive endpoints
- Use standard HTTP status codes (429 Too Many Requests)

```csharp
// ✅ Correct approach (using custom middleware or external package)
[HttpPost]
[Authorize]
[ProducesResponseType(429)]
public async Task<ActionResult<Notification>> CreateNotification([FromBody] CreateNotificationRequest request)
{
    // Rate limiting handled by middleware or attribute
}
```

---

## 5. Testing Expectations

### Test Framework
- **Primary:** xUnit.net with Moq for mocking
- **Alternative:** MSTest if xUnit not available
- **Coverage Target:** Minimum 80% code coverage
- **Test Pattern:** Arrange-Act-Assert (AAA)

### Unit Test Structure
```csharp
using Xunit;
using Moq;
using NotificationAuditService.Services;
using NotificationAuditService.Data;
using NotificationAuditService.Models;

namespace NotificationAuditService.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<AuditDbContext> _mockContext;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _mockContext = new Mock<AuditDbContext>();
            _service = new NotificationService(_mockContext.Object);
        }

        // ✅ Correct test naming and structure
        [Fact]
        public async Task CreateNotificationAsync_WithValidInput_ReturnsNotificationAndSavesToDatabase()
        {
            // Arrange
            var title = "Test Notification";
            var message = "Test message";
            var type = "Info";
            var recipientId = "user123";

            var mockDbSet = new Mock<DbSet<Notification>>();
            _mockContext.Setup(c => c.Notifications).Returns(mockDbSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateNotificationAsync(title, message, type, recipientId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(title, result.Title);
            Assert.Equal(message, result.Message);
            Assert.Equal(type, result.Type);
            Assert.Equal(recipientId, result.RecipientId);
            Assert.False(result.IsRead);
            
            mockDbSet.Verify(d => d.Add(It.IsAny<Notification>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("", "message", "Info", "user123")]
        [InlineData("title", "", "Info", "user123")]
        [InlineData("title", "message", "", "user123")]
        [InlineData("title", "message", "Info", "")]
        public async Task CreateNotificationAsync_WithInvalidInput_ThrowsArgumentException(
            string title, string message, string type, string recipientId)
        {
            // Arrange & Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateNotificationAsync(title, message, type, recipientId));
        }

        [Fact]
        public async Task GetNotificationsForUserAsync_WithValidUserId_ReturnsNotifications()
        {
            // Arrange
            var userId = "user123";
            var notifications = new List<Notification>
            {
                new() { Id = 1, RecipientId = userId, Title = "Notification 1", IsRead = false },
                new() { Id = 2, RecipientId = userId, Title = "Notification 2", IsRead = true }
            };

            var mockDbSet = new Mock<DbSet<Notification>>();
            mockDbSet.Setup(d => d.Where(It.IsAny<Expression<Func<Notification, bool>>>()))
                .Returns(notifications.AsQueryable().Where(n => n.RecipientId == userId));

            _mockContext.Setup(c => c.Notifications).Returns(mockDbSet.Object);

            // Act
            var result = await _service.GetNotificationsForUserAsync(userId);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count());
        }
    }
}
```

### Integration Test Structure
```csharp
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using NotificationAuditService;

namespace NotificationAuditService.Tests.Integration
{
    public class NotificationApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public NotificationApiTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task POST_CreateNotification_WithValidRequest_ReturnsCreatedStatusAndNotification()
        {
            // Arrange
            var request = new
            {
                title = "Integration Test Notification",
                message = "This is a test message",
                type = "Info",
                recipientId = "test-user",
                recipientEmail = "test@example.com"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/notifications", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var notification = JsonSerializer.Deserialize<Notification>(responseContent);
            Assert.NotNull(notification);
            Assert.Equal(request["title"], notification.Title);
        }

        [Fact]
        public async Task GET_GetUserNotifications_WithValidUserId_ReturnsOkStatusAndNotifications()
        {
            // Arrange
            var userId = "test-user";

            // Act
            var response = await _client.GetAsync($"/api/notifications/user/{userId}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
    }
}
```

### Test Naming Conventions
- **Format:** `MethodName_Condition_ExpectedResult`
- **Readable:** Test names should describe behavior, not implementation

```csharp
// ✅ Correct
[Fact]
public async Task CreateNotificationAsync_WithValidInput_ReturnsNotification() { }

[Theory]
[InlineData("")]
public async Task CreateNotificationAsync_WithEmptyTitle_ThrowsArgumentException(string title) { }

// ❌ Incorrect
[Fact]
public async Task Test1() { }

[Fact]
public async Task CreateNotification() { }

[Fact]
public async Task ShouldCreateNotification() { }
```

### Mock and Stub Usage
- **Mock:** Objects with expectations and verifications
- **Stub:** Objects providing predetermined responses
- **Always verify critical interactions**

```csharp
// ✅ Correct - Using Moq appropriately
[Fact]
public async Task CreateNotificationAsync_CallsSaveChangesAsync()
{
    // Arrange
    var mockContext = new Mock<AuditDbContext>();
    mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    var service = new NotificationService(mockContext.Object);

    // Act
    await service.CreateNotificationAsync("Title", "Message", "Info", "user123");

    // Assert - Verify the mock was called
    mockContext.Verify(
        c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
        Times.Once,
        "SaveChangesAsync should be called exactly once"
    );
}
```

### Test Data Builders
Use builder pattern for complex test objects:

```csharp
// ✅ Correct - Using builder for test data
public class NotificationBuilder
{
    private string _title = "Default Title";
    private string _message = "Default Message";
    private string _type = "Info";
    private string _recipientId = "user123";
    private bool _isRead = false;

    public NotificationBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public NotificationBuilder WithRecipientId(string recipientId)
    {
        _recipientId = recipientId;
        return this;
    }

    public NotificationBuilder AsRead()
    {
        _isRead = true;
        return this;
    }

    public Notification Build()
    {
        return new Notification
        {
            Title = _title,
            Message = _message,
            Type = _type,
            RecipientId = _recipientId,
            IsRead = _isRead,
            CreatedAt = DateTime.UtcNow
        };
    }
}

// Usage in tests
[Fact]
public void NotificationBuilder_CreatesValidNotification()
{
    var notification = new NotificationBuilder()
        .WithTitle("Custom Title")
        .WithRecipientId("custom-user")
        .AsRead()
        .Build();

    Assert.Equal("Custom Title", notification.Title);
    Assert.Equal("custom-user", notification.RecipientId);
    Assert.True(notification.IsRead);
}
```

### Code Coverage
- **Target:** Minimum 80% overall coverage
- **Priority:** Cover critical business logic first
- **Exclude:** Auto-generated code, infrastructure code
- **Tools:** Coverlet with OpenCover format

```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# View coverage in reports/ directory
```

### Test Isolation
- Each test must be independent
- Use setup/teardown for common operations
- Clean database state between tests

```csharp
// ✅ Correct - Isolated tests
public class NotificationServiceTests : IDisposable
{
    private readonly AuditDbContext _context;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        // Setup
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        _context = new AuditDbContext(options);
        _service = new NotificationService(_context);
    }

    [Fact]
    public async Task Test_Isolated()
    {
        // Test implementation
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

---

## 6. Additional Guidelines

### Code Organization
- Keep files focused (single responsibility)
- Max 300 lines per file (consider refactoring if exceeded)
- Related functionality in same namespace
- Use meaningful namespaces (e.g., `NotificationAuditService.Services`)

### Documentation
- Write XML documentation for public APIs
- Include parameter descriptions
- Document exceptions and return values

```csharp
// ✅ Correct
/// <summary>
/// Creates a new notification for a user.
/// </summary>
/// <param name="title">The notification title (3-255 characters)</param>
/// <param name="message">The notification message (5-2000 characters)</param>
/// <param name="type">Notification type: Info, Warning, Error, or Success</param>
/// <param name="recipientId">The ID of the recipient user</param>
/// <param name="recipientEmail">Optional email address for the recipient</param>
/// <param name="relatedEntityType">Optional type of related entity</param>
/// <param name="relatedEntityId">Optional ID of related entity</param>
/// <returns>The created notification with assigned ID</returns>
/// <exception cref="ArgumentException">Thrown when required parameters are null or empty</exception>
/// <exception cref="DbUpdateException">Thrown when database save fails</exception>
public async Task<Notification> CreateNotificationAsync(
    string title,
    string message,
    string type,
    string recipientId,
    string? recipientEmail = null,
    string? relatedEntityType = null,
    int? relatedEntityId = null)
{
    // Implementation
}
```

### Performance Considerations
- Use `.AsNoTracking()` for read-only queries
- Implement pagination for large datasets
- Use indexes on frequently queried columns
- Avoid N+1 queries with `.Include()` when needed

```csharp
// ✅ Correct - Efficient query
public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string entityName, int? days = null)
{
    var query = _context.AuditLogs
        .AsNoTracking()                      // Read-only
        .Where(a => a.EntityName == entityName);

    if (days.HasValue)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days.Value);
        query = query.Where(a => a.Timestamp >= cutoffDate);
    }

    return await query
        .OrderByDescending(a => a.Timestamp)
        .Take(1000)                          // Pagination
        .ToListAsync();
}
```

### Git Commit Standards
- Use conventional commits: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`
- Include issue/PR reference when applicable
- Keep commits atomic and focused

```
feat: Add notification service with CRUD operations
fix: Correct date filtering in audit log queries
docs: Add copilot instructions for architecture
test: Add unit tests for notification service
chore: Update dependencies to latest versions
```

---

## 7. Checklist for Copilot Code Generation

Before generating or modifying code, ensure:

- [ ] Code follows naming conventions (PascalCase, camelCase, UPPER_SNAKE_CASE)
- [ ] All public methods have XML documentation
- [ ] Services are registered in `Program.cs` with DI
- [ ] All async methods end with `Async` suffix
- [ ] Input validation is performed at controller boundary
- [ ] No sensitive data is logged
- [ ] Database queries use Entity Framework Core patterns
- [ ] Error handling includes logging and generic error messages
- [ ] Unit tests follow AAA pattern and are isolated
- [ ] Test coverage meets minimum 80% target
- [ ] CORS policies are restrictive (not `AllowAnyOrigin`)
- [ ] HTTPS is enforced in production
- [ ] No hardcoded secrets or connection strings
- [ ] Proper use of `required`, `?`, and nullable reference types
- [ ] Performance considerations (`.AsNoTracking()`, pagination, indexes)
- [ ] Code organization follows layered architecture
- [ ] Commit messages follow conventional commit format

---

**Last Updated:** 2026-07-13  
**Version:** 1.0  
**Maintainer:** Kannan2022
