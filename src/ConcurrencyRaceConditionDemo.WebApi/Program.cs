using Microsoft.EntityFrameworkCore;
using ConcurrencyRaceConditionDemo.WebApi.Data;
using ConcurrencyRaceConditionDemo.WebApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Ensure Database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/points", async (AppDbContext db) =>
{
    var member = await db.Members.FindAsync(1);
    return member != null ? Results.Ok(member) : Results.NotFound();
});

app.MapPost("/api/points/reset", async (int points, AppDbContext db) =>
{
    var member = await db.Members.FindAsync(1);
    if (member == null)
    {
        member = new Member { Id = 1, Points = points };
        db.Members.Add(member);
    }
    else
    {
        member.Points = points;
    }
    await db.SaveChangesAsync();
    return Results.Ok(member);
});

app.MapPost("/api/points/deduct-unsafe", async (AppDbContext db) =>
{
    var member = await db.Members.FindAsync(1);
    if (member == null || member.Points <= 0)
    {
        return Results.BadRequest("Points run out");
    }

    // 故意延遲 50ms，放大 Race Condition 區間，讓併發超扣更容易重現
    await Task.Delay(50);

    member.Points -= 1;
    await db.SaveChangesAsync();

    return Results.Ok(new { member.Points });
});

app.MapPost("/api/points/deduct-safe", async (AppDbContext db) =>
{
    // 利用 ExecuteUpdateAsync 進行原子更新：UPDATE Members SET Points = Points - 1 WHERE Id = 1 AND Points > 0
    var affectedRows = await db.Members
        .Where(m => m.Id == 1 && m.Points > 0)
        .ExecuteUpdateAsync(s => s.SetProperty(m => m.Points, m => m.Points - 1));

    if (affectedRows == 0)
    {
        return Results.BadRequest("Points run out");
    }

    return Results.Ok();
});

app.Run();
