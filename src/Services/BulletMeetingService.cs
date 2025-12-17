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
        var meetings = await (from baseItem in _db.BulletItems
                              join detail in _db.BulletMeetingDetails on baseItem.Id equals detail.BulletItemId
                              where baseItem.UserId == userId 
                                    && baseItem.Date >= start && baseItem.Date <= end
                                    && baseItem.Type == "meeting"
                              select new MeetingDTO 
                              { 
                                  Id = baseItem.Id, UserId = baseItem.UserId, Type = baseItem.Type, Category = baseItem.Category,
                                  Date = baseItem.Date, Title = baseItem.Title, Description = baseItem.Description, 
                                  ImgUrl = baseItem.ImgUrl, LinkUrl = baseItem.LinkUrl, OriginalStringId = baseItem.OriginalStringId,
                                  SortOrder = baseItem.Order, // Map
                                  Detail = detail
                              }).ToListAsync();

        if (meetings.Any())
        {
            var ids = meetings.Select(m => m.Id).ToList();
            var notes = await _db.BulletItemNotes.Where(n => ids.Contains(n.BulletItemId)).OrderBy(n => n.Order).ToListAsync();
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

        item.Title = dto.Title; item.Category = dto.Category; item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl; item.LinkUrl = dto.LinkUrl; item.Date = dto.Date;
        item.Order = dto.SortOrder; // Map back
        
        await _db.SaveChangesAsync();

        var detail = await _db.BulletMeetingDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletMeetingDetail { BulletItemId = item.Id }; await _db.BulletMeetingDetails.AddAsync(detail); }

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

    public async Task ToggleComplete(int id, bool isCompleted)
    {
        var detail = await _db.BulletMeetingDetails.FindAsync(id);
        if (detail != null) { detail.IsCompleted = isCompleted; await _db.SaveChangesAsync(); }
    }
    
    public async Task<int> ImportFromOldJson(int userId, string jsonContent) { return 0; }
}