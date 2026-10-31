using System;
using System.Drawing;
using System.Windows.Forms;

namespace GpgPatcher.Gui
{
    /// <summary>
    /// Draws an <see cref="ActivityReport"/> as a styled terminal screen. The log background is
    /// dark in both themes, so these colors are fixed. Runs on the UI thread only.
    /// </summary>
    internal sealed class ActivityLogRenderer : IDisposable
    {
        private const int SectionRuleWidth = 56;
        private const int GlyphColumnPixels = 26;
        private const int ValueColumnPixels = 330;

        private static readonly Color GoodColor = Color.FromArgb(74, 222, 128);
        private static readonly Color WarningColor = Color.FromArgb(251, 191, 36);
        private static readonly Color BadColor = Color.FromArgb(248, 113, 113);
        private static readonly Color InfoColor = Color.FromArgb(125, 180, 255);
        private static readonly Color MutedColor = Color.FromArgb(148, 163, 184);

        private readonly RichTextBox target;
        private readonly Color textColor;
        private readonly Font regularFont;
        private readonly Font boldFont;
        private readonly int[] tabStops;

        public ActivityLogRenderer(RichTextBox target, ThemePalette palette)
        {
            this.target = target;
            textColor = palette.CodeText;
            regularFont = palette.CreateMonoFont(10f);
            boldFont = new Font(regularFont, FontStyle.Bold);
            var scale = target.DeviceDpi / 96f;
            tabStops = new[] { (int)(GlyphColumnPixels * scale), (int)(ValueColumnPixels * scale) };
        }

        public void Render(ActivityReport report)
        {
            target.Clear();

            Append(GlyphFor(report.VerdictTone) + "\t", ColorFor(report.VerdictTone), boldFont);
            Append(report.Verdict + Environment.NewLine, ColorFor(report.VerdictTone), boldFont);

            foreach (var section in report.Sections)
            {
                AppendSectionHeading(section.Title);
                foreach (var row in section.Rows)
                {
                    AppendRow(row);
                }
            }

            target.Select(0, 0);
            target.ScrollToCaret();
        }

        public void Dispose()
        {
            boldFont.Dispose();
            regularFont.Dispose();
        }

        private void AppendSectionHeading(string title)
        {
            Append(Environment.NewLine + title + " ", MutedColor, regularFont);
            var ruleLength = Math.Max(4, SectionRuleWidth - title.Length);
            Append(new string('─', ruleLength) + Environment.NewLine, MutedColor, regularFont);
        }

        private void AppendRow(ActivityRow row)
        {
            Append(GlyphFor(row.Tone) + "\t", ColorFor(row.Tone), boldFont);
            if (row.Value == null)
            {
                Append(row.Label + Environment.NewLine, textColor, regularFont);
                return;
            }

            Append(row.Label + "\t", textColor, regularFont);
            Append(row.Value + Environment.NewLine, row.Tone == ActivityTone.Info ? Color.White : ColorFor(row.Tone), boldFont);
        }

        private void Append(string text, Color color, Font font)
        {
            target.SelectionStart = target.TextLength;
            target.SelectionLength = 0;
            target.SelectionColor = color;
            target.SelectionFont = font;
            target.SelectionTabs = tabStops;
            target.SelectionIndent = 4;
            target.SelectionHangingIndent = tabStops[0];
            target.AppendText(text);
        }

        private static string GlyphFor(ActivityTone tone)
        {
            switch (tone)
            {
                case ActivityTone.Good:
                    return "✓";
                case ActivityTone.Warning:
                    return "▲";
                case ActivityTone.Bad:
                    return "✕";
                default:
                    return "•";
            }
        }

        private static Color ColorFor(ActivityTone tone)
        {
            switch (tone)
            {
                case ActivityTone.Good:
                    return GoodColor;
                case ActivityTone.Warning:
                    return WarningColor;
                case ActivityTone.Bad:
                    return BadColor;
                default:
                    return InfoColor;
            }
        }
    }
}
