namespace NotificationAuditService.Transactions;

/// <summary>
/// Defines operations for recording and querying financial transactions. Every operation is scoped
/// to an organisation (the tenant); callers must supply the organisation resolved from the
/// authenticated principal, never a value taken from client input. The ledger is append-only.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Records a new transaction for a user within the organisation.
    /// </summary>
    /// <param name="organizationId">The owning organisation (tenant), from the authenticated principal.</param>
    /// <param name="userId">The user the transaction belongs to (required, 1–255 characters).</param>
    /// <param name="amount">The transaction amount. Must be non-zero.</param>
    /// <param name="currency">The ISO 4217 currency code (three letters).</param>
    /// <param name="description">Optional description (up to 1000 characters).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The created transaction with its assigned identifier.</returns>
    /// <exception cref="ArgumentException">Thrown when a required argument is missing, too long, or malformed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is zero.</exception>
    Task<Transaction> CreateTransactionAsync(
        string organizationId,
        string userId,
        decimal amount,
        string currency,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of transactions for a user within the organisation, newest first.
    /// </summary>
    /// <param name="organizationId">The owning organisation (tenant), from the authenticated principal.</param>
    /// <param name="userId">The user whose transactions to retrieve (required).</param>
    /// <param name="skip">Number of records to skip; negative values are treated as zero.</param>
    /// <param name="take">Page size; clamped to the range [1, 100].</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching transactions (empty when none match).</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="organizationId"/> or <paramref name="userId"/> is missing.</exception>
    Task<IReadOnlyList<Transaction>> GetTransactionsByUserAsync(
        string organizationId,
        string userId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single transaction by id, scoped to the organisation.
    /// </summary>
    /// <param name="organizationId">The owning organisation (tenant), from the authenticated principal.</param>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The transaction, or <c>null</c> if no matching transaction exists within the organisation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="organizationId"/> is missing.</exception>
    Task<Transaction?> GetTransactionByIdAsync(
        string organizationId,
        int id,
        CancellationToken cancellationToken = default);
}
