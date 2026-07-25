namespace NotificationAuditService.Transactions;

/// <summary>
/// Represents a single financial transaction. Transactions are an append-only ledger: once created
/// they are never modified or deleted (corrections are recorded as new, compensating transactions).
/// Every transaction is owned by exactly one organisation (the tenant boundary).
/// </summary>
public class Transaction
{
    /// <summary>The unique identifier of the transaction.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Identifier of the owning organisation. This is the multi-tenant isolation key and is always
    /// derived from the authenticated principal — never from client-supplied input.
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>The ID of the user the transaction belongs to, within the organisation.</summary>
    public required string UserId { get; set; }

    /// <summary>The transaction amount in minor-unit-preserving <see cref="decimal"/>. Non-zero; positive = credit, negative = debit.</summary>
    public decimal Amount { get; set; }

    /// <summary>The ISO 4217 currency code for the transaction (e.g. "INR", "USD").</summary>
    public required string Currency { get; set; }

    /// <summary>An optional description providing additional context for the transaction.</summary>
    public string? Description { get; set; }

    /// <summary>The UTC timestamp when the transaction was created.</summary>
    public DateTime CreatedAt { get; set; }
}
