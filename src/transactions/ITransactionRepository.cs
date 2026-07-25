namespace NotificationAuditService.Transactions;

/// <summary>
/// Data-access abstraction for <see cref="Transaction"/> entities. Every read and write is scoped to
/// an organisation, so persistence is the single, consistent enforcement point for tenant isolation.
/// The ledger is append-only: there is intentionally no update or delete operation.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>Persists a new transaction.</summary>
    /// <param name="transaction">The transaction to add; its <see cref="Transaction.OrganizationId"/> must already be set.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted transaction, including its generated <see cref="Transaction.Id"/>.</returns>
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single transaction by id, scoped to the given organisation.</summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="organizationId">The owning organisation; transactions in other organisations are never returned.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The transaction, or <c>null</c> if it does not exist within the organisation.</returns>
    Task<Transaction?> GetByIdAsync(int id, string organizationId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a page of transactions for a user within an organisation, newest first.</summary>
    /// <param name="organizationId">The owning organisation.</param>
    /// <param name="userId">The user whose transactions to list.</param>
    /// <param name="skip">Number of records to skip (for paging).</param>
    /// <param name="take">Maximum number of records to return.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching transactions (empty when none match).</returns>
    Task<IReadOnlyList<Transaction>> GetByUserAsync(
        string organizationId,
        string userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
