using Dashboard.Components.Bullet;
using Dashboard.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dashboard.Services
{
    public class BulletOrchestratorService
    {
        private readonly AppDbContext _db;
        private readonly BulletTaskService _taskService;
        private readonly BulletMeetingService _meetingService;
        private readonly BulletHabitService _habitService;
        private readonly BulletMediaService _mediaService;
        private readonly BulletHolidayService _holidayService;
        private readonly BulletBirthdayService _birthdayService;
        private readonly BulletAnniversaryService _anniversaryService;
        private readonly BulletVacationService _vacationService;
        private readonly BulletHealthService _healthService;
        private readonly BulletSportsService _sportsService;

        public BulletOrchestratorService(
            AppDbContext db,
            BulletTaskService taskService,
            BulletMeetingService meetingService,
            BulletHabitService habitService,
            BulletMediaService mediaService,
            BulletHolidayService holidayService,
            BulletBirthdayService birthdayService,
            BulletAnniversaryService anniversaryService,
            BulletVacationService vacationService,
            BulletHealthService healthService,
            BulletSportsService sportsService)
        {
            _db = db;
            _taskService = taskService;
            _meetingService = meetingService;
            _habitService = habitService;
            _mediaService = mediaService;
            _holidayService = holidayService;
            _birthdayService = birthdayService;
            _anniversaryService = anniversaryService;
            _vacationService = vacationService;
            _healthService = healthService;
            _sportsService = sportsService;
        }

    public async Task ProcessSaveRequest(BulletTaskService.TaskDTO item, BaseEditor.RecurrenceRequest? recur = null)
    {
        // 1. Always save the initial item first
        await CommitItemToDatabase(item);

        // 2. Check if we need to generate more items
        if (recur != null && recur.Type != "None" && recur.EndDate.HasValue)
        {
            DateTime currentTarget = item.Date;
            int currentStreak = item.HabitDetail?.StreakCount ?? 0;

            while (true)
            {
                // Advance the date based on frequency
                currentTarget = recur.Type switch
                {
                    "Daily" => currentTarget.AddDays(1),
                    "Weekly" => currentTarget.AddDays(7),
                    "Monthly" => currentTarget.AddMonths(1),
                    "TwiceMonthly" => currentTarget.AddDays(15), // Approximate or custom logic
                    _ => currentTarget.AddDays(1)
                };

                if (currentTarget.Date > recur.EndDate.Value.Date) break;

                // Create the clone for the next occurrence
                var clone = BulletMapper.CreateCopy(item);
                clone.Id = 0;
                clone.Date = currentTarget;

                // Increment streak if it's a habit
                if (clone.Type == "habit" && clone.HabitDetail != null)
                {
                    currentStreak++;
                    clone.HabitDetail.StreakCount = currentStreak;
                    clone.HabitDetail.IsCompleted = false; // New occurrences shouldn't start completed
                }

                await CommitItemToDatabase(clone);
            }
        }
    }
        private async Task HandleVacationSave(BulletTaskService.TaskDTO t)
        {
            string vGroupId = t.VacationDetail?.VacationGroupId;
            
            if (t.Id > 0 && !string.IsNullOrEmpty(vGroupId))
            {
                var groupDetails = await _db.BulletVacationDetails
                    .Include(v => v.BulletItem)
                    .Where(v => v.VacationGroupId == vGroupId)
                    .ToListAsync();

                foreach (var vd in groupDetails)
                {
                    if (vd.BulletItem != null)
                    {
                        vd.BulletItem.Title = t.Title;
                        vd.BulletItem.Category = t.Category;
                        vd.BulletItem.Description = t.Description;
                        vd.BulletItem.ImgUrl = t.ImgUrl;
                    }
                }
                
                await _vacationService.SaveVacation(new BulletVacationService.VacationDTO { Id = t.Id, UserId = t.UserId, Type = "vacation", Category = t.Category, Date = t.Date, Title = t.Title, Description = t.Description, ImgUrl = t.ImgUrl, Detail = t.VacationDetail ?? new(), Notes = t.Notes ?? new List<BulletItemNote>() });
                
                await _db.SaveChangesAsync();
            }
            else if (t.Id > 0)
            {
                await _vacationService.SaveVacation(new BulletVacationService.VacationDTO { Id = t.Id, UserId = t.UserId, Type = "vacation", Category = t.Category, Date = t.Date, Title = t.Title, Description = t.Description, ImgUrl = t.ImgUrl, Detail = t.VacationDetail ?? new(), Notes = t.Notes ?? new List<BulletItemNote>() });
            }
            else
            {
                var start = t.Date.Date;
                var end = (t.EndDate ?? start).Date;
                
                for (var dt = start; dt <= end; dt = dt.AddDays(1))
                {
                    await _vacationService.SaveVacation(new BulletVacationService.VacationDTO { Id = 0, UserId = t.UserId, Type = "vacation", Category = t.Category, Date = dt, Title = t.Title, Description = t.Description, ImgUrl = t.ImgUrl, Detail = t.VacationDetail ?? new(), Notes = t.Notes ?? new List<BulletItemNote>() });
                }
            }
        }

        public async Task SwapSortOrders(int sourceId, int targetId, List<BulletTaskService.TaskDTO> currentList)
        {
            var dbItem = await _db.BulletItems.FindAsync(sourceId);
            var dbTarget = await _db.BulletItems.FindAsync(targetId);

            if (dbItem != null && dbTarget != null)
            {
                if (dbItem.SortOrder == dbTarget.SortOrder)
                {
                    for (int i = 0; i < currentList.Count; i++)
                    {
                        var dItem = await _db.BulletItems.FindAsync(currentList[i].Id);
                        
                        if (dItem != null)
                        {
                            dItem.SortOrder = i * 10;
                        }
                    }
                    
                    await _db.SaveChangesAsync();
                    
                    dbItem = await _db.BulletItems.FindAsync(sourceId);
                    dbTarget = await _db.BulletItems.FindAsync(targetId);
                }

                int temp = dbItem.SortOrder;
                dbItem.SortOrder = dbTarget.SortOrder;
                dbTarget.SortOrder = temp;
                
                await _db.SaveChangesAsync();
            }
        }

        public async Task DeepCloneItem(
            BulletTaskService.TaskDTO source, 
            DateTime newDate, 
            bool includeNotes, 
            bool incompleteTodosOnly,
            bool removeFromSource)
        {
            var clone = BulletMapper.CreateCopy(source);
            
            clone.Date = newDate;
            clone.Id = 0;

            if (source.Type == "task" && source.Todos != null)
            {
                if (incompleteTodosOnly)
                {
                    clone.Todos = source.Todos
                        .Where(t => !t.IsCompleted)
                        .Select(t => new BulletTaskTodoItem 
                        { 
                            Content = t.Content, 
                            IsCompleted = false, 
                            Order = t.Order 
                        })
                        .ToList();
                }

                if (clone.Detail != null)
                {
                    clone.Detail.IsCompleted = false;
                }
            }

            if (!includeNotes)
            {
                clone.Notes = new List<BulletItemNote>();
            }

            if (source.Type == "task" && removeFromSource && source.Id > 0)
            {
                var dbSource = await _db.BulletItems
                    .Include(i => i.Todos)
                    .Include(i => i.DbTaskDetail)
                    .FirstOrDefaultAsync(i => i.Id == source.Id);

                if (dbSource != null)
                {
                    dbSource.Todos = dbSource.Todos
                        .Where(t => t.IsCompleted)
                        .ToList();

                    if (dbSource.DbTaskDetail != null)
                    {
                        dbSource.DbTaskDetail.IsCompleted = true;
                    }

                    await _db.SaveChangesAsync();
                }
            }

            await CommitItemToDatabase(clone);
        }
    }
}