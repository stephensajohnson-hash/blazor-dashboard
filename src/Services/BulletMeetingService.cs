using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class BulletMeetingService
{
    private readonly AppDbContext _db;

    public BulletMeetingService(AppDbContext db)
    {
        _db = db;
    }

    public class MeetingDTO : BulletItem
    {
        public BulletMeetingDetail Detail { get; set; } = new();
        public List<BulletItemNote> Notes { get; set; } = new();
    }

    // --- SAVE MEETING ---
    public async Task SaveMeeting(MeetingDTO dto)
    {
        BulletItem? item = null;
        // Fix Date Kind for PostgreSQL
        if (dto.Date.Kind == DateTimeKind.Unspecified) 
            dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

        if (dto.Id > 0)
        {
            item = await _db.BulletItems.FindAsync(dto.Id);
            if (item == null) return;
        }
        else
        {
            item = new BulletItem { UserId = dto.UserId, Type = "meeting", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        // Update Base Fields
        item.Title = dto.Title;
        item.Category = dto.Category;
        item.Date = dto.Date;
        item.Description = dto.Description;
        item.ImgUrl = dto.ImgUrl;
        item.LinkUrl = dto.LinkUrl;
        item.Type = "meeting"; // Ensure type is set

        await _db.SaveChangesAsync();

        // Update Meeting Details
        var detail = await _db.BulletMeetingDetails.FindAsync(item.Id);
        if (detail == null)
        {
            detail = new BulletMeetingDetail { BulletItemId = item.Id };
            await _db.BulletMeetingDetails.AddAsync(detail);
        }

        // Fix StartTime Kind
        if(dto.Detail.StartTime.HasValue && dto.Detail.StartTime.Value.Kind == DateTimeKind.Unspecified)
             dto.Detail.StartTime = DateTime.SpecifyKind(dto.Detail.StartTime.Value, DateTimeKind.Utc);

        detail.StartTime = dto.Detail.StartTime;
        detail.DurationMinutes = dto.Detail.DurationMinutes;
        detail.ActualDurationMinutes = dto.Detail.ActualDurationMinutes;

        await _db.SaveChangesAsync();

        // Update Notes (Delete all, re-add)
        var oldNotes = await _db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        _db.BulletItemNotes.RemoveRange(oldNotes);
        
        foreach (var n in dto.Notes)
        {
            n.Id = 0; // Reset ID to force insert
            n.BulletItemId = item.Id;
            await _db.BulletItemNotes.AddAsync(n);
        }
        await _db.SaveChangesAsync();
    }

    // --- IMPORT ---
    public async Task ImportFromOldJson(int userId, string jsonContent)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;
        JsonElement items = root;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var i)) items = i;

        if(items.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in items.EnumerateArray())
            {
                if (el.TryGetProperty("type", out var t) && t.ToString().ToLower() == "meeting")
                {
                    string title = ""; if(el.TryGetProperty("title", out var val)) title = val.ToString();
                    var item = new BulletItem { UserId = userId, Type = "meeting", CreatedAt = DateTime.UtcNow, Title = title };
                    
                    if(el.TryGetProperty("date", out val) && DateTime.TryParse(val.ToString(), out var dt)) 
                        item.Date = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    else 
                        item.Date = DateTime.UtcNow;

                    if(el.TryGetProperty("category", out val)) item.Category = val.ToString();
                    if(el.TryGetProperty("description", out val)) item.Description = val.ToString();
                    if(el.TryGetProperty("id", out val)) item.OriginalStringId = val.ToString();

                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync(); 

                    var detail = new BulletMeetingDetail { BulletItemId = item.Id };
                    
                    if(el.TryGetProperty("startTime", out val) && DateTime.TryParse(val.ToString(), out var st)) 
                        detail.StartTime = DateTime.SpecifyKind(st, DateTimeKind.Utc);
                    
                    if(el.TryGetProperty("duration", out val) && val.TryGetInt32(out var d)) detail.DurationMinutes = d;
                    
                    await _db.BulletMeetingDetails.AddAsync(detail);
                }
            }
            await _db.SaveChangesAsync();
        }
    }
}