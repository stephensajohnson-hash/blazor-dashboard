using Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

public class BulletHealthService
{
    private readonly AppDbContext _db;

    public BulletHealthService(AppDbContext db)
    {
        _db = db;
    }

    public class HealthDTO : BulletItem
    {
        public BulletHealthDetail Detail { get; set; } = new();
        public List<BulletHealthMeal> Meals { get; set; } = new();
        public List<BulletHealthWorkout> Workouts { get; set; } = new();
        public List<BulletItemNote> Notes { get; set; } = new();
    }

    public async Task<List<HealthDTO>> GetHealthItemsForRange(int userId, DateTime start, DateTime end)
    {
        var items = await (from baseItem in _db.BulletItems
                           join detail in _db.BulletHealthDetails on baseItem.Id equals detail.BulletItemId
                           where baseItem.UserId == userId 
                                 && baseItem.Date >= start && baseItem.Date <= end
                                 && baseItem.Type == "health"
                           select new HealthDTO 
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
            var meals = await _db.BulletHealthMeals.Where(m => ids.Contains(m.BulletItemId)).ToListAsync();
            var workouts = await _db.BulletHealthWorkouts.Where(w => ids.Contains(w.BulletItemId)).ToListAsync();
            var notes = await _db.BulletItemNotes.Where(n => ids.Contains(n.BulletItemId)).OrderBy(n => n.Order).ToListAsync();

            foreach (var i in items)
            {
                i.Meals = meals.Where(m => m.BulletItemId == i.Id).ToList();
                i.Workouts = workouts.Where(w => w.BulletItemId == i.Id).ToList();
                i.Notes = notes.Where(n => n.BulletItemId == i.Id).ToList();
            }
        }
        return items;
    }

    // NEW: Calculate cumulative deficit for the week (Mon -> Date)
    public async Task<double> GetWeeklyDeficit(int userId, DateTime date, User user)
    {
        // Calculate start of week (Monday)
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime monday = date.Date.AddDays(-1 * diff);
        DateTime end = date.Date.AddDays(1).AddSeconds(-1);

        // Fetch all health items for this week up to now
        var weekItems = await GetHealthItemsForRange(userId, monday, end);

        double totalDeficit = 0;

        foreach (var item in weekItems)
        {
            // 1. Calculate TDEE (Using stored or calculating fallback)
            double tdee = item.Detail.CalculatedTDEE;
            if (tdee == 0 && item.Detail.WeightLbs > 0)
            {
                // Fallback Calculation (Mifflin-St Jeor)
                double weightKg = item.Detail.WeightLbs * 0.453592;
                double heightCm = user.HeightInches * 2.54;
                double bmr = (10 * weightKg) + (6.25 * heightCm) - (5 * user.Age) + (user.Gender == "Male" ? 5 : -161);
                double multiplier = user.ActivityLevel switch { "Sedentary" => 1.2, "Light" => 1.375, "Moderate" => 1.55, "Active" => 1.725, "Extra" => 1.9, _ => 1.2 };
                tdee = bmr * multiplier;
            }
            if (tdee == 0) tdee = 2000;

            // 2. Net Calories
            double consumed = item.Meals.Sum(m => m.Calories);
            double burned = item.Workouts.Sum(w => w.CaloriesBurned);
            double net = consumed - burned;

            // 3. Deficit (TDEE - Net)
            // Positive result means we are UNDER maintenance (good for weight loss)
            // Negative result means we are OVER maintenance (surplus)
            totalDeficit += (tdee - net);
        }

        return totalDeficit;
    }

    public async Task SaveHealth(HealthDTO dto)
    {
        BulletItem? item = null;
        if (dto.Date.Kind == DateTimeKind.Unspecified) dto.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

        if (dto.Id > 0) item = await _db.BulletItems.FindAsync(dto.Id);
        else {
            item = new BulletItem { UserId = dto.UserId, Type = "health", CreatedAt = DateTime.UtcNow };
            await _db.BulletItems.AddAsync(item);
        }

        item.Title = dto.Title; item.Category = dto.Category; item.Description = dto.Description; 
        item.ImgUrl = dto.ImgUrl; item.LinkUrl = dto.LinkUrl; item.Date = dto.Date;
        item.SortOrder = dto.SortOrder;
        
        await _db.SaveChangesAsync();

        var detail = await _db.BulletHealthDetails.FindAsync(item.Id);
        if (detail == null) { detail = new BulletHealthDetail { BulletItemId = item.Id }; await _db.BulletHealthDetails.AddAsync(detail); }

        detail.WeightLbs = dto.Detail.WeightLbs;
        detail.CalculatedTDEE = dto.Detail.CalculatedTDEE;

        await _db.SaveChangesAsync();

        var oldMeals = await _db.BulletHealthMeals.Where(m => m.BulletItemId == item.Id).ToListAsync();
        _db.BulletHealthMeals.RemoveRange(oldMeals);
        foreach (var m in dto.Meals) { m.Id = 0; m.BulletItemId = item.Id; await _db.BulletHealthMeals.AddAsync(m); }

        var oldWorkouts = await _db.BulletHealthWorkouts.Where(w => w.BulletItemId == item.Id).ToListAsync();
        _db.BulletHealthWorkouts.RemoveRange(oldWorkouts);
        foreach (var w in dto.Workouts) { w.Id = 0; w.BulletItemId = item.Id; await _db.BulletHealthWorkouts.AddAsync(w); }

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
            User? user = await _db.Users.FindAsync(userId);
            
            foreach (var el in items.EnumerateArray())
            {
                string GetStr(string key) => (el.TryGetProperty(key, out var v) || el.TryGetProperty(char.ToUpper(key[0]) + key.Substring(1), out v)) ? v.ToString() : "";
                
                if (GetStr("type").ToLower() == "health")
                {
                    DateTime itemDate = DateTime.UtcNow;
                    string dateStr = GetStr("date");
                    if (DateTime.TryParse(dateStr, out var parsedDate)) itemDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);

                    var item = new BulletItem { 
                        UserId = userId, Type = "health", CreatedAt = DateTime.UtcNow, Date = itemDate,
                        Title = "Daily Health Log", Description = "", OriginalStringId = GetStr("id"),
                        Category = "health" 
                    };

                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync(); 

                    var detail = new BulletHealthDetail { BulletItemId = item.Id };
                    if (el.TryGetProperty("weightLbs", out var weightVal)) detail.WeightLbs = weightVal.GetDouble();
                    if (el.TryGetProperty("goals", out var goals))
                    {
                        if(goals.TryGetProperty("tdee", out var tdee)) detail.CalculatedTDEE = tdee.GetInt32();
                        if(user != null)
                        {
                            if(goals.TryGetProperty("protein", out var p)) user.DailyProteinGoal = p.GetInt32();
                            if(goals.TryGetProperty("fat", out var f)) user.DailyFatGoal = f.GetInt32();
                            if(goals.TryGetProperty("netCarbs", out var nc)) user.DailyCarbGoal = nc.GetInt32();
                        }
                    }
                    await _db.BulletHealthDetails.AddAsync(detail);

                    if (el.TryGetProperty("meals", out var mealsArr) && mealsArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach(var m in mealsArr.EnumerateArray())
                        {
                            var meal = new BulletHealthMeal { BulletItemId = item.Id };
                            if(m.TryGetProperty("mealType", out var mt)) meal.MealType = mt.ToString();
                            if(m.TryGetProperty("name", out var mn)) meal.Name = mn.ToString();
                            if(m.TryGetProperty("calories", out var mc)) meal.Calories = mc.GetDouble();
                            if(m.TryGetProperty("protein", out var mp)) meal.Protein = mp.GetDouble();
                            if(m.TryGetProperty("carbs", out var mcb)) meal.Carbs = mcb.GetDouble();
                            if(m.TryGetProperty("fat", out var mf)) meal.Fat = mf.GetDouble();
                            if(m.TryGetProperty("fiber", out var mfi)) meal.Fiber = mfi.GetDouble();
                            await _db.BulletHealthMeals.AddAsync(meal);
                        }
                    }

                    if (el.TryGetProperty("workouts", out var workArr) && workArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach(var workoutItem in workArr.EnumerateArray())
                        {
                            var workout = new BulletHealthWorkout { BulletItemId = item.Id };
                            if(workoutItem.TryGetProperty("description", out var desc)) workout.Name = desc.ToString();
                            if(workoutItem.TryGetProperty("calories", out var wc)) workout.CaloriesBurned = wc.GetDouble();
                            if(workoutItem.TryGetProperty("timeSpent", out var wt)) workout.TimeSpentMinutes = wt.GetInt32();
                            await _db.BulletHealthWorkouts.AddAsync(workout);
                        }
                    }
                    count++;
                }
            }
            if(user != null) _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }
        return count;
    }
}