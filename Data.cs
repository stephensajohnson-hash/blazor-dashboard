using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Dashboard;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<LinkGroup> LinkGroups { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Countdown> Countdowns { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<User> Users { get; set; } 
}

public class LinkGroup
{
    public int Id { get; set; }
    public int UserId { get; set; } // <--- NEW
    public string Name { get; set; } = "";
    public string Color { get; set; } = "blue";
    public bool IsStatic { get; set; }
    public int Order { get; set; } 
    public List<Link> Links { get; set; } = new();
}

public class Link
{
    public int Id { get; set; }
    public int UserId { get; set; } // <--- NEW
    public int LinkGroupId { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Img { get; set; } = "";
    public int Order { get; set; }
}

public class Countdown
{
    public int Id { get; set; }
    public int UserId { get; set; } // <--- NEW
    public string Name { get; set; } = "";
    public DateTime TargetDate { get; set; }
    public string LinkUrl { get; set; } = "";
    public string Img { get; set; } = "";
    public int Order { get; set; }
}

public class Stock
{
    public int Id { get; set; }
    public int UserId { get; set; } // <--- NEW
    public string Symbol { get; set; } = "";
    public string ImgUrl { get; set; } = "";
    public string LinkUrl { get; set; } = "";
    public double Shares { get; set; }
    public int Order { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string ZipCode { get; set; } = "75482"; 
    public string AvatarUrl { get; set; } = "";
}

public static class PasswordHelper
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        return HashPassword(password) == storedHash;
    }
}