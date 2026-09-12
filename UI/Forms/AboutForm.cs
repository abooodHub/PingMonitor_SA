using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace PingMonitor
{
    public sealed class AboutForm : Form
    {
        public AboutForm(bool arabic, string dataPath)
        {
            Text = arabic ? "حول البرنامج" : "About PingMonitor"; Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
            BackColor = Color.FromArgb(25, 25, 25); ForeColor = Color.White;
            ClientSize = new Size(560, 290); MinimumSize = new Size(460, 320); StartPosition = FormStartPosition.CenterParent;
            string version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 4 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            panel.Controls.Add(new Label { Text = "PingMonitor.SA.v1  •  " + version, Font = new Font(Font, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 0);
            panel.Controls.Add(new Label { Text = arabic ? "مراقبة ICMP للبنق والتذبذب وفقد الحزم.\nالقوائم محفوظة في بيانات المستخدم. سجل القياسات في ذاكرة الجلسة." :
                "ICMP latency, jitter and packet loss monitor.\nLists stay in user data. Measurement history stays in session memory.", Dock = DockStyle.Fill }, 0, 1);
            panel.Controls.Add(new TextBox { ReadOnly = true, Text = Path.GetDirectoryName(dataPath), Dock = DockStyle.Top,
                BackColor = Color.FromArgb(35, 35, 38), ForeColor = Color.White }, 0, 2);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            var folder = new ModernButton { Text = arabic ? "مجلد البيانات" : "Data folder", Size = new Size(150, 36) };
            var project = new ModernButton { Text = "GitHub", Size = new Size(110, 36) };
            var close = new ModernButton { Text = arabic ? "إغلاق" : "Close", Size = new Size(100, 36), DialogResult = DialogResult.Cancel };
            folder.Click += (s, e) => Open(() => { string directory = Path.GetDirectoryName(dataPath); Directory.CreateDirectory(directory); Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true }); });
            project.Click += (s, e) => Open(() => Process.Start(new ProcessStartInfo("https://github.com/abooodHub/PingMonitor_SA") { UseShellExecute = true }));
            buttons.Controls.AddRange(new Control[] { folder, project, close }); panel.Controls.Add(buttons, 0, 3); Controls.Add(panel);
            CancelButton = close; UiEffects.PrepareFadeIn(this);
        }
        private void Open(Action action) { try { action(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
    }
}
