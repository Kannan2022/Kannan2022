namespace NotificationAuditService.Expenses;

/// <summary>
/// Business operations for creating and querying shared expenses. Every operation is scoped to an
/// organisation (the tenant); callers must supply the organisation resolved from the authenticated
/// principal, never a value taken from client input.
/// </summary>
public interface ISharedExpenseService
{
    /// <summary>
    /// Creates a shared expense, computing each participant's share according to <paramref name="splitType"/>.
    /// </summary>
    /// <param name="organizationId">The owning organisation (tenant), from the authenticated principal.</param>
    /// <param name="groupId">The group the expense belongs to (required).</param>
    /// <param name="description">Optional description (up to 1000 characters).</param>
    /// <param name="amount">The total amount (must be greater than zero).</param>
    /// <param name="currency">The ISO 4217 currency code (three letters).</param>
    /// <param name="paidByUserId">The user who paid the total (required).</param>
    /// <param name="splitType">How to divide the amount.</param>
    /// <param name="participants">The participants; for <see cref="SplitType.Exact"/> each must supply a non-negative share that sums to the total.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The created expense with computed participant shares.</returns>
    /// <exception cref="ArgumentException">Thrown when input is missing, malformed, duplicated, or (for exact splits) the shares do not sum to the total.</exception>
    Task<SharedExpense> CreateExpenseAsync(
        string organizationId,
        string groupId,
        string? description,
        decimal amount,
        string currency,
        string paidByUserId,
        SplitType splitType,
        IReadOnlyList<(string UserId, decimal? ShareAmount)> participants,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single expense by id, scoped to the organisation.</summary>
    /// <param name="organizationId">The owning organisation (tenant).</param>
    /// <param name="id">The expense identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The expense, or <c>null</c> if no matching expense exists within the organisation.</returns>
    Task<SharedExpense?> GetExpenseByIdAsync(string organizationId, int id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all expenses for a group within the organisation, newest first.</summary>
    /// <param name="organizationId">The owning organisation (tenant).</param>
    /// <param name="groupId">The group whose expenses to retrieve.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The group's expenses (empty when none match).</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="organizationId"/> or <paramref name="groupId"/> is missing.</exception>
    Task<IReadOnlyList<SharedExpense>> GetExpensesByGroupAsync(string organizationId, string groupId, CancellationToken cancellationToken = default);
}
