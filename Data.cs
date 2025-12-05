using Microsoft.EntityFrameworkCore;
using System;

namespace Dashboard
{
    // We are putting the database context inside the Dashboard namespace
    // so App.razor can find it easily.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<LogEntry> LogEntries { get; set; }
    }

    public class LogEntry
    {
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
