using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PingMonitor
{
    public sealed class ScanProgressStrip : Control
    {
        private readonly Timer animationTimer = new Timer { Interval = 25 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private long lastFrame, finishedAt;
        private float target, displayed, finishGlow;
        private bool scanning;
        private Form ownerForm;

        public ScanProgressStrip()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            TabStop = false;
            BackColor = Color.FromArgb(42, 47, 53);
            AccessibleName = "Scan progress";
            animationTimer.Tick += Tick;
            UiEffects.MotionPreferenceChanged += PreferenceChanged;
        }
        public void SetProgress(bool active, int completed, int total)
        {
            bool wasScanning = scanning;
            scanning = active && total > 0;
            target = scanning ? Math.Max(0, Math.Min(1, completed / (float)total)) : 0;
            if (scanning && !wasScanning) displayed = 0;
            if (!scanning && wasScanning && total > 0 && completed == total)
            { displayed = 1; finishGlow = 1; finishedAt = clock.ElapsedMilliseconds; }
            else if (!scanning && wasScanning) { displayed = 0; finishGlow = 0; }
            AccessibleDescription = total == 0 ? "No targets" : completed + " / " + total;
            UpdateTimer();
            Invalidate();
        }
        private void UpdateTimer()
        {
            if (!UiEffects.CanAnimate(this))
            {
                animationTimer.Stop(); displayed = target; finishGlow = 0; Invalidate(); return;
            }
            if (scanning || finishGlow > 0)
            { if (!animationTimer.Enabled) { lastFrame = clock.ElapsedMilliseconds; animationTimer.Start(); } }
            else animationTimer.Stop();
        }
        private void Tick(object sender, EventArgs e)
        {
            if (!UiEffects.CanAnimate(this)) { UpdateTimer(); return; }
            long now = clock.ElapsedMilliseconds;
            float delta = Math.Max(0, now - lastFrame);
            lastFrame = now;
            if (scanning) displayed += (target - displayed) * (1f - (float)Math.Exp(-delta / 90f));
            else finishGlow = Math.Max(0, 1 - (now - finishedAt) / 500f);
            Invalidate();
            if (!scanning && finishGlow <= 0) animationTimer.Stop();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width <= 0 || Height <= 0 || (!scanning && finishGlow <= 0)) return;
            float width = Math.Max(3, Width * displayed);
            Color accent = scanning ? Color.FromArgb(70, 190, 230) : UiEffects.Blend(BackColor, Color.FromArgb(75, 210, 150), finishGlow);
            using (SolidBrush fill = new SolidBrush(accent)) e.Graphics.FillRectangle(fill, 0, 0, width, Height);
            if (!scanning || !UiEffects.CanAnimate(this)) return;
            float phase = (clock.ElapsedMilliseconds % 1400) / 1400f;
            RectangleF sweep = new RectangleF(-70 + (width + 140) * phase, 0, 70, Height);
            GraphicsState state = e.Graphics.Save();
            e.Graphics.SetClip(new RectangleF(0, 0, width, Height));
            using (LinearGradientBrush brush = new LinearGradientBrush(sweep, Color.Transparent, Color.Transparent, 0f))
            {
                brush.InterpolationColors = new ColorBlend { Colors = new[] { Color.Transparent, Color.FromArgb(170, Color.White), Color.Transparent }, Positions = new[] { 0f, .5f, 1f } };
                e.Graphics.FillRectangle(brush, sweep);
            }
            e.Graphics.Restore(state);
        }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); UpdateTimer(); }
        protected override void OnParentChanged(EventArgs e)
        {
            if (ownerForm != null) ownerForm.Resize -= OwnerResize;
            base.OnParentChanged(e);
            ownerForm = FindForm();
            if (ownerForm != null) ownerForm.Resize += OwnerResize;
        }
        private void OwnerResize(object sender, EventArgs e) { UpdateTimer(); }
        private void PreferenceChanged(object sender, EventArgs e) { UpdateTimer(); }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (ownerForm != null) ownerForm.Resize -= OwnerResize;
                UiEffects.MotionPreferenceChanged -= PreferenceChanged;
                animationTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
