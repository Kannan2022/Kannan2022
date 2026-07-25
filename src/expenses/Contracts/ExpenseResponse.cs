namespace NotificationAuditService.Expenses.Contracts;

/// <summary>API response representing a shared expense and its participant shares.</summary>
public class ExpenseResponse
{
    /// <summary>Unique expense identifier.</summary>
    public int Id { get; set; }

    /// <summary>Identifier of the owning organisation (the caller's own tenant).</summary>
    public required string OrganizationId { get; set; }

    /// <summary>The group the expense belongs to.</summary>
    public required string GroupId { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>The total amount of the expense.</summary>
    public decimal Amount { get; set; }

    /// <summary>The ISO 4217 currency code.</summary>
    public required string Currency { get; set; }

    /// <summary>The user who paid the total.</summary>
    public required string PaidByUserId { get; set; }

    /// <summary>How the amount was split.</summary>
    public SplitType SplitType { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>The per-participant shares.</summary>
    public required List<ParticipantShareResponse> Participants { get; set; }

    /// <summary>Maps a <see cref="SharedExpense"/> entity to its API response representation.</summary>
    public static ExpenseResponse FromEntity(SharedExpense expense) => new()
    {
        Id = expense.Id,
        OrganizationId = expense.OrganizationId,
        GroupId = expense.GroupId,
        Description = expense.Description,
        Amount = expense.Amount,
        Currency = expense.Currency,
        PaidByUserId = expense.PaidByUserId,
        SplitType = expense.SplitType,
        CreatedAt = expense.CreatedAt,
        Participants = expense.Participants
            .Select(p => new ParticipantShareResponse { UserId = p.UserId, ShareAmount = p.ShareAmount })
            .ToList()
    };
}

/// <summary>A single participant's computed share of an expense.</summary>
public class ParticipantShareResponse
{
    /// <summary>The participating user.</summary>
    public required string UserId { get; set; }

    /// <summary>The amount this user owes for the expense.</summary>
    public decimal ShareAmount { get; set; }
}
