using ChatterAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatterAPI;

public class MessageDbContext : DbContext
{
    public DbSet<Message> Messages { get; set; }

    public string DbPath { get; }
    public MessageDbContext()
    {
        var path = Directory.GetCurrentDirectory();
        DbPath = System.IO.Path.Join(path, "Database", "messages.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}
