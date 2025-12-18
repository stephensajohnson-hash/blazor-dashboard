using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class SportsService
{
    private readonly AppDbContext _db;

    public SportsService(AppDbContext db)
    {
        _db = db;
    }

    // --- LEAGUES ---

    public async Task<List<League>> GetLeagues()
    {
        return await _db.Leagues.OrderBy(l => l.Name).ToListAsync();
    }

    public async Task SaveLeague(League league)
    {
        if (league.Id == 0)
        {
            await _db.Leagues.AddAsync(league);
        }
        else
        {
            var existing = await _db.Leagues.FindAsync(league.Id);
            if (existing != null)
            {
                existing.Name = league.Name;
                existing.ImgUrl = league.ImgUrl;
                existing.LinkUrl = league.LinkUrl;
            }
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeleteLeague(int id)
    {
        var league = await _db.Leagues.FindAsync(id);
        if (league != null)
        {
            _db.Leagues.Remove(league);
            await _db.SaveChangesAsync();
        }
    }

    // --- IMPORT ---

    public async Task<int> ImportLeaguesFromJson(string jsonContent)
    {
        int count = 0;
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        // Check if root has "leagues" array
        if (root.TryGetProperty("leagues", out var leaguesArr) && leaguesArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var l in leaguesArr.EnumerateArray())
            {
                string name = l.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(name))
                {
                    // Check duplicate
                    if (!await _db.Leagues.AnyAsync(x => x.Name == name))
                    {
                        await _db.Leagues.AddAsync(new League { Name = name });
                        count++;
                    }
                }
            }
            await _db.SaveChangesAsync();
        }
        return count;
    }
}