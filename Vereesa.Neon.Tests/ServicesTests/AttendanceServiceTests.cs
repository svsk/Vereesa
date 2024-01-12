using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Services;
using Vereesa.Neon.Data.Interfaces;
using Xunit;

namespace Vereesa.Core.Tests
{
    public class AttendanceServiceTests
    {
        [Fact]
        public void UpdateAttendance_ClockHitsUtcNoon_AttendanceUpdatedCorrectly()
        {
            // Arrange
            var messagingMock = new Mock<IMessagingClient>();
            var jobScheduleMock = new Mock<IJobScheduler>();
            var attRepo = new Mock<IRepository<RaidAttendance>>();
            var summRepo = new Mock<IRepository<RaidAttendanceSummary>>();
            var usersChars = new Mock<IRepository<UsersCharacters>>();
            var logger = new Mock<ILogger<AttendanceService>>();
            var addedRaids = 0;

            attRepo
                .Setup(r => r.AddOrEditAsync(It.IsAny<RaidAttendance>()))
                .Callback(() =>
                {
                    addedRaids++;
                })
                .Returns(Task.CompletedTask);

            var target = new AttendanceService(
                messagingMock.Object,
                jobScheduleMock.Object,
                attRepo.Object,
                summRepo.Object,
                usersChars.Object
            );

            // Act
            jobScheduleMock.Raise(c => c.EveryDayAtUtcNoon += null);

            // Assert
            Assert.True(addedRaids > 0);
        }
    }
}
