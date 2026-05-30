using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConcurrencyRaceConditionDemo.WinFormClient;

public partial class Form1 : Form
{
    private readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };

    public Form1()
    {
        InitializeComponent();
        
        // 繫結事件處理程序
        this.btnReset.Click += BtnReset_Click;
        this.btnRun.Click += BtnRun_Click;
    }

    private void Log(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(new Action(() => Log(message)));
        }
        else
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
    }

    private async Task ResetPointsAsync(int points)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/points/reset?points={points}", null);
            if (response.IsSuccessStatusCode)
            {
                Log($"資料庫點數重設成功，目前點數：{points}");
            }
            else
            {
                Log($"重設點數失敗：{response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Log($"連線失敗：{ex.Message}");
        }
    }

    private async void BtnReset_Click(object? sender, EventArgs e)
    {
        btnReset.Enabled = false;
        await ResetPointsAsync((int)numQuota.Value);
        btnReset.Enabled = true;
    }

    private async void BtnRun_Click(object? sender, EventArgs e)
    {
        btnRun.Enabled = false;
        btnReset.Enabled = false;
        txtLog.Clear();

        int initialQuota = (int)numQuota.Value;
        int requestCount = (int)numRequests.Value;
        bool isSafe = rbSafe.Checked;
        bool isRedis = rbRedis.Checked;
        bool isPessimistic = rbPessimistic.Checked;
        bool isOptimistic = rbOptimistic.Checked;

        string modeName = isRedis ? "Redis (背景非同步快取扣點)" 
            : (isPessimistic ? "Pessimistic (悲觀鎖 UPDLOCK)"
            : (isOptimistic ? "Optimistic (樂觀鎖 Version)"
            : (isSafe ? "Safe (安全原子扣點)" : "Unsafe (不安全扣點)")));

        string endpoint = isRedis ? "/api/points/deduct-redis" 
            : (isPessimistic ? "/api/points/deduct-pessimistic"
            : (isOptimistic ? "/api/points/deduct-optimistic"
            : (isSafe ? "/api/points/deduct-safe" : "/api/points/deduct-unsafe")));

        Log($"=== 開始併發測試 ===");
        Log($"模式：{modeName}");
        Log($"設定初始點數：{initialQuota}，併發請求數：{requestCount}");

        // 1. 重設點數
        await ResetPointsAsync(initialQuota);

        Log($"正在發送 {requestCount} 個併發請求...");

        int successCount = 0;
        int failCount = 0;
        int errorCount = 0;

        // 2. 發送併發請求
        var tasks = Enumerable.Range(1, requestCount).Select(async i =>
        {
            try
            {
                var response = await _httpClient.PostAsync(endpoint, null);
                if (response.IsSuccessStatusCode)
                {
                    Interlocked.Increment(ref successCount);
                    Log($"請求 #{i}：成功 (200 OK)");
                }
                else
                {
                    Interlocked.Increment(ref failCount);
                    Log($"請求 #{i}：拒絕/失敗 ({(int)response.StatusCode})");
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref errorCount);
                Log($"請求 #{i}：連線錯誤 ({ex.Message})");
            }
        });

        var startTime = DateTime.Now;
        await Task.WhenAll(tasks);
        var duration = DateTime.Now - startTime;

        Log($"併發請求發送完畢，耗時 {duration.TotalMilliseconds:F2} ms。");

        if (isRedis)
        {
            Log("等待背景非同步同步 SQL Server (200ms)...");
            await Task.Delay(200);
        }

        // 3. 查詢資料庫最終點數
        int finalPoints = 0;
        try
        {
            var response = await _httpClient.GetAsync("/api/points");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                // 格式為 {"id":1,"points":X}
                var pointsString = result.Split("\"points\":")[1].Split("}")[0].Trim();
                finalPoints = int.Parse(pointsString);
            }
        }
        catch (Exception ex)
        {
            Log($"查詢最終餘額時出錯：{ex.Message}");
        }

        // 4. 計算統計結果
        int pointsDeducted = initialQuota - finalPoints;
        int overDeducted = successCount - pointsDeducted;

        Log($"=== 統計結果 ===");
        Log($"初始點數：{initialQuota}");
        Log($"總請求數：{requestCount}");
        Log($"成功扣點次數：{successCount}");
        Log($"被拒絕次數：{failCount}");
        Log($"連線錯誤次數：{errorCount}");
        Log($"資料庫最終餘額：{finalPoints}");
        Log($"理論應扣點數：{Math.Min(initialQuota, requestCount)}");
        Log($"實際扣除點數：{pointsDeducted}");

        string modeLabel = isRedis ? "Redis" : (isPessimistic ? "Pessimistic" : (isOptimistic ? "Optimistic" : (isSafe ? "Safe" : "Unsafe")));
        string summaryText = $"測試結果 (模式: {modeLabel})\r\n" +
                            $"[初始/最終] {initialQuota} -> {finalPoints}  |  " +
                            $"[成功/被拒] {successCount} / {failCount}\r\n";

        bool isOverdrawn = successCount > initialQuota;
        bool isLostUpdate = successCount != pointsDeducted;

        if (isOverdrawn || isLostUpdate)
        {
            summaryText += "⚠️ 偵測到資源競爭！";
            if (isOverdrawn)
            {
                summaryText += $"點數透支！成功次數 ({successCount}) 大於初始點數 ({initialQuota})。";
                Log($"⚠️ 發生透支超扣！原本只有 {initialQuota} 點，但卻成功處理了 {successCount} 個扣點任務！");
            }
            if (isLostUpdate)
            {
                summaryText += $"帳目不合！成功扣點 {successCount} 次，但 DB 實際僅扣除 {pointsDeducted} 點。";
                Log($"⚠️ 發生遺失更新（Lost Update）！成功 {successCount} 次但 DB 實際僅扣除 {pointsDeducted} 點，有 {successCount - pointsDeducted} 次更新被覆蓋！");
            }
        }
        else
        {
            summaryText += "✅ 扣點運作正常，未發生資源競爭。";
            Log("✅ 運作正常，沒有發生資源競爭。");
        }

        lblSummary.Text = summaryText;

        btnRun.Enabled = true;
        btnReset.Enabled = true;
    }
}
