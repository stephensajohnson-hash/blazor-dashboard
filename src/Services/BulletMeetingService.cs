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
                    
                    if(el.TryGetProperty("date", out val) && DateTime.TryParse(val.ToString(), out var dt)) item.Date = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    else item.Date = DateTime.UtcNow;

                    if(el.TryGetProperty("category", out val)) item.Category = val.ToString();
                    if(el.TryGetProperty("description", out val)) item.Description = val.ToString();
                    if(el.TryGetProperty("id", out val)) item.OriginalStringId = val.ToString();

                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync(); 

                    var detail = new BulletMeetingDetail { BulletItemId = item.Id };
                    
                    // Parse Meeting Fields
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