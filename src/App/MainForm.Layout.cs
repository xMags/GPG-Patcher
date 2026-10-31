using System;
using System.Drawing;
using System.Windows.Forms;

namespace GpgPatcher.Gui
{
    internal sealed partial class MainForm
    {
        private Control CreateActionPanel()
        {
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

return CreateToolbar(patchButton, refreshButton, verifyButton, diagnoseViewportButton, restoreButton);
        }

        private Control CreateToolbar(params ModernButton[] buttons)
        {
            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = buttons.Length,
                RowCount = 1,
                BackColor = palette.WindowBackground,
                Margin = new Padding(0, 0, 0, 12),
            };
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            for (var column = 0; column < buttons.Length; column++)
            {
                toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / buttons.Length));
                StyleToolbarButton(buttons[column], column, buttons.Length);
                toolbar.Controls.Add(buttons[column], column, 0);
            }

            return toolbar;
        }

        // Keeps each button at its preset width and pins the group to the right edge.
        private Control CreateCompactToolbar(params ModernButton[] buttons)
        {
            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = buttons.Length + 1,
                RowCount = 1,
                BackColor = palette.WindowBackground,
                Margin = new Padding(0, 0, 0, 12),
            };
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            for (var index = 0; index < buttons.Length; index++)
            {
                toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, buttons[index].Width));
                StyleToolbarButton(buttons[index], index, buttons.Length);
                buttons[index].Font = palette.CreateUiFont(9f, FontStyle.Bold);
                toolbar.Controls.Add(buttons[index], index + 1, 0);
            }

            return toolbar;
        }

        private static void StyleToolbarButton(ModernButton button, int position, int count)
        {
            button.CenterContent = true;
            button.CornerRadius = MetricCard.MildCornerRadius;
            button.RoundedSides = position == 0
                ? RoundedSides.Left
                : position == count - 1 ? RoundedSides.Right : RoundedSides.None;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(0);
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
            activityLog = new ActivityLogRenderer(outputTextBox, palette);
            outputViewport.Resize += (sender, args) => FitOutputTextBox(outputViewport);
            outputTextBox.HandleCreated += (sender, args) => FitOutputTextBox(outputViewport);

            return outputPanel;
        }

        private void FitOutputTextBox(Control viewport)
        {
            var scrollBarWidth = outputTextBox.Width - outputTextBox.ClientSize.Width;
            outputTextBox.Size = new Size(viewport.ClientSize.Width + scrollBarWidth, viewport.ClientSize.Height);
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
