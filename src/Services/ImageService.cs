using Dashboard;
using Microsoft.EntityFrameworkCore;

public class ImageService
{
    private readonly AppDbContext _db;

    public ImageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ImageUsageDTO>> GetAllImages(int userId)
    {
        // 1. Gather every unique image URL used across the whole app
        var usedUrls = new HashSet<string>();

        // From BulletItems
        var bulletImgs = await _db.BulletItems.Where(x => x.UserId == userId && x.ImgUrl != "").Select(x => x.ImgUrl).ToListAsync();
        foreach (var url in bulletImgs) usedUrls.Add(url);

        // From Teams
        var teamImgs = await _db.Teams.Where(x => x.UserId == userId && x.LogoUrl != "").Select(x => x.LogoUrl).ToListAsync();
        foreach (var url in teamImgs) usedUrls.Add(url);

        // From Recipes
        var recipeImgs = await _db.Recipes.Where(x => x.UserId == userId && x.ImageUrl != "").Select(x => x.ImageUrl).ToListAsync();
        foreach (var url in recipeImgs) usedUrls.Add(url);

        // 2. Get all uploaded images from the DB
        var storedImages = await _db.StoredImages.ToListAsync();
        
        var results = new List<ImageUsageDTO>();

        // Map Stored Images
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

        // Map Remote Images (Anything in usedUrls that doesn't start with /db-images/)
        foreach (var url in usedUrls.Where(u => !u.StartsWith("/db-images/")))
        {
            results.Add(new ImageUsageDTO 
            { 
                Url = url, 
                IsLocal = false, 
                IsUsed = true 
            });
        }

        return results;
    }

    public async Task DeleteUnusedLocalImages(int userId)
    {
        var all = await GetAllImages(userId);
        var unusedIds = all.Where(x => x.IsLocal && !x.IsUsed).Select(x => x.DbId).ToList();
        
        var toDelete = await _db.StoredImages.Where(x => unusedIds.Contains(x.Id)).ToListAsync();
        _db.StoredImages.RemoveRange(toDelete);
        await _db.SaveChangesAsync();
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