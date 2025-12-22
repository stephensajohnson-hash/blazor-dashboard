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
    }

    public class GameDTO : BulletTaskService.TaskDTO 
    {
        public new BulletGameDetail Detail { get; set; } = new();
        public League? League { get; set; }
        public Season? Season { get; set; }
        public Team? HomeTeam { get; set; }
        public Team? AwayTeam { get; set; }
        public TeamRecord? FavoriteRecord { get; set; } 
    }

    // --- CALENDAR DATA ---
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
        else if (dto.Date.Kind == DateTimeKind.Local) dto.Date = dto.Date.ToUniversalTime();

        if (dto.Id > 0) item = await _db.BulletItems.FindAsync(dto.Id);
        else {
            item = new BulletItem { UserId = dto.UserId, Type = "sports", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title; item.Category = dto.Category; item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl; item.LinkUrl = dto.LinkUrl; item.Date = dto.Date;
        item.SortOrder = dto.SortOrder;
        
        await _db.SaveChangesAsync();
        dto.Id = item.Id;

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

    // --- MANAGE SPORTS DATA ---

    public async Task AddLeague(int userId, string name, string imgUrl, string linkUrl)
    {
        _db.Leagues.Add(new League { UserId = userId, Name = name, ImgUrl = imgUrl, LinkUrl = linkUrl });
        await _db.SaveChangesAsync();
    }
    public async Task UpdateLeague(League l) { _db.Leagues.Update(l); await _db.SaveChangesAsync(); }
    public async Task DeleteLeague(int id) { var x = await _db.Leagues.FindAsync(id); if(x!=null) { _db.Leagues.Remove(x); await _db.SaveChangesAsync(); } }

    public async Task AddSeason(int userId, int leagueId, string name, string imgUrl)
    {
        _db.Seasons.Add(new Season { UserId = userId, LeagueId = leagueId, Name = name, ImgUrl = imgUrl });
        await _db.SaveChangesAsync();
    }
    public async Task UpdateSeason(Season s) { _db.Seasons.Update(s); await _db.SaveChangesAsync(); }
    public async Task DeleteSeason(int id) { var x = await _db.Seasons.FindAsync(id); if(x!=null) { _db.Seasons.Remove(x); await _db.SaveChangesAsync(); } }

    public async Task AddTeam(int userId, int leagueId, string name, string abbr, string logoUrl, bool isFav)
    {
        _db.Teams.Add(new Team { UserId = userId, LeagueId = leagueId, Name = name, Abbreviation = abbr, LogoUrl = logoUrl, IsFavorite = isFav });
        await _db.SaveChangesAsync();
    }
    public async Task UpdateTeam(Team t) { _db.Teams.Update(t); await _db.SaveChangesAsync(); }
    public async Task DeleteTeam(int id) { var x = await _db.Teams.FindAsync(id); if(x!=null) { _db.Teams.Remove(x); await _db.SaveChangesAsync(); } }

    public async Task<List<Team>> GetTeams(int userId) => await _db.Teams.Where(t => t.UserId == userId).OrderBy(t => t.Name).ToListAsync();
    public async Task<List<League>> GetLeagues(int userId) => await _db.Leagues.Where(l => l.UserId == userId).OrderBy(l => l.Name).ToListAsync();
    public async Task<List<Season>> GetSeasons(int userId) => await _db.Seasons.Where(s => s.UserId == userId).OrderByDescending(s => s.Name).ToListAsync();

    // --- SCHEDULE & FAVORITES ---

    public async Task<List<Team>> GetFavoriteTeams(int userId)
    {
        return await _db.Teams.Where(t => t.UserId == userId && t.IsFavorite).OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<int> GetMostRecentActiveSeasonId(int userId, int teamId)
    {
        // 1. Try to find the most recent COMPLETED game
        var last = await _db.BulletGameDetails
            .Where(d => (d.HomeTeamId == teamId || d.AwayTeamId == teamId) && d.IsComplete)
            .OrderByDescending(d => d.StartTime)
            .FirstOrDefaultAsync();
        
        if (last != null && last.SeasonId > 0) return last.SeasonId;

        // 2. If no completed games, find the NEXT scheduled game
        var next = await _db.BulletGameDetails
            .Where(d => (d.HomeTeamId == teamId || d.AwayTeamId == teamId))
            .OrderBy(d => d.StartTime)
            .FirstOrDefaultAsync();

        return next?.SeasonId ?? 0;
    }

    public async Task<List<GameDTO>> GetTeamSchedule(int userId, int teamId, int seasonId)
    {
        var items = await (from baseItem in _db.BulletItems
                           join detail in _db.BulletGameDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId 
                                 && detail.SeasonId == seasonId
                                 && (detail.HomeTeamId == teamId || detail.AwayTeamId == teamId)
                           orderby baseItem.Date
                           select new GameDTO 
                           { 
                               Id = baseItem.Id, UserId = baseItem.UserId, Type = baseItem.Type, Category = baseItem.Category,
                               Date = baseItem.Date, Title = baseItem.Title, Description = baseItem.Description, 
                               ImgUrl = baseItem.ImgUrl, LinkUrl = baseItem.LinkUrl, OriginalStringId = baseItem.OriginalStringId,
                               SortOrder = baseItem.SortOrder,
                               Detail = detail
                           }).ToListAsync();

        if (items.Any())
        {
            var teams = await _db.Teams.Where(t => t.UserId == userId).ToListAsync();
            foreach (var g in items)
            {
                g.HomeTeam = teams.FirstOrDefault(t => t.Id == g.Detail.HomeTeamId);
                g.AwayTeam = teams.FirstOrDefault(t => t.Id == g.Detail.AwayTeamId);
            }
        }
        return items;
    }

    public async Task<int> ImportGames(int userId, string jsonContent) { return 0; /* Imported preserved elsewhere if needed */ }
}