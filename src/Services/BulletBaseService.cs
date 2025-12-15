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
        // 1. Create Master Table
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
        ");

        // 2. Create Universal Notes Table
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""BulletItemNotes"" (
                ""Id"" serial PRIMARY KEY, 
                ""BulletItemId"" integer NOT NULL, 
                ""Content"" text, 
                ""ImgUrl"" text, 
                ""LinkUrl"" text, 
                ""Order"" integer DEFAULT 0
            );
        ");
    }

    // --- CLEANUP TOOL ---
    // Call this ONLY when you are ready to destroy the old version's data
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
            DROP TABLE IF EXISTS ""BulletLeagues"";
            DROP TABLE IF EXISTS ""BulletSeasons"";
            DROP TABLE IF EXISTS ""BulletTeams"";
        ");
    }

    public async Task<List<BulletItem>> GetItemsByDate(int userId, DateTime date)
    {
        return await _db.BulletItems
            .Where(x => x.UserId == userId && x.Date.Date == date.Date)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }
    
    public async Task DeleteItem(int itemId)
    {
        // 1. Delete Notes
        var notes = await _db.BulletItemNotes.Where(n => n.BulletItemId == itemId).ToListAsync();
        _db.BulletItemNotes.RemoveRange(notes);

        // 2. Delete Detail Rows (We accept the risk of manual cleanup or cascading delete here for simplicity)
        // In a perfect world, we'd delete from BulletTaskDetails where BulletItemId = itemId too.
        var taskDetails = await _db.BulletTaskDetails.Where(t => t.BulletItemId == itemId).ToListAsync();
        _db.BulletTaskDetails.RemoveRange(taskDetails);

        // 3. Delete Base
        var item = await _db.BulletItems.FindAsync(itemId);
        if(item != null) _db.BulletItems.Remove(item);
        
        await _db.SaveChangesAsync();
    }
}