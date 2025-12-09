using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;

namespace Dashboard;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<LinkGroup> LinkGroups { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Countdown> Countdowns { get; set; }
    public DbSet<Stock> Stocks { get; set; }
}

public class LinkGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "blue";
    public bool IsStatic { get; set; }
    public int Order { get; set; } // <--- NEW
    public List<Link> Links { get; set; } = new();
}

public class Link
{
    public int Id { get; set; }
    public int LinkGroupId { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Img { get; set; } = "";
    public int Order { get; set; } // <--- NEW
}

public class Countdown
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime TargetDate { get; set; }
    public string LinkUrl { get; set; } = "";
    public string Img { get; set; } = "";
    public int Order { get; set; } // <--- NEW
}

public class Stock
{
    public int Id { get; set; }
    public string Symbol { get; set; } = "";
    public string ImgUrl { get; set; } = "";
    public string LinkUrl { get; set; } = "";
    public double Shares { get; set; }
    public int Order { get; set; } // <--- NEW
}