using Microsoft.Extensions.Logging;
using Moq;
using Vereesa.Neon.Integrations;
using Xunit;

namespace Vereesa.Neon.Tests.IntegrationTests;

public class WarcraftLogsScraperTests
{
    [Fact]
    public void GetAttendanceFromWarcraftLogs_WithCastleNathriaRaidId_DataIntegrityOK()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<WarcraftLogsScraper>>();
        var target = new WarcraftLogsScraper(loggerMock.Object);

        // Act
        var entries = target.GetAttendanceFromWarcraftLogs("31");

        // Assert
        Assert.NotNull(entries);
        Assert.NotEmpty(entries);
    }
}
