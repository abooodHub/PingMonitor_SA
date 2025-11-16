namespace PingMonitor
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblWanIP;
        private ModernButton btnToggleIP;
        private System.Windows.Forms.Timer pingTimer;
        private DoubleBufferedListView serverListView;
        private ModernButton btnToggleTimer;
        private ModernButton btnChangeInterval;
        private ModernButton btnToggleLang;
        private System.Windows.Forms.FlowLayoutPanel pnlProviders;
        private System.Windows.Forms.ImageList imageListFlags;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblServerCount;
        private System.Windows.Forms.ToolStripStatusLabel lblActiveServers;
        private System.Windows.Forms.ToolStripStatusLabel lblOfflineServers;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.lblWanIP = new System.Windows.Forms.Label();
            this.btnToggleIP = new ModernButton();
            this.pingTimer = new System.Windows.Forms.Timer(this.components);
            this.serverListView = new DoubleBufferedListView();
            this.imageListFlags = new System.Windows.Forms.ImageList(this.components);
            this.btnToggleTimer = new ModernButton();
            this.btnChangeInterval = new ModernButton();
            this.btnToggleLang = new ModernButton();
            this.pnlProviders = new System.Windows.Forms.FlowLayoutPanel();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblServerCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblActiveServers = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblOfflineServers = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblWanIP
            // 
            this.lblWanIP.AutoSize = true;
            this.lblWanIP.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.lblWanIP.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWanIP.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.lblWanIP.Location = new System.Drawing.Point(15, 58);
            this.lblWanIP.Name = "lblWanIP";
            this.lblWanIP.Padding = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.lblWanIP.Size = new System.Drawing.Size(207, 26);
            this.lblWanIP.TabIndex = 7;
            this.lblWanIP.Text = "🌐 WAN IP: جاري التحميل...";
            this.lblWanIP.Visible = false;
            // 
            // btnToggleIP
            // 
            this.btnToggleIP.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnToggleIP.Location = new System.Drawing.Point(15, 15);
            this.btnToggleIP.Name = "btnToggleIP";
            this.btnToggleIP.Size = new System.Drawing.Size(140, 42);
            this.btnToggleIP.TabIndex = 1;
            this.btnToggleIP.Text = "👁️ إظهار IP";
            this.btnToggleIP.Click += new System.EventHandler(this.btnToggleIP_Click);
            // 
            // pingTimer
            // 
            this.pingTimer.Interval = 5000;
            // 
            // serverListView
            // 
            this.serverListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.serverListView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.serverListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.serverListView.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.serverListView.ForeColor = System.Drawing.Color.White;
            this.serverListView.FullRowSelect = true;
            this.serverListView.GridLines = true;
            this.serverListView.HideSelection = false;
            this.serverListView.Location = new System.Drawing.Point(15, 150);
            this.serverListView.Name = "serverListView";
            this.serverListView.Size = new System.Drawing.Size(971, 359);
            this.serverListView.SmallImageList = this.imageListFlags;
            this.serverListView.TabIndex = 2;
            this.serverListView.UseCompatibleStateImageBehavior = false;
            this.serverListView.View = System.Windows.Forms.View.Details;
            // 
            // imageListFlags
            // 
            this.imageListFlags.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageListFlags.ImageSize = new System.Drawing.Size(24, 24);
            this.imageListFlags.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // btnToggleTimer
            // 
            this.btnToggleTimer.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnToggleTimer.Location = new System.Drawing.Point(165, 15);
            this.btnToggleTimer.Name = "btnToggleTimer";
            this.btnToggleTimer.Size = new System.Drawing.Size(120, 42);
            this.btnToggleTimer.TabIndex = 3;
            this.btnToggleTimer.Text = "⏸ إيقاف";
            this.btnToggleTimer.Click += new System.EventHandler(this.btnToggleTimer_Click);
            // 
            // btnChangeInterval
            // 
            this.btnChangeInterval.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnChangeInterval.Location = new System.Drawing.Point(295, 15);
            this.btnChangeInterval.Name = "btnChangeInterval";
            this.btnChangeInterval.Size = new System.Drawing.Size(140, 42);
            this.btnChangeInterval.TabIndex = 4;
            this.btnChangeInterval.Text = "⏱ تغيير الفترة";
            this.btnChangeInterval.Click += new System.EventHandler(this.btnChangeInterval_Click);
            // 
            // btnToggleLang
            // 
            this.btnToggleLang.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnToggleLang.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnToggleLang.Location = new System.Drawing.Point(896, 15);
            this.btnToggleLang.Name = "btnToggleLang";
            this.btnToggleLang.Size = new System.Drawing.Size(90, 42);
            this.btnToggleLang.TabIndex = 5;
            this.btnToggleLang.Text = "🌐 en";
            this.btnToggleLang.Click += new System.EventHandler(this.btnToggleLang_Click);
            // 
            // pnlProviders
            // 
            this.pnlProviders.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlProviders.AutoScroll = true;
            this.pnlProviders.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(43)))));
            this.pnlProviders.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlProviders.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pnlProviders.Location = new System.Drawing.Point(15, 95);
            this.pnlProviders.Name = "pnlProviders";
            this.pnlProviders.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.pnlProviders.Size = new System.Drawing.Size(971, 45);
            this.pnlProviders.TabIndex = 6;
            this.pnlProviders.WrapContents = false;
            // 
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(35)))), ((int)(((byte)(38)))));
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblServerCount,
            this.lblActiveServers,
            this.lblOfflineServers});
            this.statusStrip.Location = new System.Drawing.Point(0, 519);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1000, 36);
            this.statusStrip.TabIndex = 8;
            // 
            // lblServerCount
            // 
            this.lblServerCount.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblServerCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.lblServerCount.Name = "lblServerCount";
            this.lblServerCount.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.lblServerCount.Size = new System.Drawing.Size(150, 31);
            this.lblServerCount.Text = "📊 الخوادم: 0";
            // 
            // lblActiveServers
            // 
            this.lblActiveServers.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblActiveServers.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(205)))), ((int)(((byte)(50)))));
            this.lblActiveServers.Name = "lblActiveServers";
            this.lblActiveServers.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.lblActiveServers.Size = new System.Drawing.Size(150, 31);
            this.lblActiveServers.Text = "✅ متصل: 0";
            // 
            // lblOfflineServers
            // 
            this.lblOfflineServers.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblOfflineServers.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(20)))), ((int)(((byte)(60)))));
            this.lblOfflineServers.Name = "lblOfflineServers";
            this.lblOfflineServers.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.lblOfflineServers.Size = new System.Drawing.Size(150, 31);
            this.lblOfflineServers.Text = "❌ غير متصل: 0";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.ClientSize = new System.Drawing.Size(1000, 555);
            this.Controls.Add(this.lblWanIP);
            this.Controls.Add(this.pnlProviders);
            this.Controls.Add(this.btnToggleLang);
            this.Controls.Add(this.btnChangeInterval);
            this.Controls.Add(this.btnToggleTimer);
            this.Controls.Add(this.serverListView);
            this.Controls.Add(this.btnToggleIP);
            this.Controls.Add(this.statusStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new System.Drawing.Size(1000, 555);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PingMonitor SA.v1 — أداة مراقبة البنق";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
