# Vereesa

Vereesa is a Discord bot built for the World of Warcraft guild "Neon" on Karazhan-EU. It uses Discord.Net as its base
framework, but has wrapped their API in something we consider to be slightly more usable and easy to learn than
the reference implementation provided by other sources.

## Repository Structure

### Vereesa.Core
Contains the core implementation of Vereesa. Wraps the Discord.Net API and provides the infrastructure needed to
build a simple IM bot. Currently the only platform supported is Discord.

### Vereesa.Neon
Contains Neon-specific services for Vereesa. Basically the business layer for Neon's use of the framework.

#### Vereesa.Neon.Data
Data classes and repositories used by Vereesa.Neon.

#### Vereesa.Neon.Tests
Tests for Vereesa.Neon. Don't look here 🙈

### Vereesa.TestBot
Reference implementation for a super simple bot using only Vereesa.Core. Nice to look at, or start from if you
want to build your own implementation using the Vereesa.Core framework.

### Vereesa.Web
Application hosting Vereesa for Neon.

### Vereesa.Awdeo
An experimental attempt at integrating Vereesa with YouTube to play music. Never went anywhere, and not worth looking
at.

## How can I use Vereesa?

If you want to use the core Vereesa framework, you can.
1) Clone the repository and create a new dotnet project.
2) Add a reference to **Vereesa.Core** (This is the only project that is intended to be reusable) .
3) *The rest of the projects can be deleted* unless you would like to use them for reference.

You can start out with something simple, like a console application, but you can also run Vereesa inside
other apps like a web API (See `Vereesa.Web` for reference).

A very minimal console application would look something like this:

### Program.cs

```CSharp
// See https://aka.ms/new-console-template for more information
using Vereesa.Core;
using Vereesa.Core.Discord;

// Store your Discord bot token somewhere safe.
var token = File.ReadAllText("token.local.txt").Trim();

var host = new VereesaHostBuilder()
    .AddDiscord(token)
    .Start();

Console.WriteLine("Hello, Vereesa!");

// Run until shutdown
await Task.Delay(-1);
```
Then add a service to handle commands or other interactions.

### HelloWorldService.cs
```CSharp
using Discord;
using Discord.Interactions;
using Vereesa.Core;

namespace MyBot;

public class HelloWorldService : IBotService
{
	[SlashCommand("ping", "Pings the bot")]
	public async Task HandlePing(IDiscordInteraction interaction)
	{
		await interaction.RespondAsync("Pong!");
	}
}
```
