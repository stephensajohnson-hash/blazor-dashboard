using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class BulletTaskService
{
    private readonly AppDbContext _db;

    public BulletTaskService(AppDbContext db)
    {
        _db = db;
    }

    public Task CreateTable() { return Task.CompletedTask; }

    public class TaskDTO : BulletItem
    {
        public BulletTaskDetail Detail { get; set; } = new();
        public BulletMeetingDetail MeetingDetail { get; set; } = new();
        public List<BulletItemNote> Notes { get; set; } = new();
    }

    public async Task<List<TaskDTO>> GetTasksForRange(int userId, DateTime start, DateTime end)
    {
        var s = DateTime.SpecifyKind(start.Date, DateTimeKind.Utc);
        var e = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var tasks = await (from baseItem in _db.BulletItems
                           join detail in _db.BulletTaskDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId 
                                 && baseItem.Date >= s 
                                 && baseItem.Date <= e
                                 && baseItem.Type == "task"
                           select new TaskDTO 
                           { 
                               Id = baseItem.Id, UserId = baseItem.UserId, Type = baseItem.Type, Category = baseItem.Category,
                               Date = baseItem.Date, Title = baseItem.Title, Description = baseItem.Description, 
                               ImgUrl = baseItem.ImgUrl, LinkUrl = baseItem.LinkUrl, OriginalStringId = baseItem.OriginalStringId,
                               Detail = detail
                           }).ToListAsync();

        if (tasks.Any())
        {
            var taskIds = tasks.Select(t => t.Id).ToList();
            var notes = await _db.BulletItemNotes.Where(n => taskIds.Contains(n.BulletItemId)).ToListAsync();
            foreach (var t in tasks) t.Notes = notes.Where(n => n.BulletItemId == t.Id).OrderBy(n => n.Id).ToList();
        }

        return tasks;
    }

    public async Task SaveTask(TaskDTO dto)
    {
        BulletItem? item = null;
        if (dto.Date.Kind == DateTimeKind.Unspecified) dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

        if (dto.Id > 0) item = await _db.BulletItems.FindAsync(dto.Id);
        else {
            item = new BulletItem { UserId = dto.UserId, Type = "task", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title; item.Category = dto.Category; item.Date = dto.Date;
        item.Description = dto.Description; item.ImgUrl = dto.ImgUrl; item.LinkUrl = dto.LinkUrl;
        item.Type = "task";

        await _db.SaveChangesAsync();

        var detail = await _db.BulletTaskDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletTaskDetail { BulletItemId = item.Id }; await _db.BulletTaskDetails.AddAsync(detail); }
        
        detail.Status = dto.Detail.IsCompleted ? "Done" : "Pending";
        detail.IsCompleted = dto.Detail.IsCompleted;
        detail.Priority = dto.Detail.Priority;
        detail.TicketNumber = dto.Detail.TicketNumber;
        detail.TicketUrl = dto.Detail.TicketUrl;
        
        await _db.SaveChangesAsync();

        var oldNotes = await _db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        _db.BulletItemNotes.RemoveRange(oldNotes);
        foreach (var n in dto.Notes) { n.Id = 0; n.BulletItemId = item.Id; await _db.BulletItemNotes.AddAsync(n); }
        await _db.SaveChangesAsync();
    }

    public async Task ToggleComplete(int itemId, bool isComplete)
    {
        var detail = await _db.BulletTaskDetails.FindAsync(itemId);
        if (detail != null) { 
            detail.IsCompleted = isComplete; 
            detail.Status = isComplete ? "Done" : "Pending"; 
            await _db.SaveChangesAsync(); 
        }
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

                if (GetStr("type").ToLower() == "task")
                {
                    string title = GetStr("title");
                    var item = new BulletItem { UserId = userId, Type = "task", CreatedAt = DateTime.UtcNow, Title = title };
                    
                    if(DateTime.TryParse(GetStr("date"), out var dt)) item.Date = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    else item.Date = DateTime.UtcNow;

                    item.Category = GetStr("category");
                    item.Description = GetStr("description");
                    item.OriginalStringId = GetStr("id");
                    item.ImgUrl = GetStr("img"); if(string.IsNullOrEmpty(item.ImgUrl)) item.ImgUrl = GetStr("imgUrl");
                    item.LinkUrl = GetStr("linkUrl"); if(string.IsNullOrEmpty(item.LinkUrl)) item.LinkUrl = GetStr("url");

                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync();

                    var detail = new BulletTaskDetail { BulletItemId = item.Id };
                    
                    // --- FIX COMPLETED LOGIC ---
                    string status = GetStr("status");
                    string doneStr = GetStr("isCompleted");
                    string completedStr = GetStr("completed"); // Check both naming conventions

                    bool isDone = false;
                    if (bool.TryParse(doneStr, out var b)) isDone = b;
                    else if (bool.TryParse(completedStr, out b)) isDone = b;
                    else if (status.Equals("Done", StringComparison.OrdinalIgnoreCase) || status.Equals("Completed", StringComparison.OrdinalIgnoreCase)) isDone = true;

                    detail.IsCompleted = isDone;
                    detail.Status = isDone ? "Done" : "Pending";
                    
                    detail.TicketNumber = GetStr("ticketNumber");
                    detail.TicketUrl = GetStr("ticketUrl");
                    
                    await _db.BulletTaskDetails.AddAsync(detail);
                    count++;
                }
            }
            await _db.SaveChangesAsync();
        }
        return count;
    }
}