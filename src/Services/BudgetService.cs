using Microsoft.EntityFrameworkCore;

namespace Dashboard;

public class BudgetService
{
    private readonly AppDbContext _db;

    public BudgetService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<BudgetPeriod>> GetPeriods(int userId)
    {
        _db.ChangeTracker.Clear(); // Ensure fresh data

        return await _db.BudgetPeriods
            .Where(p => p.UserId == userId)
            .Include(p => p.Cycles).ThenInclude(c => c.Items)
            .Include(p => p.Transactions).ThenInclude(t => t.Splits)
            .Include(p => p.Transfers)
            .Include(p => p.ExpectedIncome)
            .Include(p => p.WatchList)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }
}