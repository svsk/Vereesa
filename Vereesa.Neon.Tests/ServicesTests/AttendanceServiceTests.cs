using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Vereesa.Neon.Services;
using Vereesa.Neon.Data.Interfaces;
using Xunit;

namespace Vereesa.Neon.Tests.ServicesTests
{
    public class AttendanceServiceTests
    {
        [Fact]
        public async Task UpdateAttendance_ClockHitsUtcNoon_AttendanceUpdatedCorrectly()
        {
            // Arrange
            var repository = new Mock<IRepository<RaidAttendance>>();
            var raidAttendanceSummary = new Mock<IRepository<RaidAttendanceSummary>>();
            var usersCharacters = new Mock<IRepository<UsersCharacters>>();
            var logger = new Mock<ILogger<AttendanceService>>();

            var target = new AttendanceService(
                repository.Object,
                raidAttendanceSummary.Object,
                usersCharacters.Object,
                logger.Object
            );

            var addedRaids = 0;

            repository
                .Setup(r => r.AddOrEditAsync(It.IsAny<RaidAttendance>()))
                .Callback(() =>
                {
                    addedRaids++;
                })
                .Returns(Task.CompletedTask);

            // Act
            await target.UpdateAttendanceAsync(false);

            // Assert
            Assert.True(addedRaids > 0);
        }
    }
}
