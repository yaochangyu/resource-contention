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

// 建立非同步寫入 SQL Server 的 Channel 佇列，限制為單一消費者
var dbUpdateChannel = System.Threading.Channels.Channel.CreateUnbounded<int>(
    new System.Threading.Channels.UnboundedChannelOptions { SingleReader = true });

var app = builder.Build();

// Ensure Database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// 啟動背景同步 SQL Server 消費端，單執行緒處理以防 DB 併發衝突，並做資料庫減壓
_ = Task.Run(async () =>
{
    var reader = dbUpdateChannel.Reader;
    while (await reader.WaitToReadAsync())
    {
        var latestPoints = await reader.ReadAsync();
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            await db.Members
                .Where(m => m.Id == 1)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Points, latestPoints));
            
            Console.WriteLine($"[背景同步] 已將 SQL Server 點數同步更新為：{latestPoints}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[背景同步] 寫入資料庫時出錯：{ex.Message}");
        }
    }
});

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

app.MapPost("/api/points/reset", async (int points, AppDbContext db, IConnectionMultiplexer redis) =>
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

    // 將最新剩餘點數寫入 Channel，讓單執行緒背景工作處理非同步寫入 SQL Server
    dbUpdateChannel.Writer.TryWrite(result);

    return Results.Ok(new { RemainingPoints = result });
});

app.Run();
