using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;

namespace GpgPatcher
{
    /// <summary>
    /// Installs the watcher into Program Files before registering it. An admin task must never
    /// run a file ordinary programs can replace, because that would hand them admin rights.
    /// </summary>
    internal static class AutoPatchInstaller
    {
        public const string WatcherTaskName = @"\GPG Patcher\AutoPatch";
        private const string ExecutableName = "GPG Patcher.exe";
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

        public static string InstallDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "GPG Patcher");
            }
        }

        private static string InstalledExecutablePath
        {
            get { return Path.Combine(InstallDirectory, ExecutableName); }
        }

        public static bool IsEnabled()
        {
            return AutoPatchStateStore.Read().Enabled && File.Exists(InstalledExecutablePath);
        }

        public static void Enable(string installedVersion)
        {
            ScheduledTaskClient.TryEnd(WatcherTaskName);
            StopInstalledWatcher();
            CopyProgramFiles();

            ScheduledTaskClient.Register(
                WatcherTaskName,
                ScheduledTaskClient.BuildWatcherTaskXml(CurrentUserName(), InstalledExecutablePath));

            // The current version counts as handled: only a later update should trigger a patch.
            var state = AutoPatchStateStore.Read();
            state.Enabled = true;
            state.HandledVersion = installedVersion;
            state.FailedAttempts = 0;
            state.LastResult = "Watching for updates";
            AutoPatchStateStore.Write(state);

            ScheduledTaskClient.Start(WatcherTaskName);
        }

        public static void Disable()
        {
            ScheduledTaskClient.TryEnd(WatcherTaskName);
            ScheduledTaskClient.TryDelete(WatcherTaskName);
            StopInstalledWatcher();

            if (Directory.Exists(InstallDirectory))
            {
                Directory.Delete(InstallDirectory, true);
            }

            var state = AutoPatchStateStore.Read();
            state.Enabled = false;
            state.LastResult = null;
            AutoPatchStateStore.Write(state);
        }

        public static string CurrentUserName()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                return identity.Name;
            }
        }

        private static void CopyProgramFiles()
        {
            var source = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory)
                .TrimEnd(Path.DirectorySeparatorChar);
            if (string.Equals(source, InstallDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Directory.CreateDirectory(InstallDirectory);
            var programFiles = Directory.GetFiles(source)
                .Where(path => !string.Equals(Path.GetExtension(path), ".pdb", StringComparison.OrdinalIgnoreCase));
            foreach (var path in programFiles)
            {
                File.Copy(path, Path.Combine(InstallDirectory, Path.GetFileName(path)), true);
            }

            if (!File.Exists(InstalledExecutablePath))
            {
                throw new FriendlyException("GPG Patcher could not be copied to '" + InstallDirectory + "'.");
            }
        }

        private static void StopInstalledWatcher()
        {
            var target = InstalledExecutablePath;
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExecutableName)))
            {
                using (process)
                {
                    if (process.Id == Process.GetCurrentProcess().Id || !IsRunningFrom(process, target))
                    {
                        continue;
                    }

                    if (!process.WaitForExit((int)StopTimeout.TotalMilliseconds))
                    {
                        process.Kill();
                        process.WaitForExit((int)StopTimeout.TotalMilliseconds);
                    }
                }
            }

            // Windows can hold the file for a moment after the process has gone.
            Thread.Sleep(TimeSpan.FromMilliseconds(250));
        }

        private static bool IsRunningFrom(Process process, string path)
        {
            try
            {
                return process.MainModule != null
                    && string.Equals(process.MainModule.FileName, path, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                return false;
            }
        }
    }
}
