using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PingMonitor
{
    internal static class UiEffects
    {
        private static bool reduceMotion;
        public static event EventHandler MotionPreferenceChanged;
        public static bool ReduceMotion
        {
            get { return reduceMotion; }
            set
            {
                if (reduceMotion == value) return;
                reduceMotion = value;
                if (MotionPreferenceChanged != null) MotionPreferenceChanged(null, EventArgs.Empty);
            }
        }
        public static bool MotionEnabled { get { return !reduceMotion && !SystemInformation.HighContrast; } }
        public static bool CanAnimate(Control control)
        {
            Form form = control == null ? null : control.FindForm();
            return MotionEnabled && control != null && !control.IsDisposed && !control.Disposing && control.Visible &&
                (form == null || form.WindowState != FormWindowState.Minimized);
        }

        public static IDisposable Animate(Control owner, int duration, Action<float> frame)
        {
            return new Tween(owner, duration, frame);
        }

        private sealed class Tween : IDisposable
        {
            private readonly Control owner;
            private readonly Action<float> frame;
            private readonly Timer timer;
            private readonly Stopwatch clock = Stopwatch.StartNew();
            private readonly int duration;
            private bool disposed;
            public Tween(Control owner, int duration, Action<float> frame)
            {
                this.owner = owner; this.frame = frame; this.duration = Math.Max(1, duration);
                timer = new Timer { Interval = 16 };
                owner.Disposed += OwnerDisposed;
                owner.VisibleChanged += VisibilityChanged;
                MotionPreferenceChanged += PreferenceChanged;
                timer.Tick += Tick;
                if (!CanAnimate(owner)) Finish();
                else { frame(0); timer.Start(); }
            }
            private void Tick(object sender, EventArgs e)
            {
                if (!CanAnimate(owner)) { Finish(); return; }
                float t = Math.Min(1f, (float)clock.Elapsed.TotalMilliseconds / duration);
                frame(1f - (float)Math.Pow(1f - t, 3));
                if (t >= 1) Dispose();
            }
            private void Finish() { if (!disposed && !owner.IsDisposed) frame(1); Dispose(); }
            private void OwnerDisposed(object sender, EventArgs e) { Dispose(); }
            private void VisibilityChanged(object sender, EventArgs e) { if (!owner.Visible) Finish(); }
            private void PreferenceChanged(object sender, EventArgs e) { if (!MotionEnabled) Finish(); }
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                timer.Stop(); timer.Dispose();
                owner.Disposed -= OwnerDisposed;
                owner.VisibleChanged -= VisibilityChanged;
                MotionPreferenceChanged -= PreferenceChanged;
            }
        }

        public static void PrepareFadeIn(Form form, int durationMilliseconds = 210)
        {
            if (form == null || !MotionEnabled) return;

            form.Opacity = 0.93;
            EventHandler shownHandler = null;
            shownHandler = (sender, args) =>
            {
                form.Shown -= shownHandler;
                double startOpacity = form.Opacity;
                Animate(form, durationMilliseconds, p => form.Opacity = startOpacity + (1 - startOpacity) * p);
            };
            form.Shown += shownHandler;
        }

        public static void AttachColorHover(
            Control control,
            Color normalBackColor,
            Color hoverBackColor,
            Color normalForeColor,
            Color hoverForeColor)
        {
            if (control == null || control is ModernButton) return;
            IDisposable animation = null;
            Action<bool> transition = hot =>
            {
                if (animation != null) animation.Dispose();
                Color fromBack = control.BackColor, fromFore = control.ForeColor;
                Color toBack = hot && control.Enabled ? hoverBackColor : normalBackColor;
                Color toFore = hot && control.Enabled ? hoverForeColor : normalForeColor;
                animation = Animate(control, 180, p =>
                {
                    control.BackColor = Blend(fromBack, toBack, p);
                    control.ForeColor = Blend(fromFore, toFore, p);
                });
            };
            control.MouseEnter += (s, e) => transition(true);
            control.MouseLeave += (s, e) => transition(false);
            control.EnabledChanged += (s, e) => transition(false);
            control.Disposed += (s, e) => { if (animation != null) animation.Dispose(); };
        }

        public static void AttachFocusBorder(TextBox input)
        {
            Control parent = input.Parent;
            if (parent == null) return;
            float strength = 0;
            IDisposable animation = null;
            Action<bool> transition = focused =>
            {
                if (animation != null) animation.Dispose();
                float from = strength;
                animation = Animate(input, 180, p =>
                {
                    strength = from + ((focused ? 1f : 0f) - from) * p;
                    parent.Invalidate(Rectangle.Inflate(input.Bounds, 3, 3));
                });
            };
            PaintEventHandler paint = (s, e) =>
            {
                Rectangle border = Rectangle.Inflate(input.Bounds, 2, 2);
                using (Pen pen = new Pen(Blend(parent.BackColor, Color.FromArgb(80, 190, 230), strength), 1))
                    e.Graphics.DrawRectangle(pen, border);
            };
            parent.Paint += paint;
            input.GotFocus += (s, e) => transition(true);
            input.LostFocus += (s, e) => transition(false);
            input.Disposed += (s, e) => { if (animation != null) animation.Dispose(); parent.Paint -= paint; };
        }

        public static Color Blend(Color from, Color to, float amount)
        {
            amount = Math.Max(0f, Math.Min(1f, amount));
            return Color.FromArgb(
                (int)(from.A + ((to.A - from.A) * amount)),
                (int)(from.R + ((to.R - from.R) * amount)),
                (int)(from.G + ((to.G - from.G) * amount)),
                (int)(from.B + ((to.B - from.B) * amount)));
        }
    }
}
