using System;
using System.Timers;

namespace Vereesa.Core.Helpers
{
    public class TimerHelpers
    {
        public static Timer SetTimeout(Action predicate, int interval) 
        {
            var timer = new Timer(interval);
            timer.Elapsed += (object sender, ElapsedEventArgs args) => { predicate.Invoke(); };
            timer.AutoReset = false;
            timer.Start();

            return timer;
        }
    }
}