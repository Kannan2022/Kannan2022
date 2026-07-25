namespace NotificationAuditService.Expenses;

/// <summary>The computed net balances and suggested settlements for a group (web-agnostic result).</summary>
public class GroupBalances
{
    /// <summary>The group these balances are for.</summary>
    public required string GroupId { get; init; }

    /// <summary>The currency of all amounts, or <c>null</c> when the group has no expenses.</summary>
    public string? Currency { get; init; }

    /// <summary>Each user's net position (positive = owed to them, negative = they owe), ordered by user id.</summary>
    public required IReadOnlyList<UserNetBalance> Balances { get; init; }

    /// <summary>A set of transfers that clears every balance to zero.</summary>
    public required IReadOnlyList<SettlementTransfer> Settlements { get; init; }
}

/// <summary>A single user's net balance within a group.</summary>
public record UserNetBalance(string UserId, decimal NetAmount);

/// <summary>A suggested transfer from a debtor to a creditor.</summary>
public record SettlementTransfer(string FromUserId, string ToUserId, decimal Amount);
