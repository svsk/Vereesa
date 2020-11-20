using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using NodaTime;

namespace Vereesa.Core.Infrastructure
{
	public class JobScheduler : IJobScheduler, IDisposable
	{
		private Timer _timer;
		private ConcurrentDictionary<long, List<Action>> _scheduledJobs = new ConcurrentDictionary<long, List<Action>>();

		public JobScheduler()
		{
			_timer = new Timer(1000);
			_timer.AutoReset = true;

			_timer.Elapsed += HandleSecondElapsed;

			_timer.Start();
		}

		public event Func<Task> EveryDayAtUtcNoon;
		public event Func<Task> EveryFullMinute;
		public event Func<Task> EveryHalfMinute;
		public event Func<Task> EveryTenSeconds;
		public event Func<Task> EverySecond;

		private void HandleSecondElapsed(object sender, ElapsedEventArgs e)
		{
			var utcNow = e.SignalTime.ToUniversalTime();

			if (utcNow.Hour == 12 && utcNow.Minute == 0 && utcNow.Second == 0)
			{
				EveryDayAtUtcNoon?.Invoke();
			}

			if (utcNow.Second == 0)
			{
				EveryFullMinute?.Invoke();
			}

			if (utcNow.Second % 30 == 0)
			{
				EveryHalfMinute?.Invoke();
			}

			if (utcNow.Second % 10 == 0)
			{
				EveryTenSeconds?.Invoke();
			}

			EverySecond?.Invoke();

			InvokeScheduledJobs();
		}

		private void InvokeScheduledJobs()
		{
			var currentInstant = SystemClock.Instance.GetCurrentInstant().ToUnixTimeSeconds();
			var expiredKeys = _scheduledJobs.Keys.Where(k => k < currentInstant);
			var actionsToExecute = new List<Action>();

			foreach (var key in expiredKeys)
			{
				_scheduledJobs.Remove(key, out var actions);
				actionsToExecute.AddRange(actions);
			}

			foreach (var action in actionsToExecute)
			{
				action.Invoke();
			}
		}

		public void Dispose()
		{
			_timer.Stop();
			_timer.Dispose();
		}

		public void Schedule(Instant jobExecutionTime, Action jobAction)
		{
			var jobKey = jobExecutionTime.ToUnixTimeSeconds();

			if (!_scheduledJobs.ContainsKey(jobKey))
			{
				_scheduledJobs.TryAdd(jobKey, new List<Action>());
			}

			_scheduledJobs[jobKey].Add(jobAction);
		}
	}


}