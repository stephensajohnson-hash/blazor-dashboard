using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dashboard.Services;

public class PtoService
{
    private readonly AppDbContext _db;

    public PtoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PtoPolicy?> GetPolicyAsync(int userId, int year)
    {
        return await _db.PtoPolicies
            .AsNoTracking()
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Year == year);
    }

    public async Task<PtoPolicy> CreatePolicyAsync(PtoPolicy policy)
    {
        _db.PtoPolicies.Add(policy);
        await _db.SaveChangesAsync();
        return policy;
    }

    public async Task<PtoPolicy> UpdatePolicyAsync(PtoPolicy policy)
    {
        _db.PtoPolicies.Update(policy);
        await _db.SaveChangesAsync();
        return policy;
    }

    public async Task<PtoEntry> SaveEntryAsync(PtoEntry entry)
    {
        if (entry.Id == 0)
        {
            _db.PtoEntries.Add(entry);
        }
        else
        {
            _db.PtoEntries.Update(entry);
        }
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task DeleteEntryAsync(int id)
    {
        var entry = await _db.PtoEntries.FindAsync(id);
        if (entry != null)
        {
            _db.PtoEntries.Remove(entry);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Deletes any existing 'Accrual' entries for the policy year and generates a fresh schedule.
    /// </summary>
    public async Task GenerateAccrualScheduleAsync(int policyId)
    {
        var policy = await _db.PtoPolicies.FindAsync(policyId);
        if (policy == null) return;

        // 1. Remove existing auto-generated accruals
        var existingAccruals = await _db.PtoEntries
            .Where(e => e.PtoPolicyId == policyId && e.EntryType == PtoEntryType.Accrual)
            .ToListAsync();
            
        _db.PtoEntries.RemoveRange(existingAccruals);
        await _db.SaveChangesAsync(); // Save to clear the ledger

        // 2. Generate dates based on the interval
        var accrualDates = new List<DateTime>();
        DateTime currentDate = policy.AccrualStartDate;

        while (currentDate.Year == policy.Year)
        {
            accrualDates.Add(currentDate);

            switch (policy.Interval)
            {
                case PtoAccrualInterval.Weekly:
                    currentDate = currentDate.AddDays(7);
                    break;
                case PtoAccrualInterval.BiWeekly:
                    currentDate = currentDate.AddDays(14);
                    break;
                case PtoAccrualInterval.Monthly:
                    currentDate = currentDate.AddMonths(1);
                    break;
                case PtoAccrualInterval.SemiMonthly:
                    // Usually 1st and 15th
                    if (currentDate.Day < 15)
                        currentDate = new DateTime(currentDate.Year, currentDate.Month, 15);
                    else
                        currentDate = new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(1);
                    break;
                case PtoAccrualInterval.Annually:
                    currentDate = currentDate.AddYears(1); // Will exit loop immediately
                    break;
            }
        }

        // 3. Calculate hours per drop and create entries
        if (accrualDates.Count > 0)
        {
            decimal hoursPerDrop = Math.Round(policy.TotalAllowanceHours / accrualDates.Count, 2);

            foreach (var date in accrualDates)
            {
                _db.PtoEntries.Add(new PtoEntry
                {
                    PtoPolicyId = policyId,
                    Date = date,
                    Description = "Scheduled Accrual",
                    Amount = hoursPerDrop,
                    EntryType = PtoEntryType.Accrual,
                    Status = PtoEntryStatus.Approved // Accruals are considered approved/posted
                });
            }
            await _db.SaveChangesAsync();
        }
    }
}