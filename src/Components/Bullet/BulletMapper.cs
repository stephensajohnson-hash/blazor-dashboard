using Dashboard.Components.Bullet;
using System.Collections.Generic;
using System.Linq;

namespace Dashboard.Helpers
{
    public static class BulletMapper
    {
        public static BulletTaskService.TaskDTO CreateCopy(BulletTaskService.TaskDTO t)
        {
            var newItem = new BulletTaskService.TaskDTO 
            { 
                Id = 0, 
                UserId = t.UserId, 
                Type = t.Type, 
                Category = t.Category, 
                Date = t.Date, 
                EndDate = t.EndDate, 
                Title = t.Title, 
                Description = t.Description, 
                ImgUrl = t.ImgUrl, 
                LinkUrl = t.LinkUrl, 
                OriginalStringId = t.OriginalStringId, 
                SortOrder = t.SortOrder, 
                Notes = t.Notes?.Select(n => new BulletItemNote { Content = n.Content, ImgUrl = n.ImgUrl, LinkUrl = n.LinkUrl }).ToList() ?? new List<BulletItemNote>(), 
                Todos = t.Todos?.Select(x => new BulletTaskTodoItem { Content = x.Content, IsCompleted = x.IsCompleted, Order = x.Order }).ToList() ?? new List<BulletTaskTodoItem>(), 
                Meals = t.Meals?.ToList() ?? new List<BulletHealthMeal>(), 
                Workouts = t.Workouts?.ToList() ?? new List<BulletHealthWorkout>() 
            };

            if (t.Detail != null) newItem.Detail = new BulletTaskDetail { DueDate = t.Detail.DueDate, IsCompleted = t.Detail.IsCompleted, Priority = t.Detail.Priority, Status = t.Detail.Status, TicketNumber = t.Detail.TicketNumber, TicketUrl = t.Detail.TicketUrl };
            if (t.MeetingDetail != null) newItem.MeetingDetail = new BulletMeetingDetail { StartTime = t.MeetingDetail.StartTime, DurationMinutes = t.MeetingDetail.DurationMinutes, ActualDurationMinutes = t.MeetingDetail.ActualDurationMinutes, IsCompleted = t.MeetingDetail.IsCompleted };
            if (t.HabitDetail != null) newItem.HabitDetail = new BulletHabitDetail { StreakCount = t.HabitDetail.StreakCount, IsCompleted = t.HabitDetail.IsCompleted };
            if (t.VacationDetail != null) newItem.VacationDetail = new BulletVacationDetail { VacationGroupId = t.VacationDetail.VacationGroupId };
            if (t.HealthDetail != null) newItem.HealthDetail = new BulletHealthDetail { WeightLbs = t.HealthDetail.WeightLbs, CalculatedTDEE = t.HealthDetail.CalculatedTDEE };
            if (t.MediaDetail != null) newItem.MediaDetail = new BulletMediaDetail { Rating = t.MediaDetail.Rating, ReleaseYear = t.MediaDetail.ReleaseYear, Tags = t.MediaDetail.Tags };
            if (t.HolidayDetail != null) newItem.HolidayDetail = new BulletHolidayDetail { IsWorkHoliday = t.HolidayDetail.IsWorkHoliday };
            if (t.BirthdayDetail != null) newItem.BirthdayDetail = new BulletBirthdayDetail { DOB_Year = t.BirthdayDetail.DOB_Year };
            if (t.AnniversaryDetail != null) newItem.AnniversaryDetail = new BulletAnniversaryDetail { AnniversaryType = t.AnniversaryDetail.AnniversaryType, FirstYear = t.AnniversaryDetail.FirstYear };
            if (t.SportsDetail != null) newItem.SportsDetail = new BulletGameDetail { LeagueId = t.SportsDetail.LeagueId, HomeTeamId = t.SportsDetail.HomeTeamId, AwayTeamId = t.SportsDetail.AwayTeamId, HomeScore = t.SportsDetail.HomeScore, AwayScore = t.SportsDetail.AwayScore, StartTime = t.SportsDetail.StartTime, IsComplete = t.SportsDetail.IsComplete, TvChannel = t.SportsDetail.TvChannel, SeasonId = t.SportsDetail.SeasonId };
            
            return newItem;
        }

        public static BulletTaskService.TaskDTO MapMeeting(BulletMeetingService.MeetingDTO m)
        {
            return new BulletTaskService.TaskDTO { Id = m.Id, UserId = m.UserId, Type = "meeting", Category = m.Category, Date = m.Date, Title = m.Title, Description = m.Description, SortOrder = m.SortOrder, MeetingDetail = m.MeetingDetail ?? new(), ImgUrl = m.ImgUrl, LinkUrl = m.LinkUrl, Notes = m.Notes ?? new List<BulletItemNote>() };
        }

        public static BulletTaskService.TaskDTO MapBirthday(BulletBirthdayService.BirthdayDTO b)
        {
            return new BulletTaskService.TaskDTO { Id = b.Id, UserId = b.UserId, Type = "birthday", Category = b.Category, Date = b.Date, Title = b.Title, Description = b.Description, SortOrder = b.SortOrder, BirthdayDetail = b.Detail ?? new(), ImgUrl = b.ImgUrl, LinkUrl = b.LinkUrl, Notes = b.Notes ?? new List<BulletItemNote>() };
        }

        public static BulletTaskService.TaskDTO MapVacation(BulletVacationService.VacationDTO v)
        {
            return new BulletTaskService.TaskDTO { Id = v.Id, UserId = v.UserId, Type = "vacation", Category = v.Category, Date = v.Date, Title = v.Title, Description = v.Description, SortOrder = v.SortOrder, VacationDetail = v.Detail, ImgUrl = v.ImgUrl, LinkUrl = v.LinkUrl, Notes = v.Notes ?? new List<BulletItemNote>() };
        }

        public static BulletTaskService.TaskDTO MapHoliday(BulletHolidayService.HolidayDTO h)
        {
            return new BulletTaskService.TaskDTO { Id = h.Id, UserId = h.UserId, Type = "holiday", Category = h.Category, Date = h.Date, Title = h.Title, Description = h.Description, SortOrder = h.SortOrder, HolidayDetail = h.Detail, ImgUrl = h.ImgUrl, LinkUrl = h.LinkUrl, Notes = h.Notes ?? new List<BulletItemNote>() };
        }

        public static BulletTaskService.TaskDTO MapAnniversary(BulletAnniversaryService.AnniversaryDTO a)
        {
            return new BulletTaskService.TaskDTO { Id = a.Id, UserId = a.UserId, Type = "anniversary", Category = a.Category, Date = a.Date, Title = a.Title, Description = a.Description, SortOrder = a.SortOrder, AnniversaryDetail = a.Detail, ImgUrl = a.ImgUrl, LinkUrl = a.LinkUrl, Notes = a.Notes ?? new List<BulletItemNote>() };
        }

        public static BulletTaskService.TaskDTO MapGame(BulletSportsService.GameDTO g)
        {
            return new BulletTaskService.TaskDTO { Id = g.Id, UserId = g.UserId, Type = "sports", Category = g.Category, Date = g.Date, Title = g.Title, Description = g.Description, SortOrder = g.SortOrder, SportsDetail = g.Detail, ImgUrl = g.ImgUrl, LinkUrl = g.LinkUrl, Notes = g.Notes ?? new List<BulletItemNote>() };
        }

        public static BulletTaskService.TaskDTO MapMedia(BulletMediaService.MediaDTO m)
        {
            return new BulletTaskService.TaskDTO { Id = m.Id, UserId = m.UserId, Type = "media", Category = m.Category, Date = m.Date, Title = m.Title, Description = m.Description, SortOrder = m.SortOrder, MediaDetail = m.Detail, ImgUrl = m.ImgUrl, LinkUrl = m.LinkUrl, Notes = m.Notes ?? new List<BulletItemNote>() };
        }

        public static BulletTaskService.TaskDTO MapHealth(BulletHealthService.HealthDTO h)
        {
            return new BulletTaskService.TaskDTO { Id = h.Id, UserId = h.UserId, Type = "health", Category = h.Category, Date = h.Date, Title = h.Title, Description = h.Description, SortOrder = h.SortOrder, HealthDetail = h.Detail, Meals = h.Meals ?? new List<BulletHealthMeal>(), Workouts = h.Workouts ?? new List<BulletHealthWorkout>(), Notes = h.Notes ?? new List<BulletItemNote>(), ImgUrl = h.ImgUrl };
        }

        public static BulletTaskService.TaskDTO MapHabit(BulletHabitService.HabitDTO h)
        {
            return new BulletTaskService.TaskDTO { Id = h.Id, UserId = h.UserId, Type = "habit", Category = h.Category, Date = h.Date, Title = h.Title, Description = h.Description, SortOrder = h.SortOrder, HabitDetail = h.Detail, ImgUrl = h.ImgUrl };
        }
    }
}