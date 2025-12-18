using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class BulletMediaService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public BulletMediaService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public class MediaDTO : BulletItem
    {
        public BulletMediaDetail Detail { get; set; } = new();
        public List<BulletItemNote> Notes { get; set; } = new();
    }

    public async Task<List<MediaDTO>> GetMedia(int userId)
    {
        using var db = _factory.CreateDbContext();
        var items = await (from baseItem in db.BulletItems
                           join detail in db.BulletMediaDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId && baseItem.Type == "media"
                           select new MediaDTO 
                           { 
                               Id = baseItem.Id, UserId = baseItem.UserId, Type = baseItem.Type, Category = baseItem.Category,
                               Date = baseItem.Date, Title = baseItem.Title, Description = baseItem.Description, 
                               ImgUrl = baseItem.ImgUrl, LinkUrl = baseItem.LinkUrl, OriginalStringId = baseItem.OriginalStringId,
                               SortOrder = baseItem.SortOrder,
                               Detail = detail
                           }).ToListAsync();

        if (items.Any())
        {
            var ids = items.Select(i => i.Id).ToList();
            var notes = await db.BulletItemNotes.Where(n => ids.Contains(n.BulletItemId)).ToListAsync();
            foreach (var i in items) i.Notes = notes.Where(n => n.BulletItemId == i.Id).ToList();
        }
        return items;
    }

    public async Task SaveMedia(MediaDTO dto)
    {
        using var db = _factory.CreateDbContext();

        // --- UTC FIX ---
        if (dto.Date.Kind == DateTimeKind.Unspecified) dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        else if (dto.Date.Kind == DateTimeKind.Local) dto.Date = dto.Date.ToUniversalTime();
        // ---------------

        BulletItem? item = null;
        if (dto.Id > 0) item = await db.BulletItems.FindAsync(dto.Id);
        else {
            item = new BulletItem { UserId = dto.UserId, Type = "media", CreatedAt = DateTime.UtcNow };
            await db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title; item.Category = dto.Category; item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl; item.LinkUrl = dto.LinkUrl; item.Date = dto.Date;
        item.SortOrder = dto.SortOrder;
        
        await db.SaveChangesAsync();

        var detail = await db.BulletMediaDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletMediaDetail { BulletItemId = item.Id }; await db.BulletMediaDetails.AddAsync(detail); }

        // --- MAPPING FIX ---
        detail.Rating = dto.Detail.Rating;
        detail.ReleaseYear = dto.Detail.ReleaseYear;
        detail.Tags = dto.Detail.Tags;
        // -------------------

        await db.SaveChangesAsync();

        var oldNotes = await db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        db.BulletItemNotes.RemoveRange(oldNotes);
        foreach (var n in dto.Notes) { n.Id = 0; n.BulletItemId = item.Id; await db.BulletItemNotes.AddAsync(n); }
        
        await db.SaveChangesAsync();
    }

    public async Task<int> ImportFromOldJson(int userId, string jsonContent)
    {
        using var db = _factory.CreateDbContext();
        int count = 0;
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;
        JsonElement items = root;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var i)) items = i;

        if(items.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in items.EnumerateArray())
            {
                string type = (el.TryGetProperty("type", out var t) ? t.ToString() : "").ToLower();
                if (type == "media")
                {
                    DateTime date = DateTime.UtcNow;
                    if (el.TryGetProperty("date", out var d) && DateTime.TryParse(d.ToString(), out var pd)) date = DateTime.SpecifyKind(pd, DateTimeKind.Utc);

                    var item = new BulletItem { UserId = userId, Type = "media", CreatedAt = DateTime.UtcNow, Date = date, Title = (el.TryGetProperty("title", out var tit) ? tit.ToString() : ""), OriginalStringId = (el.TryGetProperty("id", out var oid) ? oid.ToString() : "") };
                    await db.BulletItems.AddAsync(item);
                    await db.SaveChangesAsync();

                    var detail = new BulletMediaDetail { BulletItemId = item.Id };
                    if(el.TryGetProperty("rating", out var rat)) detail.Rating = rat.GetInt32();
                    if(el.TryGetProperty("year", out var yr)) detail.ReleaseYear = yr.GetInt32();
                    await db.BulletMediaDetails.AddAsync(detail);
                    count++;
                }
            }
            await db.SaveChangesAsync();
        }
        return count;
    }
}