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
            // 1. Gather used URLs using AsNoTracking for speed
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

            // 2. Fetch IDs only (Avoid loading the heavy 'Data' byte array)
            var storedImageIds = await _db.StoredImages.AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync();
            
            foreach (var id in storedImageIds)
            {
                var localUrl = $"/db-images/{id}";
                results.Add(new ImageUsageDTO 
                { 
                    Url = localUrl, 
                    IsLocal = true, 
                    IsUsed = usedUrls.Contains(localUrl),
                    DbId = id
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
            Console.WriteLine($"REGISTRY_SCAN_ERROR: {ex.Message}");
        }

        return results.OrderByDescending(x => x.IsUsed).ToList();
    }

    public async Task<int> DeleteUnusedLocalImages(int userId)
    {
        try
        {
            // NEW: Use a Raw SQL Delete for maximum efficiency. 
            // This avoids loading any binary data into memory.
            string sql = @"
                DELETE FROM ""StoredImages"" 
                WHERE ""Id"" NOT IN (
                    SELECT CAST(REPLACE(""ImgUrl"", '/db-images/', '') AS INTEGER)
                    FROM ""BulletItems""
                    WHERE ""ImgUrl"" LIKE '/db-images/%'
                )
                AND ""Id"" NOT IN (
                    SELECT CAST(REPLACE(""LogoUrl"", '/db-images/', '') AS INTEGER)
                    FROM ""Teams""
                    WHERE ""LogoUrl"" LIKE '/db-images/%'
                )
                AND ""Id"" NOT IN (
                    SELECT CAST(REPLACE(""ImageUrl"", '/db-images/', '') AS INTEGER)
                    FROM ""Recipes""
                    WHERE ""ImageUrl"" LIKE '/db-images/%'
                );";

            int deletedCount = await _db.Database.ExecuteSqlRawAsync(sql);
            return deletedCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQL_PURGE_CRASH: {ex.Message}");
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