using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

public class BulletBaseService
{
    private readonly AppDbContext _db;

    public BulletBaseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task CreateBaseTablesIfMissing()
    {
        // 1. Base Table
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletItems"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""UserId"" INTEGER NOT NULL,
                ""Type"" TEXT NOT NULL DEFAULT 'task',
                ""Category"" TEXT NOT NULL DEFAULT 'personal',
                ""Date"" TIMESTAMP NOT NULL,
                ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                ""Title"" TEXT NOT NULL DEFAULT '',
                ""Description"" TEXT NOT NULL DEFAULT '',
                ""ImgUrl"" TEXT NOT NULL DEFAULT '',
                ""LinkUrl"" TEXT NOT NULL DEFAULT '',
                ""OriginalStringId"" TEXT NOT NULL DEFAULT '',
                ""Order"" INTEGER NOT NULL DEFAULT 0
            );
        ");

        try { await _db.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""BulletItems"" ADD COLUMN IF NOT EXISTS ""Order"" INTEGER NOT NULL DEFAULT 0;"); } catch { }

        // 2. Notes
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletItemNotes"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""BulletItemId"" INTEGER NOT NULL,
                ""Content"" TEXT NOT NULL DEFAULT '',
                ""ImgUrl"" TEXT NOT NULL DEFAULT '',
                ""LinkUrl"" TEXT NOT NULL DEFAULT '',
                ""Order"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_BulletItemNotes_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE
            );
        ");

        // 3. Task Details
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletTaskDetails"" (
                ""BulletItemId"" INTEGER NOT NULL PRIMARY KEY,
                ""Status"" TEXT NOT NULL DEFAULT 'Pending',
                ""IsCompleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""Priority"" TEXT NOT NULL DEFAULT 'Normal',
                ""TicketNumber"" TEXT NOT NULL DEFAULT '',
                ""TicketUrl"" TEXT NOT NULL DEFAULT '',
                ""DueDate"" TIMESTAMP NULL,
                CONSTRAINT ""FK_BulletTaskDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE
            );
        ");

        // 4. Meeting Details
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletMeetingDetails"" (
                ""BulletItemId"" INTEGER NOT NULL PRIMARY KEY,
                ""StartTime"" TIMESTAMP NULL,
                ""DurationMinutes"" INTEGER NOT NULL DEFAULT 0,
                ""ActualDurationMinutes"" INTEGER NOT NULL DEFAULT 0,
                ""IsCompleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                CONSTRAINT ""FK_BulletMeetingDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE
            );
        ");

        // 5. Habit Details
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletHabitDetails"" (
                ""BulletItemId"" INTEGER NOT NULL PRIMARY KEY,
                ""StreakCount"" INTEGER NOT NULL DEFAULT 0,
                ""Status"" TEXT NOT NULL DEFAULT 'Active',
                ""IsCompleted"" BOOLEAN NOT NULL DEFAULT FALSE,
                CONSTRAINT ""FK_BulletHabitDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE
            );
        ");
        try { await _db.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""BulletHabitDetails"" ADD COLUMN IF NOT EXISTS ""IsCompleted"" BOOLEAN NOT NULL DEFAULT FALSE;"); } catch { }

        // 6. Media Details
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletMediaDetails"" (
                ""BulletItemId"" INTEGER NOT NULL PRIMARY KEY,
                ""Rating"" INTEGER NOT NULL DEFAULT 0,
                ""ReleaseYear"" INTEGER NOT NULL DEFAULT 0,
                ""Tags"" TEXT NOT NULL DEFAULT '',
                CONSTRAINT ""FK_BulletMediaDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE
            );
        ");

        // 7. Holiday Details
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletHolidayDetails"" (
                ""BulletItemId"" INTEGER NOT NULL PRIMARY KEY,
                ""IsWorkHoliday"" BOOLEAN NOT NULL DEFAULT FALSE,
                CONSTRAINT ""FK_BulletHolidayDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE
            );
        ");

        // 8. Birthday Details
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletBirthdayDetails"" (
                ""BulletItemId"" INTEGER NOT NULL PRIMARY KEY,
                ""DOB_Year"" INTEGER NULL,
                CONSTRAINT ""FK_BulletBirthdayDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE
            );
        ");
    }

    public async Task DeleteItem(int id)
    {
        var item = await _db.BulletItems.FindAsync(id);
        if (item != null)
        {
            _db.BulletItems.Remove(item);
            await _db.SaveChangesAsync();
        }
    }
    
    public async Task UpdateItemOrders(Dictionary<int, int> updates)
    {
        if (!updates.Any()) return;
        var ids = updates.Keys.ToList();
        var items = await _db.BulletItems.Where(x => ids.Contains(x.Id)).ToListAsync();
        foreach(var item in items) { if(updates.TryGetValue(item.Id, out int newOrder)) item.SortOrder = newOrder; }
        await _db.SaveChangesAsync();
    }

    public async Task<int> ClearDataByType(int userId, string type)
    {
        var items = await _db.BulletItems.Where(x => x.UserId == userId && x.Type == type).ToListAsync();
        _db.BulletItems.RemoveRange(items);
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<string> SaveImageAsync(byte[] data, string contentType)
    {
        var img = new StoredImage
        {
            Data = data, ContentType = contentType, UploadedAt = DateTime.UtcNow, OriginalName = "upload.jpg"
        };
        await _db.StoredImages.AddAsync(img);
        await _db.SaveChangesAsync();
        
        // Return the NEW route
        return $"/db-images/{img.Id}";
    }
}