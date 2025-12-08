using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Dashboard;

// 1. The Database Context (The Bridge to Postgres)
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<LinkGroup> LinkGroups { get; set; }
    public DbSet<Link> Links { get; set; }
}

// 2. The Tables
public class LinkGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public List<Link> Links { get; set; } = new();
}

public class Link
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    
    // This connects a Link to a Group
    public int LinkGroupId { get; set; }
    public LinkGroup? LinkGroup { get; set; }
}
