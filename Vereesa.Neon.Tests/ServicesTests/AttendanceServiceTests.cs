using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Vereesa.Neon.Services;
using Vereesa.Neon.Data.Interfaces;
using Xunit;
using Vereesa.Neon.Data.Models.Attendance;
using Vereesa.Neon.Integrations;
using System.Collections.Generic;

namespace Vereesa.Neon.Tests.ServicesTests
{
    public class AttendanceServiceTests
    {
        [Fact]
        public async Task UpdateAttendance_ClockHitsUtcNoon_AttendanceUpdatedCorrectly()
        {
            // Arrange
            var attendanceRepo = new Mock<IRepository<RaidAttendance>>();
            var attendanceSummaryRepo = new Mock<IRepository<RaidAttendanceSummary>>();
            var usersCharacters = new Mock<IRepository<UsersCharacters>>();
            var warcraftLogsScraper = new Mock<IWarcraftLogsScraper>();
            var logger = new Mock<ILogger<AttendanceService>>();

            // Setup scraper to return attendance from GetAttendanceFromWarcraftLogs
            warcraftLogsScraper
                .Setup(s => s.GetAttendanceFromWarcraftLogs(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(new List<RaidAttendance> { new("1", 0, "ZoneName", "ZoneId", "LogUrl") });

            var target = new AttendanceService(
                attendanceRepo.Object,
                attendanceSummaryRepo.Object,
                usersCharacters.Object,
                warcraftLogsScraper.Object,
                logger.Object
            );

            var addedRaids = 0;

            attendanceRepo
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
