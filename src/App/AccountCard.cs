using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace GpgPatcher.Gui
{
    internal sealed class AccountCard : ModernSurfacePanel
    {
        private readonly PlayGamesAccount account;

        public AccountCard(ThemePalette palette, PlayGamesAccount account, bool exactLaunchReady)
        {
            this.account = account;
            CornerRadius = MetricCard.MildCornerRadius;
            FillColor = palette.Surface;
            BorderColor = account.IsForeground ? palette.Success : palette.Border;
            Padding = new Padding(16);
            Margin = new Padding(0, 0, 12, 12);
            Height = 142;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = FillColor,
                Margin = new Padding(0),
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            Controls.Add(layout);

            var identity = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = FillColor,
                Margin = new Padding(0),
            };
            identity.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62f));
            identity.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            identity.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Controls.Add(identity, 0, 0);

            identity.Controls.Add(new AccountAvatar(palette, account), 0, 0);

            var labels = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = FillColor,
                Margin = new Padding(0, 0, 0, 8),
            };
            labels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            labels.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            labels.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            labels.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            labels.Controls.Add(CreateLabel(account.GamerTag, palette.CreateUiFont(11f, FontStyle.Bold), palette.TextPrimary), 0, 0);
            labels.Controls.Add(CreateLabel(account.MaskedEmail, palette.CreateUiFont(8.5f, FontStyle.Regular), palette.TextSecondary), 0, 1);
            if (account.IsForeground)
            {
                var active = CreateLabel("Active", palette.CreateUiFont(8.5f, FontStyle.Bold), palette.Success);
                active.AutoSize = true;
                active.Dock = DockStyle.None;
                active.Anchor = AnchorStyles.Right;
                labels.Controls.Add(active, 1, 0);
            }

            identity.Controls.Add(labels, 1, 0);

            var canLaunch = exactLaunchReady && !string.IsNullOrWhiteSpace(account.AndroidAppLibraryId);
            var launch = new ModernButton
            {
                Text = canLaunch ? "Launch" : exactLaunchReady ? "Preparing" : "Patch required",
                Tone = ModernButtonTone.Primary,
                Palette = palette,
                CornerRadius = MetricCard.MildCornerRadius,
                Dock = DockStyle.Fill,
                Enabled = canLaunch,
                Font = palette.CreateUiFont(9f, FontStyle.Bold),
                Margin = new Padding(0),
            };
            launch.Click += (sender, args) => OnLaunchRequested();
            layout.Controls.Add(launch, 0, 1);
        }

        public event EventHandler LaunchRequested;

        public PlayGamesAccount Account
        {
            get { return account; }
        }

        private void OnLaunchRequested()
        {
            var handler = LaunchRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private static Label CreateLabel(string text, Font font, Color color)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                Font = font,
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(0),
            };
        }
    }

    internal sealed class AccountAvatar : Control
    {
        private readonly ThemePalette palette;
        private readonly string initials;
        private readonly Font initialsFont;
        private Image image;

        public AccountAvatar(ThemePalette palette, PlayGamesAccount account)
        {
            this.palette = palette;
            initials = account.Initials;
            initialsFont = palette.CreateUiFont(11f, FontStyle.Bold);
            Size = new Size(50, 50);
            Margin = new Padding(0, 0, 12, 8);
            DoubleBuffered = true;
            image = TryLoadImage(account.AvatarPath);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(bounds);
                if (image != null)
                {
                    var graphicsState = e.Graphics.Save();
                    e.Graphics.SetClip(path);
                    e.Graphics.DrawImage(image, bounds);
                    e.Graphics.Restore(graphicsState);
                }
                else
                {
                    using (var brush = new SolidBrush(palette.AccentSoft))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    TextRenderer.DrawText(
                        e.Graphics,
                        initials,
                        initialsFont,
                        bounds,
                        palette.Accent,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }

                using (var pen = new Pen(palette.BorderStrong))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && image != null)
            {
                image.Dispose();
                image = null;
            }

            if (disposing)
            {
                initialsFont.Dispose();
            }

            base.Dispose(disposing);
        }

        private static Image TryLoadImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var source = Image.FromStream(stream))
                {
                    return new Bitmap(source);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
