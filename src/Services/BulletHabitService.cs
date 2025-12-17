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

    // --- GET ---
    public async Task<List<HabitDTO>> GetHabits(int userId)
    {
        var habits = await (from baseItem in _db.BulletItems
                            join detail in _db.BulletHabitDetails on baseItem.Id equals detail.BulletItemId
                            where baseItem.UserId == userId 
                                  && detail.Status != "Archived"
                            select new HabitDTO 
                            { 
                                Id = baseItem.Id, 
                                UserId = baseItem.UserId, 
                                Type = baseItem.Type, 
                                Category = baseItem.Category,
                                Date = baseItem.Date, 
                                Title = baseItem.Title, 
                                Description = baseItem.Description, 
                                ImgUrl = baseItem.ImgUrl, 
                                LinkUrl = baseItem.LinkUrl, 
                                OriginalStringId = baseItem.OriginalStringId,
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

    // --- SAVE ---
    public async Task SaveHabit(HabitDTO dto)
    {
        // 1. Save Base Item
        BulletItem? item = null;
        if (dto.Date.Kind == DateTimeKind.Unspecified) dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

        if (dto.Id > 0) item = await _db.BulletItems.FindAsync(dto.Id);
        else {
            item = new BulletItem { UserId = dto.UserId, Type = "habit", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title; 
        item.Category = dto.Category; 
        item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl;
        item.LinkUrl = dto.LinkUrl;
        
        await _db.SaveChangesAsync();

        // 2. Save Detail
        var detail = await _db.BulletHabitDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletHabitDetail { BulletItemId = item.Id }; await _db.BulletHabitDetails.AddAsync(detail); }

        detail.StreakCount = dto.Detail.StreakCount;
        detail.Status = dto.Detail.Status;

        await _db.SaveChangesAsync();

        // 3. Save Notes
        var oldNotes = await _db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        _db.BulletItemNotes.RemoveRange(oldNotes);
        
        foreach (var n in dto.Notes) { 
            n.Id = 0; 
            n.BulletItemId = item.Id; 
            await _db.BulletItemNotes.AddAsync(n); 
        }
        await _db.SaveChangesAsync();
    }

    // --- IMPORT FROM LEGACY JSON ---
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

                if (GetStr("type").ToLower() == "habit")
                {
                    // 1. Parse Date (NEW LOGIC)
                    DateTime itemDate = DateTime.UtcNow;
                    string dateStr = GetStr("date");
                    if (DateTime.TryParse(dateStr, out var parsedDate))
                    {
                        itemDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                    }

                    // 2. Create Base Item

                    var item = new BulletItem { 
                        UserId = userId, 
                        Type = "habit", 
                        CreatedAt = DateTime.UtcNow, 
                        Date = itemDate, // <--- ASSIGN DATE HERE
                        Title = GetStr("title"),
                        Description = GetStr("description"),
                        OriginalStringId = GetStr("id"),
                        Category = GetStr("category"),
                        ImgUrl = GetStr("img"),
                        LinkUrl = GetStr("url")
                    };
                    
                    if(string.IsNullOrEmpty(item.Category)) item.Category = "health"; // Default for habits
                    if(string.IsNullOrEmpty(item.ImgUrl)) item.ImgUrl = GetStr("imgUrl");

                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync(); 

                    // B. Create Habit Detail
                    var detail = new BulletHabitDetail { BulletItemId = item.Id };
                    
                    // Handle streak (could be string or int)
                    string streakStr = GetStr("streak");
                    if (int.TryParse(streakStr, out var s)) detail.StreakCount = s;
                    
                    string status = GetStr("status");
                    detail.Status = !string.IsNullOrEmpty(status) ? status : "Active";

                    await _db.BulletHabitDetails.AddAsync(detail);

                    // C. CONVERT NOTES (The part you asked about)
                    if (el.TryGetProperty("notes", out var notesElement) && notesElement.ValueKind == JsonValueKind.Array)
                    {
                        int order = 0;
                        foreach(var noteEl in notesElement.EnumerateArray())
                        {
                            var newNote = new BulletItemNote 
                            { 
                                BulletItemId = item.Id,
                                Order = order++
                            };

                            // Legacy JSON had mix of simple strings and objects
                            if (noteEl.ValueKind == JsonValueKind.String)
                            {
                                newNote.Content = noteEl.GetString() ?? "";
                            }
                            else if (noteEl.ValueKind == JsonValueKind.Object)
                            {
                                if(noteEl.TryGetProperty("text", out var t)) newNote.Content = t.ToString();
                                else if(noteEl.TryGetProperty("content", out var c)) newNote.Content = c.ToString();
                                
                                if(noteEl.TryGetProperty("img", out var img)) newNote.ImgUrl = img.ToString();
                                if(noteEl.TryGetProperty("link", out var lnk)) newNote.LinkUrl = lnk.ToString();
                            }

                            if (!string.IsNullOrEmpty(newNote.Content) || !string.IsNullOrEmpty(newNote.ImgUrl))
                            {
                                await _db.BulletItemNotes.AddAsync(newNote);
                            }
                        }
                    }

                    count++;
                }
            }
            await _db.SaveChangesAsync();
        }
        return count;
    }
}