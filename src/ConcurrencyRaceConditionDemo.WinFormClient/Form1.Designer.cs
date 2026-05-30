namespace ConcurrencyRaceConditionDemo.WinFormClient;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.lblQuota = new System.Windows.Forms.Label();
        this.numQuota = new System.Windows.Forms.NumericUpDown();
        this.lblRequests = new System.Windows.Forms.Label();
        this.numRequests = new System.Windows.Forms.NumericUpDown();
        this.gbMode = new System.Windows.Forms.GroupBox();
        this.rbUnsafe = new System.Windows.Forms.RadioButton();
        this.rbSafe = new System.Windows.Forms.RadioButton();
        this.rbRedis = new System.Windows.Forms.RadioButton();
        this.rbPessimistic = new System.Windows.Forms.RadioButton();
        this.rbOptimistic = new System.Windows.Forms.RadioButton();
        this.btnReset = new System.Windows.Forms.Button();
        this.btnRun = new System.Windows.Forms.Button();
        this.lblSummary = new System.Windows.Forms.Label();
        this.txtLog = new System.Windows.Forms.TextBox();
        ((System.ComponentModel.ISupportInitialize)(this.numQuota)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numRequests)).BeginInit();
        this.gbMode.SuspendLayout();
        this.SuspendLayout();
        // 
        // lblQuota
        // 
        this.lblQuota.Location = new System.Drawing.Point(20, 20);
        this.lblQuota.Name = "lblQuota";
        this.lblQuota.Size = new System.Drawing.Size(120, 23);
        this.lblQuota.TabIndex = 0;
        this.lblQuota.Text = "初始點數 (Quota):";
        // 
        // numQuota
        // 
        this.numQuota.Location = new System.Drawing.Point(150, 18);
        this.numQuota.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        this.numQuota.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numQuota.Name = "numQuota";
        this.numQuota.Size = new System.Drawing.Size(80, 23);
        this.numQuota.TabIndex = 1;
        this.numQuota.Value = new decimal(new int[] { 10, 0, 0, 0 });
        // 
        // lblRequests
        // 
        this.lblRequests.Location = new System.Drawing.Point(250, 20);
        this.lblRequests.Name = "lblRequests";
        this.lblRequests.Size = new System.Drawing.Size(140, 23);
        this.lblRequests.TabIndex = 2;
        this.lblRequests.Text = "併發請求數 (Requests):";
        // 
        // numRequests
        // 
        this.numRequests.Location = new System.Drawing.Point(400, 18);
        this.numRequests.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
        this.numRequests.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numRequests.Name = "numRequests";
        this.numRequests.Size = new System.Drawing.Size(80, 23);
        this.numRequests.TabIndex = 3;
        this.numRequests.Value = new decimal(new int[] { 50, 0, 0, 0 });
        // 
        // gbMode
        // 
        this.gbMode.Controls.Add(this.rbUnsafe);
        this.gbMode.Controls.Add(this.rbSafe);
        this.gbMode.Controls.Add(this.rbRedis);
        this.gbMode.Controls.Add(this.rbPessimistic);
        this.gbMode.Controls.Add(this.rbOptimistic);
        this.gbMode.Location = new System.Drawing.Point(20, 60);
        this.gbMode.Name = "gbMode";
        this.gbMode.Size = new System.Drawing.Size(460, 95);
        this.gbMode.TabIndex = 4;
        this.gbMode.TabStop = false;
        this.gbMode.Text = "模式選擇";
        // 
        // rbUnsafe
        // 
        this.rbUnsafe.Checked = true;
        this.rbUnsafe.Location = new System.Drawing.Point(10, 25);
        this.rbUnsafe.Name = "rbUnsafe";
        this.rbUnsafe.Size = new System.Drawing.Size(140, 24);
        this.rbUnsafe.TabIndex = 0;
        this.rbUnsafe.TabStop = true;
        this.rbUnsafe.Text = "Unsafe (超扣)";
        // 
        // rbSafe
        // 
        this.rbSafe.Location = new System.Drawing.Point(160, 25);
        this.rbSafe.Name = "rbSafe";
        this.rbSafe.Size = new System.Drawing.Size(140, 24);
        this.rbSafe.TabIndex = 1;
        this.rbSafe.Text = "Safe (原子更新)";
        // 
        // rbRedis
        // 
        this.rbRedis.Location = new System.Drawing.Point(310, 25);
        this.rbRedis.Name = "rbRedis";
        this.rbRedis.Size = new System.Drawing.Size(140, 24);
        this.rbRedis.TabIndex = 2;
        this.rbRedis.Text = "Redis (背景非同步)";
        // 
        // rbPessimistic
        // 
        this.rbPessimistic.Location = new System.Drawing.Point(10, 60);
        this.rbPessimistic.Name = "rbPessimistic";
        this.rbPessimistic.Size = new System.Drawing.Size(140, 24);
        this.rbPessimistic.TabIndex = 3;
        this.rbPessimistic.Text = "悲觀鎖 (UPDLOCK)";
        // 
        // rbOptimistic
        // 
        this.rbOptimistic.Location = new System.Drawing.Point(160, 60);
        this.rbOptimistic.Name = "rbOptimistic";
        this.rbOptimistic.Size = new System.Drawing.Size(140, 24);
        this.rbOptimistic.TabIndex = 4;
        this.rbOptimistic.Text = "樂觀鎖 (Version)";
        // 
        // btnReset
        // 
        this.btnReset.Location = new System.Drawing.Point(490, 18);
        this.btnReset.Name = "btnReset";
        this.btnReset.Size = new System.Drawing.Size(80, 25);
        this.btnReset.TabIndex = 5;
        this.btnReset.Text = "重設點數";
        // 
        // btnRun
        // 
        this.btnRun.Location = new System.Drawing.Point(490, 70);
        this.btnRun.Name = "btnRun";
        this.btnRun.Size = new System.Drawing.Size(170, 45);
        this.btnRun.TabIndex = 6;
        this.btnRun.Text = "執行併發測試";
        // 
        // lblSummary
        // 
        this.lblSummary.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        this.lblSummary.Location = new System.Drawing.Point(20, 165);
        this.lblSummary.Name = "lblSummary";
        this.lblSummary.Size = new System.Drawing.Size(640, 45);
        this.lblSummary.TabIndex = 7;
        this.lblSummary.Text = "狀態: 待機中";
        this.lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // txtLog
        // 
        this.txtLog.Location = new System.Drawing.Point(20, 220);
        this.txtLog.Multiline = true;
        this.txtLog.Name = "txtLog";
        this.txtLog.ReadOnly = true;
        this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtLog.Size = new System.Drawing.Size(640, 270);
        this.txtLog.TabIndex = 8;
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(684, 511);
        this.Controls.Add(this.txtLog);
        this.Controls.Add(this.lblSummary);
        this.Controls.Add(this.btnRun);
        this.Controls.Add(this.btnReset);
        this.Controls.Add(this.gbMode);
        this.Controls.Add(this.numRequests);
        this.Controls.Add(this.lblRequests);
        this.Controls.Add(this.numQuota);
        this.Controls.Add(this.lblQuota);
        this.Name = "Form1";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "多執行緒資源競爭 (Race Condition) 演示工具";
        ((System.ComponentModel.ISupportInitialize)(this.numQuota)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numRequests)).EndInit();
        this.gbMode.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label lblQuota;
    private System.Windows.Forms.NumericUpDown numQuota;
    private System.Windows.Forms.Label lblRequests;
    private System.Windows.Forms.NumericUpDown numRequests;
    private System.Windows.Forms.GroupBox gbMode;
    private System.Windows.Forms.RadioButton rbUnsafe;
    private System.Windows.Forms.RadioButton rbSafe;
    private System.Windows.Forms.RadioButton rbRedis;
    private System.Windows.Forms.RadioButton rbPessimistic;
    private System.Windows.Forms.RadioButton rbOptimistic;
    private System.Windows.Forms.Button btnRun;
    private System.Windows.Forms.Button btnReset;
    private System.Windows.Forms.Label lblSummary;
    private System.Windows.Forms.TextBox txtLog;
}
