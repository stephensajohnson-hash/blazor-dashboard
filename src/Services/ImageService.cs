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
        var usageMap = new Dictionary<string, (DateTime Date, string Type)>();
        var results = new List<ImageUsageDTO>();

        try
        {
            // 1. Scan BulletItems
            var bulletData = await _db.BulletItems.AsNoTracking()
                .Where(x => x.UserId == userId && x.ImgUrl != null && x.ImgUrl != "")
                .Select(x => new { x.ImgUrl, x.Date, x.Type })
                .OrderBy(x => x.Date)
                .ToListAsync();

            foreach (var item in bulletData)
            {
                if (!usageMap.ContainsKey(item.ImgUrl))
                {
                    usageMap[item.ImgUrl] = (item.Date, item.Type);
                }
            }

            // 2. Scan Teams
            var teamData = await _db.Teams.AsNoTracking()
                .Where(x => x.UserId == userId && x.LogoUrl != null && x.LogoUrl != "")
                .Select(x => new { x.LogoUrl })
                .ToListAsync();

            foreach (var item in teamData)
            {
                if (!usageMap.ContainsKey(item.LogoUrl))
                {
                    usageMap[item.LogoUrl] = (DateTime.UtcNow, "sports");
                }
            }

            // 3. Fetch Stored Images IDs only (fast metadata scan)
            var storedImageIds = await _db.StoredImages.AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync();
            
            foreach (var id in storedImageIds)
            {
                var localUrl = $"/db-images/{id}";
                bool isUsed = usageMap.ContainsKey(localUrl);
                
                results.Add(new ImageUsageDTO 
                { 
                    Url = localUrl, 
                    IsLocal = true, 
                    IsUsed = isUsed,
                    DbId = id,
                    FirstUsedDate = isUsed ? usageMap[localUrl].Date : null,
                    FirstUsedType = isUsed ? usageMap[localUrl].Type : "unused"
                });
            }

            // 4. Add Remote pointers
            foreach (var kvp in usageMap.Where(u => !u.Key.StartsWith("/db-images/")))
            {
                results.Add(new ImageUsageDTO 
                { 
                    Url = kvp.Key, 
                    IsLocal = false, 
                    IsUsed = true,
                    FirstUsedDate = kvp.Value.Date,
                    FirstUsedType = kvp.Value.Type
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"QUERY_ERROR: {ex.Message}");
        }

        return results.OrderByDescending(x => x.FirstUsedDate ?? DateTime.MinValue).ToList();
    }

    public async Task<int> DeleteUnusedLocalImages(int userId)
    {
        try
        {
            // Direct SQL Delete using a simpler JOIN pattern to avoid timeouts
            string sql = @"
                DELETE FROM ""StoredImages""
                WHERE ""Id"" NOT IN (
                    SELECT CAST(REPLACE(""ImgUrl"", '/db-images/', '') AS INTEGER)
                    FROM ""BulletItems""
                    WHERE ""ImgUrl"" LIKE '/db-images/%'
                    UNION
                    SELECT CAST(REPLACE(""LogoUrl"", '/db-images/', '') AS INTEGER)
                    FROM ""Teams""
                    WHERE ""LogoUrl"" LIKE '/db-images/%'
                    UNION
                    SELECT CAST(REPLACE(""ImageUrl"", '/db-images/', '') AS INTEGER)
                    FROM ""Recipes""
                    WHERE ""ImageUrl"" LIKE '/db-images/%'
                );";

            return await _db.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PURGE_SQL_CRASH: {ex.Message}");
            return -1;
        }
    }

    public class ImageUsageDTO
    {
        public string Url { get; set; } = "";
        public bool IsLocal { get; set; }
        public bool IsUsed { get; set; }
        public int DbId { get; set; }
        public DateTime? FirstUsedDate { get; set; }
        public string FirstUsedType { get; set; } = "";
        public bool IsFavorite { get; set; }
    }
}