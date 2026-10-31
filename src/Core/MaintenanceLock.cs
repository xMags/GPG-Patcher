using System;
using System.Threading;

namespace GpgPatcher
{
    /// <summary>Stops the app and the watcher from patching or restoring at the same time.</summary>
    internal sealed class MaintenanceLock : IDisposable
    {
        private const string MutexName = @"Global\GpgPatcher.Maintenance";
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromMinutes(2);

        private readonly Mutex mutex;

        private MaintenanceLock(Mutex mutex)
        {
            this.mutex = mutex;
        }

        public static MaintenanceLock Acquire()
        {
            var mutex = new Mutex(false, MutexName);
            try
            {
                if (!mutex.WaitOne(WaitTimeout))
                {
                    throw new FriendlyException("Another patch or restore is still running. Try again in a moment.");
                }
            }
            catch (AbandonedMutexException)
            {
                // The previous owner crashed. The mutex is ours now, and each step is repeatable.
            }
            catch (FriendlyException)
            {
                mutex.Dispose();
                throw;
            }

            return new MaintenanceLock(mutex);
        }

        public void Dispose()
        {
            mutex.ReleaseMutex();
            mutex.Dispose();
        }
    }
}
