using System;
using System.Collections.Generic;
using System.Linq;

namespace GpgPatcher.Gui
{
    internal enum ActivityTone
    {
        Good,
        Warning,
        Bad,
        Info,
    }

    internal sealed class ActivityRow
    {
        public ActivityRow(ActivityTone tone, string label, string value = null)
        {
            Tone = tone;
            Label = label;
            Value = value;
        }

        public ActivityTone Tone { get; }

        public string Label { get; }

        /// <summary>Null for a full-sentence row, which is not split into columns.</summary>
        public string Value { get; }
    }

    internal sealed class ActivitySection
    {
        public ActivitySection(string title, IReadOnlyList<ActivityRow> rows)
        {
            Title = title;
            Rows = rows;
        }

        public string Title { get; }

        public IReadOnlyList<ActivityRow> Rows { get; }
    }

    internal sealed class ActivityReport
    {
        public ActivityReport(string title, ActivityTone verdictTone, string verdict, IReadOnlyList<ActivitySection> sections)
        {
            Title = title;
            VerdictTone = verdictTone;
            Verdict = verdict;
            Sections = sections;
        }

        public string Title { get; }

        public ActivityTone VerdictTone { get; }

        public string Verdict { get; }

        public IReadOnlyList<ActivitySection> Sections { get; }
    }

    /// <summary>
    /// Turns command-host output into a plain-language report. Text the command reports as a
    /// failure, warning or hint always reaches the report, so nothing actionable is hidden.
    /// </summary>
    internal static class ActivityReportBuilder
    {
        public static ActivityReport Build(string arguments, CommandResult result, CommandResult inspectResult)
        {
            var command = FirstWord(arguments);
            var summary = InspectSummary.Parse(inspectResult.StandardOutput);
            var sections = new List<ActivitySection>();

            var outcomeRows = ReadOutcomeRows(result);
            outcomeRows.InsertRange(0, ReadCommandRows(command, result));
            if (outcomeRows.Count > 0)
            {
                sections.Add(new ActivitySection("RESULT", outcomeRows));
            }

            if (ShowsInstallState(command))
            {
                sections.AddRange(BuildInstallSections(summary));
            }

            ActivityTone verdictTone;
            var verdict = ResolveVerdict(command, result, summary, out verdictTone);
            return new ActivityReport(ResolveTitle(command), verdictTone, verdict, sections);
        }

        public static ActivityReport BuildFailure(string arguments, string message)
        {
            var rows = new[] { new ActivityRow(ActivityTone.Bad, message) };
            return new ActivityReport(
                ResolveTitle(FirstWord(arguments)),
                ActivityTone.Bad,
                "Could not finish",
                new[] { new ActivitySection("RESULT", rows) });
        }

        public static ActivityReport BuildProgress(string arguments, string busyMessage, bool needsPermission)
        {
            var rows = new List<ActivityRow> { new ActivityRow(ActivityTone.Info, busyMessage + "...") };
            if (needsPermission)
            {
                rows.Add(new ActivityRow(ActivityTone.Warning, "Accept the Windows permission prompt if it appears."));
            }

            return new ActivityReport(
                ResolveTitle(FirstWord(arguments)),
                ActivityTone.Info,
                "Working",
                new[] { new ActivitySection("RESULT", rows) });
        }

        public static ActivityReport BuildCanceled(string arguments)
        {
            var rows = new[] { new ActivityRow(ActivityTone.Info, "Nothing was changed. Windows permission is needed to continue.") };
            return new ActivityReport(
                ResolveTitle(FirstWord(arguments)),
                ActivityTone.Warning,
                "Canceled",
                new[] { new ActivitySection("RESULT", rows) });
        }

        private static string FirstWord(string arguments)
        {
            var words = (arguments ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Length == 0 ? string.Empty : words[0].ToLowerInvariant();
        }

        private static bool ShowsInstallState(string command)
        {
            return command == "inspect" || command == "patch" || command == "restore";
        }

        private static string ResolveTitle(string command)
        {
            switch (command)
            {
                case "inspect":
                    return "Status";
                case "patch":
                    return "Patch";
                case "restore":
                    return "Restore";
                case "verify":
                    return "Launch check";
                case "viewport-diagnose":
                    return "Display check";
                case "add-account":
                    return "Add account";
                case "auto-patch-enable":
                case "auto-patch-disable":
                    return "Auto-patch";
                default:
                    return "Account launch";
            }
        }

        private static string ResolveVerdict(string command, CommandResult result, InspectSummary summary, out ActivityTone tone)
        {
            if (!result.Success)
            {
                tone = command == "verify" || command == "viewport-diagnose" ? ActivityTone.Warning : ActivityTone.Bad;
                return command == "verify" || command == "viewport-diagnose" ? "Not confirmed yet" : "Something went wrong";
            }

            tone = ActivityTone.Good;
            switch (command)
            {
                case "inspect":
                    if (!summary.IsCompatible)
                    {
                        tone = ActivityTone.Bad;
                        return "This Play Games version is not supported yet";
                    }

                    if (!summary.IsPatched)
                    {
                        tone = ActivityTone.Warning;
                        return "Not patched";
                    }

                    return "All good";
                case "patch":
                    return "Patch applied";
                case "restore":
                    return "Original files restored";
                case "verify":
                    return "Game is using your resolution";
                case "auto-patch-enable":
                    return "Auto-patch is on";
                case "auto-patch-disable":
                    return "Auto-patch is off";
                default:
                    return "Done";
            }
        }

        private static List<ActivityRow> ReadCommandRows(string command, CommandResult result)
        {
            var rows = new List<ActivityRow>();
            if (command == "verify")
            {
                AddValueRow(rows, result, "expected launch size:", "Target size");
                AddValueRow(rows, result, "latest launch size:", "Last launch size");
            }
            else if (command == "viewport-diagnose")
            {
                AddValueRow(rows, result, "target backing surface:", "Target size");
                AddValueRow(rows, result, "live crosvm window:", "Game window");
            }
            else if (command == "add-account" && result.Success)
            {
                rows.Add(new ActivityRow(ActivityTone.Good, "Finish signing in inside Google Play Games."));
            }
            else if (command == "patch" && result.Success)
            {
                rows.Add(new ActivityRow(ActivityTone.Info, "Launch the game, then press Verify."));
            }

            return rows;
        }

        private static void AddValueRow(List<ActivityRow> rows, CommandResult result, string prefix, string label)
        {
            var line = OutputLines(result).FirstOrDefault(candidate => candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (line == null)
            {
                return;
            }

            var value = line.Substring(prefix.Length).Trim();
            var found = !string.Equals(value, "(not found)", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "not found", StringComparison.OrdinalIgnoreCase);
            rows.Add(new ActivityRow(found ? ActivityTone.Info : ActivityTone.Warning, label, found ? value : "Not seen yet"));
        }

        private static List<ActivityRow> ReadOutcomeRows(CommandResult result)
        {
            var rows = new List<ActivityRow>();
            foreach (var line in OutputLines(result))
            {
                AddPrefixedRow(rows, line, "PASS:", ActivityTone.Good);
                AddPrefixedRow(rows, line, "WARN:", ActivityTone.Warning);
                AddPrefixedRow(rows, line, "FAIL:", ActivityTone.Bad);
                AddPrefixedRow(rows, line, "Hint:", ActivityTone.Info);
                AddPrefixedRow(rows, line, "error:", ActivityTone.Bad);
            }

            // Failure text without a known prefix must still be shown.
            var unprefixedErrors = Lines(result.StandardError)
                .Where(line => !line.StartsWith("error:", StringComparison.OrdinalIgnoreCase));
            rows.AddRange(unprefixedErrors.Select(line => new ActivityRow(ActivityTone.Bad, line)));
            return rows;
        }

        private static void AddPrefixedRow(List<ActivityRow> rows, string line, string prefix, ActivityTone tone)
        {
            if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            rows.Add(new ActivityRow(tone, Sentence(line.Substring(prefix.Length))));
        }

        private static string Sentence(string text)
        {
            var trimmed = text.Trim().Replace("viewport-diagnose", "Diagnose").Replace("rerun verify", "press Verify again");
            return trimmed.Length == 0 ? trimmed : char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
        }

        private static IEnumerable<string> OutputLines(CommandResult result)
        {
            return Lines(result.StandardOutput).Concat(Lines(result.StandardError));
        }

        private static IEnumerable<string> Lines(string text)
        {
            return (text ?? string.Empty)
                .Replace("\r\n", "\n")
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0);
        }

        private static IEnumerable<ActivitySection> BuildInstallSections(InspectSummary summary)
        {
            var playGames = new List<ActivityRow>
            {
                new ActivityRow(ActivityTone.Info, "Version", summary.Version ?? "Unknown"),
            };
            if (summary.IsCompatible)
            {
                playGames.Add(new ActivityRow(ActivityTone.Good, "Support", "Supported"));
            }
            else
            {
                playGames.Add(new ActivityRow(
                    ActivityTone.Bad,
                    string.IsNullOrWhiteSpace(summary.CompatibilityMessage)
                        ? "This Play Games version could not be checked."
                        : summary.CompatibilityMessage));
            }

            yield return new ActivitySection("PLAY GAMES", playGames);

            yield return new ActivitySection("PATCH", new[]
            {
                new ActivityRow(summary.IsPatched ? ActivityTone.Good : ActivityTone.Warning, "Patch", summary.IsPatched ? "Installed" : "Not installed"),
                new ActivityRow(summary.HasBackup ? ActivityTone.Good : ActivityTone.Info, "Backup", summary.HasBackup ? "Ready" : "None yet"),
                new ActivityRow(ActivityTone.Info, "Target resolution", summary.TargetResolution ?? "Unknown"),
            });

            yield return new ActivitySection("FEATURES", new[]
            {
                FeatureRow("Custom resolution", summary.AvailableSettingsHook, summary.LaunchSettingsHook, summary.MonitorDisplayHook, summary.RuntimeDisplayHook),
                FeatureRow("High-resolution display", summary.VirtualGuestDisplayHook, summary.ShowWindowRequestHook),
                FeatureRow("Sharper image", summary.SharpeningFilterHook),
                FeatureRow("Unlimited accounts", summary.AccountLimitBypassHook),
                FeatureRow("Add account button", summary.AddAccountDeepLinkHook),
                FeatureRow("Open a specific account", summary.ExactInstanceLaunchHook),
            });

            yield return new ActivitySection(
                "LAST GAME LAUNCH",
                summary.DisplaySize == null
                    ? new[] { new ActivityRow(ActivityTone.Info, "No launch yet since Play Games restarted.") }
                    : new[]
                    {
                        new ActivityRow(ActivityTone.Info, "Game size", summary.DisplaySize),
                        new ActivityRow(ActivityTone.Info, "Density", summary.Density ?? "Unknown"),
                    });
        }

        private static ActivityRow FeatureRow(string name, params string[] hooks)
        {
            var enabled = hooks.All(InspectSummary.IsTruthy);
            return new ActivityRow(enabled ? ActivityTone.Good : ActivityTone.Warning, name, enabled ? "On" : "Off");
        }
    }
}
