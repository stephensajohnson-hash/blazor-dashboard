using Dashboard.Components.Bullet;
using System.Collections.Generic;
using System.Linq;

namespace Dashboard.Helpers
{
    public static class BulletMapper
    {
        public static BulletTaskService.TaskDTO MapMeeting(BulletMeetingService.MeetingDTO m)
        {
            return new BulletTaskService.TaskDTO
            {
                Id = m.Id,
                UserId = m.UserId,
                Type = "meeting",
                Category = m.Category,
                Date = m.Date,
                Title = m.Title,
                Description = m.Description,
                SortOrder = m.SortOrder,
                MeetingDetail = m.MeetingDetail ?? new(),
                ImgUrl = m.ImgUrl,
                LinkUrl = m.LinkUrl,
                Notes = m.Notes ?? new List<BulletItemNote>()
            };
        }

        public static BulletTaskService.TaskDTO MapBirthday(BulletBirthdayService.BirthdayDTO b)
        {
            return new BulletTaskService.TaskDTO
            {
                Id = b.Id,
                UserId = b.UserId,
                Type = "birthday",
                Category = b.Category,
                Date = b.Date,
                Title = b.Title,
                Description = b.Description,
                SortOrder = b.SortOrder,
                BirthdayDetail = b.Detail ?? new(),
                ImgUrl = b.ImgUrl,
                LinkUrl = b.LinkUrl,
                Notes = b.Notes ?? new List<BulletItemNote>()
            };
        }

        public static BulletTaskService.TaskDTO MapVacation(BulletVacationService.VacationDTO v)
        {
            return new BulletTaskService.TaskDTO
            {
                Id = v.Id,
                UserId = v.UserId,
                Type = "vacation",
                Category = v.Category,
                Date = v.Date,
                Title = v.Title,
                Description = v.Description,
                SortOrder = v.SortOrder,
                VacationDetail = v.Detail,
                ImgUrl = v.ImgUrl,
                LinkUrl = v.LinkUrl,
                Notes = v.Notes ?? new List<BulletItemNote>()
            };
        }

        public static BulletTaskService.TaskDTO MapHoliday(BulletHolidayService.HolidayDTO h)
        {
            return new BulletTaskService.TaskDTO
            {
                Id = h.Id,
                UserId = h.UserId,
                Type = "holiday",
                Category = h.Category,
                Date = h.Date,
                Title = h.Title,
                Description = h.Description,
                SortOrder = h.SortOrder,
                HolidayDetail = h.Detail,
                ImgUrl = h.ImgUrl,
                LinkUrl = h.LinkUrl,
                Notes = h.Notes ?? new List<BulletItemNote>()
            };
        }

        public static BulletTaskService.TaskDTO MapAnniversary(BulletAnniversaryService.AnniversaryDTO a)
        {
            return new BulletTaskService.TaskDTO
            {
                Id = a.Id,
                UserId = a.UserId,
                Type = "anniversary",
                Category = a.Category,
                Date = a.Date,
                Title = a.Title,
                Description = a.Description,
                SortOrder = a.SortOrder,
                AnniversaryDetail = a.Detail,
                ImgUrl = a.ImgUrl,
                LinkUrl = a.LinkUrl,
                Notes = a.Notes ?? new List<BulletItemNote>()
            };
        }

        public static BulletTaskService.TaskDTO MapGame(BulletSportsService.GameDTO g)
        {
            return new BulletTaskService.TaskDTO
            {
                Id = g.Id,
                UserId = g.UserId,
                Type = "sports",
                Category = g.Category,
                Date = g.Date,
                Title = g.Title,
                Description = g.Description,
                SortOrder = g.SortOrder,
                SportsDetail = g.Detail,
                ImgUrl = g.ImgUrl,
                LinkUrl = g.LinkUrl,
                Notes = g.Notes ?? new List<BulletItemNote>()
            };
        }

        public static BulletTaskService.TaskDTO MapMedia(BulletMediaService.MediaDTO m)
        {
            return new BulletTaskService.TaskDTO
            {
                Id = m.Id,
                UserId = m.UserId,
                Type = "media",
                Category = m.Category,
                Date = m.Date,
                Title = m.Title,
                Description = m.Description,
                SortOrder = m.SortOrder,
                MediaDetail = m.Detail,
                ImgUrl = m.ImgUrl,
                LinkUrl = m.LinkUrl,
                Notes = m.Notes ?? new List<BulletItemNote>()
            };
        }

        public static BulletTaskService.TaskDTO MapHealth(BulletHealthService.HealthDTO h)
        {
            return new BulletTaskService.TaskDTO
            {
                Id = h.Id,
                UserId = h.UserId,
                Type = "health",
                Category = h.Category,
                Date = h.Date,
                Title = h.Title,
                Description = h.Description,
                SortOrder = h.SortOrder,
                HealthDetail = h.Detail,
                Meals = h.Meals ?? new List<BulletHealthMeal>(),
                Workouts = h.Workouts ?? new List<BulletHealthWorkout>(),
                Notes = h.Notes ?? new List<BulletItemNote>(),
                ImgUrl = h.ImgUrl
            };
        }

        public static BulletTaskService.TaskDTO MapHabit(BulletHabitService.HabitDTO h)
        {
            return new BulletTaskService.TaskDTO
            {
                Id = h.Id,
                UserId = h.UserId,
                Type = "habit",
                Category = h.Category,
                Date = h.Date,
                Title = h.Title,
                Description = h.Description,
                SortOrder = h.SortOrder,
                HabitDetail = h.Detail,
                ImgUrl = h.ImgUrl
            };
        }
    }
}