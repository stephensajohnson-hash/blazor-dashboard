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
    public DbSet<User> Users { get; set; } // <--- NEW
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = ""; // We store the hash, not the text
    public string ZipCode { get; set; } = "75482"; // Default to Sulphur Springs
    public string AvatarUrl { get; set; } = "";
}

// ... (Keep existing LinkGroup, Link, Countdown, Stock classes exactly as they are) ...

// ... ADD THIS HELPER AT THE BOTTOM OF THE FILE ...
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