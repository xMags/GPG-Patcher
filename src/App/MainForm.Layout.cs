using System;
using System.Drawing;
using System.Windows.Forms;

namespace GpgPatcher.Gui
{
    internal sealed partial class MainForm
    {
        private Control CreateActionPanel()
        {
            var maintenanceRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = palette.WindowBackground,
                Margin = new Padding(0, 0, 0, 12),
            };
            maintenanceRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            patchButton = CreateButton("Patch", ModernButtonTone.Primary, async (sender, args) => await PatchAsync());
            refreshButton = CreateButton("Refresh", ModernButtonTone.Secondary, async (sender, args) => await RefreshInspectAsync());
            verifyButton = CreateButton("Verify", ModernButtonTone.Secondary, async (sender, args) => await VerifyAsync());
            diagnoseViewportButton = CreateButton("Diagnose", ModernButtonTone.Secondary, async (sender, args) => await DiagnoseViewportAsync());
            restoreButton = CreateButton("Restore", ModernButtonTone.Danger, async (sender, args) => await RestoreAsync());

            patchButton.IconGlyph = "\uE898";
            refreshButton.IconGlyph = "\uE72C";
            verifyButton.IconGlyph = "\uE73E";
            diagnoseViewportButton.IconGlyph = "\uE9D9";
            restoreButton.IconGlyph = "\uE777";

            var actionButtons = new[] { patchButton, refreshButton, verifyButton, diagnoseViewportButton, restoreButton };
            for (var column = 0; column < actionButtons.Length; column++)
            {
                var button = actionButtons[column];
                maintenanceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
                button.CenterContent = true;
                button.SquareCorners = true;
                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(0);
                maintenanceRow.Controls.Add(button, column, 0);
            }

            return maintenanceRow;
        }

        private Control CreateMetricsGrid()
        {
            var metricsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 180,
                ColumnCount = 4,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 16),
                BackColor = palette.WindowBackground,
            };
            metricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            metricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            metricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            metricsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            metricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));
            metricsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));

            versionCard = CreateMetricCard("Version");
            compatibilityCard = CreateMetricCard("Compatibility");
            patchCard = CreateMetricCard("Patch State");
            backupCard = CreateMetricCard("Backup");
            launchSizeCard = CreateMetricCard("Launch Size");
            densityCard = CreateMetricCard("Density");
            guestDisplayCard = CreateMetricCard("Guest Display");
            targetResolutionCard = CreateMetricCard("Target Resolution");

            AddCard(metricsGrid, versionCard, 0, 0);
            AddCard(metricsGrid, compatibilityCard, 1, 0);
            AddCard(metricsGrid, patchCard, 2, 0);
            AddCard(metricsGrid, backupCard, 3, 0);
            AddCard(metricsGrid, launchSizeCard, 0, 1);
            AddCard(metricsGrid, densityCard, 1, 1);
            AddCard(metricsGrid, guestDisplayCard, 2, 1);
            AddCard(metricsGrid, targetResolutionCard, 3, 1);

            return metricsGrid;
        }

        private Control CreateOutputPanel()
        {
            var outputPanel = new ModernSurfacePanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(18, 14, 18, 14),
                CornerRadius = 22,
                FillColor = palette.Surface,
                BorderColor = palette.Border,
            };

            var outputLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = outputPanel.FillColor,
            };
            outputLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            outputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            outputPanel.Controls.Add(outputLayout);

            var outputHeaderPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = outputPanel.FillColor,
                Margin = new Padding(0),
            };
            outputHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            outputHeaderPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            outputLayout.Controls.Add(outputHeaderPanel, 0, 0);

            outputCaptionLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = palette.TextPrimary,
                Font = palette.CreateUiFont(11f, FontStyle.Bold),
                Text = "Activity log",
                TextAlign = ContentAlignment.MiddleLeft,
            };
            outputHeaderPanel.Controls.Add(outputCaptionLabel, 0, 0);

            var codePanel = new ModernSurfacePanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 0),
                Padding = new Padding(12),
                CornerRadius = 18,
                FillColor = palette.CodeBackground,
                BorderColor = palette.Border,
            };
            outputLayout.Controls.Add(codePanel, 0, 1);

            // A RichTextBox with no scrollbar ignores the mouse wheel, so the native bar stays
            // enabled and this viewport clips it out of sight.
            var outputViewport = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = palette.CodeBackground,
                Margin = new Padding(0),
            };
            codePanel.Controls.Add(outputViewport);

            outputTextBox = new RichTextBox
            {
                Location = Point.Empty,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                DetectUrls = false,
                ScrollBars = RichTextBoxScrollBars.ForcedVertical,
                WordWrap = true,
                BackColor = palette.CodeBackground,
                ForeColor = palette.CodeText,
                Font = palette.CreateMonoFont(10f),
                Margin = new Padding(0),
            };
            outputViewport.Controls.Add(outputTextBox);
            outputViewport.Resize += (sender, args) => FitOutputTextBox(outputViewport);
            outputTextBox.HandleCreated += (sender, args) => FitOutputTextBox(outputViewport);

            return outputPanel;
        }

        private void FitOutputTextBox(Control viewport)
        {
            var scrollBarWidth = outputTextBox.Width - outputTextBox.ClientSize.Width;
            outputTextBox.Size = new Size(viewport.ClientSize.Width + scrollBarWidth, viewport.ClientSize.Height);
        }

        private Control CreateFooterPanel()
        {
            var footerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 0),
                BackColor = palette.WindowBackground,
            };
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            statusChip = CreateChip();
            statusChip.Anchor = AnchorStyles.Left;
            footerPanel.Controls.Add(statusChip, 0, 0);

            statusDetailLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = palette.TextSecondary,
                Font = palette.CreateUiFont(9.25f, FontStyle.Regular),
                Margin = new Padding(12, 0, 12, 0),
                Text = "Ready.",
                TextAlign = ContentAlignment.MiddleLeft,
            };
            footerPanel.Controls.Add(statusDetailLabel, 1, 0);

            busyProgressBar = new ProgressBar
            {
                Width = 150,
                Height = 10,
                Style = ProgressBarStyle.Marquee,
                Visible = false,
                MarqueeAnimationSpeed = 24,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 9, 0, 9),
            };
            footerPanel.Controls.Add(busyProgressBar, 2, 0);

            return footerPanel;
        }

        private StatusChip CreateChip()
        {
            return new StatusChip
            {
                Font = palette.CreateUiFont(9f, FontStyle.Bold),
                FillColor = palette.Surface,
                BorderColor = palette.Border,
                TextColor = palette.TextPrimary,
            };
        }

        private ModernButton CreateButton(string text, ModernButtonTone tone, EventHandler onClick)
        {
            var button = new ModernButton
            {
                Text = text,
                Tone = tone,
                Palette = palette,
                CornerRadius = 14,
                Margin = new Padding(0, 0, 8, 0),
                Font = palette.CreateUiFont(9.5f, FontStyle.Bold),
            };
            button.Click += onClick;
            return button;
        }

        private static FlowLayoutPanel CreateButtonRow(Color backColor, int left, int top, int right, int bottom)
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = backColor,
                Margin = new Padding(left, top, right, bottom),
            };
        }

        private MetricCard CreateMetricCard(string title)
        {
            var card = new MetricCard
            {
                TitleText = title,
            };
            card.ApplyTheme(palette, false);
            return card;
        }

        private static void AddCard(TableLayoutPanel grid, Control control, int column, int row)
        {
            control.Margin = new Padding(column == 0 ? 0 : 8, row == 0 ? 0 : 8, column == 3 ? 0 : 8, row == 1 ? 0 : 8);
            grid.Controls.Add(control, column, row);
        }
    }
}
