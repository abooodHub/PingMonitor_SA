using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PingMonitor
{
    public partial class Form1
    {
        private TextBox txtQuickSearch;
        private Label searchLabel;
        private ToolStripStatusLabel lblScanStatus;
        private ToolTip monitoringTips;
        private System.Windows.Forms.Timer resultDisplayTimer;
        private System.Windows.Forms.Timer settingsSaveTimer;
        private bool resultsPending;
        private bool immediateScanRequested;
        private bool saveWarningShown;
        private int scanCompleted;
        private int scanTotal;
        private int sortColumn = -1;
        private bool sortDescending;
        private DateTime? lastResultUtc;
        private ScanProgressStrip scanProgress;
        private ToolStripMenuItem reduceMotionMenu;
        private ToolStripMenuItem detailsMenu, copyHostMenu;
        private readonly Dictionary<string, DateTime?> displayedMeasurements = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);

        private void InitializeMonitoringUi()
        {
            settingsSaveTimer = new System.Windows.Forms.Timer(components) { Interval = 600 };
            settingsSaveTimer.Tick += (s, e) => { settingsSaveTimer.Stop(); SaveSettings(); };
            searchLabel = new Label { Location = new Point(382, 66), Size = new Size(65, 24), TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right, ForeColor = Color.Silver };
            txtQuickSearch = new TextBox { Name = "txtQuickSearch", Location = new Point(450, 65), Size = new Size(265, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = Color.FromArgb(40, 40, 43), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, TabIndex = 8 };
            Controls.AddRange(new Control[] { searchLabel, txtQuickSearch });
            UiEffects.AttachFocusBorder(txtQuickSearch);
            scanProgress = new ScanProgressStrip { Name = "scanProgress", Location = new Point(serverListView.Left, serverListView.Top - 6),
                Size = new Size(serverListView.Width, 3), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            Controls.Add(scanProgress);
            txtQuickSearch.TextChanged += (s, e) => PopulateListView();
            lblScanStatus = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.Silver, Name = "lblScanStatus" };
            statusStrip.Items.Add(lblScanStatus);
            ContextMenuStrip motionMenu = new ContextMenuStrip(components) { BackColor = Color.FromArgb(35, 35, 38), ForeColor = Color.White };
            reduceMotionMenu = new ToolStripMenuItem { CheckOnClick = true, Checked = monitoringListsState.ReduceMotion };
            reduceMotionMenu.CheckedChanged += (s, e) =>
            {
                UiEffects.ReduceMotion = reduceMotionMenu.Checked;
                monitoringListsState.ReduceMotion = reduceMotionMenu.Checked;
                SaveMonitorState();
            };
            motionMenu.Items.Add(reduceMotionMenu);
            statusStrip.ContextMenuStrip = motionMenu;
            monitoringTips = new ToolTip(components);
            serverListView.ShowItemToolTips = true;
            ContextMenuStrip serverMenu = new ContextMenuStrip(components) { BackColor = Color.FromArgb(35, 35, 38), ForeColor = Color.White };
            detailsMenu = new ToolStripMenuItem(); copyHostMenu = new ToolStripMenuItem();
            detailsMenu.Click += (s, e) => ShowServerDetails();
            copyHostMenu.Click += (s, e) => CopySelectedHost();
            serverMenu.Items.AddRange(new ToolStripItem[] { detailsMenu, copyHostMenu });
            serverMenu.Opening += (s, e) => e.Cancel = serverListView.SelectedItems.Count == 0;
            serverListView.ContextMenuStrip = serverMenu;
            serverListView.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                ListViewItem item = serverListView.GetItemAt(e.X, e.Y);
                if (item == null) return;
                serverListView.SelectedItems.Clear(); item.Selected = true; item.Focused = true;
            };
            serverListView.KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.C) { CopySelectedHost(); e.SuppressKeyPress = true; } };
            serverListView.DoubleClick += (s, e) => ShowServerDetails();
            serverListView.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { ShowServerDetails(); e.Handled = true; } };
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.F) { txtQuickSearch.Focus(); txtQuickSearch.SelectAll(); e.SuppressKeyPress = true; }
                if (e.KeyCode == Keys.Escape && txtQuickSearch.Focused) { txtQuickSearch.Clear(); e.SuppressKeyPress = true; }
                if (e.KeyCode == Keys.F5 && updating) { RequestImmediateScan(); e.SuppressKeyPress = true; }
                if (e.Control && e.Shift && e.KeyCode == Keys.M) { reduceMotionMenu.Checked = !reduceMotionMenu.Checked; e.SuppressKeyPress = true; }
            };
            resultDisplayTimer = new System.Windows.Forms.Timer(components) { Interval = 120 };
            resultDisplayTimer.Tick += (s, e) =>
            {
                if (resultsPending) RefreshMeasuredRows();
                if (!scanInProgress) resultDisplayTimer.Stop();
            };
            Shown += (s, e) => RequestImmediateScan();
            InitializeReleaseTools();
        }

        private IEnumerable<ServerDefinition> GetMonitoredServers()
        {
            HashSet<string> providers = new HashSet<string>(pnlProviders.Controls.OfType<CheckBox>()
                .Where(x => x.Checked).Select(x => Convert.ToString(x.Tag)), StringComparer.OrdinalIgnoreCase);
            IEnumerable<ServerDefinition> selected = serverDefinitions;
            if (monitoringListsState != null && !string.Equals(monitoringListsState.ActiveListId, MonitoringListStore.AllServersListId, StringComparison.OrdinalIgnoreCase))
            {
                MonitoringListDefinition list = monitoringListsState.Lists.FirstOrDefault(x => x.Id == monitoringListsState.ActiveListId);
                HashSet<string> ids = new HashSet<string>(list == null ? new List<string>() : list.ServerIds, StringComparer.OrdinalIgnoreCase);
                selected = selected.Where(x => ids.Contains(x.Id));
            }
            return selected.Where(x => providers.Count == 0 || providers.Contains(x.ProviderId) ||
                (providers.Contains("middle-east") && x.RegionId == "gulf"));
        }

        private bool MatchesSearch(ServerDefinition server)
        {
            string query = txtQuickSearch.Text.Trim();
            return query.Length == 0 || new[] { GetCountryDisplay(server), GetLocationDisplay(server), GetProviderDisplay(server),
                server.Host, server.LocationAr, server.ProviderName, server.CountryCode }
                .Any(x => x.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        private void RequestImmediateScan()
        {
            if (isClosing || !updating) return;
            if (scanInProgress) { immediateScanRequested = true; return; }
            pingTimer.Stop();
            pingTimer.Interval = 1;
            pingTimer.Start();
        }

        private void ScheduleSettingsSave()
        {
            settingsSaveTimer.Stop();
            settingsSaveTimer.Start();
        }

        private async Task MeasureAndDisplayAsync(PingTarget target, SemaphoreSlim gate, CancellationToken token, int generation)
        {
            PingMeasurement result = await MeasureServerAsync(target, gate, token);
            if (isClosing || token.IsCancellationRequested || generation != scanGeneration) return;
            ServerMetrics metrics;
            if (!serverMetrics.TryGetValue(result.ServerId, out metrics))
                serverMetrics[result.ServerId] = metrics = new ServerMetrics();
            long previousEpisodes = metrics.UnresponsiveEpisodes;
            if (result.IsSuccess) metrics.RecordSuccess(result.RoundtripTime);
            else if (result.IsMeasurementError) metrics.RecordError(result.Error);
            else metrics.RecordFailure(result.Error);
            ConsiderAlert(result.ServerId, metrics, previousEpisodes);
            lastResultUtc = metrics.LastMeasuredUtc;
            scanCompleted++;
            resultsPending = true;
            if (!resultDisplayTimer.Enabled) resultDisplayTimer.Start();
        }

        private void RefreshMeasuredRows()
        {
            resultsPending = false;
            if (isClosing || IsDisposed) return;
            foreach (ListViewItem item in serverListView.Items)
            {
                ServerMetrics metrics;
                string id = ((ServerRowTag)item.Tag).ServerId;
                DateTime? previous;
                if (serverMetrics.TryGetValue(id, out metrics) && (!displayedMeasurements.TryGetValue(id, out previous) || previous != metrics.LastMeasuredUtc))
                {
                    ApplyMetricsToItem(item, metrics, true);
                    displayedMeasurements[id] = metrics.LastMeasuredUtc;
                }
            }
            UpdateStatusBar();
            UpdateScanStatus();
        }

        private string BuildMetricHint(ServerMetrics metrics)
        {
            bool ar = currentLanguage == "ar";
            string status = metrics.IsOnline ? (ar ? "استجاب لـ ICMP" : "ICMP reply received") :
                metrics.IsMeasurementError ? (ar ? "خطأ قياس" : "Measurement error") : (ar ? "لم يستجب لـ ICMP" : "No ICMP reply");
            return status + "\n" + (ar ? "آخر قياس: " : "Last measurement: ") +
                (metrics.LastMeasuredUtc.HasValue ? metrics.LastMeasuredUtc.Value.ToLocalTime().ToString("HH:mm:ss") : "—") + "\n" +
                (ar ? "عينات الفقد: " : "Loss samples: ") + metrics.SampleCount + "/" + ServerMetrics.WindowSize +
                (string.IsNullOrEmpty(metrics.LastError) ? "" : "\n" + metrics.LastError);
        }

        private void UpdateScanStatus()
        {
            if (lblScanStatus == null || isClosing || IsDisposed) return;
            scanProgress.SetProgress(updating && scanInProgress, scanCompleted, scanTotal);
            bool ar = currentLanguage == "ar";
            string time = lastResultUtc.HasValue ? lastResultUtc.Value.ToLocalTime().ToString("HH:mm:ss") : "—";
            lblScanStatus.Text = (!updating ? (ar ? "متوقف" : "Paused") : scanInProgress ?
                (ar ? "قياس " : "Scanning ") + scanCompleted + "/" + scanTotal : (ar ? "جاهز" : "Ready")) +
                "  |  " + (ar ? "آخر تحديث " : "Updated ") + time;
            lblScanStatus.ToolTipText = ar ? "الجدول يعرض نتائج ICMP. البحث يخفي الصفوف فقط ولا يوقف مراقبتها." :
                "ICMP results. Search only hides rows; monitoring continues for the selected list and providers.";
        }

        private void UpdateLanguageLabels()
        {
            UpdateReleaseToolsLanguage();
            bool ar = currentLanguage == "ar";
            btnToggleTimer.Text = updating ? (ar ? "⏸ إيقاف" : "⏸ Pause") : (ar ? "▶ تشغيل" : "▶ Start");
            btnChangeInterval.Text = ar ? "⏱ تغيير الفترة" : "⏱ Interval";
            btnManageMonitorLists.Text = ar ? "⚙ إدارة القوائم" : "⚙ Manage Lists";
            btnToggleLang.Text = ar ? "🌐 en" : "🌐 ar";
            UpdateToggleIPButtonText();
            Text = ar ? "PingMonitor.SA.v1 — أداة مراقبة البنق" : "PingMonitor.SA.v1 — Network Monitor";
            searchLabel.Text = ar ? "بحث" : "Search";
            reduceMotionMenu.Text = ar ? "تقليل الحركة  (Ctrl+Shift+M)" : "Reduce motion  (Ctrl+Shift+M)";
            detailsMenu.Text = ar ? "تفاصيل الخادم" : "Server details";
            copyHostMenu.Text = ar ? "نسخ عنوان الخادم  (Ctrl+C)" : "Copy host  (Ctrl+C)";
            reduceMotionMenu.ToolTipText = ar ? "حفظ التفضيل لهذا المستخدم" : "Save this preference for this user";
            monitoringTips.SetToolTip(txtQuickSearch, ar ? "ابحث بالمدينة أو المزود أو عنوان الخادم — Ctrl+F" : "Search city, provider or host — Ctrl+F");
            monitoringTips.SetToolTip(btnChangeInterval, ar ? "الفاصل المستهدف بين بدايات القياس. الدورات لا تتداخل." : "Target interval between scan starts. Scans never overlap.");
            monitoringTips.SetToolTip(scanProgress, ar ? "تقدم دورة القياس. لتقليل الحركة: Ctrl+Shift+M أو اضغط يمينًا على شريط الحالة." :
                "Scan progress. Reduce motion: Ctrl+Shift+M or right-click the status bar.");
            UpdateScanStatus();
        }

        private void ApplySort()
        {
            if (sortColumn < 0 || sortColumn >= serverListView.Columns.Count) return;
            for (int i = 0; i < serverListView.Columns.Count; i++)
            {
                string title = serverListView.Columns[i].Text.TrimEnd(' ', '▲', '▼');
                serverListView.Columns[i].Text = title + (i == sortColumn ? (sortDescending ? " ▼" : " ▲") : "");
            }
            serverListView.ListViewItemSorter = new ServerRowComparer(sortColumn, sortDescending, serverMetrics);
            serverListView.Sort();
        }

        private void CaptureColumnWidths()
        {
            if (serverListView.Columns.Count == 7)
                monitoringListsState.ColumnWidths = serverListView.Columns.Cast<ColumnHeader>().Select(x => (int)Math.Round(x.Width / Math.Max(1f, Font.Height / 15f))).ToList();
        }

        private void RestoreColumnWidths()
        {
            List<int> widths = monitoringListsState.ColumnWidths;
            if (widths == null || widths.Count != serverListView.Columns.Count) return;
            float scale = Math.Max(1f, Font.Height / 15f);
            for (int i = 0; i < widths.Count; i++) serverListView.Columns[i].Width = (int)Math.Round(Math.Max(40, Math.Min(800, widths[i])) * scale);
            columnLayoutScale = scale;
        }

        private void RestoreViewPreferences()
        {
            sortColumn = Math.Max(-1, Math.Min(6, monitoringListsState.SortColumn));
            sortDescending = monitoringListsState.SortDescending;
            Rectangle area = Screen.FromControl(this).WorkingArea;
            if (monitoringListsState.WindowWidth >= MinimumSize.Width && monitoringListsState.WindowHeight >= MinimumSize.Height)
                Size = new Size(Math.Min(area.Width, monitoringListsState.WindowWidth), Math.Min(area.Height, monitoringListsState.WindowHeight));
            if (monitoringListsState.Maximized) WindowState = FormWindowState.Maximized;
            RestoreColumnWidths();
        }

        private bool SaveMonitorState()
        {
            if (monitoringListsState == null) return true;
            monitoringListsState.Language = currentLanguage;
            monitoringListsState.SortColumn = sortColumn;
            monitoringListsState.SortDescending = sortDescending;
            Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            monitoringListsState.WindowWidth = bounds.Width;
            monitoringListsState.WindowHeight = bounds.Height;
            monitoringListsState.Maximized = WindowState == FormWindowState.Maximized;
            CaptureColumnWidths();
            try { monitoringListStore.Save(monitoringListsState); saveWarningShown = false; return true; }
            catch { ShowSaveError(); return false; }
        }

        private void ShowSaveError()
        {
            if (saveWarningShown) return;
            saveWarningShown = true;
            MessageBox.Show(this, currentLanguage == "ar" ?
                "تعذر حفظ التغييرات في مجلد بيانات المستخدم. التغييرات متاحة لهذه الجلسة فقط. تحقق من صلاحية الكتابة والمساحة، ثم أعد الحفظ من إدارة القوائم." :
                "Changes could not be saved to your user data folder. They remain available in this session only. Check write access and disk space, then save again from Manage Lists.",
                currentLanguage == "ar" ? "تعذر الحفظ" : "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowServerDetails()
        {
            if (serverListView.SelectedItems.Count == 0) return;
            string id = ((ServerRowTag)serverListView.SelectedItems[0].Tag).ServerId;
            ServerDefinition server = serverDefinitions.First(x => x.Id == id);
            using (ServerDetailsForm dialog = new ServerDetailsForm(server, currentLanguage, GetLocationDisplay(server) + " · " + GetProviderDisplay(server),
                () => { ServerMetrics metrics; return serverMetrics.TryGetValue(id, out metrics) ? metrics : null; }))
                dialog.ShowDialog(this);
        }

        private void CopySelectedHost()
        {
            if (serverListView.SelectedItems.Count == 0) return;
            try { Clipboard.SetText(((ServerRowTag)serverListView.SelectedItems[0].Tag).Host); }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                MessageBox.Show(this, currentLanguage == "ar" ? "الحافظة مشغولة. حاول النسخ مرة أخرى." : "Clipboard is busy. Please try again.", Text);
            }
        }
    }
}
