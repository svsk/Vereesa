using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Services;
using Vereesa.Neon.Data.Interfaces;
using Xunit;
using Discord;

namespace Vereesa.Core.Tests
{
    public class AttendanceServiceTests
    {
        [Fact]
        public async Task UpdateAttendance_ClockHitsUtcNoon_AttendanceUpdatedCorrectly()
        {
            // Arrange
            var messagingClient = new Mock<IMessagingClient>();
            var jobScheduler = new Mock<IJobScheduler>();
            var repository = new Mock<IRepository<RaidAttendance>>();
            var raidAttendanceSummary = new Mock<IRepository<RaidAttendanceSummary>>();
            var usersCharacters = new Mock<IRepository<UsersCharacters>>();
            var logger = new Mock<ILogger<AttendanceService>>();

            var target = new AttendanceService(
                messagingClient.Object,
                jobScheduler.Object,
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

            var messageMock = new Mock<IMessage>();
            messageMock
                .Setup(
                    m =>
                        m.Channel.SendMessageAsync(
                            It.IsAny<string>(),
                            false,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            MessageFlags.None
                        )
                )
                .Returns(Task.FromResult(new Mock<IUserMessage>().Object));

            // Act
            await target.ForceAttendanceUpdate(messageMock.Object);

            // Assert
            Assert.True(addedRaids > 0);
        }
    }
}
