using System;
using System.IO;
using System.Threading;

namespace GpgPatcher
{
    /// <summary>
    /// Long-running process started by the logon task. A folder event catches the moment an
    /// update is swapped in, and the timer covers anything the event misses. It holds no open
    /// handle inside Play Games' version folder, so it cannot block Google's updater.
    /// </summary>
    internal sealed class AutoPatchWatcher
    {
        private const string SingleInstanceName = @"Global\GpgPatcher.AutoPatchWatcher";
        private const long MaximumLogBytes = 256 * 1024;
        private static readonly TimeSpan IdleInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan BusyInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(20);

        private readonly Action<PlayGamesInstallLayout, PatchCommandOptions> patch;
        private readonly Action<string, string> notify;
        private readonly Func<PlayGamesInstallLayout> createLayout;
        private readonly Func<bool> isGameWindowOpen;
        private readonly string statePath;
        private readonly string logPath;

        public AutoPatchWatcher(
            Action<PlayGamesInstallLayout, PatchCommandOptions> patch,
            Action<string, string> notify)
            : this(patch, notify, PlayGamesInstallLayout.CreateDefault, ViewportDiagnostics.IsGameWindowOpen, AutoPatchStateStore.DefaultPath)
        {
        }

        internal AutoPatchWatcher(
            Action<PlayGamesInstallLayout, PatchCommandOptions> patch,
            Action<string, string> notify,
            Func<PlayGamesInstallLayout> createLayout,
            Func<bool> isGameWindowOpen,
            string statePath)
        {
            this.patch = patch;
            this.notify = notify;
            this.createLayout = createLayout;
            this.isGameWindowOpen = isGameWindowOpen;
            this.statePath = statePath;
            logPath = Path.Combine(Path.GetDirectoryName(statePath), "auto-patch.log");
        }

        public int Run()
        {
            bool isFirstInstance;
            using (var singleInstance = new Mutex(true, SingleInstanceName, out isFirstInstance))
            using (var folderChanged = new AutoResetEvent(false))
            using (var folderWatcher = CreateFolderWatcher(folderChanged))
            {
                if (!isFirstInstance)
                {
                    return 0;
                }

                Log("Watcher started");
                var retrySoon = EvaluateSafely();
                while (true)
                {
                    var woken = folderChanged.WaitOne(retrySoon ? BusyInterval : IdleInterval);
                    if (woken)
                    {
                        // The swap is several renames, and Play Games is still starting.
                        Thread.Sleep(SettleDelay);
                    }

                    retrySoon = EvaluateSafely();
                }
            }
        }

        private static FileSystemWatcher CreateFolderWatcher(AutoResetEvent folderChanged)
        {
            var root = PlayGamesInstallLayout.CreateDefault().InstallRoot;
            if (!Directory.Exists(root))
            {
                return null;
            }

            var watcher = new FileSystemWatcher(root)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.DirectoryName,
            };
            watcher.Renamed += (sender, args) => folderChanged.Set();
            watcher.Created += (sender, args) => folderChanged.Set();
            watcher.EnableRaisingEvents = true;
            return watcher;
        }

        /// <returns>True when another look is needed soon.</returns>
        internal bool EvaluateSafely()
        {
            try
            {
                return Evaluate();
            }
            catch (Exception ex)
            {
                // Files can be missing, locked or half-written during a swap, and this process
                // has nobody to report to, so any failure means "look again shortly".
                Log("Check skipped: " + ex.Message);
                return true;
            }
        }

        private bool Evaluate()
        {
            var layout = createLayout();
            var status = PatchStatusInspector.Inspect(layout);
            var state = AutoPatchStateStore.Read(statePath);
            var action = AutoPatchPolicy.Decide(
                new AutoPatchSnapshot
                {
                    InstalledVersion = status.InstalledVersion,
                    IsCompatible = status.IsCompatible,
                    IsPatched = status.IsPatched,
                    GameWindowOpen = isGameWindowOpen(),
                },
                state);

            switch (action)
            {
                case AutoPatchAction.RecordAlreadyPatched:
                    Finish(state, status.InstalledVersion, "Patched " + status.InstalledVersion);
                    return false;
                case AutoPatchAction.ReportUnsupported:
                    Finish(state, status.InstalledVersion, status.InstalledVersion + " not supported yet");
                    notify("Play Games update not supported yet", "Version " + status.InstalledVersion + " was left unpatched. It still works normally.");
                    return false;
                case AutoPatchAction.WaitForGameToClose:
                    return true;
                case AutoPatchAction.Patch:
                    return ApplyPatch(layout, status.InstalledVersion, state);
                default:
                    return false;
            }
        }

        private bool ApplyPatch(PlayGamesInstallLayout layout, string version, AutoPatchState state)
        {
            try
            {
                patch(layout, ResolveOptions(state));
            }
            catch (Exception ex)
            {
                // Counted and reported below; a crash here would stop all future auto-patching.
                return RecordFailure(version, ex);
            }

            // Patch records the handled version itself; only the summary is added here.
            var patched = AutoPatchStateStore.Read(statePath);
            Finish(patched, version, "Patched " + version);
            notify("Play Games updated and patched", "Version " + version + " is ready to play.");
            return false;
        }

        private bool RecordFailure(string version, Exception failure)
        {
            Log("Patch failed for " + version + ": " + failure.Message);
            var state = AutoPatchStateStore.Read(statePath);
            state.FailedAttempts++;
            if (state.FailedAttempts < AutoPatchPolicy.MaximumAttempts)
            {
                AutoPatchStateStore.Write(statePath, state);
                return true;
            }

            Finish(state, version, "Could not patch " + version);
            notify("Auto-patch failed", "Open GPG Patcher and press Patch. Version " + version + " is unpatched.");
            return false;
        }

        private void Finish(AutoPatchState state, string version, string result)
        {
            state.HandledVersion = version;
            state.LastResult = result;
            state.FailedAttempts = 0;
            AutoPatchStateStore.Write(statePath, state);
            Log(result);
        }

        private static PatchCommandOptions ResolveOptions(AutoPatchState state)
        {
            ResolutionProfile resolution;
            if (!ResolutionProfiles.TryParse(state.Resolution, out resolution))
            {
                resolution = ResolutionProfileStorage.ReadSelected();
            }

            return PatchCommandOptions.Create(resolution, state.PhenotypeFallback && resolution.IsUhd);
        }

        private void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                if (File.Exists(logPath) && new FileInfo(logPath).Length > MaximumLogBytes)
                {
                    File.Delete(logPath);
                }

                File.AppendAllText(logPath, DateTimeOffset.Now.ToString("u") + " " + message + Environment.NewLine);
            }
            catch (IOException)
            {
                // Logging must never stop the watcher.
            }
        }
    }
}
