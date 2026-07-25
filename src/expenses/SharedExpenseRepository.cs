using Microsoft.EntityFrameworkCore;
using NotificationAuditService.Data;

namespace NotificationAuditService.Expenses;

/// <summary>
/// Entity Framework Core implementation of <see cref="ISharedExpenseRepository"/>. All queries filter
/// by <see cref="SharedExpense.OrganizationId"/>, so no caller can reach another tenant's expenses.
/// </summary>
public class SharedExpenseRepository : ISharedExpenseRepository
{
    private readonly AuditDbContext _context;

    public SharedExpenseRepository(AuditDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SharedExpense> AddAsync(SharedExpense expense, CancellationToken cancellationToken = default)
    {
        _context.SharedExpenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);
        return expense;
    }

    /// <inheritdoc />
    public async Task<SharedExpense?> GetByIdAsync(int id, string organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.SharedExpenses
            .AsNoTracking()
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == organizationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SharedExpense>> GetByGroupAsync(
        string organizationId,
        string groupId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SharedExpenses
            .AsNoTracking()
            .Include(e => e.Participants)
            .Where(e => e.OrganizationId == organizationId && e.GroupId == groupId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
