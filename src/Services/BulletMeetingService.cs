using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

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

    public async Task<List<MeetingDTO>> GetMeetingsForRange(int userId, DateTime start, DateTime end)
    {
        var s = DateTime.SpecifyKind(start.Date, DateTimeKind.Utc);
        var e = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var meetings = await (from baseItem in _db.BulletItems
                              join detail in _db.BulletMeetingDetails on baseItem.Id equals detail.BulletItemId
                              where baseItem.UserId == userId 
                                    && baseItem.Date >= s 
                                    && baseItem.Date <= e
                                    && baseItem.Type == "meeting"
                              select new MeetingDTO 
                              { 
                                  Id = baseItem.Id, UserId = baseItem.UserId, Type = baseItem.Type, Category = baseItem.Category,
                                  Date = baseItem.Date, Title = baseItem.Title, Description = baseItem.Description, 
                                  ImgUrl = baseItem.ImgUrl, LinkUrl = baseItem.LinkUrl, OriginalStringId = baseItem.OriginalStringId,
                                  Detail = detail
                              }).ToListAsync();

        if (meetings.Any())
        {
            var ids = meetings.Select(m => m.Id).ToList();
            var notes = await _db.BulletItemNotes.Where(n => ids.Contains(n.BulletItemId)).ToListAsync();
            foreach (var m in meetings) m.Notes = notes.Where(n => n.BulletItemId == m.Id).ToList();
        }

        return meetings;
    }

    public async Task SaveMeeting(MeetingDTO dto)
    {
        BulletItem? item = null;
        if (dto.Date.Kind == DateTimeKind.Unspecified) dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

        if (dto.Id > 0) item = await _db.BulletItems.FindAsync(dto.Id);
        else {
            item = new BulletItem { UserId = dto.UserId, Type = "meeting", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title;
        item.Category = dto.Category;
        item.Date = dto.Date;
        item.Description = dto.Description;
        item.ImgUrl = dto.ImgUrl;
        item.LinkUrl = dto.LinkUrl;
        item.Type = "meeting";

        await _db.SaveChangesAsync();

        var detail = await _db.BulletMeetingDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletMeetingDetail { BulletItemId = item.Id }; await _db.BulletMeetingDetails.AddAsync(detail); }

        if(dto.Detail.StartTime.HasValue && dto.Detail.StartTime.Value.Kind == DateTimeKind.Unspecified)
             dto.Detail.StartTime = DateTime.SpecifyKind(dto.Detail.StartTime.Value, DateTimeKind.Utc);

        detail.StartTime = dto.Detail.StartTime;
        detail.DurationMinutes = dto.Detail.DurationMinutes;
        detail.ActualDurationMinutes = dto.Detail.ActualDurationMinutes;
        detail.IsCompleted = dto.Detail.IsCompleted;

        await _db.SaveChangesAsync();

        var oldNotes = await _db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        _db.BulletItemNotes.RemoveRange(oldNotes);
        foreach (var n in dto.Notes) { n.Id = 0; n.BulletItemId = item.Id; await _db.BulletItemNotes.AddAsync(n); }
        await _db.SaveChangesAsync();
    }

    public async Task ToggleComplete(int itemId, bool isComplete)
    {
        var detail = await _db.BulletMeetingDetails.FindAsync(itemId);
        if (detail != null) { detail.IsCompleted = isComplete; await _db.SaveChangesAsync(); }
    }

    public async Task<int> ImportFromOldJson(int userId, string jsonContent)
    {
        int count = 0;
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;
        JsonElement items = root;
        
        if (root.ValueKind == JsonValueKind.Object) {
            if (root.TryGetProperty("items", out var i)) items = i;
            else if (root.TryGetProperty("Items", out i)) items = i;
        }

        if(items.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in items.EnumerateArray())
            {
                string GetStr(string key) => (el.TryGetProperty(key, out var v) || el.TryGetProperty(char.ToUpper(key[0]) + key.Substring(1), out v)) ? v.ToString() : "";

                if (GetStr("type").ToLower() == "meeting")
                {
                    string title = GetStr("title");
                    var item = new BulletItem { UserId = userId, Type = "meeting", CreatedAt = DateTime.UtcNow, Title = title };
                    
                    if(DateTime.TryParse(GetStr("date"), out var dt)) item.Date = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    else item.Date = DateTime.UtcNow;

                    string rawCat = GetStr("category").ToLower();
                    if (rawCat == "personal" || rawCat == "health") item.Category = rawCat;
                    else item.Category = "work"; 

                    item.Description = GetStr("description");
                    item.OriginalStringId = GetStr("id");
                    
                    // --- NEW: Import Images and Links ---
                    item.ImgUrl = GetStr("img"); 
                    if(string.IsNullOrEmpty(item.ImgUrl)) item.ImgUrl = GetStr("imgUrl"); // Try both casing/names
                    
                    item.LinkUrl = GetStr("linkUrl");
                    if(string.IsNullOrEmpty(item.LinkUrl)) item.LinkUrl = GetStr("url");

                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync(); 

                    var detail = new BulletMeetingDetail { BulletItemId = item.Id };
                    
                    if(DateTime.TryParse(GetStr("startTime"), out var st)) detail.StartTime = DateTime.SpecifyKind(st, DateTimeKind.Utc);
                    
                    string durStr = GetStr("duration");
                    if(int.TryParse(durStr, out var d)) detail.DurationMinutes = d;
                    
                    // Import isCompleted if present
                    string doneStr = GetStr("isCompleted");
                    if(bool.TryParse(doneStr, out var b)) detail.IsCompleted = b;

                    await _db.BulletMeetingDetails.AddAsync(detail);
                    count++;
                }
            }
            await _db.SaveChangesAsync();
        }
        return count;
    }
}