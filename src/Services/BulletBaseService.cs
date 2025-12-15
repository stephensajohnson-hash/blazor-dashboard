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
        // 1. Create Tables (If they don't exist at all)
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
        ");

        // 2. FORCE ADD COLUMNS (Fixes 'Column does not exist' on existing tables)
        
        // A. Text Columns
        var textCols = new[] { "Category", "Type", "Title", "Description", "ImgUrl", "LinkUrl", "OriginalStringId" };
        foreach (var col in textCols)
        {
            await RunPatch($@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletItems' AND column_name='{col}') THEN 
                        ALTER TABLE ""BulletItems"" ADD COLUMN ""{col}"" text DEFAULT ''; 
                    END IF; 
                END $$;");
        }

        // B. Date Columns
        var dateCols = new[] { "Date", "CreatedAt" };
        foreach (var col in dateCols)
        {
            await RunPatch($@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletItems' AND column_name='{col}') THEN 
                        ALTER TABLE ""BulletItems"" ADD COLUMN ""{col}"" timestamp with time zone DEFAULT now(); 
                    END IF; 
                END $$;");
        }
        
        // C. Integer Columns
        await RunPatch(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='BulletItems' AND column_name='UserId') THEN 
                    ALTER TABLE ""BulletItems"" ADD COLUMN ""UserId"" integer DEFAULT 0; 
                END IF; 
            END $$;");
    }

    private async Task RunPatch(string sql)
    {
        try { await _db.Database.ExecuteSqlRawAsync(sql); } catch { /* Ignore harmless errors */ }
    }

    // --- REQUIRED FOR DELETE BUTTON ---
    public async Task DeleteItem(int itemId)
    {
        // 1. Delete Notes
        var notes = await _db.BulletItemNotes.Where(n => n.BulletItemId == itemId).ToListAsync();
        if(notes.Any()) _db.BulletItemNotes.RemoveRange(notes);

        // 2. Delete Details (Task)
        // Note: As we add other types (Sports, etc), we will add their detail delete calls here
        var taskDetails = await _db.BulletTaskDetails.Where(t => t.BulletItemId == itemId).ToListAsync();
        if(taskDetails.Any()) _db.BulletTaskDetails.RemoveRange(taskDetails);

        // 3. Delete Base Item
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