using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PingMonitor
{
    public sealed class MotionCheckBox : CheckBox
    {
        private float checkProgress;
        private float hoverProgress;
        private IDisposable checkAnimation, hoverAnimation;

        public MotionCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            int box = Math.Max(12, Font.Height - 3);
            return new Size(box + text.Width + 13, Math.Max(box, text.Height) + 4);
        }
        protected override void OnCheckedChanged(EventArgs e)
        {
            if (checkAnimation != null) checkAnimation.Dispose();
            float from = checkProgress, target = Checked ? 1 : 0;
            checkAnimation = UiEffects.Animate(this, 200, p => { checkProgress = from + (target - from) * p; Invalidate(); });
            base.OnCheckedChanged(e);
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); HoverTo(1); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); HoverTo(0); }
        private void HoverTo(float target)
        {
            if (hoverAnimation != null) hoverAnimation.Dispose();
            float from = hoverProgress;
            hoverAnimation = UiEffects.Animate(this, 160, p => { hoverProgress = from + (target - from) * p; Invalidate(); });
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (SystemInformation.HighContrast) { base.OnPaint(e); return; }
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(UiEffects.Blend(BackColor, Color.FromArgb(65, 92, 110), hoverProgress * 0.35f));
            int size = Math.Max(12, Font.Height - 3);
            Rectangle box = new Rectangle(2, (Height - size) / 2, size, size);
            using (SolidBrush fill = new SolidBrush(UiEffects.Blend(Color.FromArgb(55, 58, 64), Color.FromArgb(30, 155, 195), checkProgress)))
                g.FillRectangle(fill, box);
            using (Pen border = new Pen(UiEffects.Blend(Color.FromArgb(135, 140, 145), Color.FromArgb(115, 215, 255), Math.Max(checkProgress, hoverProgress))))
                g.DrawRectangle(border, box);
            if (checkProgress > 0)
                using (Pen check = new Pen(Color.FromArgb((int)(255 * checkProgress), Color.White), Math.Max(1.5f, size / 7f)))
                {
                    check.StartCap = check.EndCap = LineCap.Round;
                    g.DrawLines(check, new[] { new PointF(box.Left + size * .2f, box.Top + size * .5f),
                        new PointF(box.Left + size * .43f, box.Top + size * .73f), new PointF(box.Left + size * .82f, box.Top + size * .25f) });
                }
            Rectangle text = new Rectangle(box.Right + 5, 0, Math.Max(1, Width - box.Right - 5), Height);
            TextRenderer.DrawText(g, Text, Font, text, Enabled ? UiEffects.Blend(ForeColor, Color.FromArgb(135, 215, 255), hoverProgress) : SystemColors.GrayText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(ClientRectangle, -1, -1));
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (checkAnimation != null) checkAnimation.Dispose(); if (hoverAnimation != null) hoverAnimation.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
