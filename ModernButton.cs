using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PingMonitor
{
    public class ModernButton : Button
    {
        private Timer animationTimer;
        private float scale = 1.0f;
        private float targetScale = 1.0f;
        private int glowAlpha = 0;
        private int targetGlowAlpha = 0;

        private Color gradientColor1 = Color.FromArgb(0, 180, 240);
        private Color gradientColor2 = Color.FromArgb(0, 140, 200);
        private int borderRadius = 10;

        public ModernButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Color.White;
            Font = new Font("Segoe UI Emoji", 10.0f, FontStyle.Bold);

            animationTimer = new Timer();
            animationTimer.Interval = 16;
            animationTimer.Tick += AnimationTick;

            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;
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

        private void OnMouseEnter(object sender, EventArgs e)
        {
            targetScale = 1.05f;
            targetGlowAlpha = 80;
            if (!animationTimer.Enabled) animationTimer.Start();
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            targetScale = 1.0f;
            targetGlowAlpha = 0;
            if (!animationTimer.Enabled) animationTimer.Start();
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            targetScale = 0.95f;
            if (!animationTimer.Enabled) animationTimer.Start();
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            targetScale = ClientRectangle.Contains(e.Location) ? 1.05f : 1.0f;
            if (!animationTimer.Enabled) animationTimer.Start();
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            bool changed = false;

            if (Math.Abs(scale - targetScale) > 0.001f)
            {
                scale += (targetScale - scale) * 0.3f;
                changed = true;
            }
            else
            {
                scale = targetScale;
            }

            if (Math.Abs(glowAlpha - targetGlowAlpha) > 1)
            {
                glowAlpha += (int)((targetGlowAlpha - glowAlpha) * 0.3);
                changed = true;
            }
            else
            {
                glowAlpha = targetGlowAlpha;
            }

            if (!changed) animationTimer.Stop();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Parent != null) g.Clear(Parent.BackColor);

            int scaledWidth = (int)(Width * scale);
            int scaledHeight = (int)(Height * scale);
            int offsetX = (Width - scaledWidth) / 2;
            int offsetY = (Height - scaledHeight) / 2;

            Rectangle rect = new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight);

            using (GraphicsPath path = CreateRoundedPath(rect, borderRadius))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, gradientColor1, gradientColor2, LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }

                using (Pen borderPen = new Pen(ControlPaint.Light(gradientColor1, 0.3f), 2))
                {
                    g.DrawPath(borderPen, path);
                }

                if (glowAlpha > 0)
                {
                    Rectangle glowRect = Rectangle.Inflate(rect, 6, 6);
                    using (GraphicsPath glowPath = CreateRoundedPath(glowRect, borderRadius + 4))
                    using (PathGradientBrush glowBrush = new PathGradientBrush(glowPath))
                    {
                        glowBrush.CenterColor = Color.FromArgb(glowAlpha, Color.White);
                        glowBrush.SurroundColors = new Color[] { Color.FromArgb(0, Color.White) };
                        g.FillPath(glowBrush, glowPath);
                    }
                }
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(g, Text, Font, rect, ForeColor, flags);
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(d, d));
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}