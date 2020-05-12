using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Extensions;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Giveaways;

namespace Vereesa.Core.Services
{
    public class GiveawayService : BotServiceBase
    {
        private DiscordSocketClient _discord;
        private Random _rng;
        private ILogger<GiveawayService> _logger;
        private IRepository<Giveaway> _giveawayRepo;
        private Giveaway _giveawayBeingCreated;
        private ISocketMessageChannel _configChannel;
        private Timer _updater;
        private int _configStep = 0;
        private string _channelPromptMessage = ":tada: Alright! Let's set up your giveaway. First, what channel do you want the giveaway in?\r\nYou can type `cancel` at any time to cancel creation.\r\n\r\n`Please type the name of a channel in this server.`";
        private string _durationPromptMessage = ":tada: Sweet! The giveaway will be in {0}! Next, how long should the giveaway last?\r\n\r\n`Please enter the duration of the giveaway in seconds.\r\nAlternatively, enter a duration in minutes and include an M at the end.`";
        private string _winnerCountPromptMessage = ":tada: Neat! This giveaway will last **{0}** {1}! Now, how many winners should there be?\r\n\r\n`Please enter a number of winners between 1 and 15.`";
        private string _prizePromptMessage = ":tada: Ok! {0} winners it is! Finally, what do you want to give away?\r\n\r\n`Please enter the giveaway prize. This will also begin the giveaway.`";


        public GiveawayService(DiscordSocketClient discord, IRepository<Giveaway> giveawayRepo, Random rng, ILogger<GiveawayService> logger)
        {
            _discord = discord;
            _giveawayRepo = giveawayRepo;
            _discord.MessageReceived += EvaluateMessage;
            _rng = rng;
            _logger = logger;
            InitiateUpdateTimer();

            _logger.LogInformation($"{this.GetType().Name} loaded.");
        }

        private async Task EvaluateMessage(SocketMessage message)
        {
            var command = message.GetCommand()?.ToLowerInvariant();
            if (command == "cancel")
            {
                await CancelGiveawayConfiguration(message);
                return;
            }

            if (command == "!gcreate")
            {
                await StartGiveawayConfiguration(message);
                return;
            }

            if (command == "!gstart")
            {
                await StartGiveaway(message);
                return;
            }

            if (command == "!greroll")
            {
                await RerollGiveaway(message);
                return;
            }

            if (command == "!gcancel")
            {
                await CancelGiveaway(message);
                return;
            }

            if (MessageCanConfigure(message))
            {
                await ConfigureCurrentGiveaway(message);
            }
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

            await ConfigureCurrentGiveaway(cmdMessage);
        }

        private async Task ConfigureCurrentGiveaway(SocketMessage message)
        {

            switch (_configStep)
            {
                case 0:
                    InitializeConfigObject(message.Channel, message.Author.Username);
                    await message.Channel.SendMessageAsync(_channelPromptMessage);
                    _configStep = 1;
                    break;

                case 1:
                    if (SetGiveawayChannel(message.Content))
                    {
                        await PromptUserForDuration();
                        _configStep = 2;
                    }
                    else
                    {
                        await _configChannel.SendMessageAsync("Sorry, I didn't really understand which channel you meant. Please try again or type 'cancel' to cancel creation.");
                    }
                    break;

                case 2:
                    if (SetGiveawayDuration(message.Content, out var isMinutes))
                    {
                        await PromptUserForWinnerCount(isMinutes);
                        _configStep = 3;
                    }
                    else
                    {
                        await _configChannel.SendMessageAsync("Sorry, I didn't really understand how long you wanted it to go on for. Please try again or type 'cancel' to cancel creation.");
                    }
                    break;

                case 3:
                    if (SetGiveawayWinnerCount(message.Content))
                    {
                        await PromptUserForPrize();
                        _configStep = 4;
                    }
                    else
                    {
                        await _configChannel.SendMessageAsync("Sorry, I didn't really understand how many winners you wanted. Please try again or type 'cancel' to cancel creation.");
                    }
                    break;

                case 4:
                    _giveawayBeingCreated.Prize = message.Content;
                    await FinalizeAndAnnounceNewGiveaway();
                    await message.Channel.SendMessageAsync($":tada: Done! The giveaway for `{_giveawayBeingCreated.Prize}` is starting in {_giveawayBeingCreated.TargetChannel}!");
                    CleanupConfig();
                    break;
            }
        }

        private async Task FinalizeAndAnnounceNewGiveaway()
        {
            _giveawayBeingCreated.CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var embed = GetAnnouncementEmbed(_giveawayBeingCreated);
            var channel = _discord.GetChannel(_giveawayBeingCreated.TargetChannel.ToChannelId().Value) as ISocketMessageChannel;
            var announcementMessage = await channel.SendMessageAsync(":tada:  **G I V E A W A Y**  :tada:", false, embed);
            _giveawayBeingCreated.AnnouncementMessageId = announcementMessage.Id;
            await _giveawayRepo.AddAsync(_giveawayBeingCreated);
            await _giveawayRepo.SaveAsync();
        }

        private async Task CancelGiveawayConfiguration(SocketMessage message)
        {
            if (MessageCanConfigure(message) && message.Content.ToLowerInvariant() == "cancel")
            {
                await message.Channel.SendMessageAsync(":boom: Alright, I guess we're not having a giveaway after all...\r\n\r\n`Giveaway creation has been cancelled.`");
                CleanupConfig();
            }
        }

        private async Task StartGiveaway(SocketMessage message)
        {
            var commandParams = message.Content.Split(' ').Skip(1).ToList();
            var time = commandParams.FirstOrDefault();
            var prize = string.Join(" ", commandParams.Skip(1));
            var channel = message.Channel;

            InitializeConfigObject(channel, message.Author.Username);
            SetGiveawayChannel(message.Channel.Id.ToString());
            SetGiveawayDuration(time, out var isMinutes);
            SetGiveawayWinnerCount("1");
            _giveawayBeingCreated.Prize = prize;
            await FinalizeAndAnnounceNewGiveaway();
            CleanupConfig();
        }

        private async Task CancelGiveaway(SocketMessage message)
        {
            var giveaway = (await _giveawayRepo.GetAllAsync())
                .Where(g =>
                    g.TargetChannel.ToChannelId() == message.Channel.Id &&
                    g.WinnerNames == null &&
                    g.CreatedBy == message.Author.Username)
                .OrderByDescending(g => g.CreatedTimestamp + g.Duration)
                .FirstOrDefault();

            if (giveaway != null)
            {
                giveaway.Duration = 0;
                await ResolveGiveaway(giveaway);
                await message.Channel.SendMessageAsync($"The giveaway for {giveaway.Prize} was cancelled.");
            }
        }

        private async Task RerollGiveaway(SocketMessage message)
        {
            var giveaway = (await _giveawayRepo.GetAllAsync())
                .Where(g =>
                    g.TargetChannel.ToChannelId() == message.Channel.Id &&
                    g.WinnerNames != null &&
                    g.CreatedBy == message.Author.Username)
                .OrderByDescending(g => g.CreatedTimestamp + g.Duration)
                .FirstOrDefault();

            if (giveaway != null)
            {
                await ResolveGiveaway(giveaway);
                await AnnounceWinners(giveaway);
            }
        }

        private bool SetGiveawayChannel(string messageContent)
        {
            var guild = _discord.Guilds.Where(g => g.Channels.Any(c => c.Id == _configChannel.Id)).FirstOrDefault();
            var channelId = messageContent.ToChannelId();

            var targetChannel = guild.Channels.FirstOrDefault(c => c.Id == channelId);
            if (targetChannel == null)
            {
                return false;
            }

            _giveawayBeingCreated.TargetChannel = messageContent;

            return true;
        }

        private bool SetGiveawayDuration(string messageContent, out bool isMinutes)
        {
            messageContent = messageContent.ToLowerInvariant();
            var minuteSplit = messageContent.Split('m');
            isMinutes = false;

            if (minuteSplit.Length == 2 && messageContent.EndsWith("m"))
            {
                isMinutes = true;
            }

            var parseSuccess = int.TryParse(minuteSplit.First(), out var duration);

            if (!parseSuccess)
                return false;

            _giveawayBeingCreated.Duration = isMinutes ? duration * 60 : duration;

            return true;
        }

        private bool SetGiveawayWinnerCount(string messageContent)
        {
            var parseSuccess = int.TryParse(messageContent, out var numberOfWinners);

            if (!parseSuccess)
                return false;

            _giveawayBeingCreated.NumberOfWinners = numberOfWinners;

            return true;
        }

        #region Config prompts 

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

        #endregion

        #region Helpers

        private async Task<IUserMessage> GetGiveawayMessage(Giveaway giveaway)
        {
            var channelId = giveaway.TargetChannel.ToChannelId();
            var channel = GetChannelById(channelId);

            try
            {
                var iMessage = await channel.GetMessageAsync(giveaway.AnnouncementMessageId);
                return iMessage as IUserMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return null;
            }
        }

        private ISocketMessageChannel GetChannelById(ulong? channelId)
        {
            return _discord.Guilds.Where(g => g.Channels.Any(c => c.Id == channelId)).FirstOrDefault().Channels.First(c => c.Id == channelId) as ISocketMessageChannel;
        }

        private Embed GetAnnouncementEmbed(Giveaway giveaway)
        {
            var duration = giveaway.Duration;
            var created = giveaway.CreatedTimestamp;
            var endsAt = created + duration;

            var embed = new EmbedBuilder();
            embed.Title = giveaway.Prize;
            embed.Description = $"React with :tada: or any other emote to enter!\r\n";
            embed.Footer = new EmbedFooterBuilder();

            if (giveaway.WinnerNames != null)
            {
                var winnerNamesString = giveaway.WinnerNames.Any() ? string.Join(", ", giveaway.WinnerNames) : "Could not be determined.";

                embed.Description = $"Giveaway ended.";
                embed.Footer.Text = $"Winners: {winnerNamesString}";
            }
            else
            {
                embed.Description += $"Time remaining: **{(endsAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToDaysHoursMinutesSeconds()}**";
                embed.Footer.Text = $"Ends at {DateTimeOffset.FromUnixTimeSeconds(endsAt).ToCentralEuropeanTime()} (Server time)";
            }

            embed.Color = new Color(155, 89, 182);

            return embed.Build();
        }

        private void InitializeConfigObject(ISocketMessageChannel channel, string authorUsername)
        {
            _configChannel = channel;
            _giveawayBeingCreated = new Giveaway();
            _giveawayBeingCreated.Id = Guid.NewGuid().ToString();
            _giveawayBeingCreated.CreatedBy = authorUsername;
        }

        private void CleanupConfig()
        {
            _configStep = 0;
            _giveawayBeingCreated = null;
            _configChannel = null;
        }

        private bool MessageCanConfigure(SocketMessage message)
        {
            return _giveawayBeingCreated != null && message.Author.Username == _giveawayBeingCreated.CreatedBy && message.Channel.Id == _configChannel.Id;
        }

        #endregion

        #region Periodic updates

        private void InitiateUpdateTimer()
        {
            _updater = new Timer();
            _updater.Elapsed += UpdateActiveGiveaways;
            _updater.Interval = 10000;
            _updater.AutoReset = true;
            _updater.Start();
        }

        private async void UpdateActiveGiveaways(object sender, ElapsedEventArgs args)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var progressingGiveaways = (await _giveawayRepo.GetAllAsync()).Where(ga => now < ga.CreatedTimestamp + ga.Duration);
            var unreslovedGiveaways = (await _giveawayRepo.GetAllAsync()).Where(ga => now > ga.CreatedTimestamp + ga.Duration && ga.WinnerNames == null);

            foreach (var giveaway in progressingGiveaways)
            {
                var message = await GetGiveawayMessage(giveaway);

                try
                {
                    if (message != null)
                    {
                        await message.ModifyAsync((msg) =>
                        {
                            msg.Embed = GetAnnouncementEmbed(giveaway);
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);
                }
            }

            foreach (var giveaway in unreslovedGiveaways)
            {
                await ResolveGiveaway(giveaway);
                await AnnounceWinners(giveaway);
            }
        }

        private async Task AnnounceWinners(Giveaway giveaway)
        {
            var channel = GetChannelById(giveaway.TargetChannel.ToChannelId());

            if (giveaway.WinnerNames.Count == 0)
            {
                await channel.SendMessageAsync($"A winner for **{giveaway.Prize}** could not be determined.");
            }
            else
            {
                await channel.SendMessageAsync($"Congratulations {string.Join(", ", giveaway.WinnerNames)}! You won **{giveaway.Prize}**!");
            }
        }

        private async Task ResolveGiveaway(Giveaway giveaway)
        {
            var message = await GetGiveawayMessage(giveaway);

            if (message != null)
            {
                var participants = await GetReactingUsers(message);
                var winners = new List<IUser>();

                if (giveaway.NumberOfWinners > participants.Count)
                {
                    giveaway.NumberOfWinners = participants.Count;
                }

                for (var i = 0; i < giveaway.NumberOfWinners; i++)
                {
                    var winnerIndex = _rng.Next(0, participants.Count - 1);
                    var winner = participants[winnerIndex];
                    winners.Add(winner);
                    participants.Remove(winner);
                }

                giveaway.WinnerNames = winners.Select(w => w.Username).ToList();
                await _giveawayRepo.AddOrEditAsync(giveaway);
                await _giveawayRepo.SaveAsync();

                await message.ModifyAsync((msg) =>
                {
                    msg.Embed = GetAnnouncementEmbed(giveaway);
                });
            }
            else
            {
                _logger.LogWarning("Detected possibly deleted gieaway. Find a way to handle this.");
            }
        }

        private async Task<List<IUser>> GetReactingUsers(IUserMessage message)
        {
            var reactingUsers = new List<IUser>();

            foreach (IEmote reaction in message.Reactions.Keys)
            {
                var emojiString = reaction.ToString();
                var emojiIsCustom = emojiString.StartsWith("<:");

                //emojiString.Replace("<:", string.Empty).Replace(">", string.Empty))

                if (emojiIsCustom)
                    reactingUsers.AddRange(await message.GetReactionUsersAsync(reaction, 200).FlattenAsync());
                else
                    reactingUsers.AddRange(await message.GetReactionUsersAsync(reaction, 200).FlattenAsync());
            }

            return reactingUsers.GroupBy(u => u.Id).Select(c => c.First()).ToList();
        }

        #endregion
    }
}