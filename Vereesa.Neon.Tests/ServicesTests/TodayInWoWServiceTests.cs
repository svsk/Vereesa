using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Vereesa.Neon.Data.Models;
using Vereesa.Neon.Data.Models.Wowhead;
using Vereesa.Neon.Exceptions;
using Vereesa.Neon.Integrations.Interfaces;
using Vereesa.Neon.Services;
using Vereesa.Neon.Tests.TestResources;
using Xunit;

namespace Vereesa.Neon.Tests.ServicesTests;

public class TodayInWoWServiceTests
{
    [Fact]
    public async Task AddHuntSubscription_AddingHuntSubscription_SubscriptionAdded()
    {
        // Arrange
        var wowhead = new Mock<IWowheadClient>();
        var huntSubRepo = new InMemoryRepository<GrandHuntSubscription>();
        var stormSubRepo = new InMemoryRepository<ElementalStormSubscription>();
        var echoesRepo = new InMemoryRepository<RadiantEchoesSubscription>();
        var logger = new Mock<ILogger<TodayInWoWService>>();

        var target = new TodayInWoWService(wowhead.Object, stormSubRepo, huntSubRepo, echoesRepo, logger.Object);

        // Act
        await target.AddHuntSubscription(1, WoWZone.TheWakingShores);

        // Assert
        var subscription = huntSubRepo.GetAll().FirstOrDefault();
        Assert.NotNull(subscription);
    }

    [Fact]
    public async Task AddStormSubscription_WhereSubscriptionAlreadyExists_ThrowsAlreadySubscribedException()
    {
        // Arrange
        var wowhead = new Mock<IWowheadClient>();
        var huntSubRepo = new InMemoryRepository<GrandHuntSubscription>();
        var stormSubRepo = new InMemoryRepository<ElementalStormSubscription>();
        var echoesRepo = new InMemoryRepository<RadiantEchoesSubscription>();
        var logger = new Mock<ILogger<TodayInWoWService>>();

        var target = new TodayInWoWService(wowhead.Object, stormSubRepo, huntSubRepo, echoesRepo, logger.Object);

        await target.AddStormSubscription(1, ElementalStormType.Snowstorm, WoWZone.TheWakingShores);

        // Act & Assert
        await Assert.ThrowsAsync<AlreadySubscribedException>(
            () => target.AddStormSubscription(1, ElementalStormType.Snowstorm, WoWZone.TheWakingShores)
        );
    }

    [Fact]
    public async Task AddHuntSubscription_WhereSubscriptionAlreadyExists_ThrowsAlreadySubscribedException()
    {
        // Arrange
        var wowhead = new Mock<IWowheadClient>();
        var huntSubRepo = new InMemoryRepository<GrandHuntSubscription>();
        var stormSubRepo = new InMemoryRepository<ElementalStormSubscription>();
        var echoesRepo = new InMemoryRepository<RadiantEchoesSubscription>();
        var logger = new Mock<ILogger<TodayInWoWService>>();

        var target = new TodayInWoWService(wowhead.Object, stormSubRepo, huntSubRepo, echoesRepo, logger.Object);

        await target.AddHuntSubscription(1, WoWZone.TheWakingShores);

        // Act & Assert
        await Assert.ThrowsAsync<AlreadySubscribedException>(
            () => target.AddHuntSubscription(1, WoWZone.TheWakingShores)
        );
    }

    [Fact]
    public async Task RemoveHuntSubscription_WhereNoSubscriptionExists_ThrowsNotSubscribedException()
    {
        // Arrange
        var wowhead = new Mock<IWowheadClient>();
        var huntSubRepo = new InMemoryRepository<GrandHuntSubscription>();
        var stormSubRepo = new InMemoryRepository<ElementalStormSubscription>();
        var echoesRepo = new InMemoryRepository<RadiantEchoesSubscription>();
        var logger = new Mock<ILogger<TodayInWoWService>>();

        var target = new TodayInWoWService(wowhead.Object, stormSubRepo, huntSubRepo, echoesRepo, logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotSubscribedException>(
            () => target.RemoveHuntSubscription(1, WoWZone.TheWakingShores)
        );
    }

    [Fact]
    public async Task RemoveStormSubscription_WhereNoSubscriptionExists_ThrowsNotSubscribedException()
    {
        // Arrange
        var wowhead = new Mock<IWowheadClient>();
        var huntSubRepo = new InMemoryRepository<GrandHuntSubscription>();
        var stormSubRepo = new InMemoryRepository<ElementalStormSubscription>();
        var echoesRepo = new InMemoryRepository<RadiantEchoesSubscription>();
        var logger = new Mock<ILogger<TodayInWoWService>>();

        var target = new TodayInWoWService(wowhead.Object, stormSubRepo, huntSubRepo, echoesRepo, logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotSubscribedException>(
            () => target.RemoveStormSubscription(1, ElementalStormType.Snowstorm, WoWZone.TheWakingShores)
        );
    }

    [Fact]
    public async Task RemoveStormSubscription_WhereSubscriptionExists_SubscriptionRemoved()
    {
        // Arrange
        var wowhead = new Mock<IWowheadClient>();
        var huntSubRepo = new InMemoryRepository<GrandHuntSubscription>();
        var stormSubRepo = new InMemoryRepository<ElementalStormSubscription>();
        var echoesRepo = new InMemoryRepository<RadiantEchoesSubscription>();
        var logger = new Mock<ILogger<TodayInWoWService>>();

        var target = new TodayInWoWService(wowhead.Object, stormSubRepo, huntSubRepo, echoesRepo, logger.Object);

        await target.AddStormSubscription(1, ElementalStormType.Snowstorm, WoWZone.TheWakingShores);

        // Act
        await target.RemoveStormSubscription(1, ElementalStormType.Snowstorm, WoWZone.TheWakingShores);

        // Assert
        var subscription = stormSubRepo.GetAll().FirstOrDefault();
        Assert.Null(subscription);
    }

    [Fact]
    public async Task RemoveHuntSubscription_WhereSubscriptionExists_SubscriptionRemoved()
    {
        // Arrange
        var wowhead = new Mock<IWowheadClient>();
        var huntSubRepo = new InMemoryRepository<GrandHuntSubscription>();
        var stormSubRepo = new InMemoryRepository<ElementalStormSubscription>();
        var echoesRepo = new InMemoryRepository<RadiantEchoesSubscription>();
        var logger = new Mock<ILogger<TodayInWoWService>>();

        var target = new TodayInWoWService(wowhead.Object, stormSubRepo, huntSubRepo, echoesRepo, logger.Object);

        await target.AddHuntSubscription(1, WoWZone.TheWakingShores);

        // Act
        await target.RemoveHuntSubscription(1, WoWZone.TheWakingShores);

        // Assert
        var subscription = huntSubRepo.GetAll().FirstOrDefault();
        Assert.Null(subscription);
    }
}
