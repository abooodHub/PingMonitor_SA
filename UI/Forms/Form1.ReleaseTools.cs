using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PingMonitor
{
    public partial class Form1
    {
        private ToolStripDropDownButton toolsMenu;
        private ToolStripMenuItem aboutMenu, reportMenu, alertsMenu;
        private NotifyIcon alertIcon;
        private DateTime lastAlertUtc = DateTime.MinValue;

        private void InitializeReleaseTools()
        {
            toolsMenu = new ToolStripDropDownButton { Name = "toolsMenu", ForeColor = Color.White, DisplayStyle = ToolStripItemDisplayStyle.Text };
            toolsMenu.DropDown.BackColor = Color.FromArgb(35, 35, 38); toolsMenu.DropDown.ForeColor = Color.White;
            aboutMenu = new ToolStripMenuItem(); reportMenu = new ToolStripMenuItem();
            alertsMenu = new ToolStripMenuItem { CheckOnClick = true, Checked = monitoringListsState.AlertsEnabled };
            alertIcon = new NotifyIcon(components) { Icon = Icon ?? SystemIcons.Information, Text = "PingMonitor.SA.v1", Visible = monitoringListsState.AlertsEnabled };
            alertsMenu.CheckedChanged += (s, e) => { monitoringListsState.AlertsEnabled = alertsMenu.Checked; alertIcon.Visible = alertsMenu.Checked; SaveMonitorState(); };
            aboutMenu.Click += (s, e) => { using (var form = new AboutForm(currentLanguage == "ar", monitoringListStore.FilePath)) form.ShowDialog(this); };
            reportMenu.Click += (s, e) => ExportSessionReport();
            toolsMenu.DropDownItems.AddRange(new ToolStripItem[] { reportMenu, alertsMenu, new ToolStripSeparator(), aboutMenu });
            statusStrip.Items.Add(toolsMenu);
            KeyDown += (s, e) => { if (e.KeyCode == Keys.F1) { aboutMenu.PerformClick(); e.SuppressKeyPress = true; } };
            UpdateReleaseToolsLanguage();
        }

        private void UpdateReleaseToolsLanguage()
        {
            if (toolsMenu == null) return;
            bool ar = currentLanguage == "ar";
            toolsMenu.Text = ar ? "أدوات" : "Tools";
            aboutMenu.Text = ar ? "حول البرنامج  (F1)" : "About  (F1)";
            reportMenu.Text = ar ? "تصدير ملخص الجلسة" : "Export session summary";
            alertsMenu.Text = ar ? "تنبيه بعد 3 محاولات بلا استجابة" : "Alert after 3 unanswered probes";
            alertsMenu.ToolTipText = ar ? "اختياري؛ تنبيه واحد لكل فترة عدم استجابة، وبحد أقصى إشعار كل 30 ثانية." :
                "Optional; once per unresponsive episode, at most one notification every 30 seconds.";
        }

        private void ConsiderAlert(string serverId, ServerMetrics metrics, long previousEpisodes)
        {
            if (!monitoringListsState.AlertsEnabled || metrics.UnresponsiveEpisodes == previousEpisodes || isClosing) return;
            if ((DateTime.UtcNow - lastAlertUtc).TotalSeconds < 30) return;
            ServerDefinition server = serverDefinitions.FirstOrDefault(x => x.Id == serverId);
            if (server == null) return;
            lastAlertUtc = DateTime.UtcNow;
            alertIcon.ShowBalloonTip(5000, "PingMonitor.SA.v1", currentLanguage == "ar" ?
                server.Host + "\nلم يستجب لثلاث محاولات ICMP متتالية. هذا لا يثبت انقطاع الخدمة." :
                server.Host + "\nNo reply to three consecutive ICMP probes. This does not prove a service outage.", ToolTipIcon.Warning);
        }

        private void ExportSessionReport()
        {
            bool ar = currentLanguage == "ar";
            if (!serverMetrics.Values.Any(x => x.HasResult)) { MessageBox.Show(this, ar ? "لا توجد قياسات لهذه الجلسة بعد." : "No session measurements yet.", Text); return; }
            string note = ar ? "ملخص كل الخوادم التي قِيست منذ فتح البرنامج، بما فيها المخفية حاليًا.\nيتضمن عدد الاستجابات والمحاولات بلا استجابة وأخطاء القياس وأفضل وأسوأ زمن.\nفترة عدم الاستجابة تبدأ بعد 3 محاولات ICMP متتالية بلا رد وتنتهي عند وصول رد.\nالمجاميع تبقى في الذاكرة حتى الإغلاق؛ السجل التفصيلي محدود بآخر 180 نتيجة لكل خادم." :
                "Summary of all servers measured since startup, including currently hidden servers.\nIncludes replies, unanswered probes, measurement errors and minimum/maximum latency.\nAn unresponsive episode starts after 3 consecutive unanswered ICMP probes and ends on a reply.\nTotals stay in memory until exit; detailed history retains only 180 results per server.";
            using (var review = new TextReviewForm(ar ? "ملخص الجلسة" : "Session summary", note, ar ? "تصدير CSV" : "Export CSV", ar))
            {
                if (review.ShowDialog(this) != DialogResult.OK) return;
                using (var file = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", AddExtension = true, DefaultExt = "csv", FileName = "PingMonitor-session-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv" })
                {
                    if (file.ShowDialog(this) != DialogResult.OK) return;
                    try { File.WriteAllText(file.FileName, SessionReport.BuildCsv(serverDefinitions, serverMetrics), new UTF8Encoding(true)); }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                }
            }
        }
    }
}
