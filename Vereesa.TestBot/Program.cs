// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vereesa.Core;
using Vereesa.Core.Discord;

Console.WriteLine("Hello, World!");

var token = File.ReadAllText("token.local.txt").Trim();

var host = new VereesaHostBuilder()
    .AddDiscord(token)
    .AddDiscordChannelLogging(124446036637908995, LogLevel.Information)
    .AddServices(services =>
    {
        services.AddLogging(config => config.AddConsole());
    })
    .Start();

// Run until shutdown
await Task.Delay(-1);
