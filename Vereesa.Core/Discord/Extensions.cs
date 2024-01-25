using System;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vereesa.Core.Discord;

public static class Extensions
{
    public static VereesaHostBuilder AddDiscord(this VereesaHostBuilder builder, string discordToken)
    {
        var discord = new DiscordSocketClient(
            new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000,
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.All,
                UseInteractionSnowflakeDate = false
            }
        );

        builder.AddServices(services =>
        {
            services.AddTransient(_ => new DiscordSettings { Token = discordToken });
            services.AddSingleton(discord);
            services.AddTransient<IMessagingClient, DiscordMessagingClient>();
            services.AddTransient<IEmojiClient, DiscordEmojiClient>();
            services.AddTransient<IEventsClient, DiscordEventsClient>();
        });

        return builder;
    }

    public static VereesaHostBuilder AddDiscordChannelLogging(
        this VereesaHostBuilder builder,
        ulong logChannelId,
        LogLevel logLevel = LogLevel.Warning
    )
    {
        builder.AddServices(
            services =>
                services.AddLogging(conf =>
                {
                    var discord =
                        services
                            .FirstOrDefault(s => s.ServiceType == typeof(DiscordSocketClient))
                            ?.ImplementationInstance as DiscordSocketClient;

                    if (discord == null)
                    {
                        throw new Exception("AddDiscord before adding Discord channel logging.");
                    }

                    conf.AddProvider(new DiscordChannelLoggerProvider(discord, logChannelId, logLevel));
                })
        );

        return builder;
    }
}
