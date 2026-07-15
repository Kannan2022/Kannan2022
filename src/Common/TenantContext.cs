using Microsoft.AspNetCore.Http;

namespace NotificationAuditService.Common;

/// <summary>
/// Default <see cref="ITenantContext"/> that reads the organisation id from the authenticated
/// principal's claims on the current <see cref="HttpContext"/>.
/// </summary>
public class TenantContext : ITenantContext
{
    /// <summary>The claim type that carries the caller's organisation id.</summary>
    public const string OrganizationClaimType = "org_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string? OrganizationId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirst(OrganizationClaimType)?.Value;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    /// <inheritdoc />
    public string GetRequiredOrganizationId() =>
        OrganizationId ?? throw new InvalidOperationException(
            "No organisation is associated with the current request.");
}
