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

    // UI 控制項
    private Label lblQuota = null!;
    private NumericUpDown numQuota = null!;
    private Label lblRequests = null!;
    private NumericUpDown numRequests = null!;
    private GroupBox gbMode = null!;
    private RadioButton rbUnsafe = null!;
    private RadioButton rbSafe = null!;
    private Button btnRun = null!;
    private Button btnReset = null!;
    private TextBox txtLog = null!;
    private Label lblSummary = null!;

    public Form1()
    {
        InitializeComponent();
        InitializeComponentCustom();
    }

    private void InitializeComponentCustom()
    {
        this.Text = "多執行緒資源競爭 (Race Condition) 演示工具";
        this.Size = new System.Drawing.Size(700, 550);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Quota
        lblQuota = new Label { Text = "初始點數 (Quota):", Left = 20, Top = 20, Width = 120 };
        numQuota = new NumericUpDown { Left = 150, Top = 18, Width = 80, Minimum = 1, Maximum = 100000, Value = 10 };

        // Requests
        lblRequests = new Label { Text = "併發請求數 (Requests):", Left = 250, Top = 20, Width = 150 };
        numRequests = new NumericUpDown { Left = 400, Top = 18, Width = 80, Minimum = 1, Maximum = 1000, Value = 50 };

        // GroupBox Mode
        gbMode = new GroupBox { Text = "模式選擇", Left = 20, Top = 60, Width = 460, Height = 60 };
        rbUnsafe = new RadioButton { Text = "Unsafe (未防禦，點數會超扣)", Left = 10, Top = 25, Width = 210, Checked = true };
        rbSafe = new RadioButton { Text = "Safe (有防禦，不會超扣)", Left = 230, Top = 25, Width = 210 };
        gbMode.Controls.Add(rbUnsafe);
        gbMode.Controls.Add(rbSafe);

        // Buttons
        btnReset = new Button { Text = "重設點數", Left = 490, Top = 18, Width = 80, Height = 25 };
        btnReset.Click += BtnReset_Click;

        btnRun = new Button { Text = "執行併發測試", Left = 490, Top = 70, Width = 170, Height = 45 };
        btnRun.Click += BtnRun_Click;

        // Summary Label
        lblSummary = new Label { Text = "狀態: 待機中", Left = 20, Top = 135, Width = 640, Height = 45, BorderStyle = BorderStyle.Fixed3D, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };

        // Log TextBox
        txtLog = new TextBox { Left = 20, Top = 190, Width = 640, Height = 300, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };

        // Add to Form
        this.Controls.Add(lblQuota);
        this.Controls.Add(numQuota);
        this.Controls.Add(lblRequests);
        this.Controls.Add(numRequests);
        this.Controls.Add(gbMode);
        this.Controls.Add(btnReset);
        this.Controls.Add(btnRun);
        this.Controls.Add(lblSummary);
        this.Controls.Add(txtLog);
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
        string modeName = isSafe ? "Safe (安全扣點)" : "Unsafe (不安全扣點)";
        string endpoint = isSafe ? "/api/points/deduct-safe" : "/api/points/deduct-unsafe";

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

        string summaryText = $"測試結果 (模式: {(isSafe ? "Safe" : "Unsafe")})\r\n" +
                            $"[初始/最終] {initialQuota} -> {finalPoints}  |  " +
                            $"[成功/被拒] {successCount} / {failCount}\r\n";

        if (successCount > initialQuota)
        {
            summaryText += $"⚠️ 資源競爭發生！成功次數 ({successCount}) 大於初始額度 ({initialQuota})！超扣數量：{successCount - initialQuota}";
            Log($"⚠️ 發生超扣現象！原本只有 {initialQuota} 點，但卻成功處理了 {successCount} 個扣點任務！");
        }
        else
        {
            summaryText += $"✅ 扣點運作正常，未發生超扣。";
            Log($"✅ 沒有超扣現象。成功處理任務次數 ({successCount}) 符合額度限制。");
        }

        lblSummary.Text = summaryText;

        btnRun.Enabled = true;
        btnReset.Enabled = true;
    }
}
