using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using NotificationAuditService.Common;

namespace NotificationAuditService.Tests.TestInfrastructure;

/// <summary>
/// Boots the API in-memory for integration tests, backed by an isolated per-factory SQLite database,
/// and injects a tenant identity from the <c>X-Test-Org</c> request header. This stands in for the
/// authentication middleware a real deployment would use to populate the <c>org_id</c> claim.
/// </summary>
public class ProjectApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Header used by tests to simulate the caller's authenticated organisation.</summary>
    public const string OrganizationHeader = "X-Test-Org";

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"proj_tests_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Point the app at a throwaway database unique to this factory instance.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_databasePath}"
            });
        });

        // Insert a middleware (ahead of routing) that turns the X-Test-Org header into an org_id claim.
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter, TestTenantStartupFilter>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_databasePath))
        {
            try { File.Delete(_databasePath); } catch { /* best-effort cleanup */ }
        }
    }

    /// <summary>Inserts <see cref="TestTenantMiddleware"/> at the front of the request pipeline.</summary>
    private sealed class TestTenantStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.UseMiddleware<TestTenantMiddleware>();
            next(app);
        };
    }

    /// <summary>Populates <see cref="HttpContext.User"/> from the test header.</summary>
    private sealed class TestTenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TestTenantMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var organizationId = context.Request.Headers[OrganizationHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(organizationId))
            {
                var identity = new ClaimsIdentity(
                    new[] { new Claim(TenantContext.OrganizationClaimType, organizationId) },
                    authenticationType: "Test");
                context.User = new ClaimsPrincipal(identity);
            }

            await _next(context);
        }
    }
}
