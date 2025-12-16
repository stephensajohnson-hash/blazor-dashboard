using Dashboard;
using Microsoft.EntityFrameworkCore;

public class BulletBaseService
{
    private readonly AppDbContext _db;

    public BulletBaseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task CreateBaseTablesIfMissing()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletItems"" (
                ""Id"" serial PRIMARY KEY, 
                ""UserId"" integer NOT NULL, 
                ""Type"" text, 
                ""Category"" text, 
                ""Date"" timestamp with time zone, 
                ""CreatedAt"" timestamp with time zone, 
                ""Title"" text, 
                ""Description"" text, 
                ""ImgUrl"" text, 
                ""LinkUrl"" text, 
                ""OriginalStringId"" text
            );

            CREATE TABLE IF NOT EXISTS ""BulletItemNotes"" (
                ""Id"" serial PRIMARY KEY, 
                ""BulletItemId"" integer NOT NULL, 
                ""Content"" text, 
                ""ImgUrl"" text, 
                ""LinkUrl"" text, 
                ""Order"" integer DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS ""StoredImages"" (
                ""Id"" serial PRIMARY KEY, 
                ""Data"" bytea, 
                ""ContentType"" text, 
                ""OriginalName"" text, 
                ""UploadedAt"" timestamp with time zone DEFAULT now()
            );
        ");
        
        await RunPatch(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletItems' AND column_name='Date') THEN 
                    ALTER TABLE ""BulletItems"" ADD COLUMN ""Date"" timestamp with time zone DEFAULT now(); 
                END IF; 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletItems' AND column_name='CreatedAt') THEN 
                    ALTER TABLE ""BulletItems"" ADD COLUMN ""CreatedAt"" timestamp with time zone DEFAULT now(); 
                END IF; 
                 IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletItems' AND column_name='UserId') THEN 
                    ALTER TABLE ""BulletItems"" ADD COLUMN ""UserId"" integer DEFAULT 0; 
                END IF;
            END $$;");
    }

    private async Task RunPatch(string sql)
    {
        try { await _db.Database.ExecuteSqlRawAsync(sql); } catch { /* Ignore */ }
    }

    public async Task<int> SaveImageAsync(byte[] data, string contentType)
    {
        var img = new StoredImage { Data = data, ContentType = contentType, UploadedAt = DateTime.UtcNow };
        _db.StoredImages.Add(img);
        await _db.SaveChangesAsync();
        return img.Id;
    }

    // --- FIX: SAFE EF CORE DELETION (No Raw SQL) ---
    public async Task<int> ClearDataByType(int userId, string type)
    {
        // 1. Fetch Items to delete
        var items = await _db.BulletItems
            .Where(x => x.UserId == userId && x.Type == type)
            .ToListAsync();

        if (!items.Any()) return 0;

        var ids = items.Select(x => x.Id).ToList();

        // 2. Fetch and Remove Children (Notes)
        var notes = await _db.BulletItemNotes
            .Where(x => ids.Contains(x.BulletItemId))
            .ToListAsync();
        if(notes.Any()) _db.BulletItemNotes.RemoveRange(notes);

        // 3. Fetch and Remove Children (Task Details)
        // We use a try/catch here because BulletTaskDetails might be accessed via a different DbContext setup
        // But since we are in the same context, we can try to access it if it's mapped.
        // If not mapped in _db properties, we use raw SQL as a fallback for just this table.
        try {
             await _db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""BulletTaskDetails"" WHERE ""BulletItemId"" IN (SELECT ""Id"" FROM ""BulletItems"" WHERE ""UserId"" = {0} AND ""Type"" = {1})", 
                userId, type);
        } catch { /* Ignore if table missing */ }

        // 4. Remove Base Items
        _db.BulletItems.RemoveRange(items);
        
        // 5. Commit
        await _db.SaveChangesAsync();
        
        return items.Count;
    }

    public async Task DeleteItem(int itemId)
    {
        var notes = await _db.BulletItemNotes.Where(n => n.BulletItemId == itemId).ToListAsync();
        if(notes.Any()) _db.BulletItemNotes.RemoveRange(notes);

        await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""BulletTaskDetails"" WHERE ""BulletItemId"" = {0}", itemId);

        var item = await _db.BulletItems.FindAsync(itemId);
        if(item != null) _db.BulletItems.Remove(item);
        
        await _db.SaveChangesAsync();
    }

    public async Task DropLegacyTables()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
            DROP TABLE IF EXISTS ""BulletTasks"";
            DROP TABLE IF EXISTS ""BulletMeetings"";
            DROP TABLE IF EXISTS ""BulletMedia"";
            DROP TABLE IF EXISTS ""BulletHabits"";
            DROP TABLE IF EXISTS ""BulletMacroTrackers"";
            DROP TABLE IF EXISTS ""BulletMeals"";
            DROP TABLE IF EXISTS ""BulletWorkouts"";
            DROP TABLE IF EXISTS ""BulletHolidays"";
            DROP TABLE IF EXISTS ""BulletBirthdays"";
            DROP TABLE IF EXISTS ""BulletAnniversaries"";
            DROP TABLE IF EXISTS ""BulletVacations"";
            DROP TABLE IF EXISTS ""BulletGames"";
        ");
    }
}