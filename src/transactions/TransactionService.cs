namespace NotificationAuditService.Transactions;

/// <summary>
/// Business-logic implementation of <see cref="ITransactionService"/>. Validates input, applies
/// domain rules, emits structured logs, and delegates all persistence to
/// <see cref="ITransactionRepository"/>. The ledger is append-only — there is no update or delete.
/// </summary>
public class TransactionService : ITransactionService
{
    private const int USER_ID_MAX_LENGTH = 255;
    private const int CURRENCY_LENGTH = 3;
    private const int DESCRIPTION_MAX_LENGTH = 1000;
    private const int MAX_PAGE_SIZE = 100;

    private readonly ITransactionRepository _repository;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(ITransactionRepository repository, ILogger<TransactionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Transaction> CreateTransactionAsync(
        string organizationId,
        string userId,
        decimal amount,
        string currency,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        RequireOrganizationId(organizationId);
        var normalizedUserId = RequireNonEmpty(userId, nameof(userId), USER_ID_MAX_LENGTH, "User ID");
        var normalizedCurrency = NormalizeCurrency(currency);
        var normalizedDescription = NormalizeOptional(description, nameof(description), DESCRIPTION_MAX_LENGTH, "Description");

        if (amount == 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be zero.");
        }

        // Note: amount is logged (it is business data, not a secret); user identifiers are logged as
        // opaque ids. Do not extend this to log PII such as names or card numbers.
        _logger.LogInformation(
            "Recording transaction for organisation {OrganizationId}, user {UserId}, currency {Currency}",
            organizationId, normalizedUserId, normalizedCurrency);

        var transaction = new Transaction
        {
            OrganizationId = organizationId,
            UserId = normalizedUserId,
            Amount = amount,
            Currency = normalizedCurrency,
            Description = normalizedDescription,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(transaction, cancellationToken);

        _logger.LogInformation(
            "Recorded transaction {TransactionId} for organisation {OrganizationId}",
            transaction.Id, organizationId);
        return transaction;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetTransactionsByUserAsync(
        string organizationId,
        string userId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        RequireOrganizationId(organizationId);
        var normalizedUserId = RequireNonEmpty(userId, nameof(userId), USER_ID_MAX_LENGTH, "User ID");

        var normalizedSkip = Math.Max(0, skip);
        var normalizedTake = Math.Clamp(take, 1, MAX_PAGE_SIZE);

        return await _repository.GetByUserAsync(
            organizationId, normalizedUserId, normalizedSkip, normalizedTake, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetTransactionByIdAsync(
        string organizationId,
        int id,
        CancellationToken cancellationToken = default)
    {
        RequireOrganizationId(organizationId);

        var transaction = await _repository.GetByIdAsync(id, organizationId, cancellationToken);
        if (transaction is null)
        {
            _logger.LogWarning(
                "Transaction {TransactionId} not found for organisation {OrganizationId}",
                id, organizationId);
        }

        return transaction;
    }

    /// <summary>Guards that the tenant organisation id is present.</summary>
    private static void RequireOrganizationId(string organizationId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            throw new ArgumentException("Organisation id is required.", nameof(organizationId));
    }

    /// <summary>Validates a required string is present and within its maximum length, returning it trimmed.</summary>
    private static string RequireNonEmpty(string? value, string paramName, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{label} is required.", paramName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{label} must be {maxLength} characters or fewer.", paramName);

        return normalized;
    }

    /// <summary>Validates and normalises the ISO 4217 currency code to three upper-case letters.</summary>
    private static string NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != CURRENCY_LENGTH || !normalized.All(char.IsLetter))
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));

        return normalized;
    }

    /// <summary>Validates and normalises an optional string, returning null when blank.</summary>
    private static string? NormalizeOptional(string? value, string paramName, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{label} must be {maxLength} characters or fewer.", paramName);

        return normalized;
    }
}
