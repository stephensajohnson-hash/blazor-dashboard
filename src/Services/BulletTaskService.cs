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

    public async Task CreateTable() { /* Handled in BaseService */ }

    // --- DTO DEFINITION ---
    public class TaskDTO : BulletItem
    {
        public BulletTaskDetail Detail { get; set; } = new();
        public BulletMeetingDetail MeetingDetail { get; set; } = new(); // Required for meetings
        public List<BulletItemNote> Notes { get; set; } = new();
    }

    // --- READ ---
    public async Task<List<TaskDTO>> GetTasksForRange(int userId, DateTime start, DateTime end)
    {
        var s = DateTime.SpecifyKind(start.Date, DateTimeKind.Utc);
        var e = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        // Fetch Tasks
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

        // Load Notes
        if (tasks.Any())
        {
            var taskIds = tasks.Select(t => t.Id).ToList();
            var notes = await _db.BulletItemNotes.Where(n => taskIds.Contains(n.BulletItemId)).ToListAsync();
            foreach (var t in tasks) t.Notes = notes.Where(n => n.BulletItemId == t.Id).OrderBy(n => n.Id).ToList();
        }

        return tasks;
    }

    // --- WRITE ---
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
        item.Type = "task"; // Ensure type is forced to task if using this service

        await _db.SaveChangesAsync();

        var detail = await _db.BulletTaskDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletTaskDetail { BulletItemId = item.Id }; await _db.BulletTaskDetails.AddAsync(detail); }
        
        detail.Status = dto.Detail.IsCompleted ? "Done" : "Pending";
        detail.IsCompleted = dto.Detail.IsCompleted;
        detail.Priority = dto.Detail.Priority;
        detail.TicketNumber = dto.Detail.TicketNumber;
        detail.TicketUrl = dto.Detail.TicketUrl;
        
        await _db.SaveChangesAsync();

        // Notes
        var oldNotes = await _db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        _db.BulletItemNotes.RemoveRange(oldNotes);
        foreach (var n in dto.Notes) { n.Id = 0; n.BulletItemId = item.Id; await _db.BulletItemNotes.AddAsync(n); }
        await _db.SaveChangesAsync();
    }

    public async Task ToggleComplete(int itemId, bool isComplete)
    {
        var detail = await _db.BulletTaskDetails.FindAsync(itemId);
        if (detail != null) { detail.IsCompleted = isComplete; detail.Status = isComplete ? "Done" : "Pending"; await _db.SaveChangesAsync(); }
    }

    public async Task<int> ImportFromOldJson(int userId, string json) { return 0; /* Handled in other services */ }
}