using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class BulletBaseService
{
    private readonly AppDbContext _db;

    public BulletBaseService(AppDbContext db)
    {
        _db = db;
    }

    // 1. SETUP
    public async Task CreateBaseTablesIfMissing()
    {
        // Tables
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletItems"" (""Id"" serial PRIMARY KEY, ""UserId"" integer, ""Type"" text, ""Category"" text, ""Date"" timestamp with time zone, ""CreatedAt"" timestamp with time zone, ""Title"" text, ""Description"" text, ""ImgUrl"" text, ""LinkUrl"" text, ""OriginalStringId"" text);
            CREATE TABLE IF NOT EXISTS ""BulletItemNotes"" (""Id"" serial PRIMARY KEY, ""BulletItemId"" integer, ""Content"" text, ""ImgUrl"" text, ""LinkUrl"" text, ""Order"" integer DEFAULT 0);
            CREATE TABLE IF NOT EXISTS ""BulletTaskDetails"" (""BulletItemId"" integer PRIMARY KEY, ""Status"" text, ""IsCompleted"" boolean, ""Priority"" text, ""TicketNumber"" text, ""TicketUrl"" text, ""DueDate"" timestamp with time zone);
            CREATE TABLE IF NOT EXISTS ""StoredImages"" (""Id"" serial PRIMARY KEY, ""Data"" bytea, ""ContentType"" text, ""OriginalName"" text, ""UploadedAt"" timestamp with time zone);
        ");
        
        // Columns (Idempotent patch)
        var patch = @"DO $$ BEGIN 
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletItems' AND column_name='LinkUrl') THEN ALTER TABLE ""BulletItems"" ADD COLUMN ""LinkUrl"" text DEFAULT ''; END IF;
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletItems' AND column_name='UserId') THEN ALTER TABLE ""BulletItems"" ADD COLUMN ""UserId"" integer DEFAULT 0; END IF;
        END $$;";
        await _db.Database.ExecuteSqlRawAsync(patch);
    }

    public async Task<int> SaveImageAsync(byte[] data, string contentType)
    {
        var img = new StoredImage { Data = data, ContentType = contentType, UploadedAt = DateTime.UtcNow };
        _db.StoredImages.Add(img);
        await _db.SaveChangesAsync();
        return img.Id;
    }

    // 2. DELETE LOGIC (Safe Mode)
    public async Task<int> ClearDataByType(int userId, string type)
    {
        // Delete Children (Notes)
        try {
            await _db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""BulletItemNotes"" WHERE ""BulletItemId"" IN (SELECT ""Id"" FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1})", 
                userId, type);
        } catch { /* Ignore table missing */ }

        // Delete Children (Details)
        try {
            await _db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""BulletTaskDetails"" WHERE ""BulletItemId"" IN (SELECT ""Id"" FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1})", 
                userId, type);
        } catch { /* Ignore */ }

        // Delete Parent
        int count = 0;
        try {
            count = await _db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1}", 
                userId, type);
        } catch { /* Ignore */ }
            
        return count;
    }

    public async Task DeleteItem(int itemId)
    {
        await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletItemNotes"" WHERE ""BulletItemId"" = {0}", itemId);
        await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletTaskDetails"" WHERE ""BulletItemId"" = {0}", itemId);
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
        ");
    }
}