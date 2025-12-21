using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class BulletSportsService
{
    private readonly AppDbContext _db;

    public BulletSportsService(AppDbContext db)
    {
        _db = db;
    }

    public class TeamRecord
    {
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Ties { get; set; }
        public int TotalGames => Wins + Losses + Ties;
        public double WinPercentage => TotalGames == 0 ? 0 : (double)(Wins + (0.5 * Ties)) / TotalGames;
        public string Display => $"{Wins}-{Losses}-{Ties}";
        public string PctDisplay => TotalGames > 0 ? $".{(int)(WinPercentage * 1000):D3}" : ".000";
    }

    public class GameDTO : BulletItem
    {
        public BulletGameDetail Detail { get; set; } = new();
        public League? League { get; set; }
        public Season? Season { get; set; }
        public Team? HomeTeam { get; set; }
        public Team? AwayTeam { get; set; }
        public TeamRecord? FavoriteRecord { get; set; } 
    }

    // --- GAMES ---
    public async Task<List<GameDTO>> GetGamesForRange(int userId, DateTime start, DateTime end)
    {
        var games = await (from baseItem in _db.BulletItems
                           join detail in _db.BulletGameDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId 
                                 && baseItem.Date >= start && baseItem.Date <= end
                                 && baseItem.Type == "sports"
                           select new GameDTO 
                           { 
                               Id = baseItem.Id, UserId = baseItem.UserId, Type = baseItem.Type, Category = baseItem.Category,
                               Date = baseItem.Date, Title = baseItem.Title, Description = baseItem.Description, 
                               ImgUrl = baseItem.ImgUrl, LinkUrl = baseItem.LinkUrl, OriginalStringId = baseItem.OriginalStringId,
                               SortOrder = baseItem.SortOrder,
                               Detail = detail
                           }).ToListAsync();

        if (games.Any())
        {
            var leagueIds = games.Select(g => g.Detail.LeagueId).Distinct().ToList();
            var seasonIds = games.Select(g => g.Detail.SeasonId).Distinct().ToList();
            var teamIds = games.Select(g => g.Detail.HomeTeamId).Concat(games.Select(g => g.Detail.AwayTeamId)).Distinct().ToList();

            var leagues = await _db.Leagues.Where(l => leagueIds.Contains(l.Id)).ToListAsync();
            var seasons = await _db.Seasons.Where(s => seasonIds.Contains(s.Id)).ToListAsync();
            var teams = await _db.Teams.Where(t => teamIds.Contains(t.Id)).ToListAsync();

            foreach (var g in games)
            {
                g.League = leagues.FirstOrDefault(l => l.Id == g.Detail.LeagueId);
                g.Season = seasons.FirstOrDefault(s => s.Id == g.Detail.SeasonId);
                g.HomeTeam = teams.FirstOrDefault(t => t.Id == g.Detail.HomeTeamId);
                g.AwayTeam = teams.FirstOrDefault(t => t.Id == g.Detail.AwayTeamId);

                if (g.Detail.SeasonId > 0)
                {
                    int favTeamId = 0;
                    if (g.HomeTeam?.IsFavorite == true) favTeamId = g.HomeTeam.Id;
                    else if (g.AwayTeam?.IsFavorite == true) favTeamId = g.AwayTeam.Id;

                    if (favTeamId > 0)
                    {
                        g.FavoriteRecord = await GetTeamRecord(favTeamId, g.Detail.SeasonId, g.Date, g.Id);
                    }
                }
            }
        }
        return games;
    }

    private async Task<TeamRecord> GetTeamRecord(int teamId, int seasonId, DateTime gameDate, int currentGameId)
    {
        var record = new TeamRecord();
        var pastGames = await (from d in _db.BulletGameDetails
                               join b in _db.BulletItems on d.BulletItemId equals b.Id
                               where d.SeasonId == seasonId 
                                     && d.IsComplete 
                                     && (b.Date < gameDate || b.Id == currentGameId)
                                     && (d.HomeTeamId == teamId || d.AwayTeamId == teamId)
                               select d).ToListAsync();

        foreach (var g in pastGames)
        {
            bool isHome = g.HomeTeamId == teamId;
            int myScore = isHome ? g.HomeScore : g.AwayScore;
            int oppScore = isHome ? g.AwayScore : g.HomeScore;

            if (myScore > oppScore) record.Wins++;
            else if (myScore < oppScore) record.Losses++;
            else record.Ties++;
        }
        return record;
    }

    public async Task SaveGame(GameDTO dto)
    {
        BulletItem? item = null;
        if (dto.Date.Kind == DateTimeKind.Unspecified) dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

        if (dto.Id > 0) item = await _db.BulletItems.FindAsync(dto.Id);
        else {
            item = new BulletItem { UserId = dto.UserId, Type = "sports", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title; item.Category = dto.Category; item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl; item.LinkUrl = dto.LinkUrl; item.Date = dto.Date;
        item.SortOrder = dto.SortOrder;
        
        await _db.SaveChangesAsync();

        var detail = await _db.BulletGameDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletGameDetail { BulletItemId = item.Id }; await _db.BulletGameDetails.AddAsync(detail); }

        detail.LeagueId = dto.Detail.LeagueId;
        detail.SeasonId = dto.Detail.SeasonId;
        detail.HomeTeamId = dto.Detail.HomeTeamId;
        detail.AwayTeamId = dto.Detail.AwayTeamId;
        detail.HomeScore = dto.Detail.HomeScore;
        detail.AwayScore = dto.Detail.AwayScore;
        detail.IsComplete = dto.Detail.IsComplete;
        detail.StartTime = dto.Detail.StartTime;
        detail.TvChannel = dto.Detail.TvChannel;

        await _db.SaveChangesAsync();
    }

    public async Task ToggleComplete(int id, bool isComplete)
    {
        var detail = await _db.BulletGameDetails.FindAsync(id);
        if (detail != null) { detail.IsComplete = isComplete; await _db.SaveChangesAsync(); }
    }

    // --- REFERENCE DATA (TEAMS, LEAGUES, SEASONS) ---
    // These were missing causing the build error in TaskEditor
    
    public async Task<List<Team>> GetTeams(int userId) => await _db.Teams.Where(t => t.UserId == userId).OrderBy(t => t.Name).ToListAsync();
    public async Task<List<League>> GetLeagues(int userId) => await _db.Leagues.Where(l => l.UserId == userId).OrderBy(l => l.Name).ToListAsync();
    public async Task<List<Season>> GetSeasons(int userId) => await _db.Seasons.Where(s => s.UserId == userId).OrderByDescending(s => s.Name).ToListAsync();

    // --- IMPORT LOGIC ---
    public async Task<int> ImportGames(int userId, string jsonContent)
    {
        int count = 0;
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;
        
        JsonElement items = root;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var i)) items = i;

        if (items.ValueKind == JsonValueKind.Array)
        {
            var leagues = await _db.Leagues.Where(l => l.UserId == userId).ToListAsync();
            var teams = await _db.Teams.Where(t => t.UserId == userId).ToListAsync();
            
            foreach (var el in items.EnumerateArray())
            {
                string type = (el.TryGetProperty("type", out var t) ? t.ToString() : "").ToLower();
                if (type == "sports")
                {
                    string title = (el.TryGetProperty("title", out var tit) ? tit.ToString() : ""); 
                    string dateStr = (el.TryGetProperty("date", out var d) ? d.ToString() : "");
                    string leagueName = (el.TryGetProperty("league", out var l) ? l.ToString() : "");
                    string seasonName = (el.TryGetProperty("season", out var s) ? s.ToString() : "");
                    string originalId = (el.TryGetProperty("id", out var oid) ? oid.ToString() : "");

                    if (!string.IsNullOrEmpty(originalId))
                    {
                        var existing = await _db.BulletItems.FirstOrDefaultAsync(x => x.UserId == userId && x.OriginalStringId == originalId && x.Type == "sports");
                        if(existing != null) { _db.BulletItems.Remove(existing); await _db.SaveChangesAsync(); }
                    }
                    
                    DateTime gameDate = DateTime.UtcNow;
                    if (DateTime.TryParse(dateStr, out var pd)) gameDate = DateTime.SpecifyKind(pd, DateTimeKind.Utc);

                    var league = leagues.FirstOrDefault(x => x.Name.Equals(leagueName, StringComparison.OrdinalIgnoreCase));
                    int leagueId = league?.Id ?? 0;

                    int seasonId = 0;
                    if (!string.IsNullOrEmpty(seasonName) && leagueId > 0)
                    {
                        var season = await _db.Seasons.FirstOrDefaultAsync(x => x.UserId == userId && x.LeagueId == leagueId && x.Name == seasonName);
                        if (season == null) { season = new Season { UserId = userId, LeagueId = leagueId, Name = seasonName }; await _db.Seasons.AddAsync(season); await _db.SaveChangesAsync(); }
                        seasonId = season.Id;
                    }

                    int homeId = 0; int awayId = 0;
                    if (!string.IsNullOrEmpty(title) && title.Contains("@"))
                    {
                        var parts = title.Split('@');
                        if (parts.Length == 2)
                        {
                            string awayAbbr = parts[0].Trim(); string homeAbbr = parts[1].Trim();
                            var awayTeam = teams.FirstOrDefault(x => x.LeagueId == leagueId && x.Abbreviation.Equals(awayAbbr, StringComparison.OrdinalIgnoreCase));
                            var homeTeam = teams.FirstOrDefault(x => x.LeagueId == leagueId && x.Abbreviation.Equals(homeAbbr, StringComparison.OrdinalIgnoreCase));
                            awayId = awayTeam?.Id ?? 0; homeId = homeTeam?.Id ?? 0;
                        }
                    }

                    var item = new BulletItem { UserId = userId, Type = "sports", Category = "personal", Title = title, Date = gameDate, OriginalStringId = originalId };
                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync();

                    var detail = new BulletGameDetail { BulletItemId = item.Id, LeagueId = leagueId, SeasonId = seasonId, HomeTeamId = homeId, AwayTeamId = awayId, IsComplete = (el.TryGetProperty("status", out var st) && st.ToString() == "completed"), TvChannel = (el.TryGetProperty("tvChannel", out var tv) ? tv.ToString() : "") };
                    if (el.TryGetProperty("homeScore", out var hs) && hs.ValueKind == JsonValueKind.Number) detail.HomeScore = hs.GetInt32();
                    if (el.TryGetProperty("awayScore", out var @as) && @as.ValueKind == JsonValueKind.Number) detail.AwayScore = @as.GetInt32();
                    if (el.TryGetProperty("startTime", out var stm) && TimeSpan.TryParse(stm.ToString(), out var ts)) { detail.StartTime = gameDate.Date + ts; }
                    await _db.BulletGameDetails.AddAsync(detail);
                    count++;
                }
            }
            await _db.SaveChangesAsync();
        }
        return count;
    }

    public async Task<List<Team>> GetFavoriteTeams(int userId)
    {
        using var db = _factory.CreateDbContext();
        return await db.Teams
            .Where(t => t.UserId == userId && t.IsFavorite)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<GameDTO>> GetTeamSchedule(int userId, int teamId, int seasonId)
    {
        using var db = _factory.CreateDbContext();
        
        var items = await (from baseItem in db.BulletItems
                           join detail in db.BulletGameDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId 
                                 && detail.SeasonId == seasonId
                                 && (detail.HomeTeamId == teamId || detail.AwayTeamId == teamId)
                           orderby baseItem.Date
                           select new GameDTO 
                           { 
                               Id = baseItem.Id, 
                               UserId = baseItem.UserId, 
                               Type = baseItem.Type, 
                               Category = baseItem.Category,
                               Date = baseItem.Date, 
                               Title = baseItem.Title, 
                               Description = baseItem.Description, 
                               ImgUrl = baseItem.ImgUrl, 
                               LinkUrl = baseItem.LinkUrl, 
                               OriginalStringId = baseItem.OriginalStringId,
                               SortOrder = baseItem.SortOrder,
                               Detail = detail
                           }).ToListAsync();

        return items;
    }
}