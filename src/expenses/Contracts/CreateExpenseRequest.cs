using System.ComponentModel.DataAnnotations;

namespace NotificationAuditService.Expenses.Contracts;

/// <summary>
/// Request payload for creating a shared expense. The owning organisation is intentionally absent —
/// it is resolved from the authenticated principal so a caller cannot create an expense in another
/// tenant's organisation.
/// </summary>
public class CreateExpenseRequest
{
    /// <summary>The group the expense belongs to (1–255 characters).</summary>
    [Required(ErrorMessage = "GroupId is required.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "GroupId must be between 1 and 255 characters.")]
    public required string GroupId { get; set; }

    /// <summary>Optional description (up to 1000 characters).</summary>
    [StringLength(1000, ErrorMessage = "Description must be at most 1000 characters.")]
    public string? Description { get; set; }

    /// <summary>The total amount of the expense. Must be greater than zero.</summary>
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    /// <summary>The ISO 4217 currency code (three letters, e.g. "USD").</summary>
    [Required(ErrorMessage = "Currency is required.")]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Currency must be a 3-letter ISO 4217 code.")]
    public required string Currency { get; set; }

    /// <summary>The user who paid the total (1–255 characters).</summary>
    [Required(ErrorMessage = "PaidByUserId is required.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "PaidByUserId must be between 1 and 255 characters.")]
    public required string PaidByUserId { get; set; }

    /// <summary>How to split the amount among participants.</summary>
    [EnumDataType(typeof(SplitType), ErrorMessage = "SplitType must be Equal or Exact.")]
    public SplitType SplitType { get; set; } = SplitType.Equal;

    /// <summary>The participants sharing the expense (at least one).</summary>
    [Required(ErrorMessage = "At least one participant is required.")]
    [MinLength(1, ErrorMessage = "At least one participant is required.")]
    public required List<ExpenseParticipantInput> Participants { get; set; }
}

/// <summary>A single participant supplied when creating an expense.</summary>
public class ExpenseParticipantInput
{
    /// <summary>The participating user (1–255 characters).</summary>
    [Required(ErrorMessage = "Participant UserId is required.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Participant UserId must be between 1 and 255 characters.")]
    public required string UserId { get; set; }

    /// <summary>
    /// The exact amount this participant owes. Required for <see cref="SplitType.Exact"/>; ignored for
    /// <see cref="SplitType.Equal"/> (shares are computed).
    /// </summary>
    public decimal? ShareAmount { get; set; }
}
