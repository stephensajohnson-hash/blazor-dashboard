using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class BulletTaskService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public BulletTaskService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public class TaskDTO : BulletItem
    {
        public BulletTaskDetail Detail { get; set; } = new();
        public BulletMeetingDetail? MeetingDetail { get; set; }
        public BulletHabitDetail? HabitDetail { get; set; }
        public BulletMediaDetail? MediaDetail { get; set; }
        public BulletHolidayDetail? HolidayDetail { get; set; }
        public BulletBirthdayDetail? BirthdayDetail { get; set; }
        public BulletAnniversaryDetail? AnniversaryDetail { get; set; }
        public BulletVacationDetail? VacationDetail { get; set; }
        public BulletHealthDetail? HealthDetail { get; set; }
        public BulletGameDetail? SportsDetail { get; set; } 
        
        public List<BulletHealthMeal> Meals { get; set; } = new();
        public List<BulletHealthWorkout> Workouts { get; set; } = new();
        public List<BulletItemNote> Notes { get; set; } = new();
        public DateTime? EndDate { get; set; } 
    }

    public async Task<List<TaskDTO>> GetTasksForRange(int userId, DateTime start, DateTime end)
    {
        using var db = _factory.CreateDbContext();
        var items = await (from baseItem in db.BulletItems
                           join detail in db.BulletTaskDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId 
                                 && baseItem.Date >= start && baseItem.Date <= end
                                 && baseItem.Type == "task"
                           select new TaskDTO 
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
            var notes = await db.BulletItemNotes.Where(n => ids.Contains(n.BulletItemId)).OrderBy(n => n.Order).ToListAsync();
            foreach (var i in items) i.Notes = notes.Where(n => n.BulletItemId == i.Id).ToList();
        }
        return items;
    }

    public async Task SaveTask(TaskDTO dto)
    {
        using var db = _factory.CreateDbContext();

        // --- UTC FIX ---
        if (dto.Date.Kind == DateTimeKind.Unspecified) dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        else if (dto.Date.Kind == DateTimeKind.Local) dto.Date = dto.Date.ToUniversalTime();

        if (dto.Detail.DueDate.HasValue)
        {
            if (dto.Detail.DueDate.Value.Kind == DateTimeKind.Unspecified) 
                dto.Detail.DueDate = DateTime.SpecifyKind(dto.Detail.DueDate.Value, DateTimeKind.Utc);
            else if (dto.Detail.DueDate.Value.Kind == DateTimeKind.Local) 
                dto.Detail.DueDate = dto.Detail.DueDate.Value.ToUniversalTime();
        }
        // ---------------

        BulletItem? item = null;
        if (dto.Id > 0) item = await db.BulletItems.FindAsync(dto.Id);
        else {
            item = new BulletItem { UserId = dto.UserId, Type = "task", CreatedAt = DateTime.UtcNow };
            await db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title; item.Category = dto.Category; item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl; item.LinkUrl = dto.LinkUrl; item.Date = dto.Date;
        item.SortOrder = dto.SortOrder;
        
        await db.SaveChangesAsync();

        var detail = await db.BulletTaskDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletTaskDetail { BulletItemId = item.Id }; await db.BulletTaskDetails.AddAsync(detail); }

        // --- FIELD MAPPING FIX ---
        detail.Status = dto.Detail.Status;
        detail.Priority = dto.Detail.Priority;
        detail.IsCompleted = dto.Detail.IsCompleted;
        detail.DueDate = dto.Detail.DueDate;
        // -------------------------

        await db.SaveChangesAsync();

        var oldNotes = await db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        db.BulletItemNotes.RemoveRange(oldNotes);
        foreach (var n in dto.Notes) { n.Id = 0; n.BulletItemId = item.Id; await db.BulletItemNotes.AddAsync(n); }
        
        await db.SaveChangesAsync();
    }

    public async Task ToggleComplete(int id, bool isComplete)
    {
        using var db = _factory.CreateDbContext();
        var detail = await db.BulletTaskDetails.FindAsync(id);
        if (detail != null) { detail.IsCompleted = isComplete; await db.SaveChangesAsync(); }
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
                if (type == "task")
                {
                    DateTime date = DateTime.UtcNow;
                    if (el.TryGetProperty("date", out var d) && DateTime.TryParse(d.ToString(), out var pd)) date = DateTime.SpecifyKind(pd, DateTimeKind.Utc);

                    var item = new BulletItem { UserId = userId, Type = "task", CreatedAt = DateTime.UtcNow, Date = date, Title = (el.TryGetProperty("title", out var tit) ? tit.ToString() : ""), OriginalStringId = (el.TryGetProperty("id", out var oid) ? oid.ToString() : "") };
                    await db.BulletItems.AddAsync(item);
                    await db.SaveChangesAsync();

                    var detail = new BulletTaskDetail { BulletItemId = item.Id };
                    if(el.TryGetProperty("status", out var s)) detail.Status = s.ToString();
                    if(el.TryGetProperty("priority", out var p)) detail.Priority = p.ToString();
                    await db.BulletTaskDetails.AddAsync(detail);
                    count++;
                }
            }
            await db.SaveChangesAsync();
        }
        return count;
    }
}