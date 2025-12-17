using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class BulletHabitService
{
    private readonly AppDbContext _db;

    public BulletHabitService(AppDbContext db)
    {
        _db = db;
    }

    public class HabitDTO : BulletItem
    {
        public BulletHabitDetail Detail { get; set; } = new();
        public List<BulletItemNote> Notes { get; set; } = new();
    }

    public async Task<List<HabitDTO>> GetHabits(int userId)
    {
        var habits = await (from baseItem in _db.BulletItems
                            join detail in _db.BulletHabitDetails on baseItem.Id equals detail.BulletItemId
                            where baseItem.UserId == userId 
                            select new HabitDTO 
                            { 
                                Id = baseItem.Id, UserId = baseItem.UserId, Type = baseItem.Type, Category = baseItem.Category,
                                Date = baseItem.Date, Title = baseItem.Title, Description = baseItem.Description, 
                                ImgUrl = baseItem.ImgUrl, LinkUrl = baseItem.LinkUrl, OriginalStringId = baseItem.OriginalStringId,
                                SortOrder = baseItem.Order, // Map
                                Detail = detail
                            }).ToListAsync();

        if (habits.Any())
        {
            var ids = habits.Select(h => h.Id).ToList();
            var notes = await _db.BulletItemNotes.Where(n => ids.Contains(n.BulletItemId)).OrderBy(n => n.Order).ToListAsync();
            foreach (var h in habits) h.Notes = notes.Where(n => n.BulletItemId == h.Id).ToList();
        }
        return habits;
    }

    public async Task SaveHabit(HabitDTO dto)
    {
        BulletItem? item = null;
        if (dto.Date.Kind == DateTimeKind.Unspecified) dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

        if (dto.Id > 0) item = await _db.BulletItems.FindAsync(dto.Id);
        else {
            item = new BulletItem { UserId = dto.UserId, Type = "habit", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title; item.Category = dto.Category; item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl; item.LinkUrl = dto.LinkUrl; item.Date = dto.Date;
        item.Order = dto.SortOrder; // Map back
        
        await _db.SaveChangesAsync();

        var detail = await _db.BulletHabitDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletHabitDetail { BulletItemId = item.Id }; await _db.BulletHabitDetails.AddAsync(detail); }

        detail.StreakCount = dto.Detail.StreakCount;
        detail.Status = dto.Detail.Status;
        detail.IsCompleted = dto.Detail.IsCompleted;

        await _db.SaveChangesAsync();

        var oldNotes = await _db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        _db.BulletItemNotes.RemoveRange(oldNotes);
        foreach (var n in dto.Notes) { n.Id = 0; n.BulletItemId = item.Id; await _db.BulletItemNotes.AddAsync(n); }
        await _db.SaveChangesAsync();
    }
    
    public async Task<int> ImportFromOldJson(int userId, string jsonContent) { return 0; }
}