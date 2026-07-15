namespace NotificationAuditService.Common;

/// <summary>
/// Provides the organisation (tenant) identifier for the current request. The value originates from
/// the authenticated principal, guaranteeing that tenant scope cannot be spoofed via request input.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The organisation id of the current caller, or <c>null</c> when the request is unauthenticated
    /// or carries no organisation claim.
    /// </summary>
    string? OrganizationId { get; }

    /// <summary>
    /// Returns the current organisation id, throwing when it cannot be resolved.
    /// </summary>
    /// <returns>The non-null organisation id.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no organisation is associated with the request.</exception>
    string GetRequiredOrganizationId();
}
