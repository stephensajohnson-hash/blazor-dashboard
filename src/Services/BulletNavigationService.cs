using System;

namespace Dashboard.Services
{
    public class BulletNavigationService
    {
        public DateTime ViewDate { get; private set; } = DateTime.Today;
        public string CurrentView { get; private set; } = "day";
        public int ClientOffsetMinutes { get; set; } = 0;

        public void SetViewDate(DateTime date)
        {
            ViewDate = date;
        }

        public void SetView(string view)
        {
            CurrentView = view;
        }

        public void GoToToday()
        {
            ViewDate = DateTime.UtcNow.AddMinutes(-ClientOffsetMinutes).Date;
            CurrentView = "day";
        }

        public void MovePrev()
        {
            if (CurrentView == "day")
            {
                ViewDate = ViewDate.AddDays(-1);
            }
            else if (CurrentView == "week")
            {
                ViewDate = ViewDate.AddDays(-7);
            }
            else
            {
                ViewDate = ViewDate.AddMonths(-1);
            }
        }

        public void MoveNext()
        {
            if (CurrentView == "day")
            {
                ViewDate = ViewDate.AddDays(1);
            }
            else if (CurrentView == "week")
            {
                ViewDate = ViewDate.AddDays(7);
            }
            else
            {
                ViewDate = ViewDate.AddMonths(1);
            }
        }

        public DateTime GetWeekStart()
        {
            int diff = (7 + (ViewDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            return ViewDate.AddDays(-1 * diff).Date;
        }

        public string GetHeaderDateText()
        {
            if (CurrentView == "day")
            {
                return ViewDate.ToString("D");
            }

            if (CurrentView == "week")
            {
                var start = GetWeekStart();
                return $"Week of {start.ToString("MMM d")}";
            }

            return ViewDate.ToString("MMMM yyyy");
        }
    }
}