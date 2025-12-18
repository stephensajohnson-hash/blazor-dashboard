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

        // Meals
        var oldMeals = await _db.BulletHealthMeals.Where(m => m.BulletItemId == item.Id).ToListAsync();
        _db.BulletHealthMeals.RemoveRange(oldMeals);
        foreach (var m in dto.Meals) { m.Id = 0; m.BulletItemId = item.Id; await _db.BulletHealthMeals.AddAsync(m); }

        // Workouts
        var oldWorkouts = await _db.BulletHealthWorkouts.Where(w => w.BulletItemId == item.Id).ToListAsync();
        _db.BulletHealthWorkouts.RemoveRange(oldWorkouts);
        foreach (var w in dto.Workouts) { w.Id = 0; w.BulletItemId = item.Id; await _db.BulletHealthWorkouts.AddAsync(w); }

        // Notes
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
            // 1. Find User Goals from the latest health entry to update User table
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
                        Category = "health" // Force category
                    };

                    await _db.BulletItems.AddAsync(item);
                    await _db.SaveChangesAsync(); 

                    var detail = new BulletHealthDetail { BulletItemId = item.Id };
                    
                    if (el.TryGetProperty("weightLbs", out var w)) detail.WeightLbs = w.GetDouble();
                    
                    // Parse Goals Object
                    if (el.TryGetProperty("goals", out var goals))
                    {
                        if(goals.TryGetProperty("tdee", out var tdee)) detail.CalculatedTDEE = tdee.GetInt32();
                        
                        // Update User Goals (Using last entry wins strategy effectively)
                        if(user != null)
                        {
                            if(goals.TryGetProperty("protein", out var p)) user.DailyProteinGoal = p.GetInt32();
                            if(goals.TryGetProperty("fat", out var f)) user.DailyFatGoal = f.GetInt32();
                            if(goals.TryGetProperty("netCarbs", out var nc)) user.DailyCarbGoal = nc.GetInt32();
                            // Calorie Goal logic? Maybe derive deficit from TDEE - Goal? 
                            // For now just stick to defaults or separate update.
                        }
                    }
                    
                    await _db.BulletHealthDetails.AddAsync(detail);

                    // Parse Meals
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

                    // Parse Workouts
                    if (el.TryGetProperty("workouts", out var workArr) && workArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach(var w in workArr.EnumerateArray())
                        {
                            var workout = new BulletHealthWorkout { BulletItemId = item.Id };
                            if(w.TryGetProperty("description", out var desc)) workout.Name = desc.ToString();
                            if(w.TryGetProperty("calories", out var wc)) workout.CaloriesBurned = wc.GetDouble();
                            if(w.TryGetProperty("timeSpent", out var wt)) workout.TimeSpentMinutes = wt.GetInt32();
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