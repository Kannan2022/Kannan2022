namespace NotificationAuditService.Expenses;

/// <summary>
/// A shared expense paid by one user and split across a set of participants within a group.
/// Every expense is owned by exactly one organisation (the tenant boundary).
/// </summary>
public class SharedExpense
{
    /// <summary>The unique identifier of the expense.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Identifier of the owning organisation. This is the multi-tenant isolation key and is always
    /// derived from the authenticated principal — never from client-supplied input.
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>Identifier of the group (e.g. a trip or household) the expense belongs to.</summary>
    public required string GroupId { get; set; }

    /// <summary>Optional free-text description of the expense.</summary>
    public string? Description { get; set; }

    /// <summary>The total amount of the expense (positive).</summary>
    public decimal Amount { get; set; }

    /// <summary>The ISO 4217 currency code for the expense (e.g. "INR", "USD").</summary>
    public required string Currency { get; set; }

    /// <summary>The user who paid the total amount.</summary>
    public required string PaidByUserId { get; set; }

    /// <summary>How the amount was split among participants.</summary>
    public SplitType SplitType { get; set; }

    /// <summary>The UTC timestamp when the expense was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>The per-participant shares. Their <see cref="ExpenseParticipant.ShareAmount"/> values sum to <see cref="Amount"/>.</summary>
    public List<ExpenseParticipant> Participants { get; set; } = new();
}
