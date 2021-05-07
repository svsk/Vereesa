using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Moq;
using Vereesa.Core.Infrastructure;
using Vereesa.Core.Services;
using Vereesa.Data.Interfaces;
using Xunit;

namespace Vereesa.Core.Tests
{

	public class AttendanceServiceTests
	{
		[Fact]
		public void UpdateAttendance_ClockHitsUtcNoon_AttendanceUpdatedCorrectly()
		{
			// Arrange
			var discord = new Mock<DiscordSocketClient>();
			var jobScheduleMock = new Mock<IJobScheduler>();
			var attRepo = new Mock<IRepository<RaidAttendance>>();
			var summRepo = new Mock<IRepository<RaidAttendanceSummary>>();
			var usersChars = new Mock<IRepository<UsersCharacters>>();
			var logger = new Mock<ILogger<AttendanceService>>();
			var addedRaids = 0;

			attRepo.Setup(r => r.AddOrEditAsync(It.IsAny<RaidAttendance>()))
				.Callback(() => { addedRaids++; })
				.Returns(Task.CompletedTask);

			var target = new AttendanceService(discord.Object,
				jobScheduleMock.Object,
				attRepo.Object,
				summRepo.Object,
				usersChars.Object,
				logger.Object
			);

			// Act
			jobScheduleMock.Raise(c => c.EveryDayAtUtcNoon += null);

			// Assert
			Assert.True(addedRaids > 0);
		}
	}
}