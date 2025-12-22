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
        public string Description { get; set; } = ""; // ADDED THIS
        public string ImgUrl { get; set; } = ""; 
        public DateTime DateWatched { get; set; }
        public int ReleaseYear { get; set; }
        public double Rating { get; set; } 
        public string Notes { get; set; } = "";
        public string Genre { get; set; } = "";
        
        public BulletMediaDetail Detail { get; set; } = new();
    }

    public async Task<List<MovieDTO>> GetMovies(int userId)
    {
        var query = from i in _db.BulletItems
                    join d in _db.BulletMediaDetails on i.Id equals d.BulletItemId into details
                    from subDetail in details.DefaultIfEmpty()
                    where i.UserId == userId && (i.Type == "movie" || i.Type == "media")
                    select new { Item = i, Detail = subDetail };

        var data = await query.ToListAsync();
        
        // Fetch Notes separately to avoid tracking issues
        var itemIds = data.Select(x => x.Item.Id).ToList();
        var notes = await _db.BulletItemNotes
                             .Where(n => itemIds.Contains(n.BulletItemId))
                             .ToListAsync();

        var result = new List<MovieDTO>();

        foreach (var row in data)
        {
            var d = row.Detail ?? new BulletMediaDetail();
            var noteContent = notes.FirstOrDefault(n => n.BulletItemId == row.Item.Id)?.Content ?? "";

            result.Add(new MovieDTO
            {
                Id = row.Item.Id,
                UserId = row.Item.UserId,
                Title = row.Item.Title,
                Description = row.Item.Description, // POPULATE DESCRIPTION
                ImgUrl = row.Item.ImgUrl,
                DateWatched = row.Item.Date,
                ReleaseYear = d.ReleaseYear,
                Rating = d.Rating,
                Genre = d.Tags,
                Notes = noteContent,
                Detail = d
            });
        }

        return result;
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