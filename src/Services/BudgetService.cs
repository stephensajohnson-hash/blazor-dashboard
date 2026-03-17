using Microsoft.EntityFrameworkCore;

namespace Dashboard;

public class BudgetService
{
    private readonly AppDbContext _db;

    public BudgetService(AppDbContext db)
    {
        _db = db;
    }

    // 1. Fetches ONLY the top-level info for the navigation arrows (Super Fast)
    public async Task<List<BudgetPeriod>> GetPeriodsShallowAsync(int userId)
    {
        try 
        {
            return await _db.BudgetPeriods
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.StartDate)
                .Select(p => new BudgetPeriod 
                { 
                    Id = p.Id, 
                    DisplayName = p.DisplayName, 
                    StartDate = p.StartDate, 
                    InitialBankBalance = p.InitialBankBalance 
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SHALLOW LOAD CRASH: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"INNER: {ex.InnerException.Message}");
            return new List<BudgetPeriod>();
        }
    }

    // 2. Fetches the heavy relational data ONLY for the month you are looking at
    public async Task<BudgetPeriod?> GetPeriodDetailsAsync(int periodId)
    {
        // Force EF Core to forget any cached schema or tracking
        _db.ChangeTracker.Clear();

        try 
        {
            return await _db.BudgetPeriods
                .AsNoTracking()
                .Include(p => p.Cycles)
                    .ThenInclude(c => c.Items)
                .Include(p => p.Transactions)
                    .ThenInclude(t => t.Splits)
                .Include(p => p.Transfers)
                .Include(p => p.ExpectedIncome)
                .Include(p => p.WatchList)
                .FirstOrDefaultAsync(p => p.Id == periodId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEEP LOAD CRASH: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"INNER: {ex.InnerException.Message}");
            return null;
        }
    }

    // 3. Original method kept intact to prevent build errors if referenced elsewhere
    public async Task<List<BudgetPeriod>> GetPeriods(int userId)
    {
        _db.ChangeTracker.Clear();

        try 
        {
            return await _db.BudgetPeriods
                .Where(p => p.UserId == userId)
                .Include(p => p.Cycles)
                    .ThenInclude(c => c.Items)
                .Include(p => p.Transactions)
                    .ThenInclude(t => t.Splits)
                .Include(p => p.Transfers)
                .Include(p => p.ExpectedIncome)
                .Include(p => p.WatchList) 
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DATABASE CRASH: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"INNER: {ex.InnerException.Message}");
            return new List<BudgetPeriod>();
        }
    }
}