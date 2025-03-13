using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Models;

namespace Vereesa.Core.Discord;

public class DiscordEventsClient : IEventsClient
{
    private readonly DiscordSocketClient _discord;
    private readonly ILogger<DiscordEventsClient> _logger;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public DiscordEventsClient(DiscordSocketClient discord, ILogger<DiscordEventsClient> logger)
    {
        _discord = discord;
        _logger = logger;
    }

    public async Task<List<VereesaEvent>> GetGuildEvents(ulong guildId, bool useCache = true)
    {
        _logger.LogDebug("Getting events for guild {GuildId}", guildId);

        // The Discord API gets mad if you try to call this too often.
        await _semaphore.WaitAsync();

        try
        {
            var cacheKey = $"guild-events-{guildId}";
            if (useCache)
            {
                var cachedEvents = MemoryCache.Default.Get(cacheKey) as List<VereesaEvent>;
                if (cachedEvents != null)
                {
                    _logger.LogDebug("Retrieved events for guild {GuildId} from cache.", guildId);
                    return cachedEvents;
                }
            }

            try
            {
                var events = await GetDiscordGuildEvents(guildId);
                var mappedEvents = events.Select(MapToVereesaEvent).ToList();

                MemoryCache.Default.Set(cacheKey, mappedEvents, DateTimeOffset.Now.AddMinutes(5));
                _logger.LogDebug("Retrieved events for guild {GuildId} from Discord.", guildId);

                return mappedEvents;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get events for guild {GuildId}", guildId);
                return new();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StartEvent(ulong guildId, ulong eventId)
    {
        var guildEvent = await GetDiscordGuildEvent(guildId, eventId);
        await guildEvent.StartAsync();
    }

    private async Task<IReadOnlyCollection<RestGuildEvent>> GetDiscordGuildEvents(ulong guildId)
    {
        var guild = _discord.GetGuild(guildId);
        if (guild == null)
        {
            throw new Exception($"Guild with ID {guildId} not found.");
        }

        return await guild.GetEventsAsync();
    }

    private async Task<RestGuildEvent> GetDiscordGuildEvent(ulong guildId, ulong eventId)
    {
        var guild = _discord.GetGuild(guildId);
        if (guild == null)
        {
            throw new Exception($"Guild with ID {guildId} not found.");
        }

        var guildEvent = await guild.GetEventAsync(eventId);
        if (guildEvent == null)
        {
            throw new Exception($"Event with ID {eventId} not found.");
        }

        return guildEvent;
    }

    private VereesaEvent MapToVereesaEvent(RestGuildEvent discordEvent)
    {
        return new VereesaEvent
        {
            Id = discordEvent.Id,
            Status = MapVereesaEventStatus(discordEvent.Status),
            GuildId = discordEvent.GuildId,
            Name = discordEvent.Name,
            StartTime = discordEvent.StartTime,
            EndTime = discordEvent.EndTime,
        };
    }

    private VereesaEventStatus MapVereesaEventStatus(GuildScheduledEventStatus status)
    {
        switch (status)
        {
            case GuildScheduledEventStatus.Scheduled:
                return VereesaEventStatus.Scheduled;
            case GuildScheduledEventStatus.Active:
                return VereesaEventStatus.Active;
            case GuildScheduledEventStatus.Completed:
                return VereesaEventStatus.Completed;
            case GuildScheduledEventStatus.Cancelled:
                return VereesaEventStatus.Cancelled;
            default:
                throw new Exception($"Unknown event status: {status}");
        }
    }

    public async Task<List<ulong>> GetGuildEventParticipants(ulong guildId, ulong eventId)
    {
        var guildEvent = await _discord.GetGuild(guildId).GetEventAsync(eventId);
        var users = await guildEvent.GetUsersAsync().ToListAsync();
        return users.SelectMany(u => u.Select(u => u.Id)).ToList();
    }
}
