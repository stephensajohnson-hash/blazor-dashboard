using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        return await _db.BudgetPeriods
            .Where(p => p.UserId == userId)
            .Include(p => p.Cycles).ThenInclude(c => c.Items)
            .Include(p => p.Transactions).ThenInclude(t => t.Splits)
            .Include(p => p.Transfers)
            // --- NEW INCLUDES ---
            .Include(p => p.ExpectedIncome)
            .Include(p => p.WatchList)
            // --------------------
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task ImportBudgetJson(int userId, string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<RootDto>(json, options);

        // Handle case where root IS the period (flat structure) vs root contains "periods" array
        List<PeriodDto> periodsToImport = new();
        
        if (root?.Periods != null)
        {
            periodsToImport = root.Periods;
        }
        else 
        {
            // Try deserializing as a single period
            var singlePeriod = JsonSerializer.Deserialize<PeriodDto>(json, options);
            if(singlePeriod != null && !string.IsNullOrEmpty(singlePeriod.Id))
            {
                periodsToImport.Add(singlePeriod);
            }
        }

        if (!periodsToImport.Any()) return;

        foreach (var pDto in periodsToImport)
        {
            // 1. Check if Period Exists (Delete old to allow re-importing fixes)
            var existing = await _db.BudgetPeriods
                .Where(x => x.UserId == userId && x.StringId == pDto.Id)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                _db.BudgetPeriods.Remove(existing);
                await _db.SaveChangesAsync();
            }

            // 2. Create Period
            // Fallback for StartDate parsing
            DateTime startDate = DateTime.UtcNow;
            if(DateTime.TryParse(pDto.StartDate, out var d)) startDate = d;

            var period = new BudgetPeriod
            {
                UserId = userId,
                StringId = pDto.Id,
                DisplayName = pDto.DisplayName,
                StartDate = startDate,
                InitialBankBalance = pDto.InitialBankBalance
            };
            _db.BudgetPeriods.Add(period);
            await _db.SaveChangesAsync();

            // 3. Import Income Sources (Definitions)
            if (pDto.IncomeSources != null)
            {
                foreach (var inc in pDto.IncomeSources)
                {
                    var existInc = await _db.BudgetIncomeSources.FirstOrDefaultAsync(x => x.UserId == userId && x.StringId == inc.Id);
                    if (existInc == null)
                    {
                        _db.BudgetIncomeSources.Add(new BudgetIncomeSource 
                        { 
                            UserId = userId, 
                            StringId = inc.Id, 
                            Name = inc.Name, 
                            ImgUrl = inc.Image ?? inc.ImgUrl // Handle both names
                        });
                    }
                }
                await _db.SaveChangesAsync();
            }

            // 4. Expected Income (Projections) - NEW
            if (pDto.ExpectedIncome != null)
            {
                foreach(var exInc in pDto.ExpectedIncome)
                {
                    DateTime exDate = startDate; // Default
                    if(DateTime.TryParse(exInc.Date, out var ed)) exDate = ed;

                    var newExpected = new BudgetExpectedIncome
                    {
                        BudgetPeriodId = period.Id,
                        SourceStringId = exInc.SourceId,
                        Amount = exInc.Amount,
                        Date = exDate
                    };
                    _db.BudgetExpectedIncome.Add(newExpected);
                }
                await _db.SaveChangesAsync();
            }

            // 5. Watch List (Expected Expenses) - NORMALIZED
            if (pDto.WatchList != null)
            {
                foreach(var w in pDto.WatchList)
                {
                    DateTime? finalDate = null;

                    // Logic: If it's NOT "TBD" and parses correctly, use the date. Else null.
                    if (!string.Equals(w.DueDate, "TBD", StringComparison.OrdinalIgnoreCase) 
                        && DateTime.TryParse(w.DueDate, out var parsedDate))
                    {
                        finalDate = parsedDate;
                    }

                    var newWatch = new BudgetWatchItem
                    {
                        BudgetPeriodId = period.Id,
                        Description = w.Description,
                        Amount = w.Amount,
                        DueDate = finalDate, // Now stores strictly as Date or Null
                        ImgUrl = w.Image
                    };
                    _db.BudgetWatchItems.Add(newWatch);
                }
                await _db.SaveChangesAsync();
            }

            // 6. Cycles & Items
            var itemMap = new Dictionary<string, int>();

            if(pDto.PaycheckCycles != null)
            {
                foreach (var cDto in pDto.PaycheckCycles)
                {
                    var cycle = new BudgetCycle { BudgetPeriodId = period.Id, CycleNumber = cDto.CycleId, Label = cDto.Label };
                    _db.BudgetCycles.Add(cycle);
                    await _db.SaveChangesAsync();

                    foreach (var iDto in cDto.BudgetItems)
                    {
                        var item = new BudgetItem
                        {
                            BudgetCycleId = cycle.Id,
                            StringId = iDto.Id,
                            Name = iDto.Name,
                            PlannedAmount = iDto.PlannedAmount,
                            CarriedOver = iDto.CarriedOver,
                            ImgUrl = iDto.Image
                        };
                        _db.BudgetItems.Add(item);
                        await _db.SaveChangesAsync();
                        
                        if (!string.IsNullOrEmpty(item.StringId)) itemMap[item.StringId] = item.Id;
                    }
                }
            }

            // 7. Ledger Transactions
            if (pDto.Ledger != null)
            {
                foreach (var tDto in pDto.Ledger)
                {
                    DateTime transDate = startDate;
                    if(DateTime.TryParse(tDto.Date, out var td)) transDate = td;

                    var trans = new BudgetTransaction
                    {
                        BudgetPeriodId = period.Id,
                        StringId = tDto.Id,
                        Date = transDate,
                        Description = tDto.Description,
                        Amount = tDto.Amount,
                        Type = tDto.Type,
                        SourceStringId = tDto.SourceId,
                        LinkedBudgetItemStringId = tDto.LinkedBudgetItemId
                    };

                    if (!string.IsNullOrEmpty(tDto.LinkedBudgetItemId) && itemMap.ContainsKey(tDto.LinkedBudgetItemId))
                    {
                        trans.ResolvedBudgetItemId = itemMap[tDto.LinkedBudgetItemId];
                    }

                    _db.BudgetTransactions.Add(trans);
                    await _db.SaveChangesAsync();

                    // Splits
                    if (tDto.Splits != null)
                    {
                        foreach (var sDto in tDto.Splits)
                        {
                            var split = new BudgetTransactionSplit
                            {
                                BudgetTransactionId = trans.Id,
                                LinkedBudgetItemStringId = sDto.LinkedBudgetItemId,
                                Amount = sDto.Amount ?? 0,
                                Note = sDto.Note
                            };
                            if (!string.IsNullOrEmpty(sDto.LinkedBudgetItemId) && itemMap.ContainsKey(sDto.LinkedBudgetItemId))
                            {
                                split.ResolvedBudgetItemId = itemMap[sDto.LinkedBudgetItemId];
                            }
                            _db.BudgetTransactionSplits.Add(split);
                        }
                    }
                }
            }

            // 8. Transfers
            if (pDto.Transfers != null)
            {
                foreach (var trDto in pDto.Transfers)
                {
                    string fromKey = !string.IsNullOrEmpty(trDto.FromBudgetId) ? trDto.FromBudgetId : trDto.FromId;
                    string toKey = !string.IsNullOrEmpty(trDto.ToBudgetId) ? trDto.ToBudgetId : trDto.ToId;
                    
                    DateTime transferDate = startDate;
                    if(DateTime.TryParse(trDto.Date, out var td)) transferDate = td;

                    var transfer = new BudgetTransfer
                    {
                        BudgetPeriodId = period.Id,
                        Date = transferDate,
                        Amount = trDto.Amount,
                        Note = trDto.Note,
                        FromStringId = fromKey,
                        ToStringId = toKey
                    };

                    if (!string.IsNullOrEmpty(fromKey) && itemMap.ContainsKey(fromKey)) transfer.ResolvedFromId = itemMap[fromKey];
                    if (!string.IsNullOrEmpty(toKey) && itemMap.ContainsKey(toKey)) transfer.ResolvedToId = itemMap[toKey];

                    _db.BudgetTransfers.Add(transfer);
                }
            }

            await _db.SaveChangesAsync();
        }
    }

    // --- DTO CLASSES ---
    private class RootDto { public List<PeriodDto> Periods { get; set; } }
    
    private class PeriodDto { 
        public string Id { get; set; } 
        public string DisplayName { get; set; }
        public string StartDate { get; set; }
        public decimal InitialBankBalance { get; set; }
        public List<CycleDto> PaycheckCycles { get; set; }
        public List<LedgerDto> Ledger { get; set; }
        public List<TransferDto> Transfers { get; set; }
        public List<IncomeDto> IncomeSources { get; set; }
        
        // NEW DTO PROPERTIES
        public List<ExpectedIncomeDto> ExpectedIncome { get; set; }
        public List<WatchListDto> WatchList { get; set; }
    }

    private class CycleDto { public int CycleId { get; set; } public string Label { get; set; } public List<ItemDto> BudgetItems { get; set; } }
    private class ItemDto { public string Id { get; set; } public string Name { get; set; } public decimal PlannedAmount { get; set; } public decimal CarriedOver { get; set; } public string Image { get; set; } }
    
    private class LedgerDto { public string Id { get; set; } public string Date { get; set; } public string Description { get; set; } public decimal Amount { get; set; } public string Type { get; set; } public string SourceId { get; set; } public string LinkedBudgetItemId { get; set; } public List<SplitDto> Splits { get; set; } }
    private class SplitDto { public string LinkedBudgetItemId { get; set; } public decimal? Amount { get; set; } public string Note { get; set; } }
    
    private class TransferDto { 
        public string Id { get; set; } public string Date { get; set; } public decimal Amount { get; set; } public string Note { get; set; } 
        public string FromBudgetId { get; set; } public string ToBudgetId { get; set; }
        public string FromId { get; set; } public string ToId { get; set; }
    }
    
    private class IncomeDto { public string Id { get; set; } public string Name { get; set; } public string Image { get; set; } public string ImgUrl { get; set; } }

    // NEW DTO CLASSES
    private class ExpectedIncomeDto { public string SourceId { get; set; } public decimal Amount { get; set; } public string Date { get; set; } }
    private class WatchListDto { public string Description { get; set; } public decimal Amount { get; set; } public string DueDate { get; set; } public string Image { get; set; } }
}