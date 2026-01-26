using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Dashboard;

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
        
        public List<BulletTaskTodoItem> Todos { get; set; } = new();
        public List<BulletHealthMeal> Meals { get; set; } = new();
        public List<BulletHealthWorkout> Workouts { get; set; } = new();
        public List<BulletItemNote> Notes { get; set; } = new();
        public DateTime? EndDate { get; set; } 

        // Helper to identify matched meals for the UI highlight
        public bool MatchedByMeal(string query) => 
            !string.IsNullOrEmpty(query) && 
            Meals.Any(m => m.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<TaskDTO>> GetTasksForRange(int userId, DateTime start, DateTime end)
    {
        using var db = _factory.CreateDbContext();
        
        var items = await (from baseItem in db.BulletItems.Include(x => x.Todos)
                           join detail in db.BulletTaskDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId 
                                 && baseItem.Date >= start && baseItem.Date <= end
                                 && baseItem.Type == "task"
                           select new { baseItem, detail }).ToListAsync();

        return items.Select(x => new TaskDTO 
        { 
            Id = x.baseItem.Id, UserId = x.baseItem.UserId, Type = x.baseItem.Type, Category = x.baseItem.Category,
            Date = x.baseItem.Date, Title = x.baseItem.Title, Description = x.baseItem.Description, 
            ImgUrl = x.baseItem.ImgUrl, LinkUrl = x.baseItem.LinkUrl, OriginalStringId = x.baseItem.OriginalStringId,
            SortOrder = x.baseItem.SortOrder,
            Detail = x.detail,
            Todos = x.baseItem.Todos.OrderBy(t => t.Order).ToList()
        }).ToList();
    }

    public async Task SaveTask(TaskDTO dto)
    {
        using var db = _factory.CreateDbContext();

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
        item.Type = dto.Type; 
        
        await db.SaveChangesAsync();

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

        var existingTodos = db.BulletTaskTodoItems.Where(x => x.BulletItemId == item.Id);
        db.BulletTaskTodoItems.RemoveRange(existingTodos);

        if (dto.Todos != null)
        {
            var validTodos = dto.Todos
                .Where(t => !string.IsNullOrWhiteSpace(t.Content))
                .Select(t => new BulletTaskTodoItem
                {
                    BulletItemId = item.Id,
                    Content = t.Content,
                    IsCompleted = t.IsCompleted,
                    Order = t.Order 
                }).ToList();
                
            await db.BulletTaskTodoItems.AddRangeAsync(validTodos);

            if (validTodos.Any())
            {
                detail.IsCompleted = validTodos.All(x => x.IsCompleted);
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