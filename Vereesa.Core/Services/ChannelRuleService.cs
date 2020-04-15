using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Configuration;
using Vereesa.Core.Helpers;

namespace Vereesa.Core.Services
{
    public class ChannelRuleService
    {
        private DiscordSocketClient _discord;
        private List<ChannelRuleset> _rulesets;
        private Timer _triggerInterval;

        public ChannelRuleService(DiscordSocketClient discord, ChannelRuleSettings config)
        {
            _discord = discord;
            _discord.MessageReceived += MessageReceivedHandler;
            
            _discord.Ready -= InitializeServiceAsync;
            _discord.Ready += InitializeServiceAsync;
        }

        private async Task InitializeServiceAsync()
        {
            _rulesets = new List<ChannelRuleset>();
            _triggerInterval = await TimerHelpers.SetTimeoutAsync(TriggerScheduledRulesetsAsync, 30000, true, false);

            //Define in config later
            //ulong mediaChannelId = 124446036637908995; //botplayground channel id
            ulong mediaChannelId = 544279733530263593; // Media channel
            
            var ruleset = new ChannelRuleset(mediaChannelId, RuleComplianceLevel.Any); 
            ruleset.Triggers.Add(RulesetTriggers.Periodic);
            ruleset.Triggers.Add(RulesetTriggers.OnMessage);

            ruleset.AddRule(new ChannelRule(ChannelRuleEvaluators.MessageMustContainImage));
            ruleset.AddRule(new ChannelRule(ChannelRuleEvaluators.MessageMustContainYoutubeLink));
            ruleset.AddRule(new ChannelRule(ChannelRuleEvaluators.MessageMustContainTwitchLink));
            ruleset.AddRulesBrokenReaction(new RuleReaction(ChannelRuleReactions.DeleteMessageAsync));
            ruleset.AddRulesBrokenReaction(new RuleReaction<string>(ChannelRuleReactions.AnnounceChannelRulesAsync, "Sorry! :sparkles: Only images :frame_photo:, direct links to images :link:, and links to YouTube or Twitch videos :tv: can be posted in this channel! :pray:"));

            //Test rules and reactions
            // ruleset.AddRule(new ChannelRule<string>(ChannelRuleEvaluators.MessageTextMustContain, "Yas"));
            // ruleset.AddRulesUpheldReaction(new RuleReaction<string>(ChannelRuleReactions.ReactToMessage, ":pogchamp:"));
            // ruleset.AddRulesUpheldReaction(new RuleReaction<string>(ChannelRuleReactions.RespondToMessage, "nice"));
            // ruleset.AddRulesUpheldReaction(new RuleReaction(ChannelRuleReactions.PinMessage));
            //ruleset.AddRulesBrokenReaction(new RuleReaction<string>(ChannelRuleReactions.RespondToMessage, "Only image posts and direct links to images can be posted in this channel."));

            _rulesets.Add(ruleset);
        }

        private async Task TriggerScheduledRulesetsAsync()
        {
            var channelRulesets = _rulesets.Where(rs => rs.Triggers.Contains(RulesetTriggers.Periodic)).GroupBy(rs => rs.ChannelId);

            foreach (var channelRuleset in channelRulesets) 
            {
                try 
                 {
                    var messageChannel = (IMessageChannel)_discord.GetChannel(channelRuleset.Key);
                    var messages = await messageChannel.GetMessagesAsync(20).Flatten().ToList();

                    foreach (var message in messages) 
                    {
                        await EvaluateMessageAsync(message, channelRuleset.ToList());
                    }
                }
                catch 
                {

                }
            }
        }

        private async Task MessageReceivedHandler(IMessage receivedMessage)
        {
            var onMessageRulesets = _rulesets.Where(rs => rs.ChannelId == receivedMessage.Channel.Id && rs.Triggers.Contains(RulesetTriggers.OnMessage)).ToList();

            await EvaluateMessageAsync(receivedMessage, onMessageRulesets);
        }

        private async Task EvaluateMessageAsync(IMessage message, List<ChannelRuleset> activeRulesets)
        {
            if (message.Author.IsBot)
                return;

            foreach (var ruleset in activeRulesets) 
            {
                bool messageComplies = false;
                var evaluationResults = ruleset.Rules.Select(rule => rule.Evaluate(message)).ToList();

                if (ruleset.ComplianceLevel == RuleComplianceLevel.Any)
                    messageComplies = evaluationResults.Any(result => result == true);
                else
                    messageComplies = evaluationResults.All(result => result == true); 

                
                if (messageComplies)
                    await ruleset.InvokeRulesUpheldReactionsAsync(message);
                else
                    await ruleset.InvokeRulesBrokenReactionsAsync(message);
            }
        }
    }

    //Ruleset

    public enum RuleComplianceLevel 
    {
        All,
        Any
    }

    public class ChannelRuleset
    {
        public RuleComplianceLevel ComplianceLevel { get; }
        public ulong ChannelId { get; }
        public List<IChannelRule> Rules { get; }
        public List<RulesetTriggers> Triggers { get; }

        private List<IRuleReaction> _rulesUpheldReactions;
        private List<IRuleReaction> _rulesBrokenReactions;

        public ChannelRuleset(ulong channelId, RuleComplianceLevel complianceLevel) 
        {
            ComplianceLevel = complianceLevel;
            ChannelId = channelId;
            
            Rules = new List<IChannelRule>();
            Triggers = new List<RulesetTriggers>();

            _rulesUpheldReactions = new List<IRuleReaction>();
            _rulesBrokenReactions = new List<IRuleReaction>();
        }

        public void AddRule(IChannelRule rule) 
        {
            Rules.Add(rule);
        }

        public void AddRulesBrokenReaction(IRuleReaction ruleReaction)
        {
            _rulesBrokenReactions.Add(ruleReaction);
        }

        public void AddRulesUpheldReaction(IRuleReaction ruleReaction)
        {
            _rulesUpheldReactions.Add(ruleReaction);
        }

        public async Task InvokeRulesUpheldReactionsAsync(IMessage message)
        {
            foreach (var reaction in _rulesUpheldReactions) 
            {
                await reaction.InvokeAsync(message);
            }
        }

        public async Task InvokeRulesBrokenReactionsAsync(IMessage message)
        {
            foreach (var reaction in _rulesBrokenReactions)
            {
                await reaction.InvokeAsync(message);
            }
        }
    }

    //Channel Rules
    
    public interface IChannelRule
    {
        bool Evaluate(IMessage message);
    }
    
    public class ChannelRule : IChannelRule
    {
        private Func<IMessage, bool> _ruleEvaluator;

        public ChannelRule(Func<IMessage, bool> ruleEvaluator) 
        {
            _ruleEvaluator = ruleEvaluator;
        }

        public bool Evaluate(IMessage message)
        {
            return _ruleEvaluator.Invoke(message);
        }
    }

    public class ChannelRule<T> : IChannelRule
    {
        private T _ruleParameter;
        private Func<IMessage, T, bool> _ruleEvaluator;

        public ChannelRule(Func<IMessage, T, bool> ruleEvaluator, T ruleParameter)
        {
            _ruleEvaluator = ruleEvaluator;
            _ruleParameter = ruleParameter;
        }

        public bool Evaluate(IMessage message)
        {
            return _ruleEvaluator.Invoke(message, _ruleParameter);
        }
    }

    public class ChannelRuleEvaluators
    {
        public static bool MessageTextMustContain(IMessage message, string mustExistString) 
        {
            return message.Content.Contains(mustExistString);
        }

        public static bool MessageMustContainImage(IMessage message) 
        {
            var imagePattern = @"https?:\/\/(.*?)\/(.*?)\.(png|jpg|jpeg|gif)";

            if (Regex.IsMatch(message.Content, imagePattern) || message.Attachments.Any(att => Regex.IsMatch(att.Url, imagePattern)))
            {
                return true;
            }

            if (message.Embeds.Any(embed => embed.Image != null)) 
            {
                return true;
            }
            
            return false;
        }

        public static bool MessageMustContainYoutubeLink(IMessage message) 
        {
            var youtubePattern = @"http(?:s?):\/\/(?:www\.)?youtu(?:be\.com\/watch\?v=|\.be\/)([\w\-\\_]*)(&(amp;)?‌​[\w\?‌​=]*)?";

            if (Regex.IsMatch(message.Content, youtubePattern) || message.Attachments.Any(att => Regex.IsMatch(att.Url, youtubePattern)))
            {
                return true;
            }
            
            return false;
        }

        public static bool MessageMustContainTwitchLink(IMessage message)
        {
            var twitchPattern = @"http(?:s?):\/\/www.twitch.tv\/videos\/(\d{1,20})";

            if (Regex.IsMatch(message.Content, twitchPattern) || message.Attachments.Any(att => Regex.IsMatch(att.Url, twitchPattern)))
            {
                return true;
            }

            return false;
        }
    }

    

    

    // Reactions

    public interface IRuleReaction
    {
        Task InvokeAsync(IMessage message);
    }

    public class RuleReaction : IRuleReaction
    {
        private Func<IMessage, Task> _reactionAction;

        public RuleReaction(Func<IMessage, Task> reactionAction)
        {
            _reactionAction = reactionAction;
        }

        public async Task InvokeAsync(IMessage message)
        {
            await _reactionAction.Invoke(message);
        }
    }

    public class RuleReaction<T> : IRuleReaction
    {
        private T _reactionParameter;
        private Func<IMessage, T, Task> _reactionAction;

        public RuleReaction(Func<IMessage, T, Task> reactionAction, T reactionParameter)
        {
            _reactionAction = reactionAction;
            _reactionParameter = reactionParameter;
        }

        public async Task InvokeAsync(IMessage message)
        {
            await _reactionAction.Invoke(message, _reactionParameter);
        }
    }

    public class ChannelRuleReactions
    {
        public static async Task RespondToMessageAsync(IMessage message, string response)
        {
            await message.Channel.SendMessageAsync(response);
        }

        public static async Task DeleteMessageAsync(IMessage message)
        {
            await message.DeleteAsync();
        }

        public static async Task PinMessageAsync(IMessage message)
        {
            await ((IUserMessage)message).PinAsync();
        }

        public static async Task ReactToMessageAsync(IMessage message, string reactionEmoji)
        {
            IEmote emote = null;

            //Emojis will yield 2 if string.Length is used.
            if (new StringInfo(reactionEmoji).LengthInTextElements == 1)
            {
                emote = new Emoji(reactionEmoji);
            } 
            else 
            {
                reactionEmoji = reactionEmoji.Replace(":", string.Empty);
                emote = ((SocketGuildChannel)message.Channel).Guild.Emotes.FirstOrDefault(e => e.Name == reactionEmoji);
            }

            if (emote != null)
            {
                await ((IUserMessage)message).AddReactionAsync(emote);
            }
        }

        public static async Task AnnounceChannelRulesAsync(IMessage message, string channelRules)
        {
            //Check if rules have been posted lately (within the last 5 posts).
            var previousMessages = await message.Channel.GetMessagesAsync(5).Flatten().ToList();
            var shouldPostRules = previousMessages.Any(msg => msg.Author.IsBot && msg.Content == channelRules) == false;

            if (shouldPostRules) 
            {
                await message.Channel.SendMessageAsync(channelRules);
            }
        }
    }





    public enum RulesetTriggers
    {
        Periodic,
        OnMessage
    }
}