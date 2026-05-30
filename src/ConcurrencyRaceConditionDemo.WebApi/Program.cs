using Microsoft.EntityFrameworkCore;
using ConcurrencyRaceConditionDemo.WebApi.Data;
using ConcurrencyRaceConditionDemo.WebApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, Microsoft.Extensions.Logging.LogLevel.Information)
           .EnableSensitiveDataLogging());

// 註冊 Redis 連線服務
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect("127.0.0.1:6379"));

var app = builder.Build();

// Ensure Database is created and seeded (先刪除後建立，確保結構變更時能順利更新 schema)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureDeleted();
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

app.MapGet("/api/points/redis", async (IConnectionMultiplexer redis) =>
{
    var dbRedis = redis.GetDatabase();
    var val = await dbRedis.StringGetAsync("member:1:points");
    int points = val.HasValue ? (int)val : 0;
    return Results.Ok(new { Id = 1, Points = points });
});

app.MapPost("/api/points/reset", async (int points, AppDbContext db, IConnectionMultiplexer redis) =>
{
    var member = await db.Members.FindAsync(1);
    if (member == null)
    {
        member = new Member { Id = 1, Points = points, Version = 1 };
        db.Members.Add(member);
    }
    else
    {
        member.Points = points;
        member.Version = 1;
    }
    await db.SaveChangesAsync();

    // 同步重設 Redis 快取
    var dbRedis = redis.GetDatabase();
    await dbRedis.StringSetAsync("member:1:points", points);

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
    // 故意延遲 50ms，放大 Race Condition 區間，讓併發超扣更容易重現
    await Task.Delay(50);

    return Results.Ok();
});

app.MapPost("/api/points/deduct-redis", async (IConnectionMultiplexer redis) =>
{
    var dbRedis = redis.GetDatabase();
    
    // Lua 腳本：讀取並原子判斷點數是否大於 0。是的話減 1 並回傳剩餘值；已為 0 則回傳 -2；未初始化回傳 -1。
    string luaScript = @"
        local key = KEYS[1]
        local current = redis.call('get', key)
        if not current then
            return -1
        end
        current = tonumber(current)
        if current > 0 then
            local newValue = current - 1
            redis.call('set', key, newValue)
            return newValue
        else
            return -2
        end";

    var result = (int)await dbRedis.ScriptEvaluateAsync(
        luaScript, 
        new RedisKey[] { "member:1:points" });

    if (result == -1)
    {
        return Results.BadRequest("Redis key not initialized");
    }
    if (result == -2)
    {
        return Results.BadRequest("Points run out");
    }

    return Results.Ok(new { RemainingPoints = result });
});

app.MapPost("/api/points/deduct-pessimistic", async (AppDbContext db) =>
{
    // 開啟交易以實行鎖定
    using var tx = await db.Database.BeginTransactionAsync();
    try
    {
        // 利用 FromSqlRaw 執行 UPDLOCK, HOLDLOCK 強制行級鎖定
        var member = await db.Members
            .FromSqlRaw("SELECT * FROM Members WITH (UPDLOCK, HOLDLOCK) WHERE Id = 1")
            .SingleOrDefaultAsync();

        if (member == null || member.Points <= 0)
        {
            return Results.BadRequest("Points run out");
        }

        // 故意延遲 50ms 放大併發時間，可便於觀察鎖定排隊行為
        await Task.Delay(50);

        member.Points -= 1;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Results.Ok(new { member.Points });
    }
    catch (Exception)
    {
        await tx.RollbackAsync();
        throw;
    }
});

app.MapPost("/api/points/deduct-optimistic", async (AppDbContext db) =>
{
    var member = await db.Members.FindAsync(1);
    if (member == null || member.Points <= 0)
    {
        return Results.BadRequest("Points run out");
    }

    // 故意延遲 50ms 放大併發時間，使樂觀鎖衝突更容易發生
    await Task.Delay(50);

    // 扣點並手動遞增自訂 Version 版本號
    member.Points -= 1;
    member.Version += 1;

    try
    {
        await db.SaveChangesAsync();
        return Results.Ok(new { member.Points });
    }
    catch (DbUpdateConcurrencyException)
    {
        // 版本號衝突
        return Results.BadRequest("Optimistic concurrency conflict");
    }
});

app.Run();
