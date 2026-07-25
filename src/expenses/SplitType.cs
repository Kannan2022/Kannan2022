namespace NotificationAuditService.Expenses;

/// <summary>How the total amount of a <see cref="SharedExpense"/> is divided among its participants.</summary>
public enum SplitType
{
    /// <summary>Divide the total equally; any remainder pennies are distributed deterministically.</summary>
    Equal,

    /// <summary>Use the exact per-participant share amounts supplied by the caller (must sum to the total).</summary>
    Exact
}
