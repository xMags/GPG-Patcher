using System;
using System.Drawing;
using System.Windows.Forms;

namespace GpgPatcher.Gui
{
    internal sealed partial class MainForm
    {
        private enum AppPage
        {
            Home,
            Instances,
            Settings,
        }

        private Control CreateNavigationPanel()
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = palette.Surface,
                Padding = new Padding(16, 18, 16, 16),
                Margin = new Padding(0),
            };
            sidebar.Paint += (sender, args) =>
            {
                using (var dividerPen = new Pen(palette.Border))
                {
                    args.Graphics.DrawLine(dividerPen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
                }
            };
            sidebar.Resize += (sender, args) => sidebar.Invalidate();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = sidebar.BackColor,
                Margin = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            sidebar.Controls.Add(layout);

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "NAVIGATION",
                ForeColor = palette.TextSecondary,
                Font = palette.CreateUiFont(8f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(8, 0, 0, 4),
            }, 0, 0);

            homeNavigationButton = CreateNavigationButton("Home", "\uE80F", (sender, args) => NavigateTo(AppPage.Home));
            instancesNavigationButton = CreateNavigationButton("Accounts", "\uE716", (sender, args) => NavigateTo(AppPage.Instances));
            settingsNavigationButton = CreateNavigationButton("Settings", "\uE713", (sender, args) => NavigateTo(AppPage.Settings));
            layout.Controls.Add(homeNavigationButton, 0, 1);
            layout.Controls.Add(instancesNavigationButton, 0, 2);
            layout.Controls.Add(settingsNavigationButton, 0, 3);

            return sidebar;
        }

        private Control CreatePageHost()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = palette.WindowBackground,
                Margin = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            pageHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = palette.WindowBackground,
                Margin = new Padding(0),
            };
            layout.Controls.Add(pageHost, 0, 0);

            homePage = CreateHomePage();
            instancesPage = CreateInstancesPage();
            settingsPage = CreateSettingsPage();
            pageHost.Controls.Add(homePage);
            pageHost.Controls.Add(instancesPage);
            pageHost.Controls.Add(settingsPage);

            return layout;
        }

        private Control CreateHomePage()
        {
            var page = CreatePageContainer();
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(24, 20, 24, 20),
                BackColor = palette.WindowBackground,
                Margin = new Padding(0),
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 196f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.Controls.Add(CreateActionPanel(), 0, 0);
            root.Controls.Add(CreateMetricsGrid(), 0, 1);
            root.Controls.Add(CreateOutputPanel(), 0, 2);
            page.Controls.Add(root);
            return page;
        }

        private Control CreateInstancesPage()
        {
            var page = CreatePageContainer();
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(24, 20, 24, 20),
                BackColor = palette.WindowBackground,
                Margin = new Padding(0),
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            page.Controls.Add(root);

            addAccountButton = CreateButton("Add Account", ModernButtonTone.Primary, async (sender, args) => await AddAccountAsync());
            addAccountButton.IconGlyph = "\uE710";
            addAccountButton.Width = 176;
            refreshAccountsButton = CreateButton("Refresh", ModernButtonTone.Secondary, async (sender, args) => await RefreshAccountsAsync());
            refreshAccountsButton.IconGlyph = "\uE72C";
            refreshAccountsButton.Width = 136;
            root.Controls.Add(CreateCompactToolbar(addAccountButton, refreshAccountsButton), 0, 0);

            accountsFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = palette.WindowBackground,
                Margin = new Padding(0),
            };
            accountsFlowPanel.ClientSizeChanged += (sender, args) => ResizeAccountCards();
            root.Controls.Add(accountsFlowPanel, 0, 1);

            return page;
        }

        private Control CreateSettingsPage()
        {
            var page = CreatePageContainer();
            var settingsContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = palette.WindowBackground,
                Padding = new Padding(24, 20, 24, 20),
                Margin = new Padding(0),
            };
            page.Controls.Add(settingsContent);

            var settingsStack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 244,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = palette.WindowBackground,
                Margin = new Padding(0),
            };
            settingsStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            settingsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
            settingsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 12f));
            settingsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 82f));
            settingsContent.Controls.Add(settingsStack);

            settingsStack.Controls.Add(CreateResolutionProfileCard(), 0, 0);
            settingsStack.Controls.Add(CreatePhenotypeFallbackCard(), 0, 2);
            ApplyResolutionSelectionPresentation();

            return page;
        }

        private Control CreateResolutionProfileCard()
        {
            var resolutionCard = new ModernSurfacePanel
            {
                Dock = DockStyle.Fill,
                CornerRadius = MetricCard.MildCornerRadius,
                FillColor = palette.Surface,
                BorderColor = palette.Border,
                Padding = new Padding(24, 14, 24, 14),
                Margin = new Padding(0),
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = resolutionCard.FillColor,
                Margin = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            resolutionCard.Controls.Add(layout);

            layout.Controls.Add(CreatePageLabel("Portrait resolution", 12f, FontStyle.Bold, palette.TextPrimary), 0, 0);

            var buttonRow = CreateButtonRow(resolutionCard.FillColor, 0, 4, 0, 4);
            layout.Controls.Add(buttonRow, 0, 1);

            var profiles = ResolutionProfiles.All;
            resolutionProfileButtons = new ModernButton[profiles.Count];
            for (var index = 0; index < profiles.Count; index++)
            {
                var profile = profiles[index];
                var button = CreateButton(
                    profile.Name + "   " + profile.Value,
                    ModernButtonTone.Secondary,
                    (sender, args) => SelectResolutionProfile(profile));
                button.Width = 188;
                button.Height = 44;
                button.CornerRadius = MetricCard.MildCornerRadius;
                button.AccessibleName = profile.Name + " resolution " + profile.Value;
                button.Tag = profile.Value;
                resolutionProfileButtons[index] = button;
                buttonRow.Controls.Add(button);
            }

            var stateLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = resolutionCard.FillColor,
                Margin = new Padding(0),
            };
            stateLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            stateLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.Controls.Add(stateLayout, 0, 2);

            appliedResolutionLabel = CreatePageLabel(string.Empty, 8.5f, FontStyle.Bold, palette.Accent);
            appliedResolutionLabel.AutoSize = true;
            appliedResolutionLabel.Dock = DockStyle.Left;
            appliedResolutionLabel.Padding = new Padding(0, 0, 22, 0);
            stateLayout.Controls.Add(appliedResolutionLabel, 0, 0);

            resolutionApplyMessageLabel = CreatePageLabel(string.Empty, 8.5f, FontStyle.Regular, palette.TextSecondary);
            stateLayout.Controls.Add(resolutionApplyMessageLabel, 1, 0);

            return resolutionCard;
        }

        private Control CreatePhenotypeFallbackCard()
        {
            var fallbackCard = new ModernSurfacePanel
            {
                Dock = DockStyle.Fill,
                CornerRadius = MetricCard.MildCornerRadius,
                FillColor = palette.Surface,
                BorderColor = palette.Border,
                Padding = new Padding(24, 12, 24, 12),
                Margin = new Padding(0),
            };

            var settingLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = fallbackCard.FillColor,
                Margin = new Padding(0),
            };
            settingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            settingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
            settingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
            settingLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            settingLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
            fallbackCard.Controls.Add(settingLayout);

            settingLayout.Controls.Add(CreatePageLabel("UHD fallback", 12f, FontStyle.Bold, palette.TextPrimary), 0, 0);
            phenotypeFallbackNoteLabel = CreatePageLabel(string.Empty, 9f, FontStyle.Regular, palette.TextSecondary);
            settingLayout.Controls.Add(phenotypeFallbackNoteLabel, 0, 1);

            phenotypeFallbackStateLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Off",
                ForeColor = palette.TextSecondary,
                Font = palette.CreateUiFont(8.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 0, 10, 0),
            };
            settingLayout.Controls.Add(phenotypeFallbackStateLabel, 1, 0);
            settingLayout.SetRowSpan(phenotypeFallbackStateLabel, 2);

            phenotypeFallbackToggle = new ToggleSwitch
            {
                Palette = palette,
                AccessibleName = "UHD fallback",
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
            };
            settingLayout.Controls.Add(phenotypeFallbackToggle, 2, 0);
            settingLayout.SetRowSpan(phenotypeFallbackToggle, 2);
            phenotypeFallbackToggle.CheckedChanged += (sender, args) =>
            {
                ApplyPhenotypeAvailabilityPresentation();
            };

            return fallbackCard;
        }

        private void SelectResolutionProfile(ResolutionProfile profile)
        {
            selectedResolutionProfile = profile;
            if (!profile.IsUhd)
            {
                phenotypeFallbackToggle.Checked = false;
            }

            try
            {
                ResolutionProfileStorage.WriteSelected(profile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Could not remember the selected resolution: " + ex.Message,
                    "GPG Patcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            ApplyResolutionSelectionPresentation();
        }

        private void ApplyResolutionSelectionPresentation()
        {
            if (resolutionProfileButtons != null)
            {
                foreach (var button in resolutionProfileButtons)
                {
                    var selected = string.Equals(
                        button.Tag as string,
                        selectedResolutionProfile.Value,
                        StringComparison.Ordinal);
                    button.Tone = selected ? ModernButtonTone.Primary : ModernButtonTone.Secondary;
                    button.Invalidate();
                }
            }

            if (appliedResolutionLabel != null)
            {
                appliedResolutionLabel.Text = "APPLIED  " + appliedResolutionProfile.Name + "  " + appliedResolutionProfile.Value;
            }

            if (resolutionApplyMessageLabel != null)
            {
                var matchesApplied = string.Equals(
                    selectedResolutionProfile.Value,
                    appliedResolutionProfile.Value,
                    StringComparison.Ordinal);
                resolutionApplyMessageLabel.Text = matchesApplied
                    ? string.Empty
                    : selectedResolutionProfile.Name + " applies on next Patch";
                resolutionApplyMessageLabel.ForeColor = matchesApplied ? palette.TextSecondary : palette.Warning;
            }

            ApplyPhenotypeAvailabilityPresentation();
        }

        private void ApplyPhenotypeAvailabilityPresentation()
        {
            if (phenotypeFallbackToggle == null)
            {
                return;
            }

            var available = selectedResolutionProfile.IsUhd;
            phenotypeFallbackToggle.Enabled = available && !UseWaitCursor;
            if (!available && phenotypeFallbackToggle.Checked)
            {
                phenotypeFallbackToggle.Checked = false;
            }

            if (phenotypeFallbackStateLabel != null)
            {
                phenotypeFallbackStateLabel.Text = available
                    ? (phenotypeFallbackToggle.Checked ? "On" : "Off")
                    : "Unavailable";
                phenotypeFallbackStateLabel.ForeColor = available && phenotypeFallbackToggle.Checked
                    ? palette.Accent
                    : palette.TextSecondary;
            }

            if (phenotypeFallbackNoteLabel != null)
            {
                phenotypeFallbackNoteLabel.Text = available
                    ? "Only if UHD stops working. Applies on next Patch."
                    : "UHD only.";
            }
        }

        private Panel CreatePageContainer()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = palette.WindowBackground,
                Margin = new Padding(0),
            };
        }

        private Label CreatePageLabel(string text, float size, FontStyle style, Color color)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                ForeColor = color,
                Font = palette.CreateUiFont(size, style),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(0),
            };
        }

        private ModernButton CreateNavigationButton(string text, string iconGlyph, EventHandler onClick)
        {
            var button = CreateButton(text, ModernButtonTone.Ghost, onClick);
            button.IconGlyph = iconGlyph;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(0, 4, 0, 4);
            return button;
        }

        private void NavigateTo(AppPage page)
        {
            if (homePage == null || instancesPage == null || settingsPage == null)
            {
                return;
            }

            homePage.Visible = page == AppPage.Home;
            instancesPage.Visible = page == AppPage.Instances;
            settingsPage.Visible = page == AppPage.Settings;

            var activePage = page == AppPage.Home
                ? homePage
                : page == AppPage.Instances ? instancesPage : settingsPage;
            activePage.BringToFront();

            SetNavigationButtonState(homeNavigationButton, page == AppPage.Home);
            SetNavigationButtonState(instancesNavigationButton, page == AppPage.Instances);
            SetNavigationButtonState(settingsNavigationButton, page == AppPage.Settings);

            if (page == AppPage.Instances)
            {
                BeginInvoke(new Action(async () => await RefreshAccountsAsync()));
            }
        }

        private static void SetNavigationButtonState(ModernButton button, bool isActive)
        {
            if (button == null)
            {
                return;
            }

            button.Tone = isActive ? ModernButtonTone.Primary : ModernButtonTone.Ghost;
            button.Invalidate();
        }
    }
}
