using Microsoft.EntityFrameworkCore;
using NotificationAuditService.Data;

namespace NotificationAuditService.Transactions;

/// <summary>
/// Entity Framework Core implementation of <see cref="ITransactionRepository"/>. All queries filter
/// by <see cref="Transaction.OrganizationId"/>, so no caller can reach another tenant's ledger.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly AuditDbContext _context;

    public TransactionRepository(AuditDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetByIdAsync(int id, string organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == organizationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByUserAsync(
        string organizationId,
        string userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId && t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
