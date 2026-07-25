namespace NotificationAuditService.Expenses;

/// <summary>
/// A single participant's share of a <see cref="SharedExpense"/> — the amount this user owes for it.
/// </summary>
public class ExpenseParticipant
{
    /// <summary>The unique identifier of the participant row.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="SharedExpense"/>.</summary>
    public int SharedExpenseId { get; set; }

    /// <summary>The user who owes this share.</summary>
    public required string UserId { get; set; }

    /// <summary>The amount this user owes for the expense (non-negative).</summary>
    public decimal ShareAmount { get; set; }
}
