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

    public async Task CreateTable()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletTaskDetails"" (
                ""BulletItemId"" integer PRIMARY KEY, 
                ""Status"" text, 
                ""IsCompleted"" boolean DEFAULT false, 
                ""Priority"" text, 
                ""TicketNumber"" text, 
                ""TicketUrl"" text,
                ""DueDate"" timestamp with time zone
            );
        ");
    }

    public class TaskDTO : BulletItem
    {
        public BulletTaskDetail Detail { get; set; } = new();
    }

    // --- READ ---

    public async Task<List<TaskDTO>> GetTasksForRange(int userId, DateTime start, DateTime end)
    {
        // Fetch all tasks within the date range
        var query = from baseItem in _db.BulletItems
                    join detail in _db.BulletTaskDetails on baseItem.Id equals detail.BulletItemId
                    where baseItem.UserId == userId 
                          && baseItem.Date >= start.Date 
                          && baseItem.Date <= end.Date
                          && baseItem.Type == "task"
                    select new TaskDTO 
                    { 
                        Id = baseItem.Id, UserId = baseItem.UserId, Type = baseItem.Type, Category = baseItem.Category,
                        Date = baseItem.Date, Title = baseItem.Title, Description = baseItem.Description, 
                        ImgUrl = baseItem.ImgUrl, LinkUrl = baseItem.LinkUrl, OriginalStringId = baseItem.OriginalStringId,
                        Detail = detail
                    };

        return await query.ToListAsync();
    }

    // --- WRITE ---

    public async Task SaveTask(TaskDTO dto)
    {
        BulletItem? item = null;

        if (dto.Id > 0)
        {
            // UPDATE
            item = await _db.BulletItems.FindAsync(dto.Id);
            if (item == null) return; // Should not happen
        }
        else
        {
            // CREATE
            item = new BulletItem { UserId = dto.UserId, Type = "task", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        // Map Base Fields
        item.Title = dto.Title;
        item.Category = dto.Category;
        item.Date = dto.Date; // Important: Save as UTC or local depending on your preference, usually keep as is from UI
        item.Description = dto.Description;
        item.ImgUrl = dto.ImgUrl;
        item.LinkUrl = dto.LinkUrl;

        await _db.SaveChangesAsync(); // Save base to get ID

        // Manage Detail
        var detail = await _db.BulletTaskDetails.FindAsync(item.Id);
        if (detail == null)
        {
            detail = new BulletTaskDetail { BulletItemId = item.Id };
            await _db.BulletTaskDetails.AddAsync(detail);
        }

        // Map Detail Fields
        detail.Status = dto.Detail.IsCompleted ? "Done" : "Pending";
        detail.IsCompleted = dto.Detail.IsCompleted;
        detail.Priority = dto.Detail.Priority;
        detail.TicketNumber = dto.Detail.TicketNumber;
        detail.TicketUrl = dto.Detail.TicketUrl;

        await _db.SaveChangesAsync();
    }

    public async Task ToggleComplete(int itemId, bool isComplete)
    {
        var detail = await _db.BulletTaskDetails.FindAsync(itemId);
        if (detail != null)
        {
            detail.IsCompleted = isComplete;
            detail.Status = isComplete ? "Done" : "Pending";
            await _db.SaveChangesAsync();
        }
    }

    // --- IMPORT (Existing) ---
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
                if (el.TryGetProperty("type", out var t) && t.ToString().ToLower() == "task")
                {
                    string title = ""; if(el.TryGetProperty("title", out var val)) title = val.ToString();
                    var item = new BulletItem { UserId = userId, Type = "task", CreatedAt = DateTime.UtcNow, Title = title };
                    
                    if(el.TryGetProperty("date", out val) && DateTime.TryParse(val.ToString(), out var dt)) item.Date = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    if(el.TryGetProperty("category", out val)) item.Category = val.ToString();
                    if(el.TryGetProperty("description", out val)) item.Description = val.ToString();
                    if(el.TryGetProperty("imgUrl", out val)) item.ImgUrl = val.ToString();
                    if(el.TryGetProperty("linkUrl", out val)) item.LinkUrl = val.ToString();
                    if(el.TryGetProperty("id", out val)) item.OriginalStringId = val.ToString();

                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync(); 

                    var detail = new BulletTaskDetail { BulletItemId = item.Id };
                    if(el.TryGetProperty("ticketNumber", out val)) detail.TicketNumber = val.ToString();
                    if(el.TryGetProperty("ticketUrl", out val)) detail.TicketUrl = val.ToString();
                    if(el.TryGetProperty("priority", out val)) detail.Priority = val.ToString();
                    
                    if(el.TryGetProperty("isCompleted", out val) && val.ValueKind == JsonValueKind.True) { detail.IsCompleted = true; detail.Status = "Done"; }
                    else if(el.TryGetProperty("status", out val) && val.ToString().ToLower() == "completed") { detail.IsCompleted = true; detail.Status = "Done"; }
                    
                    await _db.BulletTaskDetails.AddAsync(detail);
                }
            }
            await _db.SaveChangesAsync();
        }
    }
}