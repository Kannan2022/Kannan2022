namespace NotificationAuditService.Expenses;

/// <summary>
/// Computes who owes whom within a group, from its shared expenses. Scoped to an organisation.
/// </summary>
public interface IBalanceCalculationService
{
    /// <summary>
    /// Calculates each user's net balance for a group and a set of settlements that clears them.
    /// </summary>
    /// <param name="organizationId">The owning organisation (tenant), from the authenticated principal.</param>
    /// <param name="groupId">The group to calculate balances for.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The group's balances and suggested settlements (empty when the group has no expenses).</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="organizationId"/> or <paramref name="groupId"/> is missing.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the group's expenses span more than one currency.</exception>
    Task<GroupBalances> CalculateGroupBalancesAsync(
        string organizationId,
        string groupId,
        CancellationToken cancellationToken = default);
}
