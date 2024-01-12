using System.Timers;
using Timer = System.Timers.Timer;

namespace Vereesa.Neon.Helpers
{
    public class TimerHelpers
    {
        public static Timer SetTimeout(
            Action predicate,
            int interval,
            bool autoRefresh = false,
            bool runImmediately = false
        )
        {
            var timer = new Timer(interval);
            timer.Elapsed += (object sender, ElapsedEventArgs args) =>
            {
                predicate.Invoke();
            };
            timer.AutoReset = autoRefresh;
            timer.Start();

            if (runImmediately)
                predicate.Invoke();

            return timer;
        }

        public static async Task<Timer> SetTimeoutAsync(
            Func<Task> predicate,
            int interval,
            bool autoRefresh = false,
            bool runImmediately = false
        )
        {
            var timer = new Timer(interval);
            timer.Elapsed += async (object sender, ElapsedEventArgs args) =>
            {
                await predicate.Invoke();
            };
            timer.AutoReset = autoRefresh;
            timer.Start();

            if (runImmediately)
                await predicate.Invoke();

            return timer;
        }
    }
}
