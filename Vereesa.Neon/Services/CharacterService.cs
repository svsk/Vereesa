using System.ComponentModel;
using Discord;
using RestSharp;
using Vereesa.Core;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Helpers;

namespace Vereesa.Neon.Services
{
    public class CharacterService : IBotModule
    {
        private IMessagingClient _messaging;
        private IRepository<UsersCharacters> _userCharactersRepository;
        private ulong _responsibleRole = WellknownRoles.Officer;
        public const string BlobContainer = "users-characters.json";

        public CharacterService(IMessagingClient messaging, IRepository<UsersCharacters> userCharactersRepository)
        {
            _messaging = messaging;
            _userCharactersRepository = userCharactersRepository;
        }

        [AsyncHandler]
        [Authorize("Officer")]
        [OnCommand("!character assign")]
        [WithArgument("discordUser", 0)]
        [WithArgument("characterName", 1)]
        [Description("Assigns a WoW character to a Discord user.")]
        [CommandUsage("`!character assign <mention Discord user> <WoW Character Name>-<WoW Character Realm Name>`")]
        public async Task HandleAssignCommandAsync(IMessage message, string discordUser, string characterName)
        {
            var userId = MentionUtils.ParseUser(discordUser);
            var result = AssignCharacter(userId, characterName);
            await message.Channel.SendMessageAsync(result);
        }

        [OnCommand("!character claim")]
        [Description("Claims a WoW character.")]
        [WithArgument("characterName", 0)]
        [CommandUsage("`!character claim <WoW Character Name>-<WoW Character Realm Name>`")]
        [AsyncHandler]
        public async Task HandleClaimCommandAsync(IMessage message, string characterName)
        {
            var userId = message.Author.Id;

            if (!TryValidateCharacterName(characterName, out var errors))
            {
                await message.Channel.SendMessageAsync(
                    $"I couldn't assign {characterName} to {message.Author.Mention} because " + $"{errors.Join(", ")}."
                );

                return;
            }

            var response = await _messaging.Prompt(
                _responsibleRole,
                $"Does {characterName} belong to {userId.MentionPerson()} (Answer `yes` to confirm)?",
                message.Channel,
                1200000
            );

            if (response?.Content.ToLowerInvariant() == "yes")
            {
                var result = AssignCharacter(userId, characterName);
                await message.Channel.SendMessageAsync(result);
            }
            else
            {
                await message.Channel.SendMessageAsync(
                    $"Gave up on assigning {characterName} to {message.Author.Mention}."
                );
            }
        }

        [OnCommand("!character unclaim")]
        [WithArgument("characterName", 0)]
        [CommandUsage("`!character unclaim <WoW Character Name>-<WoW Character Realm Name>`")]
        [Description("Releases the claim you have on a WoW character.")]
        [AsyncHandler]
        public async Task HandleUnclaimCommandAsync(IMessage message, string characterName)
        {
            var userId = message.Author.Id;

            var result = UnassignCharacter(userId, characterName);
            await message.Channel.SendMessageAsync(result);
        }

        private string UnassignCharacter(ulong userId, string characterName)
        {
            var usersCharacters =
                _userCharactersRepository.FindById(BlobContainer) ?? new UsersCharacters(BlobContainer);

            try
            {
                usersCharacters.RemoveChar(userId, characterName.ToLowerInvariant());
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            _userCharactersRepository.AddOrEdit(usersCharacters);

            return "Removed";
        }

        [OnCommand("!character list")]
        [Description("Lists the WoW characters you have claimed.")]
        [AsyncHandler]
        public async Task ListCharacters(IMessage message)
        {
            var characters = string.Join("\n", GetUserCharacters(message.Author).Select(c => c.ToTitleCase()));
            var howToClaim =
                $"If you want to claim characters to have stuff like attendance tied to your Discord "
                + "user instead of your WoW characters type `!claim <character name>-<realm name>`.";

            await message.Channel.SendMessageAsync($"**Your claimed characters:**\n{characters}\n\n{howToClaim}");
        }

        private List<string> GetUserCharacters(IUser author)
        {
            return _userCharactersRepository
                .FindById(BlobContainer)
                .CharacterMap.TryGetValue(author.Id, out var characters)
                ? characters
                : new List<string>();
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

            var usersCharacters =
                _userCharactersRepository.FindById(BlobContainer) ?? new UsersCharacters(BlobContainer);

            try
            {
                usersCharacters.AddChar(discordUserId.Value, characterToClaim.Trim().ToLowerInvariant());
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
            var realmName = charAndRealm.Split("-").Last().Trim().Replace(" ", "-").Replace("'", string.Empty);

            var restClient = new RestClient("https://worldofwarcraft.com");
            var request = new RestRequest($"/en-gb/character/eu/{realmName}/{characterName}", Method.Get);

            var validationResult = restClient.Execute(request);

            if (!validationResult.IsSuccessful)
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
                throw new InvalidOperationException(
                    $"Character already claimed by {existingClaim.Key.MentionPerson()}."
                );
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

        public void RemoveChar(ulong userId, string characterName)
        {
            if (this.CharacterMap.ContainsKey(userId) && this.CharacterMap[userId].Contains(characterName))
            {
                this.CharacterMap[userId].Remove(characterName);
            }
            else
            {
                throw new InvalidOperationException("Character not claimed by user.");
            }
        }
    }
}
