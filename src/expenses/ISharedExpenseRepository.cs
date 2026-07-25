namespace NotificationAuditService.Expenses;

/// <summary>
/// Data-access abstraction for <see cref="SharedExpense"/> aggregates (expense + participants).
/// Every read and write is scoped to an organisation, so persistence is the single, consistent
/// enforcement point for tenant isolation.
/// </summary>
public interface ISharedExpenseRepository
{
    /// <summary>Persists a new expense together with its participants.</summary>
    /// <param name="expense">The expense to add; its <see cref="SharedExpense.OrganizationId"/> must already be set.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted expense, including generated identifiers.</returns>
    Task<SharedExpense> AddAsync(SharedExpense expense, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single expense (with participants) by id, scoped to the organisation.</summary>
    /// <param name="id">The expense identifier.</param>
    /// <param name="organizationId">The owning organisation; expenses in other organisations are never returned.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The expense, or <c>null</c> if it does not exist within the organisation.</returns>
    Task<SharedExpense?> GetByIdAsync(int id, string organizationId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all expenses (with participants) for a group within an organisation, newest first.</summary>
    /// <param name="organizationId">The owning organisation.</param>
    /// <param name="groupId">The group whose expenses to load.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The group's expenses (empty when none match).</returns>
    Task<IReadOnlyList<SharedExpense>> GetByGroupAsync(
        string organizationId,
        string groupId,
        CancellationToken cancellationToken = default);
}
