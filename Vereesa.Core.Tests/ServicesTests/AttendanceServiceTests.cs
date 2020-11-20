using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Vereesa.Core.Infrastructure;
using Vereesa.Core.Services;
using Vereesa.Data.Interfaces;

namespace Vereesa.Core.Tests
{
	[TestClass]
	public class AttendanceServiceTests
	{
		[TestMethod]
		public void UpdateAttendance_ClockHitsUtcNoon_AttendanceUpdatedCorrectly()
		{
			// Arrange
			var discord = new Mock<DiscordSocketClient>();
			var jobScheduleMock = new Mock<IJobScheduler>();
			var attRepo = new Mock<IRepository<RaidAttendance>>();
			var summRepo = new Mock<IRepository<RaidAttendanceSummary>>();
			var logger = new Mock<ILogger<AttendanceService>>();
			var addedRaids = 0;

			attRepo.Setup(r => r.AddOrEditAsync(It.IsAny<RaidAttendance>()))
				.Callback(() => { addedRaids++; })
				.Returns(Task.CompletedTask);

			var target = new AttendanceService(discord.Object, jobScheduleMock.Object, attRepo.Object, summRepo.Object, logger.Object);

			// Act
			jobScheduleMock.Raise(c => c.EveryDayAtUtcNoon += null);

			// Assert
			Assert.IsTrue(addedRaids > 0);
		}
	}
}