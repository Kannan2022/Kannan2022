using System.ComponentModel.DataAnnotations;

namespace NotificationAuditService.Transactions.Contracts;

/// <summary>
/// Request payload for recording a transaction. The owning organisation is intentionally absent —
/// it is resolved from the authenticated principal so a caller cannot record a transaction against
/// another tenant's organisation.
/// </summary>
public class CreateTransactionRequest
{
    /// <summary>The user the transaction belongs to, within the caller's organisation (1–255 characters).</summary>
    [Required(ErrorMessage = "UserId is required.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "UserId must be between 1 and 255 characters.")]
    public required string UserId { get; set; }

    /// <summary>The transaction amount. Must be non-zero (positive = credit, negative = debit).</summary>
    [Required(ErrorMessage = "Amount is required.")]
    [Range(typeof(decimal), "-79228162514264337593543950335", "79228162514264337593543950335", ErrorMessage = "Amount is out of range.")]
    public decimal Amount { get; set; }

    /// <summary>The ISO 4217 currency code (three letters, e.g. "USD").</summary>
    [Required(ErrorMessage = "Currency is required.")]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Currency must be a 3-letter ISO 4217 code.")]
    public required string Currency { get; set; }

    /// <summary>Optional free-text description (up to 1000 characters).</summary>
    [StringLength(1000, ErrorMessage = "Description must be at most 1000 characters.")]
    public string? Description { get; set; }
}
