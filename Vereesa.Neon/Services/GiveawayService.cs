using System.ComponentModel;
using System.Runtime.Serialization;
using Discord;
using Microsoft.Extensions.Logging;
using Vereesa.Core;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Data.Models.Giveaways;
using Vereesa.Neon.Extensions;
using Vereesa.Neon.Helpers;

namespace Vereesa.Neon.Services
{
    public class GiveawayService : IBotService
    {
        private Random _rng;
        private IJobScheduler _jobScheduler;
        private ILogger<GiveawayService> _logger;
        private readonly IMessagingClient _messaging;
        private IRepository<Giveaway> _giveawayRepo;
        private Func<string> _channelPromptMessage = () =>
            ":tada: Alright! Let's set up your giveaway. First, what channel do you want the giveaway in?\r\nYou can type `cancel` at any time to cancel creation.\r\n\r\n`Please type the name of a channel in this server.`";

        private Func<string, string> _durationPromptMessage = (giveawayChannel) =>
            $":tada: Sweet! The giveaway will be in {giveawayChannel}! Next, how long should the giveaway last?\r\n\r\n`Please enter the duration of the giveaway in seconds.\r\nAlternatively, enter a duration in minutes and include an M at the end.`";

        private Func<long, string, string> _winnerCountPromptMessage = (duration, timeUnit) =>
            $":tada: Neat! This giveaway will last **{duration}** {timeUnit}! Now, how many winners should there be?\r\n\r\n`Please enter a number of winners between 1 and 15.`";

        private Func<int, string> _prizePromptMessage = (numberOfWinners) =>
            $":tada: Ok! {numberOfWinners} winners it is! Finally, what do you want to give away?\r\n\r\n`Please enter the giveaway prize. This will also begin the giveaway.`";

        private string _cancelKeyword = "cancel";

        public GiveawayService(
            IMessagingClient messaging,
            IRepository<Giveaway> giveawayRepo,
            Random rng,
            IJobScheduler jobScheduler,
            ILogger<GiveawayService> logger
        )
        {
            _messaging = messaging;
            _giveawayRepo = giveawayRepo;
            _rng = rng;
            _jobScheduler = jobScheduler;
            _logger = logger;
        }

        [OnCommand("!gcreate")]
        [Description("Starts a wized to create a giveaway.")]
        [AsyncHandler]
        public async Task StartGiveawayConfiguration(IMessage cmdMessage)
        {
            if (cmdMessage.Channel.Id == (await cmdMessage.Author.CreateDMChannelAsync())?.Id)
            {
                await cmdMessage.Channel.SendMessageAsync(
                    $"I can't create giveaways from direct messages. Please use the !gcreate command in a channel on the server where you want the giveaway to be created."
                );
                return;
            }

            await ConfigureGiveaway(cmdMessage.Channel, cmdMessage.Author);
        }

        private async Task ConfigureGiveaway(IMessageChannel configChannel, IUser author)
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var channel = await PromptUntilReceived(
                cancellationTokenSource,
                _channelPromptMessage(),
                author,
                configChannel,
                ParseChannel
            );

            var duration = await PromptUntilReceived(
                cancellationTokenSource,
                _durationPromptMessage(channel.MentionChannel()),
                author,
                configChannel,
                ParseDuration
            );

            var winnerCount = await PromptUntilReceived(
                cancellationTokenSource,
                _winnerCountPromptMessage(duration.duration, duration.timeUnit),
                author,
                configChannel,
                ParseWinnerCount
            );

            var prize = await PromptUntilReceived(
                cancellationTokenSource,
                _prizePromptMessage(winnerCount),
                author,
                configChannel,
                (messageContent) => messageContent
            );

            if (cancellationTokenSource.IsCancellationRequested)
            {
                // cancel
                await configChannel.SendMessageAsync(
                    ":boom: Alright, I guess we're not having a giveaway after all...\r\n\r\n`Giveaway creation has been cancelled.`"
                );
            }
            else
            {
                // finalize and do!
                var giveaway = CreateGiveaway(author.Username);
                giveaway.TargetChannel = channel.ToString();
                giveaway.Duration = CalculateDuration(duration);
                giveaway.NumberOfWinners = winnerCount;
                giveaway.Prize = prize;
                await FinalizeAndAnnounceNewGiveaway(giveaway);

                await configChannel.SendMessageAsync(
                    $":tada: Done! The giveaway for `{prize}` is starting in {channel.MentionChannel()}!"
                );
            }
        }

        private int CalculateDuration((int duration, string timeUnit) durationTuple) =>
            durationTuple.timeUnit == "minutes" ? durationTuple.duration * 60 : durationTuple.duration;

        private int ParseWinnerCount(string messageContent)
        {
            if (!int.TryParse(messageContent, out var winnerCount) || winnerCount < 1 || winnerCount > 15)
            {
                throw new PromptParseValidationException(
                    "Please put in a single numeric value for number of winners (between 1 and 15)."
                );
            }

            return winnerCount;
        }

        private async Task<T> PromptUntilReceived<T>(
            CancellationTokenSource cancellationTokenSource,
            string promptMessage,
            IUser author,
            IMessageChannel configChannel,
            Func<string, T> parsingFunction
        )
        {
            List<string> problems = new List<string>();
            T result = default(T);
            Func<IMessage, bool> creationCancelled = (IMessage message) =>
                message?.Content.Equals(_cancelKeyword, StringComparison.InvariantCultureIgnoreCase) ?? true;

            var parseCompleted = false;
            while (!cancellationTokenSource.IsCancellationRequested && !parseCompleted)
            {
                var currentPromptMessage = problems.Any() ? CreateProblemMessage(problems) : promptMessage;
                problems.Clear();
                var promptResponse = await _messaging.Prompt(author, currentPromptMessage, configChannel, 60000);

                if (creationCancelled(promptResponse))
                {
                    cancellationTokenSource.Cancel();
                }
                else
                {
                    try
                    {
                        result = parsingFunction(promptResponse.Content);
                        parseCompleted = true;
                    }
                    catch (PromptParseValidationException ex)
                    {
                        problems.Add(ex.Message);
                    }
                }
            }

            return result;
        }

        private (int duration, string timeUnit) ParseDuration(string messageContent)
        {
            messageContent = messageContent.ToLowerInvariant();
            var minuteSplit = messageContent.Split('m');
            var isMinutes = (minuteSplit.Length == 2 && messageContent.EndsWith("m"));

            if (!int.TryParse(minuteSplit.First().Trim(), out var duration))
            {
                throw new PromptParseValidationException(
                    "Please input a numeric value as duration in seconds. You can also add an 'm' at the end if you want to give it in minutes instead."
                );
            }

            return (duration, isMinutes ? "minutes" : "seconds");
        }

        private string CreateProblemMessage(List<string> problems)
        {
            return $"😅 I didn't quite get that... {problems.Join(" ")}";
        }

        private ulong ParseChannel(string messageContent)
        {
            var channelId = messageContent.ToChannelId();
            var targetChannel = channelId != null ? _messaging.GetChannelById(channelId.Value) : null;
            if (targetChannel == null)
            {
                throw new PromptParseValidationException("I couldn't figure out what channel you meant.");
            }

            return targetChannel.Id;
        }

        private async Task FinalizeAndAnnounceNewGiveaway(Giveaway giveaway)
        {
            giveaway.CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var embed = GetAnnouncementEmbed(giveaway);
            var channel = _messaging.GetChannelById(giveaway.TargetChannel.ToChannelId().Value) as IMessageChannel;

            var announcementMessage = await channel.SendMessageAsync(
                ":tada:  **G I V E A W A Y**  :tada:",
                false,
                embed
            );
            giveaway.AnnouncementMessageId = announcementMessage.Id;
            await _giveawayRepo.AddAsync(giveaway);
            await _giveawayRepo.SaveAsync();
        }

        [OnCommand("!gstart")]
        [Description("Instantly starts a giveaway.")]
        [CommandUsage("`!gstart <duration> <prize>`")]
        public async Task StartGiveaway(IMessage message)
        {
            var commandParams = message.Content.Split(' ').Skip(1).ToList();
            var durationTuple = ParseDuration(commandParams.FirstOrDefault());
            var prize = string.Join(" ", commandParams.Skip(1));
            var channel = message.Channel;

            var giveaway = CreateGiveaway(message.Author.Username);
            giveaway.TargetChannel = message.Channel.Id.ToString();
            giveaway.Duration = CalculateDuration(durationTuple);
            giveaway.NumberOfWinners = 1;
            giveaway.Prize = prize;

            await FinalizeAndAnnounceNewGiveaway(giveaway);
        }

        [OnCommand("!gcancel")]
        [Description("Cancels the last giveaway you created in the channel where the command is sent.")]
        public async Task CancelGiveaway(IMessage message)
        {
            var giveaway = (await _giveawayRepo.GetAllAsync())
                .Where(
                    g =>
                        g.TargetChannel.ToChannelId() == message.Channel.Id
                        && g.WinnerNames == null
                        && g.CreatedBy == message.Author.Username
                )
                .OrderByDescending(g => g.CreatedTimestamp + g.Duration)
                .FirstOrDefault();

            if (giveaway != null)
            {
                giveaway.Duration = 0;
                await ResolveGiveaway(giveaway);
                await message.Channel.SendMessageAsync($"The giveaway for {giveaway.Prize} was cancelled.");
            }
        }

        [OnCommand("!greroll")]
        [Description("Rerolls the last giveaway you created in the channel where the command is sent.")]
        public async Task RerollGiveaway(IMessage message)
        {
            var giveaway = (await _giveawayRepo.GetAllAsync())
                .Where(
                    g =>
                        g.TargetChannel.ToChannelId() == message.Channel.Id
                        && g.WinnerNames != null
                        && g.CreatedBy == message.Author.Username
                )
                .OrderByDescending(g => g.CreatedTimestamp + g.Duration)
                .FirstOrDefault();

            if (giveaway != null)
            {
                await ResolveGiveaway(giveaway);
                await AnnounceWinners(giveaway);
            }
        }

        #region Helpers

        private async Task<IUserMessage> GetGiveawayMessage(Giveaway giveaway)
        {
            var channelId = giveaway.TargetChannel.ToChannelId().Value;
            var channel = _messaging.GetChannelById(channelId) as IMessageChannel;

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
                var winnerNamesString = giveaway.WinnerNames.Any()
                    ? string.Join(", ", giveaway.WinnerNames)
                    : "Could not be determined.";

                embed.Description = $"Giveaway ended.";
                embed.Footer.Text = $"Winners: {winnerNamesString}";
            }
            else
            {
                embed.Description +=
                    $"Time remaining: **{(endsAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToDaysHoursMinutesSeconds()}**";
                embed.Footer.Text =
                    $"Ends at {DateTimeOffset.FromUnixTimeSeconds(endsAt).ToCentralEuropeanTime()} (Server time)";
            }

            embed.Color = VereesaColors.VereesaPurple;

            return embed.Build();
        }

        private Giveaway CreateGiveaway(string authorUsername)
        {
            var giveaway = new Giveaway();
            giveaway.Id = Guid.NewGuid().ToString();
            giveaway.CreatedBy = authorUsername;
            return giveaway;
        }

        #endregion

        #region Periodic updates

        [OnReady]
        public Task InitiateUpdateTimer()
        {
            _jobScheduler.EveryTenSeconds -= UpdateActiveGiveaways;
            _jobScheduler.EveryTenSeconds += UpdateActiveGiveaways;
            return Task.CompletedTask;
        }

        private async Task UpdateActiveGiveaways()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var progressingGiveaways = (await _giveawayRepo.GetAllAsync()).Where(
                ga => now < ga.CreatedTimestamp + ga.Duration
            );
            var unreslovedGiveaways = (await _giveawayRepo.GetAllAsync()).Where(
                ga => now > ga.CreatedTimestamp + ga.Duration && ga.WinnerNames == null
            );

            foreach (var giveaway in progressingGiveaways)
            {
                var message = await GetGiveawayMessage(giveaway);

                try
                {
                    if (message != null)
                    {
                        await message.ModifyAsync(
                            (msg) =>
                            {
                                msg.Embed = GetAnnouncementEmbed(giveaway);
                            }
                        );
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
            var channel = _messaging.GetChannelById(giveaway.TargetChannel.ToChannelId().Value) as IMessageChannel;

            if (giveaway.WinnerNames.Count == 0)
            {
                await channel.SendMessageAsync($"A winner for **{giveaway.Prize}** could not be determined.");
            }
            else
            {
                await channel.SendMessageAsync(
                    $"Congratulations {string.Join(", ", giveaway.WinnerNames)}! You won **{giveaway.Prize}**!"
                );
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
                    var winnerIndex = _rng.Next(0, participants.Count);
                    var winner = participants[winnerIndex];
                    winners.Add(winner);
                    participants.Remove(winner);
                }

                giveaway.WinnerNames = winners.Select(w => w.Username).ToList();
                await _giveawayRepo.AddOrEditAsync(giveaway);
                await _giveawayRepo.SaveAsync();

                await message.ModifyAsync(
                    (msg) =>
                    {
                        msg.Embed = GetAnnouncementEmbed(giveaway);
                    }
                );
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

    [Serializable]
    internal class PromptParseValidationException : Exception
    {
        public PromptParseValidationException() { }

        public PromptParseValidationException(string message)
            : base(message) { }

        public PromptParseValidationException(string message, Exception innerException)
            : base(message, innerException) { }

        protected PromptParseValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
