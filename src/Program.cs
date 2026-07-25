using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NotificationAuditService.Audit;
using NotificationAuditService.Common;
using NotificationAuditService.Data;
using NotificationAuditService.Expenses;
using NotificationAuditService.Notifications;
using NotificationAuditService.Projects;
using NotificationAuditService.Transactions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=audit.db"));

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Project feature: model -> repository -> service -> controller
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();

// Transaction feature: model -> repository -> service -> controller
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

// Expense-splitting feature: model -> repository -> service(s) -> controller
builder.Services.AddScoped<ISharedExpenseRepository, SharedExpenseRepository>();
builder.Services.AddScoped<ISharedExpenseService, SharedExpenseService>();
builder.Services.AddScoped<IBalanceCalculationService, BalanceCalculationService>();

// Multi-tenant context resolved from the authenticated principal
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    context.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Exposed so integration tests can bootstrap the application via
/// <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public partial class Program { }