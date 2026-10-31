using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GpgPatcher.Gui
{
    internal enum ModernButtonTone
    {
        Primary,
        Secondary,
        Danger,
        Ghost,
    }

    internal sealed class ModernButton : Button
    {
        private const int DefaultIconLeft = 14;
        private const int IconSlotWidth = 24;
        private const int IconTextGap = 6;

        private bool isHovered;
        private bool isPressed;
        private readonly Font fluentIconFont;

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            AutoSize = false;
            Size = new Size(124, 42);
            Cursor = Cursors.Hand;
            Font = SystemFonts.MessageBoxFont;
            fluentIconFont = CreateFluentIconFont(12f);

            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);
        }

        public int CornerRadius { get; set; }

        public ModernButtonTone Tone { get; set; }

        public ThemePalette Palette { get; set; }

        public string IconGlyph { get; set; }

        public bool CenterContent { get; set; }

        public bool SquareCorners { get; set; }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            isPressed = false;
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var palette = Palette;
            if (palette == null)
            {
                base.OnPaint(e);
                return;
            }

            e.Graphics.Clear(Parent == null ? palette.WindowBackground : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            var colors = ResolveColors(palette);

            using (var path = RoundedDrawing.CreateRoundedRectangle(bounds, ResolveCornerRadius()))
            using (var fillBrush = new SolidBrush(colors.Fill))
            using (var borderPen = new Pen(colors.Border))
            {
                e.Graphics.FillPath(fillBrush, path);
                e.Graphics.DrawPath(borderPen, path);
            }

            if (string.IsNullOrEmpty(IconGlyph))
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    bounds,
                    colors.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else
            {
                var iconLeft = ResolveIconLeft();
                var iconBounds = new Rectangle(iconLeft, 0, IconSlotWidth, Height - 1);
                TextRenderer.DrawText(
                    e.Graphics,
                    IconGlyph,
                    fluentIconFont,
                    iconBounds,
                    colors.Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                var textLeft = iconLeft + IconSlotWidth + IconTextGap;
                var textBounds = new Rectangle(textLeft, 0, Math.Max(0, Width - textLeft - DefaultIconLeft + 4), Height - 1);
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    textBounds,
                    colors.Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }

            if (Focused)
            {
                var focusBounds = Rectangle.Inflate(bounds, -4, -4);
                ControlPaint.DrawFocusRectangle(e.Graphics, focusBounds, colors.Text, Color.Transparent);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fluentIconFont.Dispose();
            }

            base.Dispose(disposing);
        }

        private int ResolveCornerRadius()
        {
            if (SquareCorners)
            {
                return 0;
            }

            return CornerRadius <= 0 ? 14 : CornerRadius;
        }

        private int ResolveIconLeft()
        {
            if (!CenterContent)
            {
                return DefaultIconLeft;
            }

            var textWidth = TextRenderer.MeasureText(Text, Font).Width;
            var contentWidth = IconSlotWidth + IconTextGap + textWidth;
            return Math.Max(DefaultIconLeft, (Width - contentWidth) / 2);
        }

        internal static Font CreateFluentIconFont(float size)
        {
            var font = new Font("Segoe Fluent Icons", size, FontStyle.Regular, GraphicsUnit.Point);
            if (string.Equals(font.Name, "Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase))
            {
                return font;
            }

            font.Dispose();
            return new Font("Segoe MDL2 Assets", size, FontStyle.Regular, GraphicsUnit.Point);
        }

        private ButtonColors ResolveColors(ThemePalette palette)
        {
            if (!Enabled)
            {
                return new ButtonColors
                {
                    Fill = palette.IsDark ? Color.FromArgb(35, 47, 68) : Color.FromArgb(232, 238, 247),
                    Border = palette.Border,
                    Text = palette.TextSecondary,
                };
            }

            switch (Tone)
            {
                case ModernButtonTone.Primary:
                    return new ButtonColors
                    {
                        Fill = isPressed ? palette.AccentPressed : isHovered ? palette.AccentHover : palette.Accent,
                        Border = palette.Accent,
                        Text = Color.White,
                    };
                case ModernButtonTone.Danger:
                    return new ButtonColors
                    {
                        Fill = isPressed ? Blend(palette.Danger, palette.BorderStrong, 0.18f) : isHovered ? Blend(palette.Danger, palette.WindowBackground, 0.12f) : palette.DangerSoft,
                        Border = palette.Danger,
                        Text = palette.IsDark ? Color.FromArgb(255, 230, 230) : palette.Danger,
                    };
                case ModernButtonTone.Ghost:
                    return new ButtonColors
                    {
                        Fill = isPressed ? Blend(palette.SurfaceTint, palette.AccentSoft, 0.45f) : isHovered ? palette.SurfaceTint : Color.Transparent,
                        Border = isHovered ? palette.BorderStrong : palette.Border,
                        Text = palette.TextPrimary,
                    };
                default:
                    return new ButtonColors
                    {
                        Fill = isPressed ? Blend(palette.SurfaceTint, palette.BorderStrong, 0.25f) : isHovered ? palette.SurfaceTint : palette.Surface,
                        Border = isHovered ? palette.BorderStrong : palette.Border,
                        Text = palette.TextPrimary,
                    };
            }
        }

        private static Color Blend(Color a, Color b, float amount)
        {
            var inverse = 1f - amount;
            return Color.FromArgb(
                (int)(a.R * inverse + b.R * amount),
                (int)(a.G * inverse + b.G * amount),
                (int)(a.B * inverse + b.B * amount));
        }

        private struct ButtonColors
        {
            public Color Fill;

            public Color Border;

            public Color Text;
        }
    }
}
