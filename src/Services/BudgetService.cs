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
        // 1. Force EF Core to forget any cached schema or tracking
        _db.ChangeTracker.Clear();

        try 
        {
            // 2. Load the data with explicit Includes
            // Note: If this still returns 0, it means the UserId check is failing 
            // or the data was wiped by a DROP TABLE command during the fix.
            return await _db.BudgetPeriods
                .Where(p => p.UserId == userId)
                .Include(p => p.Cycles)
                    .ThenInclude(c => c.Items)
                .Include(p => p.Transactions)
                    .ThenInclude(t => t.Splits)
                .Include(p => p.Transfers)
                .Include(p => p.ExpectedIncome)
                .Include(p => p.WatchList) // This now loads the new structure
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            // Log the actual DB error to console
            Console.WriteLine($"DATABASE CRASH: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"INNER: {ex.InnerException.Message}");
            return new List<BudgetPeriod>();
        }
    }
}