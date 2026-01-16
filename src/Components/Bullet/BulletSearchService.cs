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
                if (Results.Count == 0) return 1;
                return (int)Math.Ceiling(Results.Count / (double)PageSize);
            }
        }

        public async Task ExecuteSearch(int userId)
        {
            // Removed the "if Query is empty return" guard.
            // We now proceed regardless of text, allowing date/type scans.
            IsSearching = true;
            Results.Clear();
            CurrentPage = 1;

            // We delegate the heavy lifting to the existing SearchItems method in BaseService
            var searchResults = await _baseService.SearchItems(userId, Query, Type, Start, End);
            
            if (searchResults != null)
            {
                Results.AddRange(searchResults);
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
            if (CurrentPage > 1) CurrentPage--;
        }

        public void MoveToNextPage()
        {
            if (CurrentPage < TotalPages) CurrentPage++;
        }
    }
}