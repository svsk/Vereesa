using Vereesa.Neon.Integrations.Interfaces;
using Vereesa.Neon.Data.Models;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Data.Models.Wowhead;
using Microsoft.Extensions.Logging;
using Vereesa.Neon.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Vereesa.Neon.Data.Repositories;
using Vereesa.Neon.Integrations;

namespace Vereesa.Neon.Services;

public static class TodayInWoWServiceExtensions
{
    public static IServiceCollection AddTodayInWoWService(this IServiceCollection services)
    {
        services.AddTransient<TodayInWoWService>();
        services.AddTransient<
            IRepository<ElementalStormSubscription>,
            AzureStorageRepository<ElementalStormSubscription>
        >();
        services.AddTransient<IRepository<GrandHuntSubscription>, AzureStorageRepository<GrandHuntSubscription>>();
        services.AddTransient<
            IRepository<RadiantEchoesSubscription>,
            AzureStorageRepository<RadiantEchoesSubscription>
        >();
        services.AddTransient<IWowheadClient, WowheadClient>();

        return services;
    }
}

public class TodayInWoWService
{
    private readonly IWowheadClient _wowhead;
    private readonly IRepository<ElementalStormSubscription> _subscriptionRepository;
    private readonly IRepository<GrandHuntSubscription> _huntSubscriptionRepository;
    private readonly IRepository<RadiantEchoesSubscription> _radiantEchoesSubscriptionRepository;
    private readonly ILogger<TodayInWoWService> _logger;

    private TimeSpan _warnThreshold = TimeSpan.FromMinutes(10);

    public TodayInWoWService(
        IWowheadClient wowhead,
        IRepository<ElementalStormSubscription> subscriptionRepository,
        IRepository<GrandHuntSubscription> huntSubscriptionRepository,
        IRepository<RadiantEchoesSubscription> radiantEchoesSubscriptionRepository,
        ILogger<TodayInWoWService> logger
    )
    {
        _wowhead = wowhead;
        _subscriptionRepository = subscriptionRepository;
        _huntSubscriptionRepository = huntSubscriptionRepository;
        _radiantEchoesSubscriptionRepository = radiantEchoesSubscriptionRepository;
        _logger = logger;
    }

    public async Task AddHuntSubscription(ulong userId, WoWZone wowZone)
    {
        var subscription = await GetHuntSubscription(userId, wowZone);
        if (subscription != null)
        {
            throw new AlreadySubscribedException();
        }

        subscription = new GrandHuntSubscription
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Zone = wowZone
        };

        await _huntSubscriptionRepository.AddAsync(subscription);
        await _huntSubscriptionRepository.SaveAsync();

        _logger.LogWarning("User {UserId} subscribed to Grand Hunts in {Zone}", userId, WoWZoneHelper.GetName(wowZone));
    }

    public async Task RemoveHuntSubscription(ulong userId, WoWZone wowZone)
    {
        var subscription = await GetHuntSubscription(userId, wowZone);
        if (subscription == null)
        {
            throw new NotSubscribedException();
        }

        await _huntSubscriptionRepository.DeleteAsync(subscription);
        await _huntSubscriptionRepository.SaveAsync();

        _logger.LogWarning(
            "User {UserId} unsubscribed from Grand Hunts in {Zone}",
            userId,
            WoWZoneHelper.GetName(wowZone)
        );
    }

    public async Task AddStormSubscription(ulong userId, ElementalStormType stormType, WoWZone wowZone)
    {
        var subscription = await GetStormSubscription(userId, stormType, wowZone);

        if (subscription != null)
        {
            throw new AlreadySubscribedException();
        }

        subscription = new ElementalStormSubscription
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Type = stormType,
            Zone = wowZone
        };

        await _subscriptionRepository.AddAsync(subscription);
        await _subscriptionRepository.SaveAsync();

        _logger.LogWarning(
            "User {UserId} subscribed to {StormType}s in {Zone}",
            userId,
            stormType,
            WoWZoneHelper.GetName(wowZone)
        );
    }

    public async Task RemoveStormSubscription(ulong userId, ElementalStormType stormType, WoWZone zone)
    {
        var subscription = await GetStormSubscription(userId, stormType, zone);
        if (subscription == null)
        {
            throw new NotSubscribedException();
        }

        await _subscriptionRepository.DeleteAsync(subscription);
        await _subscriptionRepository.SaveAsync();

        _logger.LogWarning(
            "User {UserId} unsubscribed from {StormType}s in {Zone}",
            subscription.UserId,
            subscription.Type,
            WoWZoneHelper.GetName(subscription.Zone)
        );
    }

    private async Task<ElementalStormSubscription?> GetStormSubscription(
        ulong userId,
        ElementalStormType stormType,
        WoWZone wowZone
    )
    {
        return (await _subscriptionRepository.GetAllAsync()).FirstOrDefault(
            x => x.UserId == userId && x.Type == stormType && x.Zone == wowZone
        );
    }

    private async Task<GrandHuntSubscription?> GetHuntSubscription(ulong userId, WoWZone wowZone)
    {
        return (await _huntSubscriptionRepository.GetAllAsync()).FirstOrDefault(
            x => x.UserId == userId && x.Zone == wowZone
        );
    }

    public async Task<IEnumerable<ElementalStorm>> GetCurrentElementalStorms()
    {
        var currentStorms = await _wowhead.GetCurrentElementalStorms();

        if (currentStorms == null || !currentStorms.Any())
        {
            throw new NotFoundException("No storms found");
        }

        return currentStorms;
    }

    /// <summary>
    /// Represents a match between a subscription and an ongoing elemental storm.
    /// </summary>
    public record ElementalStormSubscriptionMatch(ElementalStormSubscription Subscription, ElementalStorm Storm);

    public async Task<List<ElementalStormSubscriptionMatch>> GetDueStormSubscriptions()
    {
        var currentElementalStorms = (await _wowhead.GetCurrentElementalStorms())
            ?.Where(storm => storm.Status == "Active")
            .ToList();

        if (currentElementalStorms == null || !currentElementalStorms.Any())
        {
            return new();
        }

        var subscriptions = await _subscriptionRepository.GetAllAsync();

        var dueSubscriptions = new List<ElementalStormSubscriptionMatch>();

        foreach (var storm in currentElementalStorms)
        {
            foreach (var subscription in subscriptions)
            {
                var subscriptionIsForCurrentStorm =
                    subscription.Type == storm.Type && subscription.Zone == storm.ZoneId;

                if (!subscriptionIsForCurrentStorm)
                {
                    continue;
                }

                var recentlyNotified =
                    subscription.LastNotifiedAt.HasValue && subscription.LastNotifiedAt.Value >= storm.StartingAt;

                if (recentlyNotified)
                {
                    continue;
                }

                dueSubscriptions.Add(new(subscription, storm));
            }
        }

        return dueSubscriptions;
    }

    public async Task SetSubscriptionNotified(ElementalStormSubscription subscription, DateTimeOffset lastNotifiedAt)
    {
        subscription.LastNotifiedAt = lastNotifiedAt;
        await _subscriptionRepository.AddOrEditAsync(subscription);
    }

    /// <summary>
    /// Represents a match between a subscription and an ongoing grand hunt.
    /// </summary>
    public record GrandHuntSubscriptionMatch(GrandHuntSubscription Subscription, GrandHunt Hunt);

    public async Task<List<GrandHuntSubscriptionMatch>> GetDueHuntSubscriptions()
    {
        var currentGrandHunts = (await _wowhead.GetCurrentGrandHunts())?.ToList();

        if (currentGrandHunts == null || !currentGrandHunts.Any())
        {
            return new();
        }

        var subscriptions = await _huntSubscriptionRepository.GetAllAsync();

        var dueSubscriptions = new List<GrandHuntSubscriptionMatch>();

        foreach (var grandHunt in currentGrandHunts)
        {
            foreach (var subscription in subscriptions)
            {
                var subscriptionIsForCurrentHunt = subscription.Zone == grandHunt.ZoneId;

                if (!subscriptionIsForCurrentHunt)
                {
                    continue;
                }

                var recentlyNotified =
                    subscription.LastNotifiedAt.HasValue && subscription.LastNotifiedAt.Value >= grandHunt.Time;

                if (recentlyNotified)
                {
                    continue;
                }

                dueSubscriptions.Add(new(subscription, grandHunt));
            }
        }

        return dueSubscriptions;
    }

    public async Task SetHuntSubscriptionNotified(GrandHuntSubscription subscription, DateTimeOffset lastNotifiedAt)
    {
        subscription.LastNotifiedAt = lastNotifiedAt;
        await _huntSubscriptionRepository.AddOrEditAsync(subscription);
    }

    public async Task AddRadiantEchoesSubscription(ulong userId, WoWZone wowZone)
    {
        var subscription = await GetRadiantEchoesSubscription(userId, wowZone);
        if (subscription != null)
        {
            throw new AlreadySubscribedException();
        }

        subscription = new RadiantEchoesSubscription
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Zone = wowZone
        };

        await _radiantEchoesSubscriptionRepository.AddAsync(subscription);
        await _radiantEchoesSubscriptionRepository.SaveAsync();

        _logger.LogWarning(
            "User {UserId} subscribed to Radiant Echoes in {Zone}",
            userId,
            WoWZoneHelper.GetName(wowZone)
        );
    }

    private async Task<RadiantEchoesSubscription?> GetRadiantEchoesSubscription(ulong userId, WoWZone wowZone)
    {
        return (await _radiantEchoesSubscriptionRepository.GetAllAsync()).FirstOrDefault(
            x => x.UserId == userId && x.Zone == wowZone
        );
    }

    public async Task RemoveRadiantEchoesSubscription(ulong userId, WoWZone wowZone)
    {
        var subscription = await GetRadiantEchoesSubscription(userId, wowZone);
        if (subscription == null)
        {
            throw new NotSubscribedException();
        }

        await _radiantEchoesSubscriptionRepository.DeleteAsync(subscription);
        await _radiantEchoesSubscriptionRepository.SaveAsync();

        _logger.LogWarning(
            "User {UserId} unsubscribed from Radiant Echoes in {Zone}",
            userId,
            WoWZoneHelper.GetName(wowZone)
        );
    }

    /// <summary>
    /// Represents a match between a subscription and an ongoing Radiant Echoes event.
    /// </summary>
    public record RadiantEchoesSubscriptionMatch(
        RadiantEchoesSubscription Subscription,
        RadiantEchoesEvent EchoesEvent
    );

    public async Task<List<RadiantEchoesSubscriptionMatch>> GetDueRadiantEchoesSubscriptions()
    {
        var currentRadiantEchoesEvents = (await _wowhead.GetCurrentRadiantEchoesEvents())?.ToList();

        if (currentRadiantEchoesEvents == null || !currentRadiantEchoesEvents.Any())
        {
            return new();
        }

        var subscriptions = await _radiantEchoesSubscriptionRepository.GetAllAsync();

        var dueSubscriptions = new List<RadiantEchoesSubscriptionMatch>();

        foreach (var radiantEchoes in currentRadiantEchoesEvents)
        {
            foreach (var subscription in subscriptions)
            {
                var subscriptionIsForCurrentEvent = subscription.Zone == radiantEchoes.ZoneId;

                if (!subscriptionIsForCurrentEvent)
                {
                    continue;
                }

                var warnAt = radiantEchoes.StartedAt - _warnThreshold;

                var recentlyNotified =
                    subscription.LastNotifiedAt.HasValue && subscription.LastNotifiedAt.Value >= warnAt;

                var eventIsLessThanThresholdAway = radiantEchoes.StartedAt <= DateTimeOffset.Now + _warnThreshold;

                if (recentlyNotified || !eventIsLessThanThresholdAway)
                {
                    continue;
                }

                dueSubscriptions.Add(new(subscription, radiantEchoes));
            }
        }

        return dueSubscriptions;
    }

    public async Task SetRadiantEchoesSubscriptionNotified(
        RadiantEchoesSubscription subscription,
        DateTimeOffset lastNotifiedAt
    )
    {
        subscription.LastNotifiedAt = lastNotifiedAt;
        await _radiantEchoesSubscriptionRepository.AddOrEditAsync(subscription);
    }
}
