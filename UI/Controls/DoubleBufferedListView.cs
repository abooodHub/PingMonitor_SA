using System;
using System.Drawing;
using System.Windows.Forms;

namespace PingMonitor
{
    public class DoubleBufferedListView : ListView
    {
        private ListViewItem hoveredItem, previousHoveredItem;
        private float hoverProgress, entranceProgress = 1, selectionProgress = 1;
        private IDisposable hoverAnimation, entranceAnimation, selectionAnimation;
        public DoubleBufferedListView()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
        }

        public void BeginContentTransition()
        {
            if (entranceAnimation != null) entranceAnimation.Dispose();
            if (hoverAnimation != null) hoverAnimation.Dispose();
            hoveredItem = previousHoveredItem = null;
            entranceAnimation = UiEffects.Animate(this, 280, p => { entranceProgress = p; Invalidate(); });
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            SetHoveredItem(GetItemAt(e.X, e.Y));
        }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); SetHoveredItem(null); }
        private void SetHoveredItem(ListViewItem item)
        {
            if (ReferenceEquals(item, hoveredItem)) return;
            if (hoverAnimation != null) hoverAnimation.Dispose();
            previousHoveredItem = hoveredItem;
            hoveredItem = item;
            hoverAnimation = UiEffects.Animate(this, 150, p =>
            {
                hoverProgress = p;
                InvalidateItem(previousHoveredItem); InvalidateItem(hoveredItem);
            });
        }
        protected override void OnItemSelectionChanged(ListViewItemSelectionChangedEventArgs e)
        {
            base.OnItemSelectionChanged(e);
            if (selectionAnimation != null) selectionAnimation.Dispose();
            selectionAnimation = UiEffects.Animate(this, 170, p => { selectionProgress = p; Invalidate(); });
        }
        public Color GetRowBackground(ListViewItem item, Color background)
        {
            if (SystemInformation.HighContrast) return item.Selected ? SystemColors.Highlight : SystemColors.Window;
            background = UiEffects.Blend(background, Color.FromArgb(50, 85, 108), (1 - entranceProgress) * 0.2f);
            if (item.Selected) return UiEffects.Blend(background, Color.FromArgb(36, 77, 110), 0.7f + selectionProgress * 0.3f);
            float hover = ReferenceEquals(item, hoveredItem) ? hoverProgress : ReferenceEquals(item, previousHoveredItem) ? 1 - hoverProgress : 0;
            return UiEffects.Blend(background, Color.FromArgb(47, 66, 80), hover * 0.75f);
        }
        private void InvalidateItem(ListViewItem item)
        {
            if (item != null && item.ListView == this && item.Index >= 0) Invalidate(item.Bounds);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (hoverAnimation != null) hoverAnimation.Dispose();
                if (entranceAnimation != null) entranceAnimation.Dispose();
                if (selectionAnimation != null) selectionAnimation.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
