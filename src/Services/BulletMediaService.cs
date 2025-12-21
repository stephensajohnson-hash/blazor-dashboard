using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Dashboard;

public class BulletMediaService
{
    private readonly IServiceScopeFactory _factory;

    public BulletMediaService(IServiceScopeFactory factory)
    {
        _factory = factory;
    }

    public class MediaDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = "media";
        public string Category { get; set; } = "personal";
        public DateTime Date { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImgUrl { get; set; } = "";
        public string LinkUrl { get; set; } = "";
        public string OriginalStringId { get; set; } = "";
        public BulletMediaDetail Detail { get; set; } = new();
        public List<BulletItemNote> Notes { get; set; } = new();
        public int SortOrder { get; set; }
    }

    public async Task<List<MediaDTO>> GetMedia(int userId)
    {
        using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var items = await db.BulletItems
            .Where(i => i.UserId == userId && i.Type == "media")
            .OrderBy(i => i.Date)
            .ToListAsync();

        var dtos = new List<MediaDTO>();
        foreach (var i in items)
        {
            var detail = await db.BulletMediaDetails.FirstOrDefaultAsync(d => d.BulletItemId == i.Id) ?? new BulletMediaDetail();
            var notes = await db.BulletItemNotes.Where(n => n.BulletItemId == i.Id).OrderBy(n => n.Order).ToListAsync();

            dtos.Add(new MediaDTO
            {
                Id = i.Id,
                UserId = i.UserId,
                Type = i.Type,
                Category = i.Category,
                Date = i.Date,
                Title = i.Title,
                Description = i.Description,
                ImgUrl = i.ImgUrl,
                LinkUrl = i.LinkUrl,
                OriginalStringId = i.OriginalStringId,
                SortOrder = i.SortOrder,
                Detail = detail,
                Notes = notes
            });
        }
        return dtos;
    }

    public async Task SaveMedia(MediaDTO dto)
    {
        using var scope = _factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        BulletItem? item = null;
        if (dto.Id > 0)
        {
            item = await db.BulletItems.FindAsync(dto.Id);
        }

        if (item == null)
        {
            item = new BulletItem { UserId = dto.UserId, Type = "media", CreatedAt = DateTime.UtcNow };
            db.BulletItems.Add(item);
        }

        item.Category = dto.Category;
        item.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        item.Title = dto.Title;
        item.Description = dto.Description;
        item.ImgUrl = dto.ImgUrl;
        item.LinkUrl = dto.LinkUrl;
        item.SortOrder = dto.SortOrder;
        
        await db.SaveChangesAsync();
        dto.Id = item.Id;

        // Detail
        var detail = await db.BulletMediaDetails.FirstOrDefaultAsync(d => d.BulletItemId == item.Id);
        if (detail == null)
        {
            detail = new BulletMediaDetail { BulletItemId = item.Id };
            db.BulletMediaDetails.Add(detail);
        }
        detail.Rating = dto.Detail.Rating;
        detail.ReleaseYear = dto.Detail.ReleaseYear;
        detail.Tags = dto.Detail.Tags;

        // Notes
        var existingNotes = await db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        db.BulletItemNotes.RemoveRange(existingNotes);
        if (dto.Notes != null)
        {
            int order = 0;
            foreach (var n in dto.Notes)
            {
                db.BulletItemNotes.Add(new BulletItemNote
                {
                    BulletItemId = item.Id,
                    Content = n.Content,
                    ImgUrl = n.ImgUrl,
                    LinkUrl = n.LinkUrl,
                    Order = order++
                });
            }
        }

        await db.SaveChangesAsync();
    }
}