namespace NotificationAuditService.Expenses;

/// <summary>
/// Default <see cref="IBalanceCalculationService"/>. Nets each user's paid-minus-owed position for a
/// group and derives a greedy set of settlements. All arithmetic is done in integer minor units so
/// balances and settlements reconcile exactly to zero.
/// </summary>
public class BalanceCalculationService : IBalanceCalculationService
{
    private readonly ISharedExpenseRepository _repository;
    private readonly ILogger<BalanceCalculationService> _logger;

    public BalanceCalculationService(ISharedExpenseRepository repository, ILogger<BalanceCalculationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GroupBalances> CalculateGroupBalancesAsync(
        string organizationId,
        string groupId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            throw new ArgumentException("Organisation id is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("GroupId is required.", nameof(groupId));

        var expenses = await _repository.GetByGroupAsync(organizationId, groupId.Trim(), cancellationToken);
        if (expenses.Count == 0)
        {
            return new GroupBalances
            {
                GroupId = groupId.Trim(),
                Currency = null,
                Balances = Array.Empty<UserNetBalance>(),
                Settlements = Array.Empty<SettlementTransfer>()
            };
        }

        var currencies = expenses.Select(e => e.Currency).Distinct(StringComparer.Ordinal).ToList();
        if (currencies.Count > 1)
        {
            // Netting across currencies would be silently wrong; require a single currency per group.
            throw new InvalidOperationException(
                "Balance calculation requires a single currency; this group has expenses in multiple currencies.");
        }

        var currency = currencies[0];

        // Net position per user, in minor units. Payer is credited the full amount; each participant
        // is debited their share. Because shares sum to the total, all nets sum to zero.
        var net = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var expense in expenses)
        {
            Add(net, expense.PaidByUserId, MoneyMath.ToMinor(expense.Amount));
            foreach (var participant in expense.Participants)
            {
                Add(net, participant.UserId, -MoneyMath.ToMinor(participant.ShareAmount));
            }
        }

        var balances = net
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new UserNetBalance(kv.Key, MoneyMath.FromMinor(kv.Value)))
            .ToList();

        var settlements = BuildSettlements(net);

        _logger.LogInformation(
            "Calculated balances for organisation {OrganizationId}, group {GroupId}: {UserCount} users, {SettlementCount} settlements",
            organizationId, groupId, balances.Count, settlements.Count);

        return new GroupBalances
        {
            GroupId = groupId.Trim(),
            Currency = currency,
            Balances = balances,
            Settlements = settlements
        };
    }

    private static void Add(Dictionary<string, long> net, string userId, long minor)
    {
        net[userId] = net.TryGetValue(userId, out var current) ? current + minor : minor;
    }

    /// <summary>
    /// Greedily matches the largest debtors to creditors, producing a small set of transfers.
    /// Debtors and creditors are ordered by user id so the result is deterministic.
    /// </summary>
    private static List<SettlementTransfer> BuildSettlements(Dictionary<string, long> net)
    {
        var debtors = net.Where(kv => kv.Value < 0)
            .Select(kv => (UserId: kv.Key, Amount: -kv.Value))
            .OrderBy(x => x.UserId, StringComparer.Ordinal)
            .ToList();
        var creditors = net.Where(kv => kv.Value > 0)
            .Select(kv => (UserId: kv.Key, Amount: kv.Value))
            .OrderBy(x => x.UserId, StringComparer.Ordinal)
            .ToList();

        var settlements = new List<SettlementTransfer>();
        int i = 0, j = 0;
        while (i < debtors.Count && j < creditors.Count)
        {
            var debtor = debtors[i];
            var creditor = creditors[j];
            var transfer = Math.Min(debtor.Amount, creditor.Amount);

            if (transfer > 0)
            {
                settlements.Add(new SettlementTransfer(debtor.UserId, creditor.UserId, MoneyMath.FromMinor(transfer)));
            }

            debtors[i] = (debtor.UserId, debtor.Amount - transfer);
            creditors[j] = (creditor.UserId, creditor.Amount - transfer);

            if (debtors[i].Amount == 0) i++;
            if (creditors[j].Amount == 0) j++;
        }

        return settlements;
    }
}
