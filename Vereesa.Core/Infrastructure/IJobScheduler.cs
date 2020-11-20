using System;
using System.Threading.Tasks;
using NodaTime;

namespace Vereesa.Core.Infrastructure
{
	public interface IJobScheduler
	{
		event Func<Task> EverySecond;
		event Func<Task> EveryTenSeconds;
		event Func<Task> EveryHalfMinute;
		event Func<Task> EveryFullMinute;
		event Func<Task> EveryDayAtUtcNoon;
		void Schedule(Instant jobExecutionTime, Action jobAction);
	}
}