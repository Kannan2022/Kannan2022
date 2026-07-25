namespace NotificationAuditService.Expenses;

/// <summary>
/// Business-logic implementation of <see cref="ISharedExpenseService"/>. Validates input, computes
/// participant shares in integer minor units so they always sum exactly to the total, emits
/// structured logs, and delegates persistence to <see cref="ISharedExpenseRepository"/>.
/// </summary>
public class SharedExpenseService : ISharedExpenseService
{
    private const int ID_MAX_LENGTH = 255;
    private const int DESCRIPTION_MAX_LENGTH = 1000;
    private const int CURRENCY_LENGTH = 3;

    private readonly ISharedExpenseRepository _repository;
    private readonly ILogger<SharedExpenseService> _logger;

    public SharedExpenseService(ISharedExpenseRepository repository, ILogger<SharedExpenseService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SharedExpense> CreateExpenseAsync(
        string organizationId,
        string groupId,
        string? description,
        decimal amount,
        string currency,
        string paidByUserId,
        SplitType splitType,
        IReadOnlyList<(string UserId, decimal? ShareAmount)> participants,
        CancellationToken cancellationToken = default)
    {
        RequireOrganizationId(organizationId);
        var normalizedGroupId = RequireNonEmpty(groupId, nameof(groupId), ID_MAX_LENGTH, "GroupId");
        var normalizedPaidBy = RequireNonEmpty(paidByUserId, nameof(paidByUserId), ID_MAX_LENGTH, "PaidByUserId");
        var normalizedCurrency = NormalizeCurrency(currency);
        var normalizedDescription = NormalizeOptional(description, nameof(description), DESCRIPTION_MAX_LENGTH, "Description");

        if (amount <= 0m)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        if (!Enum.IsDefined(splitType))
            throw new ArgumentException($"'{splitType}' is not a valid split type.", nameof(splitType));
        if (participants is null || participants.Count == 0)
            throw new ArgumentException("At least one participant is required.", nameof(participants));

        var normalizedParticipants = NormalizeParticipants(participants);
        var shares = splitType == SplitType.Equal
            ? SplitEqually(amount, normalizedParticipants)
            : SplitExactly(amount, normalizedParticipants);

        _logger.LogInformation(
            "Creating {SplitType} expense for organisation {OrganizationId}, group {GroupId}, {ParticipantCount} participants",
            splitType, organizationId, normalizedGroupId, shares.Count);

        var expense = new SharedExpense
        {
            OrganizationId = organizationId,
            GroupId = normalizedGroupId,
            Description = normalizedDescription,
            Amount = amount,
            Currency = normalizedCurrency,
            PaidByUserId = normalizedPaidBy,
            SplitType = splitType,
            CreatedAt = DateTime.UtcNow,
            Participants = shares
        };

        await _repository.AddAsync(expense, cancellationToken);

        _logger.LogInformation(
            "Created expense {ExpenseId} for organisation {OrganizationId}, group {GroupId}",
            expense.Id, organizationId, normalizedGroupId);
        return expense;
    }

    /// <inheritdoc />
    public async Task<SharedExpense?> GetExpenseByIdAsync(string organizationId, int id, CancellationToken cancellationToken = default)
    {
        RequireOrganizationId(organizationId);
        return await _repository.GetByIdAsync(id, organizationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SharedExpense>> GetExpensesByGroupAsync(string organizationId, string groupId, CancellationToken cancellationToken = default)
    {
        RequireOrganizationId(organizationId);
        var normalizedGroupId = RequireNonEmpty(groupId, nameof(groupId), ID_MAX_LENGTH, "GroupId");
        return await _repository.GetByGroupAsync(organizationId, normalizedGroupId, cancellationToken);
    }

    /// <summary>Trims and de-duplicates the participant user ids.</summary>
    private static List<(string UserId, decimal? ShareAmount)> NormalizeParticipants(
        IReadOnlyList<(string UserId, decimal? ShareAmount)> participants)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<(string, decimal?)>(participants.Count);
        foreach (var (userId, share) in participants)
        {
            var normalizedUserId = RequireNonEmpty(userId, nameof(participants), ID_MAX_LENGTH, "Participant UserId");
            if (!seen.Add(normalizedUserId))
                throw new ArgumentException($"Participant '{normalizedUserId}' appears more than once.", nameof(participants));
            result.Add((normalizedUserId, share));
        }
        return result;
    }

    /// <summary>Splits the total equally, distributing any remainder cents to the first participants.</summary>
    private static List<ExpenseParticipant> SplitEqually(decimal amount, List<(string UserId, decimal? ShareAmount)> participants)
    {
        var totalMinor = MoneyMath.ToMinor(amount);
        var count = participants.Count;
        var baseShare = totalMinor / count;
        var remainder = (int)(totalMinor % count);

        return participants
            .Select((p, index) => new ExpenseParticipant
            {
                UserId = p.UserId,
                ShareAmount = MoneyMath.FromMinor(baseShare + (index < remainder ? 1 : 0))
            })
            .ToList();
    }

    /// <summary>Uses caller-supplied shares, validating they are non-negative and sum exactly to the total.</summary>
    private static List<ExpenseParticipant> SplitExactly(decimal amount, List<(string UserId, decimal? ShareAmount)> participants)
    {
        var totalMinor = MoneyMath.ToMinor(amount);
        long sumMinor = 0;
        var result = new List<ExpenseParticipant>(participants.Count);

        foreach (var (userId, share) in participants)
        {
            if (share is null)
                throw new ArgumentException($"An exact share amount is required for participant '{userId}'.", nameof(participants));
            if (share.Value < 0m)
                throw new ArgumentException($"Share amount for participant '{userId}' cannot be negative.", nameof(participants));

            var minor = MoneyMath.ToMinor(share.Value);
            sumMinor += minor;
            result.Add(new ExpenseParticipant { UserId = userId, ShareAmount = MoneyMath.FromMinor(minor) });
        }

        if (sumMinor != totalMinor)
            throw new ArgumentException($"Participant shares must sum to the total amount ({amount:0.00}).", nameof(participants));

        return result;
    }

    private static void RequireOrganizationId(string organizationId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            throw new ArgumentException("Organisation id is required.", nameof(organizationId));
    }

    private static string RequireNonEmpty(string? value, string paramName, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{label} is required.", paramName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{label} must be {maxLength} characters or fewer.", paramName);
        return normalized;
    }

    private static string NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));
        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != CURRENCY_LENGTH || !normalized.All(char.IsLetter))
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));
        return normalized;
    }

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
