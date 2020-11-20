using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RestSharp;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Data.Interfaces;

namespace Vereesa.Core.Services
{
	public class CharacterService : BotServiceBase
	{
		private IRepository<UsersCharacters> _userCharactersRepository;

		private SocketRole _gmRole => Discord.GetRole(124251327650988036);
		private SocketRole _officerRole => Discord.GetRole(124251615489294337);
		private SocketRole _responsibleRole => _officerRole;
		private string _repoKey = "users-characters.json";

		public CharacterService(DiscordSocketClient discord,
			IRepository<UsersCharacters> userCharactersRepository)
			: base(discord)
		{
			_userCharactersRepository = userCharactersRepository;
		}

		[OnCommand("!assign")]
		[Authorize("Officer")]
		[CommandUsage("`!assign <mention Discord user> <WoW Character Name>-<WoW Character Realm Name>`")]
		public async Task HandleAssignCommandAsync(IMessage message)
		{
			var characterToClaim = message.GetCommandArgs().Skip(1).Join(" ");
			var userId = message.MentionedUserIds.FirstOrDefault();

			var result = AssignCharacter(userId, characterToClaim);
			await message.Channel.SendMessageAsync(result);
		}

		[OnCommand("!claim")]
		public async Task HandleClaimCommandAsync(IMessage message)
		{
			var characterToClaim = message.GetCommandArgs().Join(" ");
			var userId = message.Author.Id;

			var response = await Prompt(_responsibleRole,
				$"Does {characterToClaim} belong to {userId.MentionPerson()} (Answer `yes` to confirm)?",
					message.Channel, 1200000);

			if (response?.Content.ToLowerInvariant() == "yes")
			{
				var result = AssignCharacter(userId, characterToClaim);
				await message.Channel.SendMessageAsync(result);
			}
			else
			{
				await message.Channel.SendMessageAsync(
					$"Gave up on assigning {characterToClaim} to {message.Author.Mention}."
				);
			}
		}

		private string AssignCharacter(ulong? discordUserId, string characterToClaim)
		{
			if (!TryValidateDiscordUserId(discordUserId, out var errors))
			{
				return $"I couldn't assign {characterToClaim} because I was unable to find a valid Discord user.";
			}

			if (!TryValidateCharacterName(characterToClaim, out errors))
			{
				return $"I couldn't assign {characterToClaim} to {discordUserId.Value.MentionPerson()} because "
					+ $"{errors.Join(", ")}.";
			}

			var usersCharacters = _userCharactersRepository.FindById(_repoKey) ??
				new UsersCharacters(_repoKey);

			try
			{
				usersCharacters.AddChar(discordUserId.Value, characterToClaim.ToLowerInvariant());
			}
			catch (InvalidOperationException ex)
			{
				return ex.Message;
			}

			_userCharactersRepository.AddOrEdit(usersCharacters);

			return $"{characterToClaim} has been assigned to {discordUserId.Value.MentionPerson()}.";
		}

		private bool TryValidateDiscordUserId(ulong? discordUserId, out List<string> errors)
		{
			errors = new List<string>();

			if (discordUserId == null)
			{
				errors.Add("User ID is null");
			}

			return errors.Count == 0;
		}

		private bool TryValidateCharacterName(string charAndRealm, out List<string> errors)
		{
			errors = new List<string>();

			if (!charAndRealm.Contains("-"))
			{
				errors.Add("the name doesn't include a realm (format should be `<character name>-<realm name>`)");
			}

			var characterName = charAndRealm.Split("-").First().Trim();
			var realmName = charAndRealm.Split("-").Last()
				.Trim()
				.Replace(" ", "-")
				.Replace("'", string.Empty);

			var restClient = new RestClient("https://worldofwarcraft.com");
			var request = new RestRequest($"/en-gb/character/eu/{realmName}/{characterName}",
				Method.GET);

			var validationResult = restClient.Execute(request);

			if (validationResult.StatusCode == HttpStatusCode.NotFound)
			{
				errors.Add("the specified character was not found on the specified realm");
			}

			return errors.Count == 0;
		}
	}

	public class UsersCharacters : IEntity
	{
		public UsersCharacters(string id)
		{
			this.Id = id;
			this.CharacterMap = new Dictionary<ulong, List<string>>();
		}

		public string Id { get; set; }

		public Dictionary<ulong, List<string>> CharacterMap { get; set; }

		public void AddChar(ulong userId, string characterName)
		{
			var existingClaim = this.CharacterMap.FirstOrDefault(kv => kv.Value.Contains(characterName));

			if (!existingClaim.Equals(default(KeyValuePair<ulong, List<string>>)))
			{
				throw new InvalidOperationException($"Character already claimed by {existingClaim.Key.MentionPerson()}.");
			}

			var userIdKey = userId.ToString();

			if (!this.CharacterMap.ContainsKey(userId))
			{
				this.CharacterMap.Add(userId, new List<string> { characterName });
			}
			else
			{
				this.CharacterMap[userId].Add(characterName);
			}
		}
	}
}