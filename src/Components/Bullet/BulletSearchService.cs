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
            if (string.IsNullOrWhiteSpace(Query))
            {
                ClearSearch();
                return;
            }

            CurrentPage = 1;
            IsSearching = true;

            var results = await _baseService.SearchItems(
                userId, 
                Query, 
                Type, 
                Start, 
                End
            );

            Results = results
                .OrderBy(x => x.Date)
                .ToList();
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