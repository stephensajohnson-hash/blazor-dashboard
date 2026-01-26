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

    public async Task<int> CloneYearlyItems(int userId, int sourceYear, int targetYear, bool includeBirthdays, bool includeAnniversaries, bool includeHolidays)
    {
        int count = 0;
        var typesToCopy = new List<string>();
        if (includeBirthdays) typesToCopy.Add("birthday");
        if (includeAnniversaries) typesToCopy.Add("anniversary");
        if (includeHolidays) typesToCopy.Add("holiday");

        if (!typesToCopy.Any()) return 0;

        // 1. Get all source items
        var sourceItems = await _db.BulletItems
            .Where(x => x.UserId == userId && 
                        typesToCopy.Contains(x.Type) && 
                        x.Date.Year == sourceYear)
            .ToListAsync();

        foreach (var item in sourceItems)
        {
            // Calculate new date (keeping month and day)
            DateTime newDate;
            try 
            {
                newDate = new DateTime(targetYear, item.Date.Month, item.Date.Day);
            } 
            catch 
            {
                // Handle Feb 29th cases for non-leap years - move to Feb 28
                newDate = new DateTime(targetYear, 2, 28);
            }

            // 2. Create the new Base Item
            var newItem = new BulletItem
            {
                UserId = userId,
                Type = item.Type,
                Category = item.Category,
                Title = item.Title,
                Description = item.Description,
                Date = newDate,
                ImgUrl = item.ImgUrl,
                LinkUrl = item.LinkUrl,
                SortOrder = item.SortOrder,
                OriginalStringId = item.OriginalStringId // Keep metadata
            };

            _db.BulletItems.Add(newItem);
            await _db.SaveChangesAsync(); // Save to get the new ID for FKs

            // 3. Clone the Specific Details based on Type
            if (item.Type == "birthday")
            {
                var detail = await _db.BulletBirthdayDetails.FirstOrDefaultAsync(d => d.BulletItemId == item.Id);
                if (detail != null) 
                    _db.BulletBirthdayDetails.Add(new BulletBirthdayDetail { BulletItemId = newItem.Id, DOB_Year = detail.DOB_Year });
            }
            else if (item.Type == "anniversary")
            {
                var detail = await _db.BulletAnniversaryDetails.FirstOrDefaultAsync(d => d.BulletItemId == item.Id);
                if (detail != null) 
                    _db.BulletAnniversaryDetails.Add(new BulletAnniversaryDetail { BulletItemId = newItem.Id, FirstYear = detail.FirstYear, AnniversaryType = detail.AnniversaryType });
            }
            else if (item.Type == "holiday")
            {
                var detail = await _db.BulletHolidayDetails.FirstOrDefaultAsync(d => d.BulletItemId == item.Id);
                if (detail != null) 
                    _db.BulletHolidayDetails.Add(new BulletHolidayDetail { BulletItemId = newItem.Id, IsWorkHoliday = detail.IsWorkHoliday });
            }

            count++;
        }

        await _db.SaveChangesAsync();
        return count;
    }

    public async Task CreateBaseTablesIfMissing()
    {
        // 1. User Goals
        try { await _db.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""WeeklyCalorieDeficitGoal"" INTEGER NOT NULL DEFAULT 3500;"); } catch { }
        try { await _db.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""DailyProteinGoal"" INTEGER NOT NULL DEFAULT 150;"); } catch { }
        try { await _db.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""DailyFatGoal"" INTEGER NOT NULL DEFAULT 70;"); } catch { }
        try { await _db.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""DailyCarbGoal"" INTEGER NOT NULL DEFAULT 200;"); } catch { }

        // 2. Core Bullet Header Table
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletItems"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" INTEGER NOT NULL, ""Type"" TEXT NOT NULL DEFAULT 'task', ""Category"" TEXT NOT NULL DEFAULT 'personal', ""Date"" TIMESTAMP NOT NULL, ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(), ""Title"" TEXT NOT NULL DEFAULT '', ""Description"" TEXT NOT NULL DEFAULT '', ""ImgUrl"" TEXT NOT NULL DEFAULT '', ""LinkUrl"" TEXT NOT NULL DEFAULT '', ""OriginalStringId"" TEXT NOT NULL DEFAULT '', ""Order"" INTEGER NOT NULL DEFAULT 0);");
        try { await _db.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""BulletItems"" ADD COLUMN IF NOT EXISTS ""Order"" INTEGER NOT NULL DEFAULT 0;"); } catch { }

        // 3. Detail Tables (Foreign Key linked to BulletItems)
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletItemNotes"" (""Id"" SERIAL PRIMARY KEY, ""BulletItemId"" INTEGER NOT NULL, ""Content"" TEXT NOT NULL DEFAULT '', ""ImgUrl"" TEXT NOT NULL DEFAULT '', ""LinkUrl"" TEXT NOT NULL DEFAULT '', ""Order"" INTEGER NOT NULL DEFAULT 0, CONSTRAINT ""FK_BulletItemNotes_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletTaskDetails"" (""BulletItemId"" INTEGER NOT NULL PRIMARY KEY, ""Status"" TEXT NOT NULL DEFAULT 'Pending', ""IsCompleted"" BOOLEAN NOT NULL DEFAULT FALSE, ""Priority"" TEXT NOT NULL DEFAULT 'Normal', ""TicketNumber"" TEXT NOT NULL DEFAULT '', ""TicketUrl"" TEXT NOT NULL DEFAULT '', ""DueDate"" TIMESTAMP NULL, CONSTRAINT ""FK_BulletTaskDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletMeetingDetails"" (""BulletItemId"" INTEGER NOT NULL PRIMARY KEY, ""StartTime"" TIMESTAMP NULL, ""DurationMinutes"" INTEGER NOT NULL DEFAULT 0, ""ActualDurationMinutes"" INTEGER NOT NULL DEFAULT 0, ""IsCompleted"" BOOLEAN NOT NULL DEFAULT FALSE, CONSTRAINT ""FK_BulletMeetingDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletHabitDetails"" (""BulletItemId"" INTEGER NOT NULL PRIMARY KEY, ""StreakCount"" INTEGER NOT NULL DEFAULT 0, ""Status"" TEXT NOT NULL DEFAULT 'Active', ""IsCompleted"" BOOLEAN NOT NULL DEFAULT FALSE, CONSTRAINT ""FK_BulletHabitDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletMediaDetails"" (""BulletItemId"" INTEGER NOT NULL PRIMARY KEY, ""Rating"" INTEGER NOT NULL DEFAULT 0, ""ReleaseYear"" INTEGER NOT NULL DEFAULT 0, ""Tags"" TEXT NOT NULL DEFAULT '', CONSTRAINT ""FK_BulletMediaDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletHolidayDetails"" (""BulletItemId"" INTEGER NOT NULL PRIMARY KEY, ""IsWorkHoliday"" BOOLEAN NOT NULL DEFAULT FALSE, CONSTRAINT ""FK_HolidayDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletBirthdayDetails"" (""BulletItemId"" INTEGER NOT NULL PRIMARY KEY, ""DOB_Year"" INTEGER NULL, CONSTRAINT ""FK_BirthdayDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletAnniversaryDetails"" (""BulletItemId"" INTEGER NOT NULL PRIMARY KEY, ""AnniversaryType"" TEXT NOT NULL DEFAULT 'Other', ""FirstYear"" INTEGER NULL, CONSTRAINT ""FK_AnnivDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletVacationDetails"" (""BulletItemId"" INTEGER NOT NULL PRIMARY KEY, ""VacationGroupId"" TEXT NOT NULL DEFAULT '', CONSTRAINT ""FK_VacationDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletHealthDetails"" (""BulletItemId"" INTEGER NOT NULL PRIMARY KEY, ""WeightLbs"" DOUBLE PRECISION NOT NULL DEFAULT 0, ""CalculatedTDEE"" INTEGER NOT NULL DEFAULT 0, CONSTRAINT ""FK_HealthDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletHealthMeals"" (""Id"" SERIAL PRIMARY KEY, ""BulletItemId"" INTEGER NOT NULL, ""MealType"" TEXT NOT NULL DEFAULT 'Breakfast', ""Name"" TEXT NOT NULL DEFAULT '', ""Calories"" DOUBLE PRECISION NOT NULL DEFAULT 0, ""Protein"" DOUBLE PRECISION NOT NULL DEFAULT 0, ""Carbs"" DOUBLE PRECISION NOT NULL DEFAULT 0, ""Fat"" DOUBLE PRECISION NOT NULL DEFAULT 0, ""Fiber"" DOUBLE PRECISION NOT NULL DEFAULT 0, CONSTRAINT ""FK_HealthMeals_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletHealthWorkouts"" (""Id"" SERIAL PRIMARY KEY, ""BulletItemId"" INTEGER NOT NULL, ""Name"" TEXT NOT NULL DEFAULT '', ""CaloriesBurned"" DOUBLE PRECISION NOT NULL DEFAULT 0, ""TimeSpentMinutes"" INTEGER NOT NULL DEFAULT 0, CONSTRAINT ""FK_Workouts_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");

        // 4. Sports Data Tables
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""Leagues"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" INTEGER NOT NULL DEFAULT 0, ""Name"" TEXT NOT NULL DEFAULT '', ""ImgUrl"" TEXT NOT NULL DEFAULT '', ""LinkUrl"" TEXT NOT NULL DEFAULT '');");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""Seasons"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" INTEGER NOT NULL DEFAULT 0, ""LeagueId"" INTEGER NOT NULL DEFAULT 0, ""Name"" TEXT NOT NULL DEFAULT '', ""ImgUrl"" TEXT NOT NULL DEFAULT '');");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""Teams"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" INTEGER NOT NULL DEFAULT 0, ""LeagueId"" INTEGER NOT NULL DEFAULT 0, ""Name"" TEXT NOT NULL DEFAULT '', ""Abbreviation"" TEXT NOT NULL DEFAULT '', ""LogoUrl"" TEXT NOT NULL DEFAULT '', ""IsFavorite"" BOOLEAN NOT NULL DEFAULT FALSE);");
        await _db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""BulletGameDetails"" (""BulletItemId"" INTEGER NOT NULL PRIMARY KEY, ""LeagueId"" INTEGER NOT NULL DEFAULT 0, ""SeasonId"" INTEGER NOT NULL DEFAULT 0, ""HomeTeamId"" INTEGER NOT NULL DEFAULT 0, ""AwayTeamId"" INTEGER NOT NULL DEFAULT 0, ""HomeScore"" INTEGER NOT NULL DEFAULT 0, ""AwayScore"" INTEGER NOT NULL DEFAULT 0, ""IsComplete"" BOOLEAN NOT NULL DEFAULT FALSE, ""StartTime"" TIMESTAMP NULL, ""TvChannel"" TEXT NOT NULL DEFAULT '', CONSTRAINT ""FK_GameDetails_BulletItems"" FOREIGN KEY (""BulletItemId"") REFERENCES ""BulletItems""(""Id"") ON DELETE CASCADE);");
    }

    public async Task<List<BulletTaskService.TaskDTO>> SearchItems(int userId, string query, string type, DateTime? start, DateTime? end)
    {
        try
        {
            // Start the base query filtered by UserId
            var baseQuery = _db.BulletItems.AsNoTracking()
                .Where(x => x.UserId == userId);

            // ONLY apply date filters if they are provided (not null)
            if (start.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.Date >= start.Value);
            }
            if (end.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.Date <= end.Value);
            }

            // Apply Text Search logic
            if (!string.IsNullOrWhiteSpace(query))
            {
                var lowerQuery = query.ToLower();
                baseQuery = baseQuery.Where(x => 
                    x.Title.ToLower().Contains(lowerQuery) || 
                    x.Description.ToLower().Contains(lowerQuery) ||
                    // Search within Meals associated with the item
                    _db.BulletHealthMeals.Any(m => m.BulletItemId == x.Id && m.Name.ToLower().Contains(lowerQuery)));
            }

            // Apply Type filter
            if (!string.IsNullOrWhiteSpace(type) && type != "all")
            {
                baseQuery = baseQuery.Where(x => x.Type == type);
            }

            // Execute database fetch with all required Includes
            var items = await baseQuery
                .Include(x => x.DbTaskDetail)
                .Include(x => x.DbMeetingDetail)
                .Include(x => x.DbHabitDetail)
                .Include(x => x.DbMediaDetail)
                .Include(x => x.DbHolidayDetail)
                .Include(x => x.DbBirthdayDetail)
                .Include(x => x.DbAnniversaryDetail)
                .Include(x => x.DbVacationDetail)
                .Include(x => x.DbHealthDetail)
                .Include(x => x.DbSportsDetail)
                .Include(x => x.Notes)
                .Include(x => x.Meals) 
                .Include(x => x.Workouts)
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            // Map results to DTOs
            return items.Select(t => new BulletTaskService.TaskDTO
            {
                Id = t.Id,
                UserId = t.UserId,
                Type = t.Type,
                Category = t.Category,
                Date = t.Date,
                Title = t.Title,
                Description = t.Description,
                ImgUrl = t.ImgUrl,
                LinkUrl = t.LinkUrl,
                OriginalStringId = t.OriginalStringId,
                SortOrder = t.SortOrder,
                Detail = t.DbTaskDetail ?? new(),
                MeetingDetail = t.DbMeetingDetail,
                HabitDetail = t.DbHabitDetail,
                MediaDetail = t.DbMediaDetail,
                HolidayDetail = t.DbHolidayDetail,
                BirthdayDetail = t.DbBirthdayDetail,
                AnniversaryDetail = t.DbAnniversaryDetail,
                VacationDetail = t.DbVacationDetail,
                HealthDetail = t.DbHealthDetail,
                SportsDetail = t.DbSportsDetail,
                Notes = t.Notes?.OrderBy(n => n.Order).ToList() ?? new(),
                Meals = t.Meals?.ToList() ?? new(),
                Workouts = t.Workouts?.ToList() ?? new()
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SEARCH_SQL_CRASH: {ex.Message}");
            return new List<BulletTaskService.TaskDTO>();
        }
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
        foreach(var item in items)
        {
            if(updates.TryGetValue(item.Id, out int newOrder))
            {
                item.SortOrder = newOrder;
            }
        }
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
            Data = data, 
            ContentType = contentType, 
            UploadedAt = DateTime.UtcNow, 
            OriginalName = "upload.jpg" 
        };
        await _db.StoredImages.AddAsync(img);
        await _db.SaveChangesAsync();
        return $"/db-images/{img.Id}";
    }
}