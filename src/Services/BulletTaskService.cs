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

    public class TaskDTO : BulletItem
    {
        public BulletTaskDetail Detail { get; set; } = new();
        public BulletMeetingDetail MeetingDetail { get; set; } = new();
        
        // --- THIS MUST BE HERE ---
        public BulletHabitDetail HabitDetail { get; set; } = new(); 
        // -------------------------

        public List<BulletItemNote> Notes { get; set; } = new();
    }

    public async Task<List<TaskDTO>> GetTasksForRange(int userId, DateTime start, DateTime end)
    {
        var tasks = await (from baseItem in _db.BulletItems
                           join detail in _db.BulletTaskDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId 
                                 && baseItem.Date >= start && baseItem.Date <= end
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
            var ids = tasks.Select(t => t.Id).ToList();
            var notes = await _db.BulletItemNotes.Where(n => ids.Contains(n.BulletItemId)).OrderBy(n => n.Order).ToListAsync();
            foreach (var t in tasks) t.Notes = notes.Where(n => n.BulletItemId == t.Id).ToList();
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

        item.Title = dto.Title; item.Category = dto.Category; item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl; item.LinkUrl = dto.LinkUrl; item.Date = dto.Date;
        
        await _db.SaveChangesAsync();

        var detail = await _db.BulletTaskDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletTaskDetail { BulletItemId = item.Id }; await _db.BulletTaskDetails.AddAsync(detail); }

        detail.Status = dto.Detail.Status;
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

    public async Task ToggleComplete(int id, bool isCompleted)
    {
        var detail = await _db.BulletTaskDetails.FindAsync(id);
        if (detail != null) {
            detail.IsCompleted = isCompleted;
            await _db.SaveChangesAsync();
        }
    }
    
    // Import Logic (Existing code...)
    public async Task<int> ImportFromOldJson(int userId, string jsonContent)
    {
        // ... (Keep existing import code) ...
        return 0; // Placeholder to save space in this response, keep your existing logic
    }
}