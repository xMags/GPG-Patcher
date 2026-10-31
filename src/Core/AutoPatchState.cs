using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GpgPatcher
{
    /// <summary>
    /// What auto-patch remembers between runs. The resolution and fallback choice live here
    /// because a Play Games update replaces the folder that records what was applied.
    /// </summary>
    internal sealed class AutoPatchState
    {
        public bool Enabled { get; set; }

        /// <summary>The last version auto-patch finished with, whatever the outcome.</summary>
        public string HandledVersion { get; set; }

        public string Resolution { get; set; }

        public bool PhenotypeFallback { get; set; }

        public string LastResult { get; set; }

        public int FailedAttempts { get; set; }
    }

    internal static class AutoPatchStateStore
    {
        public static string DefaultPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    GpgConstants.AppDataDirectoryName,
                    "auto-patch-state.txt");
            }
        }

        public static AutoPatchState Read()
        {
            return Read(DefaultPath);
        }

        public static void Write(AutoPatchState state)
        {
            Write(DefaultPath, state);
        }

        public static AutoPatchState Read(string path)
        {
            var state = new AutoPatchState();
            if (!File.Exists(path))
            {
                return state;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(path))
            {
                var separator = line.IndexOf('=');
                if (separator > 0)
                {
                    values[line.Substring(0, separator)] = line.Substring(separator + 1);
                }
            }

            state.Enabled = ReadBool(values, nameof(AutoPatchState.Enabled));
            state.HandledVersion = ReadString(values, nameof(AutoPatchState.HandledVersion));
            state.Resolution = ReadString(values, nameof(AutoPatchState.Resolution));
            state.PhenotypeFallback = ReadBool(values, nameof(AutoPatchState.PhenotypeFallback));
            state.LastResult = ReadString(values, nameof(AutoPatchState.LastResult));

            int attempts;
            state.FailedAttempts = int.TryParse(ReadString(values, nameof(AutoPatchState.FailedAttempts)), out attempts)
                ? attempts
                : 0;
            return state;
        }

        public static void Write(string path, AutoPatchState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var builder = new StringBuilder();
            AppendValue(builder, nameof(AutoPatchState.Enabled), state.Enabled.ToString());
            AppendValue(builder, nameof(AutoPatchState.HandledVersion), state.HandledVersion);
            AppendValue(builder, nameof(AutoPatchState.Resolution), state.Resolution);
            AppendValue(builder, nameof(AutoPatchState.PhenotypeFallback), state.PhenotypeFallback.ToString());
            AppendValue(builder, nameof(AutoPatchState.LastResult), state.LastResult);
            AppendValue(builder, nameof(AutoPatchState.FailedAttempts), state.FailedAttempts.ToString());

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Written through a temporary file so a reader never sees half a state.
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, builder.ToString());
            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
        }

        private static void AppendValue(StringBuilder builder, string name, string value)
        {
            var singleLine = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
            builder.Append(name).Append('=').Append(singleLine).Append(Environment.NewLine);
        }

        private static string ReadString(Dictionary<string, string> values, string name)
        {
            string value;
            return values.TryGetValue(name, out value) && value.Length > 0 ? value : null;
        }

        private static bool ReadBool(Dictionary<string, string> values, string name)
        {
            return string.Equals(ReadString(values, name), bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }
    }
}
