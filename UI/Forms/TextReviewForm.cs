using System;
using System.Drawing;
using System.Windows.Forms;

namespace PingMonitor
{
    public sealed class TextReviewForm : Form
    {
        public TextReviewForm(string title, string text, string acceptText, bool arabic)
        {
            Text = title; Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(25, 25, 25); ForeColor = Color.White;
            AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(620, 390); MinimumSize = new Size(440, 300); StartPosition = FormStartPosition.CenterParent;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 2, ColumnCount = 1 };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            var content = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(35, 35, 38), ForeColor = Color.White, Text = text, BorderStyle = BorderStyle.FixedSingle,
                RightToLeft = arabic ? RightToLeft.Yes : RightToLeft.No };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var close = new ModernButton { Text = arabic ? "إلغاء" : "Cancel", Size = new Size(110, 36), DialogResult = DialogResult.Cancel };
            var accept = new ModernButton { Text = acceptText, Size = new Size(170, 36), DialogResult = DialogResult.OK };
            buttons.Controls.Add(close); buttons.Controls.Add(accept); layout.Controls.Add(content, 0, 0); layout.Controls.Add(buttons, 0, 1);
            Controls.Add(layout); AcceptButton = accept; CancelButton = close; UiEffects.PrepareFadeIn(this);
        }
    }
}
