using Dashboard.Components.Bullet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dashboard.Services
{
    public class BulletSearchService
    {
        private readonly BulletBaseService _baseService;

        public BulletSearchService(BulletBaseService baseService)
        {
            _baseService = baseService;
        }

        public string Query { get; set; } = "";
        public string Type { get; set; } = "all";
        public DateTime Start { get; set; } = DateTime.Today.AddYears(-1);
        public DateTime End { get; set; } = DateTime.Today.AddYears(1);

        public bool IsSearching { get; set; } = false;
        public List<BulletTaskService.TaskDTO> Results { get; private set; } = new();
        
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        public int TotalPages
        {
            get
            {
                if (Results.Count == 0)
                {
                    return 1;
                }

                return (int)Math.Ceiling(Results.Count / (double)PageSize);
            }
        }

        public async Task ExecuteSearch(int userId)
        {
            // 1. Remove the 'if (string.IsNullOrWhiteSpace(Query)) return;' line
            
            IsSearching = true;
            Results.Clear();
            CurrentPage = 1;

            using var db = _factory.CreateDbContext();

            // Start with a base query for the user
            var query = db.BulletItems
                .Include(i => i.Todos)
                .Include(i => i.DbTaskDetail)
                .Include(i => i.DbHabitDetail)
                .Include(i => i.DbHealthDetail)
                .Include(i => i.DbMeetingDetail)
                .Where(i => i.UserId == userId);

            // Filter by Text (Only if Query is provided)
            if (!string.IsNullOrWhiteSpace(Query))
            {
                query = query.Where(i => EF.Functions.Like(i.Title, $"%{Query}%") 
                                    || EF.Functions.Like(i.Description, $"%{Query}%"));
            }

            // Filter by Type (Advanced)
            if (!string.IsNullOrWhiteSpace(Type) && Type != "all")
            {
                query = query.Where(i => i.Type == Type);
            }

            // Filter by Date Range (Advanced)
            if (Start.HasValue) query = query.Where(i => i.Date >= Start.Value);
            if (End.HasValue) query = query.Where(i => i.Date <= End.Value);

            // Order and Execute
            var dbItems = await query
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            // Map to DTOs for the UI
            foreach (var item in dbItems)
            {
                Results.Add(BulletMapper.MapAny(item));
            }
        }

        public void ClearSearch()
        {
            Query = "";
            IsSearching = false;
            Results.Clear();
            CurrentPage = 1;
        }

        public IEnumerable<BulletTaskService.TaskDTO> GetPagedResults()
        {
            return Results
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize);
        }

        public void MoveToPrevPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage = CurrentPage - 1;
            }
        }

        public void MoveToNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage = CurrentPage + 1;
            }
        }
    }
}