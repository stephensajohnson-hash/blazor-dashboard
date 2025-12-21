using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class BulletMeetingService
{
    // Restored IDbContextFactory as requested
    private readonly IDbContextFactory<AppDbContext> _factory;

    public BulletMeetingService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public class MeetingDTO : BulletTaskService.TaskDTO 
    {
        // Inherits base properties to ensure compatibility with Calendar
    }

    public async Task<List<MeetingDTO>> GetMeetingsForRange(int userId, DateTime start, DateTime end)
    {
        using var db = _factory.CreateDbContext();
        var items = await (from baseItem in db.BulletItems
                           join detail in db.BulletMeetingDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId 
                                 && baseItem.Date >= start && baseItem.Date <= end
                                 && baseItem.Type == "meeting"
                           orderby baseItem.Date
                           select new MeetingDTO 
                           { 
                               Id = baseItem.Id, 
                               UserId = baseItem.UserId, 
                               Type = baseItem.Type, 
                               Category = baseItem.Category,
                               Date = baseItem.Date, 
                               Title = baseItem.Title, 
                               Description = baseItem.Description, 
                               ImgUrl = baseItem.ImgUrl, 
                               LinkUrl = baseItem.LinkUrl, 
                               OriginalStringId = baseItem.OriginalStringId,
                               SortOrder = baseItem.SortOrder,
                               MeetingDetail = detail // Map to MeetingDetail property
                           }).ToListAsync();

        if (items.Any())
        {
            var ids = items.Select(i => i.Id).ToList();
            var notes = await db.BulletItemNotes.Where(n => ids.Contains(n.BulletItemId)).OrderBy(n => n.Order).ToListAsync();
            foreach (var i in items) i.Notes = notes.Where(n => n.BulletItemId == i.Id).ToList();
        }
        return items;
    }

    public async Task SaveMeeting(MeetingDTO dto)
    {
        using var db = _factory.CreateDbContext();
        
        // --- 1. UTC FIX (Preserved) ---
        if (dto.Date.Kind == DateTimeKind.Unspecified) dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        else if (dto.Date.Kind == DateTimeKind.Local) dto.Date = dto.Date.ToUniversalTime();

        // Handle nested detail time
        var detailSource = dto.MeetingDetail ?? new BulletMeetingDetail();

        if (detailSource.StartTime.HasValue)
        {
            if (detailSource.StartTime.Value.Kind == DateTimeKind.Unspecified) 
                detailSource.StartTime = DateTime.SpecifyKind(detailSource.StartTime.Value, DateTimeKind.Utc);
            else if (detailSource.StartTime.Value.Kind == DateTimeKind.Local) 
                detailSource.StartTime = detailSource.StartTime.Value.ToUniversalTime();
        }
        // ------------------

        BulletItem? item = null;
        if (dto.Id > 0) item = await db.BulletItems.FindAsync(dto.Id);
        
        if (item == null) {
            item = new BulletItem { UserId = dto.UserId, Type = "meeting", CreatedAt = DateTime.UtcNow };
            await db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title; 
        item.Category = dto.Category; 
        item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl; 
        item.LinkUrl = dto.LinkUrl; 
        item.Date = dto.Date;
        item.SortOrder = dto.SortOrder;
        
        await db.SaveChangesAsync();
        dto.Id = item.Id; // Ensure ID is set back to DTO

        var detail = await db.BulletMeetingDetails.FindAsync(item.Id);
        if (detail == null) { 
            detail = new BulletMeetingDetail { BulletItemId = item.Id }; 
            await db.BulletMeetingDetails.AddAsync(detail); 
        }

        // --- 2. FIELD MAPPING FIX ---
        // Explicitly map properties from the DTO's detail to the DB entity
        detail.StartTime = detailSource.StartTime;
        detail.DurationMinutes = detailSource.DurationMinutes;
        detail.ActualDurationMinutes = detailSource.ActualDurationMinutes;
        detail.IsCompleted = detailSource.IsCompleted; // Fixes persistence
        // ----------------------------

        await db.SaveChangesAsync();

        // Save Notes
        var oldNotes = await db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        db.BulletItemNotes.RemoveRange(oldNotes);
        if (dto.Notes != null)
        {
            foreach (var n in dto.Notes) { n.Id = 0; n.BulletItemId = item.Id; await db.BulletItemNotes.AddAsync(n); }
        }
        
        await db.SaveChangesAsync();
    }

    public async Task ToggleComplete(int id, bool isComplete)
    {
        using var db = _factory.CreateDbContext();
        var detail = await db.BulletMeetingDetails.FindAsync(id);
        if (detail != null) 
        { 
            detail.IsCompleted = isComplete; 
            await db.SaveChangesAsync(); 
        }
    }

    // (Preserved Import Logic)
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
                if (type == "meeting")
                {
                    DateTime date = DateTime.UtcNow;
                    if (el.TryGetProperty("date", out var d) && DateTime.TryParse(d.ToString(), out var pd)) date = DateTime.SpecifyKind(pd, DateTimeKind.Utc);

                    var item = new BulletItem { UserId = userId, Type = "meeting", CreatedAt = DateTime.UtcNow, Date = date, Title = (el.TryGetProperty("title", out var tit) ? tit.ToString() : ""), OriginalStringId = (el.TryGetProperty("id", out var oid) ? oid.ToString() : "") };
                    await db.BulletItems.AddAsync(item);
                    await db.SaveChangesAsync();

                    var detail = new BulletMeetingDetail { BulletItemId = item.Id };
                    if(el.TryGetProperty("duration", out var dur)) detail.DurationMinutes = dur.GetInt32();
                    if(el.TryGetProperty("time", out var tm)) { 
                        if(TimeSpan.TryParse(tm.ToString(), out var ts)) detail.StartTime = date.Date + ts;
                    }
                    await db.BulletMeetingDetails.AddAsync(detail);
                    count++;
                }
            }
            await db.SaveChangesAsync();
        }
        return count;
    }
}