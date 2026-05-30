using Microsoft.EntityFrameworkCore;
using ConcurrencyRaceConditionDemo.WebApi.Models;

namespace ConcurrencyRaceConditionDemo.WebApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Member> Members { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 初始化種子資料，讓測試端點好操作
        modelBuilder.Entity<Member>().HasData(
            new Member { Id = 1, Points = 0 }
        );
    }
}
