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
    public async Task<List<League>> GetLeagues(int userId) => await _db.Leagues.Where(l => l.UserId == userId).OrderBy(l => l.Name).ToListAsync();

    public async Task SaveLeague(League league)
    {
        if (league.Id == 0) await _db.Leagues.AddAsync(league);
        else {
            var existing = await _db.Leagues.FindAsync(league.Id);
            if (existing != null) {
                existing.Name = league.Name;
                existing.ImgUrl = league.ImgUrl;
                existing.LinkUrl = league.LinkUrl;
            }
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeleteLeague(int id) { var l = await _db.Leagues.FindAsync(id); if (l != null) { _db.Leagues.Remove(l); await _db.SaveChangesAsync(); } }
    
    public async Task ClearLeagues(int userId) 
    { 
        var items = await _db.Leagues.Where(x => x.UserId == userId).ToListAsync(); 
        _db.Leagues.RemoveRange(items); 
        await _db.SaveChangesAsync(); 
    }

    // --- SEASONS ---
    public async Task<List<Season>> GetSeasons(int userId) => await _db.Seasons.Where(s => s.UserId == userId).OrderByDescending(s => s.Name).ToListAsync();

    public async Task SaveSeason(Season season)
    {
        if (season.Id == 0) await _db.Seasons.AddAsync(season);
        else {
            var existing = await _db.Seasons.FindAsync(season.Id);
            if (existing != null) {
                existing.Name = season.Name;
                existing.ImgUrl = season.ImgUrl;
                existing.LeagueId = season.LeagueId;
            }
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeleteSeason(int id) { var s = await _db.Seasons.FindAsync(id); if (s != null) { _db.Seasons.Remove(s); await _db.SaveChangesAsync(); } }
    
    public async Task ClearSeasons(int userId) 
    { 
        var items = await _db.Seasons.Where(x => x.UserId == userId).ToListAsync(); 
        _db.Seasons.RemoveRange(items); 
        await _db.SaveChangesAsync(); 
    }

    // --- TEAMS ---
    public async Task<List<Team>> GetTeams(int userId) => await _db.Teams.Where(t => t.UserId == userId).OrderBy(t => t.Name).ToListAsync();

    public async Task SaveTeam(Team team)
    {
        if (team.Id == 0) await _db.Teams.AddAsync(team);
        else {
            var existing = await _db.Teams.FindAsync(team.Id);
            if (existing != null) {
                existing.Name = team.Name;
                existing.Abbreviation = team.Abbreviation;
                existing.LogoUrl = team.LogoUrl;
                existing.LeagueId = team.LeagueId;
                existing.IsFavorite = team.IsFavorite;
            }
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeleteTeam(int id) { var t = await _db.Teams.FindAsync(id); if (t != null) { _db.Teams.Remove(t); await _db.SaveChangesAsync(); } }
    
    public async Task ClearTeams(int userId) 
    { 
        var items = await _db.Teams.Where(x => x.UserId == userId).ToListAsync(); 
        _db.Teams.RemoveRange(items); 
        await _db.SaveChangesAsync(); 
    }

    // --- IMPORT ---
    public async Task<int> ImportSportsData(int userId, string jsonContent)
    {
        int count = 0;
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        // 1. IMPORT LEAGUES FIRST (Crucial so IDs exist)
        if (root.TryGetProperty("leagues", out var leaguesArr) && leaguesArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var l in leaguesArr.EnumerateArray())
            {
                string name = l.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(name))
                {
                    if (!await _db.Leagues.AnyAsync(x => x.UserId == userId && x.Name == name))
                    {
                        await _db.Leagues.AddAsync(new League { UserId = userId, Name = name });
                        count++;
                    }
                }
            }
            await _db.SaveChangesAsync(); // Save immediately to generate IDs
        }

        // 2. IMPORT TEAMS
        if (root.TryGetProperty("teams", out var teamsArr) && teamsArr.ValueKind == JsonValueKind.Array)
        {
            // Refresh local league cache to get the new IDs
            var userLeagues = await _db.Leagues.Where(l => l.UserId == userId).ToListAsync();

            foreach (var t in teamsArr.EnumerateArray())
            {
                string name = t.TryGetProperty("name", out var n) ? n.ToString() : "";
                string abbr = t.TryGetProperty("abbr", out var a) ? a.ToString() : "";
                string leagueName = t.TryGetProperty("league", out var l) ? l.ToString() : "";
                
                // MAPPING LOGO: Check "logo", "logoUrl", or "img"
                string logo = "";
                if(t.TryGetProperty("logo", out var lg)) logo = lg.ToString();
                else if(t.TryGetProperty("logoUrl", out lg)) logo = lg.ToString();
                else if(t.TryGetProperty("img", out lg)) logo = lg.ToString();

                bool isFav = t.TryGetProperty("isFavorite", out var f) && f.GetBoolean();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    // Find League ID
                    var league = userLeagues.FirstOrDefault(x => x.Name.Equals(leagueName, StringComparison.OrdinalIgnoreCase));
                    int leagueId = league?.Id ?? 0;

                    var existingTeam = await _db.Teams.FirstOrDefaultAsync(x => x.UserId == userId && x.Name == name);
                    
                    if (existingTeam == null)
                    {
                        await _db.Teams.AddAsync(new Team 
                        { 
                            UserId = userId, 
                            Name = name, 
                            Abbreviation = abbr, 
                            LeagueId = leagueId, 
                            LogoUrl = logo, 
                            IsFavorite = isFav 
                        });
                        count++;
                    }
                    else
                    {
                        // Update existing team if missing data
                        bool changed = false;
                        if(existingTeam.LeagueId == 0 && leagueId != 0) { existingTeam.LeagueId = leagueId; changed = true; }
                        if(string.IsNullOrEmpty(existingTeam.LogoUrl) && !string.IsNullOrEmpty(logo)) { existingTeam.LogoUrl = logo; changed = true; }
                        if(string.IsNullOrEmpty(existingTeam.Abbreviation) && !string.IsNullOrEmpty(abbr)) { existingTeam.Abbreviation = abbr; changed = true; }
                        if(changed) _db.Teams.Update(existingTeam);
                    }
                }
            }
            await _db.SaveChangesAsync();
        }

        return count;
    }
}