using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;

namespace GpgPatcher
{
    /// <summary>Thin wrapper over schtasks.exe. Registering and running these tasks needs elevation.</summary>
    internal static class ScheduledTaskClient
    {
        private const string SchemaNamespace = "http://schemas.microsoft.com/windows/2004/02/mit/task";

        public static string BuildWatcherTaskXml(string userName, string executablePath)
        {
            return BuildTaskXml(
                "<Triggers><LogonTrigger><Enabled>true</Enabled><UserId>" + SecurityElement.Escape(userName) + "</UserId></LogonTrigger></Triggers>",
                userName,
                "HighestAvailable",
                executablePath,
                "--headless auto-patch-watch",
                Path.GetDirectoryName(executablePath),
                "<RestartOnFailure><Interval>PT1M</Interval><Count>3</Count></RestartOnFailure>");
        }

        public static string BuildUserLevelStartTaskXml(string userName, string executablePath, string arguments)
        {
            return BuildTaskXml(
                "<Triggers />",
                userName,
                "LeastPrivilege",
                executablePath,
                arguments,
                Path.GetDirectoryName(executablePath),
                string.Empty);
        }

        public static void Register(string taskName, string xml)
        {
            var xmlPath = Path.Combine(Path.GetTempPath(), "gpg-patcher-task-" + Guid.NewGuid().ToString("N") + ".xml");
            File.WriteAllText(xmlPath, xml, Encoding.Unicode);
            try
            {
                Run("/Create /F /TN " + Quote(taskName) + " /XML " + Quote(xmlPath));
            }
            finally
            {
                File.Delete(xmlPath);
            }
        }

        public static void Start(string taskName)
        {
            Run("/Run /TN " + Quote(taskName));
        }

        public static void TryEnd(string taskName)
        {
            TryRun("/End /TN " + Quote(taskName));
        }

        public static void TryDelete(string taskName)
        {
            TryRun("/Delete /F /TN " + Quote(taskName));
        }

        private static string BuildTaskXml(
            string triggers,
            string userName,
            string runLevel,
            string executablePath,
            string arguments,
            string workingDirectory,
            string extraSettings)
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-16\"?>"
                + "<Task version=\"1.4\" xmlns=\"" + SchemaNamespace + "\">"
                + "<RegistrationInfo><Description>GPG Patcher</Description></RegistrationInfo>"
                + triggers
                + "<Principals><Principal id=\"Author\">"
                + "<UserId>" + SecurityElement.Escape(userName) + "</UserId>"
                + "<LogonType>InteractiveToken</LogonType>"
                + "<RunLevel>" + runLevel + "</RunLevel>"
                + "</Principal></Principals>"
                + "<Settings>"
                + "<MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>"
                + "<DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>"
                + "<StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>"
                + "<StartWhenAvailable>true</StartWhenAvailable>"
                + "<ExecutionTimeLimit>PT0S</ExecutionTimeLimit>"
                + extraSettings
                + "</Settings>"
                + "<Actions Context=\"Author\"><Exec>"
                + "<Command>" + SecurityElement.Escape(executablePath) + "</Command>"
                + "<Arguments>" + SecurityElement.Escape(arguments) + "</Arguments>"
                + "<WorkingDirectory>" + SecurityElement.Escape(workingDirectory) + "</WorkingDirectory>"
                + "</Exec></Actions>"
                + "</Task>";
        }

        private static void Run(string arguments)
        {
            string error;
            if (!TryRun(arguments, out error))
            {
                throw new FriendlyException("Windows Task Scheduler refused the request: " + error);
            }
        }

        private static void TryRun(string arguments)
        {
            string error;
            TryRun(arguments, out error);
        }

        private static bool TryRun(string arguments, out string error)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                process.Start();
                process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }
    }
}
