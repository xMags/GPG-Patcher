using System;

namespace GpgPatcher
{
    internal enum AutoPatchAction
    {
        None,
        RecordAlreadyPatched,
        Patch,
        WaitForGameToClose,
        ReportUnsupported,
    }

    internal sealed class AutoPatchSnapshot
    {
        public string InstalledVersion { get; set; }

        public bool IsCompatible { get; set; }

        public bool IsPatched { get; set; }

        public bool GameWindowOpen { get; set; }
    }

    internal static class AutoPatchPolicy
    {
        public const int MaximumAttempts = 3;

        /// <summary>
        /// Acts only when the version differs from the last one handled. Without that rule the
        /// watcher would undo Restore seconds after the user pressed it.
        /// </summary>
        public static AutoPatchAction Decide(AutoPatchSnapshot snapshot, AutoPatchState state)
        {
            if (string.IsNullOrWhiteSpace(snapshot.InstalledVersion)
                || string.Equals(snapshot.InstalledVersion, state.HandledVersion, StringComparison.OrdinalIgnoreCase))
            {
                return AutoPatchAction.None;
            }

            if (snapshot.IsPatched)
            {
                return AutoPatchAction.RecordAlreadyPatched;
            }

            if (!snapshot.IsCompatible)
            {
                return AutoPatchAction.ReportUnsupported;
            }

            return snapshot.GameWindowOpen ? AutoPatchAction.WaitForGameToClose : AutoPatchAction.Patch;
        }
    }
}
