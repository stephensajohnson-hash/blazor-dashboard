using Microsoft.EntityFrameworkCore;
using Dashboard.Models;

namespace Dashboard.Services;

public class PtoService
{
    private readonly AppDbContext _db;

    public PtoService(AppDbContext db)
    {
        _db = db;
    }

    // Get the policy for a specific user and year
    public async Task<PtoPolicy?> GetPolicyAsync(int userId, int year)
    {
        return await _db.PtoPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Year == year);
    }

    // Create a new policy
    public async Task<PtoPolicy> CreatePolicyAsync(PtoPolicy policy)
    {
        _db.PtoPolicies.Add(policy);
        await _db.SaveChangesAsync();
        return policy;
    }

    // Update an existing policy
    public async Task<PtoPolicy> UpdatePolicyAsync(PtoPolicy policy)
    {
        _db.PtoPolicies.Update(policy);
        await _db.SaveChangesAsync();
        return policy;
    }
}