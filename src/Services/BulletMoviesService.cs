using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class BulletMoviesService
{
    private readonly AppDbContext _db;

    public BulletMoviesService(AppDbContext db)
    {
        _db = db;
    }

    public class MovieDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string ImgUrl { get; set; } = ""; 
        public DateTime DateWatched { get; set; }
        public int ReleaseYear { get; set; }
        public double Rating { get; set; } 
        public string Notes { get; set; } = "";
        public string Genre { get; set; } = "";
        
        // Helper to store the full detail for the editor
        public BulletMediaDetail Detail { get; set; } = new();
    }

    public async Task<List<MovieDTO>> GetMovies(int userId)
    {
        // 1. Fetch Movies and their Media Details (Left Join)
        var query = from i in _db.BulletItems
                    join d in _db.BulletMediaDetails on i.Id equals d.BulletItemId into details
                    from subDetail in details.DefaultIfEmpty() // Left Join
                    where i.UserId == userId && (i.Type == "movie" || i.Type == "media")
                    select new { Item = i, Detail = subDetail };

        var data = await query.ToListAsync();
        
        // 2. Fetch Notes separately (Safe way, avoids CS1061 error)
        var itemIds = data.Select(x => x.Item.Id).ToList();
        var notes = await _db.BulletItemNotes
                             .Where(n => itemIds.Contains(n.BulletItemId))
                             .ToListAsync();

        var result = new List<MovieDTO>();

        foreach (var row in data)
        {
            var d = row.Detail ?? new BulletMediaDetail();
            
            // Find the matching note manually
            var noteContent = notes.FirstOrDefault(n => n.BulletItemId == row.Item.Id)?.Content ?? "";

            result.Add(new MovieDTO
            {
                Id = row.Item.Id,
                UserId = row.Item.UserId,
                Title = row.Item.Title,
                ImgUrl = row.Item.ImgUrl,
                DateWatched = row.Item.Date,
                ReleaseYear = d.ReleaseYear, // Real DB value
                Rating = d.Rating,           // Real DB value
                Genre = d.Tags,              // Mapping Tags to Genre
                Notes = noteContent,         // Now safely fetched
                Detail = d
            });
        }

        return result;
    }

    public async Task SaveMovie(MovieDTO dto)
    {
        BulletItem? item = null;
        if (dto.Id > 0) item = await _db.BulletItems.FindAsync(dto.Id);
        else
        {
            // We save as "movie" going forward to distinguish from generic media if needed
            item = new BulletItem { UserId = dto.UserId, Type = "movie", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        // Ensure UTC Date
        if (dto.DateWatched.Kind == DateTimeKind.Unspecified) dto.DateWatched = DateTime.SpecifyKind(dto.DateWatched, DateTimeKind.Utc);
        else if (dto.DateWatched.Kind == DateTimeKind.Local) dto.DateWatched = dto.DateWatched.ToUniversalTime();

        item.Title = dto.Title;
        item.Date = dto.DateWatched;
        item.ImgUrl = dto.ImgUrl;
        item.Category = dto.Notes; 
        
        // Save metadata in Description for portability (optional backup)
        item.Description = $"{dto.ReleaseYear}|{dto.Rating}|{dto.Genre}";

        await _db.SaveChangesAsync();
        Console.WriteLine($"[MoviesService] Saved movie: {dto.Title}");
    }

    public async Task DeleteMovie(int id)
    {
        var item = await _db.BulletItems.FindAsync(id);
        if (item != null)
        {
            _db.BulletItems.Remove(item);
            await _db.SaveChangesAsync();
        }
    }
}