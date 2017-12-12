using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Vereesa.Core.Extensions;
using Vereesa.Data.Models.Giveaways;
using Vereesa.Data.Repositories;

namespace Vereesa.Core.Services
{
    public class GiveawayService
    {
        private DiscordSocketClient _discord;
        private JsonRepository<Giveaway> _giveawayRepo;
        private Giveaway _giveawayBeingCreated;
        private ISocketMessageChannel _configChannel;
        private int _configStep = 0;
        private string _channelPromptMessage = ":tada: Alright! Let's set up your giveaway. First, what channel do you want the giveaway in?\r\nYou can type `cancel` at any time to cancel creation.\r\n\r\n`Please type the name of a channel in this server.`";
        private string _durationPromptMessage = ":tada: Sweet! The giveaway will be in {0}! Next, how long should the giveaway last?\r\n\r\n`Please enter the duration of the giveaway in seconds.\r\nAlternatively, enter a duration in minutes and include an M at the end.`";
        private string _winnerCountPromptMessage = ":tada: Neat! This giveaway will last **{0}** {1}! Now, how many winners should there be?\r\n\r\n`Please enter a number of winners between 1 and 15.`";
        private string _prizePromptMessage = ":tada: Ok! {0} winners it is! Finally, what do you want to give away?\r\n\r\n`Please enter the giveaway prize. This will also begin the giveaway.`";

        public GiveawayService(DiscordSocketClient discord, JsonRepository<Giveaway> giveawayRepo)
        {
            _discord = discord;
            _giveawayRepo = giveawayRepo;
            _discord.MessageReceived += EvaluateMessage;
        }

        private async Task EvaluateMessage(SocketMessage message)
        {
            if (message.Content == "!gcreate")
            {
                await StartGiveawayConfiguration(message);
                _configStep = 1;
                return;
            }

            if (!MessageCanConfigure(message))
            {
                return;
            }

            if (message.Content.ToLowerInvariant() == "cancel")
            {
                await message.Channel.SendMessageAsync(":boom: Alright, I guess we're not having a giveaway after all...\r\n\r\n`Giveaway creation has been cancelled.`");
                CleanupConfig();
                return;
            }

            if (_configStep == 1)
            {
                if (SetGiveawayChannel(message))
                {
                    await PromptUserForDuration();
                    _configStep = 2;
                }
                else
                {
                    await _configChannel.SendMessageAsync("Sorry, I didn't really understand which channel you meant. Please try again or type 'cancel' to cancel creation.");
                }

                return;
            }

            if (_configStep == 2)
            {
                if (SetGiveAwayDuration(message, out var isMinutes))
                {
                    await PromptUserForWinnerCount(isMinutes);
                    _configStep = 3;
                }
                else
                {
                    await _configChannel.SendMessageAsync("Sorry, I didn't really understand how long you wanted it to go on for. Please try again or type 'cancel' to cancel creation.");
                }

                return;
            }

            if (_configStep == 3)
            {
                if (SetGiveawayWinnerCount(message))
                {
                    await PromptUserForPrize();
                    _configStep = 4;
                }
                else
                {
                    await _configChannel.SendMessageAsync("Sorry, I didn't really understand how many winners you wanted. Please try again or type 'cancel' to cancel creation.");
                }

                return;
            }

            if (_configStep == 4)
            {
                _giveawayBeingCreated.Prize = message.Content;
                _giveawayBeingCreated.CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                _giveawayRepo.Add(_giveawayBeingCreated);
                _giveawayRepo.Save();

                await message.Channel.SendMessageAsync($":tada: Done! The giveaway for the `{_giveawayBeingCreated.Prize}` is starting in {_giveawayBeingCreated.TargetChannel}!");

                CleanupConfig();
                return;
            }
        }

        private void CleanupConfig()
        {
            _configStep = 0;
            _giveawayBeingCreated = null;
            _configChannel = null;
        }

        private async Task PromptUserForPrize()
        {
            await _configChannel.SendMessageAsync(String.Format(_prizePromptMessage, _giveawayBeingCreated.NumberOfWinners));
        }

        private async Task PromptUserForWinnerCount(bool isMinutes)
        {
            await _configChannel.SendMessageAsync(String.Format(_winnerCountPromptMessage, isMinutes ? _giveawayBeingCreated.Duration / 60 : _giveawayBeingCreated.Duration, isMinutes ? "minutes" : "seconds"));
        }

        private async Task PromptUserForDuration()
        {
            await _configChannel.SendMessageAsync(String.Format(_durationPromptMessage, _giveawayBeingCreated.TargetChannel));
        }

        private bool SetGiveawayChannel(SocketMessage message)
        {
            var guild = _discord.Guilds.Where(g => g.Channels.Any(c => c.Id == _configChannel.Id)).FirstOrDefault();
            var channelId = message.Content.ToChannelId();

            var targetChannel = guild.Channels.FirstOrDefault(c => c.Id == channelId);
            if (targetChannel == null)
            {
                return false;
            }

            _giveawayBeingCreated.TargetChannel = message.Content;

            return true;
        }

        private bool SetGiveAwayDuration(SocketMessage message, out bool isMinutes)
        {
            var minuteSplit = message.Content.Split('m');
            isMinutes = false;

            if (minuteSplit.Length == 2 && message.Content.EndsWith("m"))
            {
                isMinutes = true;
            }

            var parseSuccess = int.TryParse(minuteSplit.First(), out var duration);

            if (!parseSuccess)
                return false;

            _giveawayBeingCreated.Duration = isMinutes ? duration * 60 : duration;

            return true;
        }

        private bool SetGiveawayWinnerCount(SocketMessage message)
        {
            var parseSuccess = int.TryParse(message.Content, out var numberOfWinners);

            if (!parseSuccess)
                return false;

            _giveawayBeingCreated.NumberOfWinners = numberOfWinners;

            return true;
        }

        private bool MessageCanConfigure(SocketMessage message)
        {
            return _giveawayBeingCreated != null && message.Author.Username == _giveawayBeingCreated.CreatedBy && message.Channel.Id == _configChannel.Id;
        }

        private async Task StartGiveawayConfiguration(SocketMessage cmdMessage)
        {
            if (cmdMessage.Channel.Id == (await cmdMessage.Author.GetOrCreateDMChannelAsync())?.Id)
            {
                await cmdMessage.Channel.SendMessageAsync($"I can't create giveaways from direct messages. Please use the !gcreate command in a channel on the server where you want the giveaway to be created.");
                return;
            }

            if (_giveawayBeingCreated != null)
            {
                await cmdMessage.Channel.SendMessageAsync($"{_giveawayBeingCreated.CreatedBy} is currently creating a giveaway. Please wait until they have finished their configuration to create another.");
                return;
            }

            _configChannel = cmdMessage.Channel;
            _giveawayBeingCreated = new Giveaway();
            _giveawayBeingCreated.Id = Guid.NewGuid().ToString();
            _giveawayBeingCreated.CreatedBy = cmdMessage.Author.Username;

            await cmdMessage.Channel.SendMessageAsync(_channelPromptMessage);
            _configStep = 1;
        }
    }
}