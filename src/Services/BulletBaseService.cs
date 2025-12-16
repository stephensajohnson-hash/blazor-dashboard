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
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""BulletItems"" (""Id"" serial PRIMARY KEY, ""UserId"" integer, ""Type"" text, ""Category"" text, ""Date"" timestamp with time zone, ""CreatedAt"" timestamp with time zone, ""Title"" text, ""Description"" text, ""ImgUrl"" text, ""LinkUrl"" text, ""OriginalStringId"" text);
                CREATE TABLE IF NOT EXISTS ""BulletItemNotes"" (""Id"" serial PRIMARY KEY, ""BulletItemId"" integer, ""Content"" text, ""ImgUrl"" text, ""LinkUrl"" text, ""Order"" integer DEFAULT 0);
                CREATE TABLE IF NOT EXISTS ""BulletTaskDetails"" (""BulletItemId"" integer PRIMARY KEY, ""Status"" text, ""IsCompleted"" boolean, ""Priority"" text, ""TicketNumber"" text, ""TicketUrl"" text, ""DueDate"" timestamp with time zone);
                CREATE TABLE IF NOT EXISTS ""BulletMeetingDetails"" (""BulletItemId"" integer PRIMARY KEY, ""StartTime"" timestamp with time zone, ""DurationMinutes"" integer, ""ActualDurationMinutes"" integer);
                CREATE TABLE IF NOT EXISTS ""StoredImages"" (""Id"" serial PRIMARY KEY, ""Data"" bytea, ""ContentType"" text, ""OriginalName"" text, ""UploadedAt"" timestamp with time zone);
            ");

            // PATCH: Add IsCompleted to Meetings
            await _db.Database.ExecuteSqlRawAsync(@"DO $$ BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletMeetingDetails' AND column_name='IsCompleted') THEN 
                    ALTER TABLE ""BulletMeetingDetails"" ADD COLUMN ""IsCompleted"" boolean DEFAULT false; 
                END IF;
            END $$;");
            
        } catch (Exception ex) { Console.WriteLine("Error creating tables: " + ex.Message); }
    }

    public async Task<int> SaveImageAsync(byte[] data, string contentType)
    {
        var img = new StoredImage { Data = data, ContentType = contentType, UploadedAt = DateTime.UtcNow };
        _db.StoredImages.Add(img);
        await _db.SaveChangesAsync();
        return img.Id;
    }

    public async Task<int> ClearDataByType(int userId, string type)
    {
        try { await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletItemNotes"" WHERE ""BulletItemId"" IN (SELECT ""Id"" FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1})", userId, type); } catch {}
        
        if (type == "task") { try { await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletTaskDetails"" WHERE ""BulletItemId"" IN (SELECT ""Id"" FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1})", userId, type); } catch {} }
        else if (type == "meeting") { try { await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletMeetingDetails"" WHERE ""BulletItemId"" IN (SELECT ""Id"" FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1})", userId, type); } catch {} }

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
        // (Legacy Drop logic preserved if needed)
    }
}