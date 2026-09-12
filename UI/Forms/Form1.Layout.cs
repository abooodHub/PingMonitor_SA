using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PingMonitor
{
    public partial class Form1
    {
        private bool arrangingLayout;
        private bool responsiveLayoutReady;
        private float columnLayoutScale = 1;
        private void InitializeResponsiveLayout()
        {
            responsiveLayoutReady = true;
            Layout += (s, e) => ArrangeMainLayout(Math.Max(1f, Font.Height / 15f));
            Shown += (s, e) =>
            {
                Rectangle area = Screen.FromControl(this).WorkingArea;
                MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width), Math.Min(MinimumSize.Height, area.Height));
                if (Width > area.Width || Height > area.Height) Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
                ArrangeMainLayout(Math.Max(1f, Font.Height / 15f));
            };
            ArrangeMainLayout(Math.Max(1f, Font.Height / 15f));
        }

        // Explicit scale allows repeatable layout tests without changing the user's display settings.
        private void ArrangeMainLayout(float scale)
        {
            if (!responsiveLayoutReady || arrangingLayout || IsDisposed) return;
            arrangingLayout = true;
            SuspendLayout();
            try
            {
                Func<int, int> px = value => (int)Math.Round(value * scale);
                cmbMonitorList.ItemHeight = Math.Max(px(25), cmbMonitorList.Font.Height + px(6));
                if (Math.Abs(columnLayoutScale - scale) > .01f)
                {
                    int[] minimumWidths = { 50, 140, 170, 185, 100, 115, 120 };
                    for (int i = 0; i < Math.Min(minimumWidths.Length, serverListView.Columns.Count); i++)
                    {
                        int logicalWidth = monitoringListsState.ColumnWidths != null && monitoringListsState.ColumnWidths.Count == 7 ?
                            Math.Max(40, Math.Min(800, monitoringListsState.ColumnWidths[i])) : minimumWidths[i];
                        serverListView.Columns[i].Width = px(logicalWidth);
                    }
                    columnLayoutScale = scale;
                }
                int margin = px(15), gap = px(10), rowHeight = px(42), x = margin, y = margin;
                Control[] header = { btnToggleIP, btnToggleTimer, btnChangeInterval, cmbMonitorList, btnManageMonitorLists, btnToggleLang };
                int[] widths = { 140, 120, 140, 265, 155, 90 };
                for (int i = 0; i < header.Length; i++)
                {
                    int width = Math.Min(px(widths[i]), ClientSize.Width - 2 * margin);
                    if (x > margin && x + width > ClientSize.Width - margin) { x = margin; y += rowHeight + gap; }
                    header[i].SetBounds(x, y, width, rowHeight); x += width + gap;
                }
                y += rowHeight + px(8);
                int searchWidth = Math.Min(px(265), Math.Max(px(120), ClientSize.Width / 3));
                txtQuickSearch.SetBounds(ClientSize.Width - margin - searchWidth, y, searchWidth, px(25));
                searchLabel.SetBounds(txtQuickSearch.Left - px(68), y, px(65), px(25));
                lblWanIP.AutoSize = false;
                lblWanIP.AutoEllipsis = true;
                lblWanIP.SetBounds(margin, y, Math.Max(px(100), searchLabel.Left - margin - gap), px(28));
                y += px(30);
                pnlProviders.WrapContents = true;
                pnlProviders.AutoScroll = false;
                pnlProviders.SetBounds(margin, y, ClientSize.Width - 2 * margin, px(45));
                int providerX = pnlProviders.Padding.Left, providerHeight = 0, providerRowsHeight = 0;
                foreach (Control control in pnlProviders.Controls)
                {
                    Size preferred = control.GetPreferredSize(Size.Empty);
                    int width = preferred.Width + control.Margin.Horizontal;
                    int height = preferred.Height + control.Margin.Vertical;
                    if (providerX > pnlProviders.Padding.Left && providerX + width > pnlProviders.ClientSize.Width - pnlProviders.Padding.Right)
                    { providerRowsHeight += providerHeight; providerHeight = 0; providerX = pnlProviders.Padding.Left; }
                    providerX += width; providerHeight = Math.Max(providerHeight, height);
                }
                pnlProviders.Height = Math.Max(px(45), providerRowsHeight + providerHeight + pnlProviders.Padding.Vertical + 4);
                int tableY = pnlProviders.Bottom + px(10);
                scanProgress.SetBounds(margin, tableY - px(6), ClientSize.Width - margin * 2, Math.Max(3, px(3)));
                serverListView.SetBounds(margin, tableY, ClientSize.Width - 2 * margin, Math.Max(px(50), statusStrip.Top - tableY - gap));
            }
            finally { ResumeLayout(false); arrangingLayout = false; }
        }
    }
}
