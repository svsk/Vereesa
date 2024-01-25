using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Vereesa.Core.Models;

namespace Vereesa.Core.Discord;

public class DiscordEventsClient : IEventsClient
{
    private readonly DiscordSocketClient _discord;

    public DiscordEventsClient(DiscordSocketClient discord)
    {
        _discord = discord;
    }

    public async Task<List<VereesaEvent>> GetGuildEvents(ulong guildId) =>
        (await GetDiscordGuildEvents(guildId)).Select(MapToVereesaEvent).ToList();

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
}
