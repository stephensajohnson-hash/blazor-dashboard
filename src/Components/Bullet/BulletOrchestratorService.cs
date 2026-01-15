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

        public async Task CommitItemToDatabase(BulletTaskService.TaskDTO t)
        {
            if (t.Id > 0)
            {
                var dbItem = await _db.BulletItems.FindAsync(t.Id);
                
                if (dbItem != null)
                {
                    dbItem.Type = t.Type;
                    dbItem.Category = t.Category;
                    dbItem.Description = t.Description;
                    
                    await _db.SaveChangesAsync();
                }
            }

            string type = t.Type?.ToLower().Trim() ?? "task";

            if (type == "meeting")
            {
                await _meetingService.SaveMeeting(new BulletMeetingService.MeetingDTO { Id = t.Id, UserId = t.UserId, Type = "meeting", Category = t.Category, Date = t.Date, Title = t.Title, Description = t.Description, ImgUrl = t.ImgUrl, LinkUrl = t.LinkUrl, MeetingDetail = t.MeetingDetail ?? new(), Notes = t.Notes ?? new List<BulletItemNote>() });
            }
            else if (type == "habit")
            {
                await _habitService.SaveHabit(new BulletHabitService.HabitDTO { Id = t.Id, UserId = t.UserId, Type = "habit", Category = t.Category, Date = t.Date, Title = t.Title, Description = t.Description, ImgUrl = t.ImgUrl, Detail = t.HabitDetail ?? new(), Notes = t.Notes ?? new List<BulletItemNote>() });
            }
            else if (type == "media")
            {
                await _mediaService.SaveMedia(new BulletMediaService.MediaDTO { Id = t.Id, UserId = t.UserId, Type = "media", Category = t.Category, Date = t.Date, Title = t.Title, Description = t.Description, ImgUrl = t.ImgUrl, Detail = t.MediaDetail ?? new(), Notes = t.Notes ?? new List<BulletItemNote>() });
            }
            else if (type == "holiday")
            {
                await _holidayService.SaveHoliday(new BulletHolidayService.HolidayDTO { Id = t.Id, UserId = t.UserId, Type = "holiday", Category = t.Category, Date = t.Date, Title = t.Title, Description = t.Description, ImgUrl = t.ImgUrl, LinkUrl = t.LinkUrl, Detail = t.HolidayDetail ?? new() });
            }
            else if (type == "birthday")
            {
                await _birthdayService.SaveBirthday(new BulletBirthdayService.BirthdayDTO { Id = t.Id, UserId = t.UserId, Type = "birthday", Category = t.Category, Date = t.Date, Title = t.Title, Description = t.Description, ImgUrl = t.ImgUrl, LinkUrl = t.LinkUrl, Detail = t.BirthdayDetail ?? new() });
            }
            else if (type == "anniversary")
            {
                await _anniversaryService.SaveAnniversary(new BulletAnniversaryService.AnniversaryDTO { Id = t.Id, UserId = t.UserId, Type = "anniversary", Category = t.Category, Date = t.Date, Title = t.Title, Description = t.Description, ImgUrl = t.ImgUrl, LinkUrl = t.LinkUrl, Detail = t.AnniversaryDetail ?? new() });
            }
            else if (type == "vacation")
            {
                await HandleVacationSave(t);
            }
            else if (type == "health")
            {
                await _healthService.SaveHealth(new BulletHealthService.HealthDTO { Id = t.Id, UserId = t.UserId, Date = t.Date, Title = t.Title, Description = t.Description, Category = t.Category, Detail = t.HealthDetail ?? new(), Meals = t.Meals ?? new List<BulletHealthMeal>(), Workouts = t.Workouts ?? new List<BulletHealthWorkout>(), Notes = t.Notes ?? new List<BulletItemNote>() });
            }
            else if (type == "sports")
            {
                await _sportsService.SaveGame(new BulletSportsService.GameDTO { Id = t.Id, UserId = t.UserId, Type = "sports", Category = t.Category, Date = t.Date, Title = t.Title, Description = t.Description, ImgUrl = t.ImgUrl, Detail = t.SportsDetail ?? new() });
            }
            else
            {
                await _taskService.SaveTask(t);
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
            bool incompleteTodosOnly)
        {
            // 1. Generate the base copy using the existing Mapper
            var clone = BulletMapper.CreateCopy(source);
            
            // 2. Adjust the date and reset ID for a new record
            clone.Date = newDate;
            clone.Id = 0;

            // 3. Handle Task-Specific Todo Filtering
            if (source.Type == "task" && incompleteTodosOnly && source.Todos != null)
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
                    
                if (clone.Detail != null)
                {
                    clone.Detail.IsCompleted = false;
                }
            }

            // 4. Handle Note Exclusion
            if (!includeNotes)
            {
                clone.Notes = new List<BulletItemNote>();
            }

            // 5. Commit using the existing pipeline logic
            await CommitItemToDatabase(clone);
        }
    }
}