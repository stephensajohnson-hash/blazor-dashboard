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
            // 1. Gather used URLs with high-performance queries
            var bulletImgs = await _db.BulletItems.AsNoTracking()
                .Where(x => x.UserId == userId && x.ImgUrl != null && x.ImgUrl != "")
                .Select(x => x.ImgUrl).ToListAsync();
            foreach (var url in bulletImgs) usedUrls.Add(url);

            var teamImgs = await _db.Teams.AsNoTracking()
                .Where(x => x.UserId == userId && x.LogoUrl != null && x.LogoUrl != "")
                .Select(x => x.LogoUrl).ToListAsync();
            foreach (var url in teamImgs) usedUrls.Add(url);

            var recipeImgs = await _db.Recipes.AsNoTracking()
                .Where(x => x.UserId == userId && x.ImageUrl != null && x.ImageUrl != "")
                .Select(x => x.ImageUrl).ToListAsync();
            foreach (var url in recipeImgs) usedUrls.Add(url);

            // 2. Fetch Stored Images Metadata only
            var storedImages = await _db.StoredImages.AsNoTracking()
                .Select(x => new { x.Id })
                .ToListAsync();
            
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

            // 3. Add Remote used images
            foreach (var url in usedUrls.Where(u => !u.StartsWith("/db-images/")))
            {
                results.Add(new ImageUsageDTO { Url = url, IsLocal = false, IsUsed = true });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"REGISTRY_SCAN_FAILURE: {ex.Message}");
        }

        // Return used items first, then unused
        return results.OrderByDescending(x => x.IsUsed).ToList();
    }

    public async Task<int> DeleteUnusedLocalImages(int userId)
    {
        try
        {
            // Get the list of all IDs currently in use
            var bulletUsage = await _db.BulletItems.AsNoTracking()
                .Where(x => x.UserId == userId && x.ImgUrl.StartsWith("/db-images/"))
                .Select(x => x.ImgUrl).ToListAsync();
            
            var usedIds = bulletUsage
                .Select(u => u.Replace("/db-images/", ""))
                .Select(s => int.TryParse(s, out var id) ? id : -1)
                .Where(id => id != -1)
                .ToHashSet();

            // Find images in DB that are NOT in that hashset
            var toDelete = await _db.StoredImages
                .Where(x => !usedIds.Contains(x.Id))
                .ToListAsync();

            int count = toDelete.Count;
            if (count > 0)
            {
                _db.StoredImages.RemoveRange(toDelete);
                await _db.SaveChangesAsync();
            }
            return count;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PURGE_CRASH: {ex.Message}");
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