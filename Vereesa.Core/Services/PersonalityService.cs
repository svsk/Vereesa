using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using RestSharp;
using Vereesa.Core.Infrastructure;
using Vereesa.Data.Interfaces;

namespace Vereesa.Core.Services
{
	public class PersonalityService : BotServiceBase
	{
		private readonly IRepository<Personality> _personalityRepo;
		private readonly ILogger<PersonalityService> _logger;

		public PersonalityService(DiscordSocketClient discord, IRepository<Personality> personalityRepo,
		ILogger<PersonalityService> logger) : base(discord)
		{
			_personalityRepo = personalityRepo;
			_logger = logger;
		}

		[OnCommand("!personality list")]
		[AsyncHandler]
		public async Task ListPersonalityTypes(IMessage message)
		{
			await message.Channel.SendMessageAsync("Fetching personalities...");
			var personalities = await _personalityRepo.GetAllAsync();

			var groups = personalities.GroupBy(p => p.Code);

			await message.Channel.SendMessageAsync("We have collected the following personalities:\n" +
				string.Join("\n", groups.Select(g => $"{g.First().Type} ({g.Key}): {g.Count()}")));
		}

		[OnCommand("!personality")]
		[Description("Shows your personality for all to see.")]
		[CommandUsage("`!personality`")]
		[AsyncHandler]
		public async Task GetPersonality(IMessage message)
		{
			var personality = await _personalityRepo.FindByIdAsync(message.Author.Id.ToString());
			if (personality == null)
			{
				await message.Channel.SendMessageAsync("You haven't set your personality yet. " +
					"Take the test here and set your personality via the `!personality set` command. " +
					"\n\n https://www.16personalities.com/free-personality-test");

				return;
			}

			await message.Channel.SendMessageAsync(
				$"{message.Author.Mention}, you are\n" +
				$"Type: {personality.Type}\n" +
				$"Code: {personality.Code}\n" +
				$"Role: {personality.Role}\n" +
				$"Strategy: {personality.Strategy}"
			);
		}

		[OnCommand("!personality set")]
		[Description("Set your personality via a 16personalities.com profile.")]
		[CommandUsage("`!personality set <16personalities.com profile url>`")]
		[WithArgument("link", 0)]
		[AsyncHandler]
		public async Task SetPersonality(IMessage message, string link)
		{
			if (string.IsNullOrWhiteSpace(link))
			{
				await message.Channel.SendMessageAsync("Please include a link to your 16personalities.com profile." +
					"You can find a sharable link to your profile at https://www.16personalities.com/profile");
				return;
			}

			if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
			{
				await message.Channel.SendMessageAsync(
					"I couldn't understand the link you sent. Are you sure it's correct?");
				return;
			}

			var client = new RestClient();
			var request = new RestRequest(uri, Method.GET);

			try
			{
				var result = await client.ExecuteAsync(request);

				var doc = new HtmlDocument();
				doc.LoadHtml(result.Content);

				var summaryTable = doc.DocumentNode.SelectNodes(
					"//*[contains(@class, \"info-table\")]")?.FirstOrDefault();

				var statStrings = summaryTable?.InnerText
					.Replace("\n", "")
					.Replace("\\n", "")
					.Replace("  ", "")
					.Split("?");

				var stats = statStrings.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s =>
				{
					var statAndValue = s.Split(":");
					return (statAndValue[0], statAndValue[1]);
				});

				var personality = new Personality
				{
					Id = message.Author.Id.ToString(),
					Type = (stats.First(i => i.Item1 == "Type").Item2),
					Code = (stats.First(i => i.Item1 == "Code").Item2),
					Role = (stats.First(i => i.Item1 == "Role").Item2),
					Strategy = (stats.First(i => i.Item1 == "Strategy").Item2),
				};

				await _personalityRepo.AddOrEditAsync(personality);

				await message.Channel.SendMessageAsync($"OK, {message.Author.Mention}! I'll remember that you are a " +
					$"{personality.Type} ({personality.Code}). With an {personality.Role} role " +
					$"and {personality.Strategy} strategy.\n\nYou can check it at any time using `!personality`.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to set personality type.");
				await message.Channel.SendMessageAsync("I wasn't able to set your personality 😟 Please try again. " +
					"If the problem persists, please notify Veinalsh to fix it.");
			}
		}
	}

	public class Personality : IEntity
	{
		public string Id { get; set; }
		public string Type { get; set; }
		public string Code { get; set; }
		public string Role { get; set; }
		public string Strategy { get; set; }
	}
}