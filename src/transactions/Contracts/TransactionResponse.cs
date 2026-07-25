namespace NotificationAuditService.Transactions.Contracts;

/// <summary>
/// API response contract representing a transaction. Decoupled from the <see cref="Transaction"/>
/// entity so that internal storage changes do not leak into the public API surface.
/// </summary>
public class TransactionResponse
{
    /// <summary>Unique transaction identifier.</summary>
    public int Id { get; set; }

    /// <summary>Identifier of the owning organisation (the caller's own tenant).</summary>
    public required string OrganizationId { get; set; }

    /// <summary>The user the transaction belongs to.</summary>
    public required string UserId { get; set; }

    /// <summary>The transaction amount (positive = credit, negative = debit).</summary>
    public decimal Amount { get; set; }

    /// <summary>The ISO 4217 currency code.</summary>
    public required string Currency { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Maps a <see cref="Transaction"/> entity to its API response representation.</summary>
    /// <param name="transaction">The entity to map.</param>
    /// <returns>A populated <see cref="TransactionResponse"/>.</returns>
    public static TransactionResponse FromEntity(Transaction transaction) => new()
    {
        Id = transaction.Id,
        OrganizationId = transaction.OrganizationId,
        UserId = transaction.UserId,
        Amount = transaction.Amount,
        Currency = transaction.Currency,
        Description = transaction.Description,
        CreatedAt = transaction.CreatedAt
    };
}
