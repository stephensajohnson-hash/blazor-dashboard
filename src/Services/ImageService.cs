using Dashboard;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ImageService
{
    private readonly AppDbContext _db;

    public ImageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ImageUsageDTO>> GetAllImages(int userId)
    {
        var usedUrls = new HashSet<string>();
        var results = new List<ImageUsageDTO>();

        try
        {
            // 1. Gather used URLs from all known tables with individual try-catches
            // BulletItems
            try {
                var bulletImgs = await _db.BulletItems
                    .Where(x => x.UserId == userId && x.ImgUrl != null && x.ImgUrl != "")
                    .Select(x => x.ImgUrl)
                    .ToListAsync();
                foreach (var url in bulletImgs) usedUrls.Add(url);
            } catch (Exception ex) { Console.WriteLine($"Registry Scan Error (Bullets): {ex.Message}"); }

            // Teams
            try {
                var teamImgs = await _db.Teams
                    .Where(x => x.UserId == userId && x.LogoUrl != null && x.LogoUrl != "")
                    .Select(x => x.LogoUrl)
                    .ToListAsync();
                foreach (var url in teamImgs) usedUrls.Add(url);
            } catch (Exception ex) { Console.WriteLine($"Registry Scan Error (Teams): {ex.Message}"); }

            // Recipes
            try {
                var recipeImgs = await _db.Recipes
                    .Where(x => x.UserId == userId && x.ImageUrl != null && x.ImageUrl != "")
                    .Select(x => x.ImageUrl)
                    .ToListAsync();
                foreach (var url in recipeImgs) usedUrls.Add(url);
            } catch (Exception ex) { Console.WriteLine($"Registry Scan Error (Recipes): {ex.Message}"); }

            // 2. Get all uploaded images
            var storedImages = await _db.StoredImages.AsNoTracking().ToListAsync();
            
            foreach (var img in storedImages)
            {
                var localUrl = $"/db-images/{img.Id}";
                results.Add(new ImageUsageDTO 
                { 
                    Url = localUrl, 
                    IsLocal = true, 
                    IsUsed = usedUrls.Contains(localUrl),
                    DbId = img.Id
                });
            }

            // 3. Add Remote images that are in use
            foreach (var url in usedUrls.Where(u => !u.StartsWith("/db-images/")))
            {
                results.Add(new ImageUsageDTO 
                { 
                    Url = url, 
                    IsLocal = false, 
                    IsUsed = true 
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TOTAL_REGISTRY_CRASH: {ex.Message}");
        }

        return results.OrderByDescending(x => x.IsUsed).ToList();
    }

    public async Task<int> DeleteUnusedLocalImages(int userId)
    {
        try
        {
            var all = await GetAllImages(userId);
            var unusedIds = all.Where(x => x.IsLocal && !x.IsUsed).Select(x => x.DbId).ToList();
            
            if (!unusedIds.Any()) return 0;

            var toDelete = await _db.StoredImages.Where(x => unusedIds.Contains(x.Id)).ToListAsync();
            int count = toDelete.Count;
            
            _db.StoredImages.RemoveRange(toDelete);
            await _db.SaveChangesAsync();
            return count;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PURGE_ERROR: {ex.Message}");
            return -1;
        }
    }

    public class ImageUsageDTO
    {
        public string Url { get; set; } = "";
        public bool IsLocal { get; set; }
        public bool IsUsed { get; set; }
        public int DbId { get; set; }
        public bool IsFavorite { get; set; }
    }
}