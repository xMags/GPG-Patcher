using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace GpgPatcher.Gui
{
    /// <summary>Shows a Windows notification from a process that has no window.</summary>
    internal static class BalloonNotifier
    {
        private static readonly TimeSpan DisplayTime = TimeSpan.FromSeconds(6);

        public static void Show(string title, string message)
        {
            var icon = BrandingAssets.TryLoadApplicationIcon();
            try
            {
                using (var notifyIcon = new NotifyIcon())
                {
                    notifyIcon.Icon = icon ?? SystemIcons.Application;
                    notifyIcon.Text = "GPG Patcher";
                    notifyIcon.Visible = true;
                    notifyIcon.ShowBalloonTip((int)DisplayTime.TotalMilliseconds, title, message, ToolTipIcon.None);

                    // The icon needs a pumping message loop, and removing it early hides the toast.
                    var timer = Stopwatch.StartNew();
                    while (timer.Elapsed < DisplayTime)
                    {
                        Application.DoEvents();
                        Thread.Sleep(50);
                    }

                    notifyIcon.Visible = false;
                }
            }
            finally
            {
                if (icon != null)
                {
                    icon.Dispose();
                }
            }
        }
    }
}
