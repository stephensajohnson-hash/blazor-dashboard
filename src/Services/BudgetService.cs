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
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task ImportBudgetJson(int userId, string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<RootDto>(json, options);

        if (root == null || root.Periods == null) return;

        foreach (var pDto in root.Periods)
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
            var period = new BudgetPeriod
            {
                UserId = userId,
                StringId = pDto.Id,
                DisplayName = pDto.DisplayName,
                StartDate = DateTime.Parse(pDto.StartDate),
                InitialBankBalance = pDto.InitialBankBalance
            };
            _db.BudgetPeriods.Add(period);
            await _db.SaveChangesAsync();

            // 3. Import Income Sources (Global check)
            if (pDto.IncomeSources != null)
            {
                foreach (var inc in pDto.IncomeSources)
                {
                    var existInc = await _db.BudgetIncomeSources.FirstOrDefaultAsync(x => x.UserId == userId && x.StringId == inc.Id);
                    if (existInc == null)
                    {
                        _db.BudgetIncomeSources.Add(new BudgetIncomeSource { UserId = userId, StringId = inc.Id, Name = inc.Name, ImgUrl = inc.Image });
                    }
                }
                await _db.SaveChangesAsync();
            }

            // 4. Cycles & Items (Build Mapping Dictionary)
            var itemMap = new Dictionary<string, int>();

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

            // 5. Ledger Transactions
            if (pDto.Ledger != null)
            {
                foreach (var tDto in pDto.Ledger)
                {
                    var trans = new BudgetTransaction
                    {
                        BudgetPeriodId = period.Id,
                        StringId = tDto.Id,
                        Date = DateTime.Parse(tDto.Date),
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

            // 6. Transfers
            if (pDto.Transfers != null)
            {
                foreach (var trDto in pDto.Transfers)
                {
                    string fromKey = !string.IsNullOrEmpty(trDto.FromBudgetId) ? trDto.FromBudgetId : trDto.FromId;
                    string toKey = !string.IsNullOrEmpty(trDto.ToBudgetId) ? trDto.ToBudgetId : trDto.ToId;

                    var transfer = new BudgetTransfer
                    {
                        BudgetPeriodId = period.Id,
                        Date = DateTime.Parse(trDto.Date),
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
    private class IncomeDto { public string Id { get; set; } public string Name { get; set; } public string Image { get; set; } }
}