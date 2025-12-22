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
        public string ImgUrl { get; set; } = ""; // Poster
        public DateTime DateWatched { get; set; }
        public int ReleaseYear { get; set; }
        public double Rating { get; set; } // 0 to 5 or 0 to 10
        public string Notes { get; set; } = "";
        public string Genre { get; set; } = "";
    }

    // --- GET DATA ---
    public async Task<List<MovieDTO>> GetMovies(int userId)
    {
        // Assuming 'movies' are stored in BulletItems with Type = "movie"
        // And specific details (Rating, ReleaseYear) are in JSON or a generic column
        // For this example, I will assume a standard mapping to BulletItems
        
        var items = await _db.BulletItems
            .Where(i => i.UserId == userId && i.Type == "movie")
            .ToListAsync();

        var result = new List<MovieDTO>();

        foreach (var item in items)
        {
            // Parse details from the generic Description or a JSON column if you have one.
            // PRO TIP: If you don't have a specific 'BulletMovieDetails' table yet, 
            // we can stick ReleaseYear and Rating in the generic 'Category' or 'Description' for now.
            // Here I assume you might store them in the existing fields:
            
            // Description format example: "2024|4.5|Action" (Year|Rating|Genre)
            // Or if you have a real detail table, swap this logic.
            
            var parts = (item.Description ?? "").Split('|');
            int relYear = DateTime.Now.Year;
            double rating = 0;
            string genre = "";

            if (parts.Length > 0 && int.TryParse(parts[0], out int y)) relYear = y;
            if (parts.Length > 1 && double.TryParse(parts[1], out double r)) rating = r;
            if (parts.Length > 2) genre = parts[2];

            result.Add(new MovieDTO
            {
                Id = item.Id,
                UserId = item.UserId,
                Title = item.Title,
                ImgUrl = item.ImgUrl,
                DateWatched = item.Date, // This is Date Watched
                ReleaseYear = relYear,
                Rating = rating,
                Genre = genre,
                Notes = item.Category // Using Category for notes for now
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
            item = new BulletItem { UserId = dto.UserId, Type = "movie", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        // Ensure UTC Date
        if (dto.DateWatched.Kind == DateTimeKind.Unspecified) dto.DateWatched = DateTime.SpecifyKind(dto.DateWatched, DateTimeKind.Utc);
        else if (dto.DateWatched.Kind == DateTimeKind.Local) dto.DateWatched = dto.DateWatched.ToUniversalTime();

        item.Title = dto.Title;
        item.Date = dto.DateWatched;
        item.ImgUrl = dto.ImgUrl;
        item.Category = dto.Notes; // Mapping Notes to Category column
        
        // Packing details into Description column (simple storage solution)
        item.Description = $"{dto.ReleaseYear}|{dto.Rating}|{dto.Genre}";

        await _db.SaveChangesAsync();
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