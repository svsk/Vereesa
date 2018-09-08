using System;
using System.Timers;

namespace Vereesa.Core.Helpers
{
    public class TimerHelpers
    {
        public static Timer SetTimeout(Action predicate, int interval, bool autoRefresh = false, bool runImmediately = false) 
        {
            var timer = new Timer(interval);
            timer.Elapsed += (object sender, ElapsedEventArgs args) => { predicate.Invoke(); };
            timer.AutoReset = autoRefresh;
            timer.Start();

            if (runImmediately)
                predicate.Invoke();

            return timer;
        }
    }
}