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
                                .Include(x => x.Todos) // Added to pull checklist
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
                                Detail = detail,
                                Todos = baseItem.Todos.OrderBy(t => t.Order).ToList() // Map Todos
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
        
        // 1. Save main item
        var item = await db.BulletItems.FindAsync(dto.Id);
        if (item == null)
        {
            item = new BulletItem { UserId = dto.UserId, CreatedAt = DateTime.UtcNow };
            db.BulletItems.Add(item);
        }
        item.Title = dto.Title;
        item.Description = dto.Description;
        item.Date = dto.Date;
        item.Category = dto.Category;
        item.ImgUrl = dto.ImgUrl;
        item.LinkUrl = dto.LinkUrl;
        await db.SaveChangesAsync();

        // 2. Save detail
        var detail = await db.BulletTaskDetails.FindAsync(item.Id);
        if (detail == null)
        {
            detail = new BulletTaskDetail { BulletItemId = item.Id };
            db.BulletTaskDetails.Add(detail);
        }
        detail.Priority = dto.Detail.Priority;
        detail.DueDate = dto.Detail.DueDate;
        detail.TicketNumber = dto.Detail.TicketNumber;
        detail.TicketUrl = dto.Detail.TicketUrl;
        detail.IsCompleted = dto.Detail.IsCompleted;

        // 3. SAVE TO-DO LIST (Surgical addition)
        var existingTodos = db.BulletTaskTodoItems.Where(x => x.BulletItemId == item.Id);
        db.BulletTaskTodoItems.RemoveRange(existingTodos);

        if (dto.Todos != null)
        {
            var validTodos = dto.Todos
                .Where(t => !string.IsNullOrWhiteSpace(t.Content))
                .Select((t, index) => new BulletTaskTodoItem
                {
                    BulletItemId = item.Id,
                    Content = t.Content,
                    IsCompleted = t.IsCompleted,
                    Order = index 
                }).ToList();
                
            await db.BulletTaskTodoItems.AddRangeAsync(validTodos);

            if (validTodos.Any())
            {
                bool allDone = validTodos.All(x => x.IsCompleted);
                detail.IsCompleted = allDone;
            }
        }

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