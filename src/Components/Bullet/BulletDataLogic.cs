using Dashboard.Components.Bullet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dashboard.Helpers
{
    public static class BulletDataLogic
    {
        public static List<BulletTaskService.TaskDTO> GetSortedItems(List<BulletTaskService.TaskDTO> items)
        {
            if (items == null)
            {
                return new List<BulletTaskService.TaskDTO>();
            }

            return items
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => GetTypePriority(t.Type))
                .ThenBy(t => t.MeetingDetail?.StartTime)
                .ThenBy(t => t.Id)
                .ToList();
        }

        public static int GetTypePriority(string type)
        {
            return type switch
            {
                "health" => -3,
                "vacation" => -2,
                "habit" => -1,
                "holiday" => 0,
                "meeting" => 1,
                "task" => 2,
                "birthday" => 3,
                "anniversary" => 4,
                "media" => 5,
                _ => 6
            };
        }

        public static List<BulletTaskService.TaskDTO> GetItemsForColumn(
            List<BulletTaskService.TaskDTO> allItems, 
            DateTime date, 
            string category)
        {
            var dayItems = allItems
                .Where(t => t.Date.Date == date.Date);

            if (category.Equals("personal", StringComparison.OrdinalIgnoreCase))
            {
                return dayItems
                    .Where(t => !t.Category.Trim().Equals("work", StringComparison.OrdinalIgnoreCase) && 
                               !t.Category.Trim().Equals("health", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return dayItems
                .Where(t => t.Category.Trim().Equals(category.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static List<BulletTaskService.TaskDTO> GetItemsForDate(
            List<BulletTaskService.TaskDTO> allItems, 
            DateTime date)
        {
            return allItems
                .Where(t => t.Date.Date == date.Date)
                .ToList();
        }

        public static List<BulletSportsService.GameDTO> GetFavoriteGamesForDate(
            List<BulletSportsService.GameDTO> allGames, 
            List<Team> teams, 
            DateTime date)
        {
            var favoriteTeamIds = teams
                .Where(t => t.IsFavorite)
                .Select(t => t.Id)
                .ToList();

            return allGames
                .Where(g => g.Date.Date == date.Date && 
                           (favoriteTeamIds.Contains(g.Detail.HomeTeamId) || 
                            favoriteTeamIds.Contains(g.Detail.AwayTeamId)))
                .ToList();
        }
    }
}