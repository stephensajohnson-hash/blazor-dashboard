using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class BulletTaskService
{
    private readonly AppDbContext _db;

    public BulletTaskService(AppDbContext db)
    {
        _db = db;
    }

    // ... (Keep existing CreateTable, DTO, GetTasksForRange, SaveTask, ToggleComplete methods) ...
    // ... Copy them from your current file if needed, or I can reprint the whole file if you prefer.
    // ... Assuming standard methods exist, here is the updated Import method:

    public async Task<int> ImportFromOldJson(int userId, string jsonContent)
    {
        int count = 0;
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;
        JsonElement items = root;
        
        // Handle both direct array or { "items": [] } object
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var i)) items = i;

        if(items.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in items.EnumerateArray())
            {
                // Import only TASKS here
                string type = "task";
                if (el.TryGetProperty("type", out var t)) type = t.ToString().ToLower();

                if (type == "task")
                {
                    string title = ""; if(el.TryGetProperty("title", out var val)) title = val.ToString();
                    
                    var item = new BulletItem { UserId = userId, Type = "task", CreatedAt = DateTime.UtcNow, Title = title };
                    
                    if(el.TryGetProperty("date", out val) && DateTime.TryParse(val.ToString(), out var dt)) item.Date = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    else item.Date = DateTime.UtcNow;

                    if(el.TryGetProperty("category", out val)) item.Category = val.ToString();
                    if(el.TryGetProperty("description", out val)) item.Description = val.ToString();
                    if(el.TryGetProperty("id", out val)) item.OriginalStringId = val.ToString();

                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync();

                    // Add Details
                    var detail = new BulletTaskDetail { BulletItemId = item.Id };
                    if(el.TryGetProperty("status", out val)) detail.Status = val.ToString();
                    if(el.TryGetProperty("isCompleted", out val)) detail.IsCompleted = val.GetBoolean();
                    
                    await _db.BulletTaskDetails.AddAsync(detail);
                    count++;
                }
            }
            await _db.SaveChangesAsync();
        }
        return count;
    }
}