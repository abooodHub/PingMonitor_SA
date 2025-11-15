using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PingMonitor
{
    public class AnimatedButton : Button
    {
        // خصائص الانيميشن
        private float hoverScale = 1.0F;
        private float targetScale = 1.0F;
        private Timer animationTimer;
        private int shadowSize = 0;
        private int targetShadowSize = 8;

        // خصائص الألوان
        private Color baseColor;
        private Color hoverColor;
        private Color currentColor;

        public AnimatedButton()
        {
            // إعداد الزر
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 10.0F, FontStyle.Bold);
            this.ForeColor = Color.White;

            // إعداد مؤقت الانيميشن
            animationTimer = new Timer();
            animationTimer.Interval = 16; // ~60 FPS
            animationTimer.Tick += AnimationTick;

            // ربط الأحداث
            this.MouseEnter += OnMouseEnterCustom;
            this.MouseLeave += OnMouseLeaveCustom;
            this.MouseDown += OnMouseDownCustom;
            this.MouseUp += OnMouseUpCustom;

            // تفعيل الرسم المخصص
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        // تعيين الألوان
        public void SetColors(Color baseCol, Color hoverCol)
        {
            baseColor = baseCol;
            hoverColor = hoverCol;
            currentColor = baseCol;
            this.BackColor = baseCol;
        }

        // حدث دخول الماوس
        private void OnMouseEnterCustom(object sender, EventArgs e)
        {
            targetScale = 1.05F;
            targetShadowSize = 12;
            currentColor = hoverColor;
            if (!animationTimer.Enabled)
            {
                animationTimer.Start();
            }
        }

        // حدث خروج الماوس
        private void OnMouseLeaveCustom(object sender, EventArgs e)
        {
            targetScale = 1.0F;
            targetShadowSize = 8;
            currentColor = baseColor;
            if (!animationTimer.Enabled)
            {
                animationTimer.Start();
            }
        }

        // حدث الضغط
        private void OnMouseDownCustom(object sender, MouseEventArgs e)
        {
            targetScale = 0.95F;
            targetShadowSize = 4;
        }

        // حدث رفع الضغط
        private void OnMouseUpCustom(object sender, MouseEventArgs e)
        {
            if (this.ClientRectangle.Contains(e.Location))
            {
                targetScale = 1.05F;
                targetShadowSize = 12;
            }
            else
            {
                targetScale = 1.0F;
                targetShadowSize = 8;
            }
        }

        // تحديث الانيميشن
        private void AnimationTick(object sender, EventArgs e)
        {
            bool changed = false;

            // تحديث التكبير بسلاسة
            if (Math.Abs(hoverScale - targetScale) > 0.001F)
            {
                hoverScale += (targetScale - hoverScale) * 0.3F;
                changed = true;
            }
            else
            {
                hoverScale = targetScale;
            }

            // تحديث الظل بسلاسة
            if (Math.Abs(shadowSize - targetShadowSize) > 0.5)
            {
                shadowSize += (int)((targetShadowSize - shadowSize) * 0.3);
                changed = true;
            }
            else
            {
                shadowSize = targetShadowSize;
            }

            // إيقاف المؤقت إذا انتهت الحركة
            if (!changed)
            {
                animationTimer.Stop();
            }

            this.Invalidate();
        }

        // رسم الزر المخصص
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(this.Parent.BackColor);

            // حساب الأبعاد مع التكبير
            int scaledWidth = (int)(this.Width * hoverScale);
            int scaledHeight = (int)(this.Height * hoverScale);
            int offsetX = (this.Width - scaledWidth) / 2;
            int offsetY = (this.Height - scaledHeight) / 2;

            Rectangle buttonRect = new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight);

            // رسم الظل
            if (shadowSize > 0)
            {
                using (GraphicsPath shadowPath = new GraphicsPath())
                {
                    Rectangle shadowRect = new Rectangle(
                        buttonRect.X + 2,
                        buttonRect.Y + 2,
                        buttonRect.Width,
                        buttonRect.Height
                    );
                    shadowPath.AddRectangle(shadowRect);

                    using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                    {
                        shadowBrush.CenterColor = Color.FromArgb(60, Color.Black);
                        shadowBrush.SurroundColors = new Color[] { Color.FromArgb(0, Color.Black) };
                        g.FillPath(shadowBrush, shadowPath);
                    }
                }
            }

            // رسم الزر بالتدرج
            using (GraphicsPath buttonPath = new GraphicsPath())
            {
                int cornerRadius = 8;
                AddRoundedRectangle(buttonPath, buttonRect, cornerRadius);

                // تدرج لوني جميل
                using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                    buttonRect,
                    currentColor,
                    ControlPaint.Light(currentColor, 0.2F),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(gradientBrush, buttonPath);
                }

                // رسم حد خفيف
                using (Pen borderPen = new Pen(ControlPaint.Light(currentColor, 0.3F), 2))
                {
                    g.DrawPath(borderPen, buttonPath);
                }
            }

            // رسم النص
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;

            using (SolidBrush textBrush = new SolidBrush(this.ForeColor))
            {
                g.DrawString(this.Text, this.Font, textBrush, buttonRect, sf);
            }
        }

        // إضافة مستطيل بزوايا دائرية
        private void AddRoundedRectangle(GraphicsPath path, Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            // الزاوية العلوية اليسرى
            path.AddArc(arc, 180, 90);

            // الزاوية العلوية اليمنى
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // الزاوية السفلية اليمنى
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // الزاوية السفلية اليسرى
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (animationTimer != null)
                {
                    animationTimer.Stop();
                    animationTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
