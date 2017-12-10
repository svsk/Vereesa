using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Vereesa.Core.Extensions;
using Vereesa.Data;
using Vereesa.Data.Models.Giveaways;

namespace Vereesa.Core.Services
{
    public class GiveawayService
    {
        private DiscordSocketClient _discord;
        private JsonRepository<Giveaway> _giveawayRepo;
        private Giveaway _giveawayBeingCreated;
        private ISocketMessageChannel _configChannel;
        private int _configStep = 0;
        private string _channelPromptMessage = "Alright! Let's set up your giveaway. First, what channel do you want the giveaway in? You can type [cancel] at any time to cancel creation.";
        private string _durationPromptMessage = "Sweet! The giveaway will be in {0}. Next how long should the giveaway last?";
        private string _winnerCountPromptMessage = "Neat this giveaway will last {0} seconds! Now, how many winners will there be?";
        private string _prizePromptMessage = "Ok! {0} winners it is! Finally what do you want to give away?";

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

            if (message.Content == "cancel")
            {
                await message.Channel.SendMessageAsync("OK! I'll forget what you told me about this giveaway. If you want to create a new one, simply use the command !gcreate to set one up.");
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
                if (SetGiveAwayDuration(message))
                {
                    await PromptUserForWinnerCount();
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

                await message.Channel.SendMessageAsync($"Cool! I've created the giveaway in {_giveawayBeingCreated.TargetChannel}!");

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

        private async Task PromptUserForWinnerCount()
        {
            await _configChannel.SendMessageAsync(String.Format(_winnerCountPromptMessage, _giveawayBeingCreated.Duration));
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

        private bool SetGiveAwayDuration(SocketMessage message)
        {
            var parseSuccess = int.TryParse(message.Content, out var durationInSeconds);

            if (!parseSuccess)
                return false;

            _giveawayBeingCreated.Duration = durationInSeconds;

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
            if (cmdMessage.Channel.Id == (await cmdMessage.Author.GetOrCreateDMChannelAsync())?.Id) {
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