using System.Windows.Forms;

namespace PingMonitor
{
    public class DoubleBufferedListView : ListView
    {
        public DoubleBufferedListView()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
        }
    }
}
