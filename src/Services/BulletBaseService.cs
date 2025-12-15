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
        // ... (Keep existing table creation logic) ...
        // Ensure StoredImages exists
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""StoredImages"" (
                ""Id"" serial PRIMARY KEY, 
                ""Data"" bytea, 
                ""ContentType"" text, 
                ""OriginalName"" text, 
                ""UploadedAt"" timestamp with time zone DEFAULT now()
            );
        ");
        
        // ... (Keep existing column patch logic) ...
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

    // --- IMAGES ---
    public async Task<int> SaveImageAsync(byte[] data, string contentType)
    {
        var img = new StoredImage { Data = data, ContentType = contentType, UploadedAt = DateTime.UtcNow };
        _db.StoredImages.Add(img);
        await _db.SaveChangesAsync();
        return img.Id;
    }

    // --- DATA CLEANUP ---
    public async Task ClearDataByType(int userId, string type)
    {
        var items = await _db.BulletItems.Where(x => x.UserId == userId && x.Type == type).ToListAsync();
        foreach(var item in items)
        {
            await DeleteItem(item.Id);
        }
    }

    public async Task DeleteItem(int itemId)
    {
        var notes = await _db.BulletItemNotes.Where(n => n.BulletItemId == itemId).ToListAsync();
        if(notes.Any()) _db.BulletItemNotes.RemoveRange(notes);

        var taskDetails = await _db.BulletTaskDetails.Where(t => t.BulletItemId == itemId).ToListAsync();
        if(taskDetails.Any()) _db.BulletTaskDetails.RemoveRange(taskDetails);

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