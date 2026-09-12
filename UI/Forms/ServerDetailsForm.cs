using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PingMonitor
{
    public sealed class ServerDetailsForm : Form
    {
        private readonly bool arabic;
        private readonly ServerDefinition server;
        private readonly Func<ServerMetrics> readMetrics;
        private readonly Label[] statValues = new Label[6];
        private readonly Label status;
        private readonly HistoryChart chart;
        private readonly Timer refreshTimer;
        private readonly Button export;
        private DateTime? displayedTime;

        public ServerDetailsForm(ServerDefinition server, string language, string title, Func<ServerMetrics> readMetrics)
        {
            this.server = server;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            this.readMetrics = readMetrics;
            arabic = language == "ar";
            Text = arabic ? "تفاصيل الخادم" : "Server details";
            ClientSize = new Size(800, 580);
            MinimumSize = new Size(780, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(25, 25, 25);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10);
            Controls.Add(new Label { Text = title, Location = new Point(20, 16), Size = new Size(755, 30), Font = new Font(Font, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right });
            Controls.Add(new TextBox { Text = server.Host, ReadOnly = true, Location = new Point(20, 51), Size = new Size(755, 28),
                BackColor = BackColor, ForeColor = Color.FromArgb(100, 200, 255), BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, AccessibleName = "Host" });
            TableLayoutPanel summary = new TableLayoutPanel { Location = new Point(20, 90), Size = new Size(755, 72), ColumnCount = 6,
                RowCount = 1, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            string[] captions = arabic ? new[] { "البنق الحالي", "المتوسط", "الأدنى", "الأعلى", "الفقد", "التذبذب" } :
                new[] { "Current", "Average", "Minimum", "Maximum", "Loss", "Jitter" };
            for (int i = 0; i < 6; i++)
            {
                summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6));
                Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(35, 35, 38), Margin = new Padding(0, 0, 5, 0) };
                Label caption = new Label { Text = captions[i], Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Silver, Font = new Font(Font.FontFamily, 9) };
                statValues[i] = new Label { Text = "—", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(100, 200, 255), Font = new Font(Font, FontStyle.Bold), RightToLeft = RightToLeft.No };
                card.Controls.Add(statValues[i]); card.Controls.Add(caption); summary.Controls.Add(card, i, 0);
            }
            chart = new HistoryChart(arabic) { Location = new Point(20, 173), Size = new Size(755, 230),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            status = new Label { Location = new Point(20, 415), Size = new Size(755, 60), AutoEllipsis = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, ForeColor = Color.Silver,
                RightToLeft = arabic ? RightToLeft.Yes : RightToLeft.No };
            Label note = new Label { Location = new Point(20, 482), Size = new Size(755, 45),
                Name = "historyNote",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, ForeColor = Color.Silver,
                Text = arabic ? "الإحصاءات: آخر 20 محاولة ICMP. التذبذب بين الاستجابات المتجاورة فقط.\nالرسم والتصدير: آخر 180 نتيجة لهذه الجلسة. الأحمر: لم يستجب، البرتقالي: خطأ قياس." :
                    "Statistics: last 20 ICMP attempts; jitter uses adjacent replies only.\nChart/export: last 180 session results. Red: no reply; orange: measurement error." };
            export = new ModernButton { Text = arabic ? "تصدير CSV" : "Export CSV", Location = new Point(20, 535), Size = new Size(145, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            Button close = new ModernButton { Text = arabic ? "إغلاق" : "Close", Location = new Point(675, 535), Size = new Size(100, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right, DialogResult = DialogResult.Cancel };
            foreach (Button button in new[] { export, close })
            { button.FlatStyle = FlatStyle.Flat; button.BackColor = Color.FromArgb(60, 70, 90); button.ForeColor = Color.White; ((ModernButton)button).GradientColor1 = button.BackColor; ((ModernButton)button).GradientColor2 = Color.FromArgb(75, 85, 105); }
            export.Click += ExportHistory;
            Controls.AddRange(new Control[] { summary, chart, status, note, export, close });
            CancelButton = close;
            bool arranging = false;
            Layout += (s, e) =>
            {
                if (arranging) return;
                arranging = true;
                try
                {
                    float scale = Font.SizeInPoints / 10f * DeviceDpi / 96f;
                    Func<int, int> px = value => (int)Math.Round(value * scale);
                    int margin = px(20), bottom = ClientSize.Height - px(45), width = ClientSize.Width - 2 * margin;
                    export.SetBounds(margin, bottom, px(145), px(32)); close.SetBounds(ClientSize.Width - margin - px(100), bottom, px(100), px(32));
                    note.SetBounds(margin, bottom - px(53), width, px(45));
                    status.SetBounds(margin, note.Top - px(67), width, px(60));
                    chart.SetBounds(margin, summary.Bottom + px(11), width, Math.Max(px(50), status.Top - summary.Bottom - px(23)));
                }
                finally { arranging = false; }
            };
            refreshTimer = new Timer { Interval = 500 };
            refreshTimer.Tick += (s, e) => RefreshMetrics();
            RefreshMetrics();
            refreshTimer.Start();
            UiEffects.PrepareFadeIn(this);
        }

        private void RefreshMetrics()
        {
            ServerMetrics metrics = readMetrics();
            export.Enabled = metrics != null && metrics.HasResult;
            if (metrics == null || !metrics.HasResult)
            {
                status.Text = arabic ? "بانتظار أول قياس…" : "Waiting for the first measurement…";
                return;
            }
            if (displayedTime == metrics.LastMeasuredUtc) return;
            displayedTime = metrics.LastMeasuredUtc;
            string current = metrics.IsOnline ? metrics.PingMilliseconds + " ms" : "—";
            string average = metrics.SuccessfulSampleCount == 0 ? "—" : metrics.AverageMilliseconds.ToString("F1") + " ms";
            string loss = metrics.SampleCount == 0 ? "—" : metrics.LossPercent.ToString("F1") + "%";
            string[] values = { current, average, metrics.SuccessfulSampleCount == 0 ? "—" : metrics.MinimumMilliseconds + " ms",
                metrics.SuccessfulSampleCount == 0 ? "—" : metrics.MaximumMilliseconds + " ms", loss, metrics.JitterMilliseconds.ToString("F1") + " ms" };
            for (int i = 0; i < values.Length; i++) statValues[i].Text = values[i];
            string state = metrics.IsOnline ? (arabic ? "استجاب" : "Replied") : metrics.IsMeasurementError ?
                (arabic ? "خطأ قياس" : "Measurement error") : (arabic ? "لم يستجب" : "No reply");
            status.Text = state + "  ·  " + metrics.LastMeasuredUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") +
                "  ·  " + (arabic ? "عدد عينات الفقد: " : "Loss samples: ") + metrics.SampleCount + "/20" +
                (string.IsNullOrEmpty(metrics.LastError) ? "" : "\n" + metrics.LastError);
            chart.Samples = metrics.GetHistory();
            chart.Invalidate();
        }

        private void ExportHistory(object sender, EventArgs e)
        {
            ServerMetrics metrics = readMetrics();
            if (metrics == null) return;
            using (SaveFileDialog dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", DefaultExt = "csv", AddExtension = true,
                FileName = "PingMonitor-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try { File.WriteAllText(dialog.FileName, BuildCsv(server.Host, metrics.GetHistory()), new UTF8Encoding(true)); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, arabic ? "تعذر التصدير" : "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
        }

        public static string BuildCsv(string host, MetricSample[] samples)
        {
            StringBuilder csv = new StringBuilder("TimestampUtc,Host,Protocol,Status,PingMs,Error\r\n");
            foreach (MetricSample sample in samples)
                csv.AppendLine(string.Join(",", new[] { CsvCell(sample.TimestampUtc.ToString("o", CultureInfo.InvariantCulture)), CsvCell(host), "ICMP",
                    sample.RoundtripTime.HasValue ? "Reply" : sample.IsMeasurementError ? "MeasurementError" : "NoReply",
                    sample.RoundtripTime.HasValue ? sample.RoundtripTime.Value.ToString(CultureInfo.InvariantCulture) : "", CsvCell(sample.Error) }));
            return csv.ToString();
        }

        private static string CsvCell(string value)
        {
            value = value ?? string.Empty;
            string trimmed = value.TrimStart();
            if (trimmed.Length > 0 && "=+-@".IndexOf(trimmed[0]) >= 0) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && refreshTimer != null) refreshTimer.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class HistoryChart : Control
    {
        private readonly bool arabic;
        private MetricSample[] samples = new MetricSample[0];
        private IDisposable revealAnimation, scaleAnimation;
        private float reveal = 1, markerGlow;
        private double verticalMaximum = 10;
        public MetricSample[] Samples
        {
            get { return samples; }
            set
            {
                samples = value ?? new MetricSample[0];
                double target = Math.Max(10, samples.Where(x => x.RoundtripTime.HasValue).Select(x => (double)x.RoundtripTime.Value).DefaultIfEmpty(10).Max() * 1.15);
                double from = verticalMaximum;
                if (scaleAnimation != null) scaleAnimation.Dispose();
                scaleAnimation = UiEffects.Animate(this, 260, p => { verticalMaximum = from + (target - from) * p; markerGlow = 1 - p; Invalidate(); });
            }
        }
        public HistoryChart(bool arabic)
        {
            this.arabic = arabic;
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(30, 30, 33);
            ForeColor = Color.Silver;
            AccessibleName = arabic ? "سجل زمن الاستجابة" : "Response time history";
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) return;
            if (revealAnimation != null) revealAnimation.Dispose();
            revealAnimation = UiEffects.Animate(this, 500, p => { reveal = p; Invalidate(); });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            int labelHeight = Font.Height;
            int left = Math.Max(55, (int)Math.Ceiling(e.Graphics.MeasureString(verticalMaximum.ToString("F0"), Font).Width) + 15);
            int top = labelHeight + 12;
            Rectangle plot = new Rectangle(left, top, Math.Max(1, Width - left - 20), Math.Max(1, Height - top - labelHeight - 12));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            double max = verticalMaximum;
            using (Pen grid = new Pen(Color.FromArgb(55, 55, 60)))
            using (Pen line = new Pen(Color.FromArgb(80, 200, 150), 2))
            using (SolidBrush text = new SolidBrush(ForeColor))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = plot.Top + plot.Height * i / 4;
                    e.Graphics.DrawLine(grid, plot.Left, y, plot.Right, y);
                    e.Graphics.DrawString((max * (4 - i) / 4).ToString("F0"), Font, text, 5, y - labelHeight / 2);
                }
                e.Graphics.DrawString("ms", Font, text, 5, 3);
                if (Samples.Length == 0)
                {
                    TextRenderer.DrawText(e.Graphics, arabic ? "بانتظار النتائج" : "Waiting for results", Font, plot, ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    return;
                }
                DateTime first = Samples[0].TimestampUtc, last = Samples[Samples.Length - 1].TimestampUtc;
                double duration = Math.Max(1, (last - first).TotalMilliseconds);
                PointF? previous = null;
                GraphicsState clipState = e.Graphics.Save();
                e.Graphics.SetClip(new RectangleF(plot.Left - 3, plot.Top - 3, (plot.Width + 6) * reveal, plot.Height + 6));
                foreach (MetricSample sample in Samples)
                {
                    float x = plot.Left + (float)((sample.TimestampUtc - first).TotalMilliseconds / duration * plot.Width);
                    if (!sample.RoundtripTime.HasValue)
                    {
                        previous = null;
                        using (Pen marker = new Pen(sample.IsMeasurementError ? Color.Orange : Color.Crimson, 2))
                            e.Graphics.DrawLine(marker, x, plot.Bottom - 8, x, plot.Bottom);
                        continue;
                    }
                    PointF point = new PointF(x, plot.Bottom - (float)(sample.RoundtripTime.Value / max * plot.Height));
                    if (previous.HasValue) e.Graphics.DrawLine(line, previous.Value, point);
                    e.Graphics.FillEllipse(Brushes.MediumAquamarine, point.X - 2, point.Y - 2, 4, 4);
                    previous = point;
                }
                if (previous.HasValue && markerGlow > 0)
                {
                    float radius = 3 + (1 - markerGlow) * 9;
                    using (Pen halo = new Pen(Color.FromArgb((int)(120 * markerGlow), Color.MediumAquamarine), 2))
                        e.Graphics.DrawEllipse(halo, previous.Value.X - radius, previous.Value.Y - radius, radius * 2, radius * 2);
                }
                e.Graphics.Restore(clipState);
                e.Graphics.DrawString(first.ToLocalTime().ToString("HH:mm:ss"), Font, text, plot.Left, plot.Bottom + 6);
                string end = last.ToLocalTime().ToString("HH:mm:ss");
                e.Graphics.DrawString(end, Font, text, plot.Right - e.Graphics.MeasureString(end, Font).Width, plot.Bottom + 6);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (revealAnimation != null) revealAnimation.Dispose(); if (scaleAnimation != null) scaleAnimation.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
