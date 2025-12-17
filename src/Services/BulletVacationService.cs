using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class BulletVacationService
{
    private readonly AppDbContext _db;

    public BulletVacationService(AppDbContext db)
    {
        _db = db;
    }

    public class VacationDTO : BulletItem
    {
        public BulletVacationDetail Detail { get; set; } = new();
        public List<BulletItemNote> Notes { get; set; } = new();
    }

    public async Task<List<VacationDTO>> GetVacationsForRange(int userId, DateTime start, DateTime end)
    {
        var items = await (from baseItem in _db.BulletItems
                           join detail in _db.BulletVacationDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId 
                                 && baseItem.Date >= start && baseItem.Date <= end
                                 && baseItem.Type == "vacation"
                           select new VacationDTO 
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
            var notes = await _db.BulletItemNotes.Where(n => ids.Contains(n.BulletItemId)).OrderBy(n => n.Order).ToListAsync();
            foreach (var i in items) i.Notes = notes.Where(n => n.BulletItemId == i.Id).ToList();
        }
        return items;
    }

    public async Task SaveVacation(VacationDTO dto)
    {
        BulletItem? item = null;
        if (dto.Date.Kind == DateTimeKind.Unspecified) dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

        if (dto.Id > 0) item = await _db.BulletItems.FindAsync(dto.Id);
        else {
            item = new BulletItem { UserId = dto.UserId, Type = "vacation", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title; item.Category = dto.Category; item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl; item.LinkUrl = dto.LinkUrl; item.Date = dto.Date;
        item.SortOrder = dto.SortOrder;
        
        await _db.SaveChangesAsync();

        var detail = await _db.BulletVacationDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletVacationDetail { BulletItemId = item.Id }; await _db.BulletVacationDetails.AddAsync(detail); }

        detail.VacationGroupId = dto.Detail.VacationGroupId;

        await _db.SaveChangesAsync();

        var oldNotes = await _db.BulletItemNotes.Where(n => n.BulletItemId == item.Id).ToListAsync();
        _db.BulletItemNotes.RemoveRange(oldNotes);
        foreach (var n in dto.Notes) { n.Id = 0; n.BulletItemId = item.Id; await _db.BulletItemNotes.AddAsync(n); }
        await _db.SaveChangesAsync();
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
                string GetAny(params string[] keys) { foreach(var k in keys) { var val = GetStr(k); if(!string.IsNullOrEmpty(val)) return val; } return ""; }

                if (GetStr("type").ToLower() == "vacation")
                {
                    DateTime itemDate = DateTime.UtcNow;
                    string dateStr = GetStr("date");
                    if (DateTime.TryParse(dateStr, out var parsedDate)) itemDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);

                    var item = new BulletItem { 
                        UserId = userId, Type = "vacation", CreatedAt = DateTime.UtcNow, Date = itemDate,
                        Title = GetStr("title"), Description = GetStr("description"), OriginalStringId = GetStr("id"),
                        Category = GetStr("category"), ImgUrl = GetAny("img", "imgUrl", "image"), LinkUrl = GetAny("url", "link", "linkUrl")
                    };
                    
                    if(string.IsNullOrEmpty(item.Category)) item.Category = "personal";

                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync(); 

                    var detail = new BulletVacationDetail { BulletItemId = item.Id };
                    detail.VacationGroupId = GetStr("vacationGroupId");
                    
                    await _db.BulletVacationDetails.AddAsync(detail);

                    if (el.TryGetProperty("notes", out var notesElement) && notesElement.ValueKind == JsonValueKind.Array)
                    {
                        int order = 0;
                        foreach(var noteEl in notesElement.EnumerateArray()) {
                            var newNote = new BulletItemNote { BulletItemId = item.Id, Order = order++ };
                            if (noteEl.ValueKind == JsonValueKind.String) newNote.Content = noteEl.GetString() ?? "";
                            else if (noteEl.ValueKind == JsonValueKind.Object) {
                                if(noteEl.TryGetProperty("text", out var t)) newNote.Content = t.ToString();
                                else if(noteEl.TryGetProperty("content", out var c)) newNote.Content = c.ToString();
                                if(noteEl.TryGetProperty("img", out var img)) newNote.ImgUrl = img.ToString();
                                if(noteEl.TryGetProperty("link", out var lnk)) newNote.LinkUrl = lnk.ToString();
                            }
                            if (!string.IsNullOrEmpty(newNote.Content) || !string.IsNullOrEmpty(newNote.ImgUrl)) await _db.BulletItemNotes.AddAsync(newNote);
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