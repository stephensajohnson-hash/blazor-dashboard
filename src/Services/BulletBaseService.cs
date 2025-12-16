using Dashboard;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

public class BulletBaseService
{
    private readonly AppDbContext _db;

    public BulletBaseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task CreateBaseTablesIfMissing()
    {
        try {
            // Base Tables
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""BulletItems"" (""Id"" serial PRIMARY KEY, ""UserId"" integer, ""Type"" text, ""Category"" text, ""Date"" timestamp with time zone, ""CreatedAt"" timestamp with time zone, ""Title"" text, ""Description"" text, ""ImgUrl"" text, ""LinkUrl"" text, ""OriginalStringId"" text);
                CREATE TABLE IF NOT EXISTS ""BulletItemNotes"" (""Id"" serial PRIMARY KEY, ""BulletItemId"" integer, ""Content"" text, ""ImgUrl"" text, ""LinkUrl"" text, ""Order"" integer DEFAULT 0);
                CREATE TABLE IF NOT EXISTS ""BulletTaskDetails"" (""BulletItemId"" integer PRIMARY KEY, ""Status"" text, ""IsCompleted"" boolean, ""Priority"" text, ""TicketNumber"" text, ""TicketUrl"" text, ""DueDate"" timestamp with time zone);
                CREATE TABLE IF NOT EXISTS ""StoredImages"" (""Id"" serial PRIMARY KEY, ""Data"" bytea, ""ContentType"" text, ""OriginalName"" text, ""UploadedAt"" timestamp with time zone);
            ");

            // Meeting Table
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""BulletMeetingDetails"" (
                    ""BulletItemId"" integer PRIMARY KEY, 
                    ""StartTime"" timestamp with time zone, 
                    ""DurationMinutes"" integer DEFAULT 0, 
                    ""ActualDurationMinutes"" integer DEFAULT 0
                );
            ");
            
            // Patches
            await _db.Database.ExecuteSqlRawAsync(@"DO $$ BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletItems' AND column_name='LinkUrl') THEN ALTER TABLE ""BulletItems"" ADD COLUMN ""LinkUrl"" text DEFAULT ''; END IF;
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletTaskDetails' AND column_name='TicketUrl') THEN ALTER TABLE ""BulletTaskDetails"" ADD COLUMN ""TicketUrl"" text DEFAULT ''; END IF;
            END $$;");
        } catch (Exception ex) {
            Console.WriteLine("Error creating tables: " + ex.Message);
        }
    }

    public async Task<int> SaveImageAsync(byte[] data, string contentType)
    {
        var img = new StoredImage { Data = data, ContentType = contentType, UploadedAt = DateTime.UtcNow };
        _db.StoredImages.Add(img);
        await _db.SaveChangesAsync();
        return img.Id;
    }

    // Safe Deletion
    public async Task<int> ClearDataByType(int userId, string type)
    {
        // 1. Delete Notes (Children)
        try { await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletItemNotes"" WHERE ""BulletItemId"" IN (SELECT ""Id"" FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1})", userId, type); } catch {}

        // 2. Delete Details (Children) - Based on type
        if(type == "task") {
            try { await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletTaskDetails"" WHERE ""BulletItemId"" IN (SELECT ""Id"" FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1})", userId, type); } catch {}
        }
        else if(type == "meeting") {
            try { await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletMeetingDetails"" WHERE ""BulletItemId"" IN (SELECT ""Id"" FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1})", userId, type); } catch {}
        }

        // 3. Delete Parent
        return await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1}", userId, type);
    }

    public async Task DeleteItem(int itemId)
    {
        try { await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletItemNotes"" WHERE ""BulletItemId"" = {0}", itemId); } catch {}
        try { await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletTaskDetails"" WHERE ""BulletItemId"" = {0}", itemId); } catch {}
        try { await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletMeetingDetails"" WHERE ""BulletItemId"" = {0}", itemId); } catch {}
        await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletItems"" WHERE ""Id"" = {0}", itemId);
    }

    public async Task DropLegacyTables()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
            DROP TABLE IF EXISTS ""BulletTasks""; DROP TABLE IF EXISTS ""BulletMeetings""; 
            DROP TABLE IF EXISTS ""BulletMedia""; DROP TABLE IF EXISTS ""BulletHabits""; 
            DROP TABLE IF EXISTS ""BulletMacroTrackers""; DROP TABLE IF EXISTS ""BulletMeals""; 
            DROP TABLE IF EXISTS ""BulletWorkouts""; DROP TABLE IF EXISTS ""BulletHolidays""; 
            DROP TABLE IF EXISTS ""BulletBirthdays""; DROP TABLE IF EXISTS ""BulletAnniversaries""; 
            DROP TABLE IF EXISTS ""BulletVacations""; DROP TABLE IF EXISTS ""BulletGames"";
            DROP TABLE IF EXISTS ""BulletLeagues""; DROP TABLE IF EXISTS ""BulletSeasons""; DROP TABLE IF EXISTS ""BulletTeams"";
        ");
    }
}