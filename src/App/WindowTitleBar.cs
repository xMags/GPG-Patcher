using System;
using System.Drawing;
using System.Windows.Forms;

namespace GpgPatcher.Gui
{
    /// <summary>
    /// Replaces the native caption. It lets hit tests fall through to the form so Windows keeps
    /// handling drag, double-click maximize, snapping and the system menu.
    /// The logo is borrowed from the form, which owns its lifetime.
    /// </summary>
    internal sealed class WindowTitleBar : Panel
    {
        private const int WmNcHitTest = 0x0084;
        private const int HtTransparent = -1;
        private const string MinimizeGlyph = "";
        private const string MaximizeGlyph = "";
        private const string RestoreGlyph = "";
        private const string CloseGlyph = "";

        private readonly ThemePalette palette;
        private readonly Image logo;
        private readonly string title;
        private readonly Font titleFont;
        private readonly WindowCaptionButton maximizeButton;

        public WindowTitleBar(ThemePalette palette, Image logo, string title)
        {
            this.palette = palette;
            this.logo = logo;
            this.title = title;
            titleFont = palette.CreateUiFont(14f, FontStyle.Bold);

            DoubleBuffered = true;
            Dock = DockStyle.Top;
            Height = 52;
            BackColor = palette.Surface;

            var minimizeButton = CreateCaptionButton(MinimizeGlyph, false);
            maximizeButton = CreateCaptionButton(MaximizeGlyph, false);
            var closeButton = CreateCaptionButton(CloseGlyph, true);

            minimizeButton.Click += (sender, args) => MinimizeClicked?.Invoke(this, EventArgs.Empty);
            maximizeButton.Click += (sender, args) => MaximizeClicked?.Invoke(this, EventArgs.Empty);
            closeButton.Click += (sender, args) => CloseClicked?.Invoke(this, EventArgs.Empty);

            // Docking runs from the last control added, so Close ends up on the outside edge.
            Controls.Add(minimizeButton);
            Controls.Add(maximizeButton);
            Controls.Add(closeButton);
        }

        public event EventHandler MinimizeClicked;

        public event EventHandler MaximizeClicked;

        public event EventHandler CloseClicked;

        public void SetMaximized(bool maximized)
        {
            maximizeButton.Glyph = maximized ? RestoreGlyph : MaximizeGlyph;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmNcHitTest)
            {
                m.Result = new IntPtr(HtTransparent);
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var left = 24;
            if (logo != null)
            {
                var logoSize = Height - 14;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(logo, new Rectangle(left, (Height - logoSize) / 2, logoSize, logoSize));
                left += logoSize + 8;
            }

            TextRenderer.DrawText(
                e.Graphics,
                title,
                titleFont,
                new Rectangle(left, 0, Math.Max(0, Width - left), Height - 1),
                palette.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

            using (var borderPen = new Pen(palette.Border))
            {
                e.Graphics.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                titleFont.Dispose();
            }

            base.Dispose(disposing);
        }

        private WindowCaptionButton CreateCaptionButton(string glyph, bool isClose)
        {
            return new WindowCaptionButton(palette, isClose)
            {
                Glyph = glyph,
                Dock = DockStyle.Right,
                Width = 46,
                Margin = new Padding(0),
            };
        }
    }

    internal sealed class WindowCaptionButton : Control
    {
        private static readonly Color CloseHoverFill = Color.FromArgb(196, 43, 28);

        private readonly ThemePalette palette;
        private readonly bool isClose;
        private readonly Font glyphFont;
        private string glyph;
        private bool isHovered;

        public WindowCaptionButton(ThemePalette palette, bool isClose)
        {
            this.palette = palette;
            this.isClose = isClose;
            glyphFont = ModernButton.CreateFluentIconFont(8f);

            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }

        public string Glyph
        {
            get { return glyph; }
            set
            {
                glyph = value;
                Invalidate();
            }
        }

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
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var hoverFill = isClose ? CloseHoverFill : palette.SurfaceTint;
            var hoverText = isClose ? Color.White : palette.TextPrimary;

            using (var fillBrush = new SolidBrush(isHovered ? hoverFill : palette.Surface))
            {
                e.Graphics.FillRectangle(fillBrush, ClientRectangle);
            }

            // Stops one pixel short so the title bar's bottom border stays unbroken.
            TextRenderer.DrawText(
                e.Graphics,
                glyph,
                glyphFont,
                new Rectangle(0, 0, Width, Height - 1),
                isHovered ? hoverText : palette.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

            using (var borderPen = new Pen(palette.Border))
            {
                e.Graphics.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                glyphFont.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
