namespace NotificationAuditService.Expenses.Contracts;

/// <summary>
/// The net balances for a group and a suggested set of settlements that clears them.
/// </summary>
public class GroupBalanceResponse
{
    /// <summary>The group these balances are for.</summary>
    public required string GroupId { get; set; }

    /// <summary>The currency of all amounts, or <c>null</c> when the group has no expenses.</summary>
    public string? Currency { get; set; }

    /// <summary>Each user's net position (positive = owed to them, negative = they owe).</summary>
    public required List<UserBalance> Balances { get; set; }

    /// <summary>A minimal set of transfers that settles every balance to zero.</summary>
    public required List<Settlement> Settlements { get; set; }
}

/// <summary>A single user's net balance within a group.</summary>
public class UserBalance
{
    /// <summary>The user.</summary>
    public required string UserId { get; set; }

    /// <summary>Net amount: positive means the user is owed money; negative means the user owes money.</summary>
    public decimal NetAmount { get; set; }
}

/// <summary>A suggested transfer from a debtor to a creditor.</summary>
public class Settlement
{
    /// <summary>The user who should pay.</summary>
    public required string FromUserId { get; set; }

    /// <summary>The user who should be paid.</summary>
    public required string ToUserId { get; set; }

    /// <summary>The amount to transfer (positive).</summary>
    public decimal Amount { get; set; }
}
