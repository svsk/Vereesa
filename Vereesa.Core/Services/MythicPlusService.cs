// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Discord.WebSocket;
// using Vereesa.Core.Configuration;
// using Vereesa.Core.Extensions;
// using Vereesa.Core.Integrations.Interfaces;
// using Vereesa.Core.Infrastructure;

// namespace Vereesa.Core.Services
// {
// 	public class MythicPlusService : BotServiceBase
// 	{
// 		private ISpreadsheetClient _spreadsheet;
// 		private MythicPlusServiceSettings _settings;

// 		public MythicPlusService(DiscordSocketClient discord, MythicPlusServiceSettings settings, ISpreadsheetClient spreadsheet)
// 			: base(discord)
// 		{
// 			discord.MessageReceived -= OnMessageReceivedAsync;
// 			discord.MessageReceived += OnMessageReceivedAsync;
// 			_spreadsheet = spreadsheet;
// 			_settings = settings;
// 		}

// 		private async Task OnMessageReceivedAsync(SocketMessage message)
// 		{
// 			switch (message.GetCommand())
// 			{
// 				case MythicPlusCommand.AddKey:
// 					var keystone = ParseMessage(message.Content);
// 					AddKey(message.Author.Username, keystone.Dungeon, keystone.Level);
// 					break;
// 				case MythicPlusCommand.ListKeys:
// 					var keys = GetKeys();
// 					await message.Channel.SendMessageAsync(string.Join(Environment.NewLine, keys));
// 					break;
// 			}
// 		}

// 		private class MythicPlusCommand
// 		{
// 			public const string AddKey = "!addkey";
// 			public const string ListKeys = "!listkeys";
// 		}

// 		public (string Dungeon, int Level) ParseMessage(string message)
// 		{
// 			throw new NotImplementedException();
// 		}

// 		public void AddKey(string playerName, string dungeonName, int keyLevel)
// 		{
// 			_spreadsheet.Open(_settings.SheetId);


// 		}

// 		public List<string> GetKeys()
// 		{
// 			_spreadsheet.Open(_settings.SheetId);

// 			return _spreadsheet.GetValueRange("Key Scheduler (10.06-16.06)!E13:F42")
// 				.Where(row => row.Count > 0)
// 				.Where(row => row[0].ToString() != "OPEN")
// 				.Select(row => $"{row[0]} ({row[1]})")
// 				.ToList();
// 		}
// 	}
// }

