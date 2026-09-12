using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PingMonitor
{
    public class ModernButton : Button
    {
        private Timer animationTimer;
        private float hoverProgress;
        private float pressProgress;
        private float focusProgress;
        private float targetHover;
        private float targetPress;
        private float targetFocus;
        private float rippleProgress = 1f;
        private float sheenProgress = 1f;
        private PointF rippleOrigin;
        private readonly Stopwatch frameClock = new Stopwatch();

        private Color gradientColor1 = Color.FromArgb(0, 180, 240);
        private Color gradientColor2 = Color.FromArgb(0, 140, 200);
        private int borderRadius = 10;

        public ModernButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            ForeColor = Color.White;
            Font = new Font("Segoe UI Emoji", 10.0f, FontStyle.Bold);
            Cursor = Cursors.Hand;

            animationTimer = new Timer { Interval = 15 };
            animationTimer.Tick += AnimationTick;
            UiEffects.MotionPreferenceChanged += MotionPreferenceChanged;
        }

        public Color GradientColor1
        {
            get { return gradientColor1; }
            set { gradientColor1 = value; Invalidate(); }
        }

        public Color GradientColor2
        {
            get { return gradientColor2; }
            set { gradientColor2 = value; Invalidate(); }
        }

        public int BorderRadius
        {
            get { return borderRadius; }
            set { borderRadius = Math.Max(0, value); Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            targetHover = Enabled ? 1f : 0f;
            if (Enabled) sheenProgress = 0f;
            StartAnimation();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            targetHover = 0f;
            targetPress = 0f;
            StartAnimation();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && Enabled)
            {
                targetPress = 1f;
                rippleOrigin = e.Location;
                rippleProgress = 0f;
                StartAnimation();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            targetPress = 0f;
            targetHover = Enabled && ClientRectangle.Contains(e.Location) ? 1f : 0f;
            StartAnimation();
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            targetPress = 0f;
            StartAnimation();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            targetFocus = 1f;
            StartAnimation();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            targetFocus = 0f;
            targetPress = 0f;
            StartAnimation();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && Enabled)
            {
                if (targetPress == 0)
                {
                    rippleOrigin = new PointF(Width / 2f, Height / 2f);
                    rippleProgress = 0f;
                }
                targetPress = 1f;
                StartAnimation();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                targetPress = 0f;
                StartAnimation();
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            targetHover = 0f;
            targetPress = 0f;
            StartAnimation();
        }

        private void StartAnimation()
        {
            if (!UiEffects.CanAnimate(this)) { FinishAnimation(); return; }
            if (animationTimer != null && !animationTimer.Enabled) { frameClock.Restart(); animationTimer.Start(); }
        }

        private void MotionPreferenceChanged(object sender, EventArgs e) { if (!UiEffects.MotionEnabled) FinishAnimation(); }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) FinishAnimation();
        }
        private void FinishAnimation()
        {
            if (animationTimer != null) animationTimer.Stop();
            hoverProgress = targetHover; pressProgress = targetPress; focusProgress = targetFocus;
            rippleProgress = sheenProgress = 1;
            Invalidate();
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            if (!UiEffects.CanAnimate(this)) { FinishAnimation(); return; }
            float elapsed = (float)frameClock.Elapsed.TotalMilliseconds;
            frameClock.Restart();
            bool changed = false;
            hoverProgress = Approach(hoverProgress, targetHover, elapsed, ref changed);
            pressProgress = Approach(pressProgress, targetPress, elapsed, ref changed);
            focusProgress = Approach(focusProgress, targetFocus, elapsed, ref changed);
            rippleProgress = Math.Min(1, rippleProgress + elapsed / 420f);
            sheenProgress = Math.Min(1, sheenProgress + elapsed / 600f);
            changed |= rippleProgress < 1 || sheenProgress < 1;

            if (!changed) animationTimer.Stop();
            Invalidate();
        }

        private static float Approach(float value, float target, float elapsed, ref bool changed)
        {
            float difference = target - value;
            if (Math.Abs(difference) <= 0.012f) return target;
            changed = true;
            return value + difference * (1f - (float)Math.Exp(-elapsed / 65f));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            if (Parent != null) graphics.Clear(Parent.BackColor);

            Rectangle baseRect = new Rectangle(2, 1, Math.Max(1, Width - 5), Math.Max(1, Height - 5));
            int pressInset = (int)Math.Round(pressProgress * 2f);
            int pressOffset = (int)Math.Round(pressProgress);
            Rectangle buttonRect = Rectangle.Inflate(baseRect, -pressInset, -pressInset);
            buttonRect.Offset(0, pressOffset);

            Color topColor = UiEffects.Blend(gradientColor1, Color.White, hoverProgress * 0.10f);
            Color bottomColor = UiEffects.Blend(gradientColor2, Color.White, hoverProgress * 0.05f);
            topColor = UiEffects.Blend(topColor, Color.Black, pressProgress * 0.09f);
            bottomColor = UiEffects.Blend(bottomColor, Color.Black, pressProgress * 0.12f);
            if (!Enabled)
            {
                topColor = UiEffects.Blend(topColor, Color.Gray, 0.55f);
                bottomColor = UiEffects.Blend(bottomColor, Color.FromArgb(65, 65, 68), 0.62f);
            }

            Rectangle shadowRect = buttonRect;
            shadowRect.Offset(0, 2);
            using (GraphicsPath shadowPath = CreateRoundedPath(shadowRect, borderRadius))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb((int)(36 + (hoverProgress * 24)), Color.Black)))
                graphics.FillPath(shadowBrush, shadowPath);

            using (GraphicsPath path = CreateRoundedPath(buttonRect, borderRadius))
            {
                using (LinearGradientBrush fillBrush = new LinearGradientBrush(buttonRect, topColor, bottomColor, LinearGradientMode.Vertical))
                    graphics.FillPath(fillBrush, path);

                int glowAlpha = (int)(18 + (hoverProgress * 58) + (focusProgress * 28));
                using (Pen glowPen = new Pen(Color.FromArgb(Math.Min(120, glowAlpha), Color.White), 2f))
                    graphics.DrawPath(glowPen, path);

                Rectangle highlightRect = Rectangle.Inflate(buttonRect, -3, -3);
                highlightRect.Height = Math.Max(1, highlightRect.Height / 2);
                using (GraphicsPath highlightPath = CreateRoundedPath(highlightRect, Math.Max(1, borderRadius - 2)))
                using (LinearGradientBrush highlightBrush = new LinearGradientBrush(
                    highlightRect,
                    Color.FromArgb((int)(35 + (hoverProgress * 25)), Color.White),
                    Color.FromArgb(0, Color.White),
                    LinearGradientMode.Vertical))
                    graphics.FillPath(highlightBrush, highlightPath);

                GraphicsState saved = graphics.Save();
                graphics.SetClip(path, CombineMode.Intersect);
                if (sheenProgress < 1)
                {
                    float band = Math.Max(30, buttonRect.Width * 0.32f);
                    RectangleF sweep = new RectangleF(buttonRect.Left - band + (buttonRect.Width + band * 2) * sheenProgress,
                        buttonRect.Top, band, buttonRect.Height);
                    using (LinearGradientBrush sheen = new LinearGradientBrush(sweep, Color.Transparent, Color.Transparent, 20f))
                    {
                        sheen.InterpolationColors = new ColorBlend { Colors = new[] { Color.Transparent, Color.FromArgb(48, Color.White), Color.Transparent },
                            Positions = new[] { 0f, 0.5f, 1f } };
                        graphics.FillRectangle(sheen, sweep);
                    }
                }
                if (rippleProgress < 1)
                {
                    float radius = Math.Max(Width, Height) * (0.12f + rippleProgress);
                    using (SolidBrush ripple = new SolidBrush(Color.FromArgb((int)(52 * (1 - rippleProgress)), Color.White)))
                        graphics.FillEllipse(ripple, rippleOrigin.X - radius, rippleOrigin.Y - radius, radius * 2, radius * 2);
                }
                graphics.Restore(saved);
            }

            Rectangle textRect = buttonRect;
            textRect.Offset(0, pressOffset);
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            Color textColor = Enabled ? ForeColor : Color.FromArgb(170, ForeColor);
            TextRenderer.DrawText(graphics, Text, Font, textRect, textColor, flags);

            if (Focused && ShowFocusCues)
            {
                Rectangle focusRect = Rectangle.Inflate(buttonRect, -5, -5);
                ControlPaint.DrawFocusRectangle(graphics, focusRect, Color.White, Color.Transparent);
            }
        }

        private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int safeRadius = Math.Min(radius, Math.Max(1, Math.Min(rect.Width, rect.Height) / 2));
            int diameter = safeRadius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && animationTimer != null)
            {
                UiEffects.MotionPreferenceChanged -= MotionPreferenceChanged;
                animationTimer.Stop();
                animationTimer.Dispose();
                animationTimer = null;
            }
            base.Dispose(disposing);
        }
    }
}
