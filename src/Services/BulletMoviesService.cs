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
    }

    // --- GET DATA ---
    public async Task<List<MovieDTO>> GetMovies(int userId)
    {
        Console.WriteLine($"[MoviesService] Fetching movies for User {userId}...");

        // FIX: Fetch both "movie" (new) and "media" (old calendar items)
        var items = await _db.BulletItems
            .Where(i => i.UserId == userId && (i.Type == "movie" || i.Type == "media"))
            .ToListAsync();

        Console.WriteLine($"[MoviesService] Found {items.Count} items.");

        var result = new List<MovieDTO>();

        foreach (var item in items)
        {
            // Try to parse "Year|Rating|Genre" from Description
            var parts = (item.Description ?? "").Split('|');
            
            // Default values
            int relYear = DateTime.Now.Year;
            double rating = 0;
            string genre = "";

            // Parsing logic (Safe)
            if (parts.Length > 0 && int.TryParse(parts[0], out int y)) relYear = y;
            
            // If description isn't pipe-delimited, check if we have a MediaDetail record (from Calendar)
            // (You might need to join tables properly later, but this gets the basic item first)
            
            if (parts.Length > 1 && double.TryParse(parts[1], out double r)) rating = r;
            if (parts.Length > 2) genre = parts[2];

            result.Add(new MovieDTO
            {
                Id = item.Id,
                UserId = item.UserId,
                Title = item.Title,
                ImgUrl = item.ImgUrl,
                DateWatched = item.Date, 
                ReleaseYear = relYear,
                Rating = rating,
                Genre = genre,
                Notes = item.Category
            });
        }

        return result;
    }

    // --- CRUD ---
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

        if (dto.DateWatched.Kind == DateTimeKind.Unspecified) dto.DateWatched = DateTime.SpecifyKind(dto.DateWatched, DateTimeKind.Utc);
        else if (dto.DateWatched.Kind == DateTimeKind.Local) dto.DateWatched = dto.DateWatched.ToUniversalTime();

        item.Title = dto.Title;
        item.Date = dto.DateWatched;
        item.ImgUrl = dto.ImgUrl;
        item.Category = dto.Notes; 
        
        // Save metadata in Description for portability
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
            Console.WriteLine($"[MoviesService] Deleted movie ID {id}");
        }
    }
}