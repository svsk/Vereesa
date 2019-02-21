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
            _triggerInterval = TimerHelpers.SetTimeout(TriggerScheduledRulesets, 30000, true, false);
            _rulesets = new List<ChannelRuleset>();

            
        }

        private void TriggerScheduledRulesets()
        {
            var channelRulesets = _rulesets.Where(rs => rs.Triggers.Contains(RulesetTriggers.Periodic)).GroupBy(rs => rs.ChannelId);

            foreach (var channelRuleset in channelRulesets) 
            {
                try 
                 {
                    var messageChannel = (IMessageChannel)_discord.GetChannel(channelRuleset.Key);
                    var messages = messageChannel.GetMessagesAsync(20).Flatten().ToList().GetAwaiter().GetResult();

                    foreach (var message in messages) 
                    {
                        EvaluateMessage(message, channelRuleset.ToList());
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

            EvaluateMessage(receivedMessage, onMessageRulesets);
        }

        private void EvaluateMessage(IMessage message, List<ChannelRuleset> activeRulesets)
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
                    ruleset.InvokeRulesUpheldReactions(message);
                else
                    ruleset.InvokeRulesBrokenReactions(message);
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

        public void InvokeRulesUpheldReactions(IMessage message)
        {
            _rulesUpheldReactions.ForEach(reaction => reaction.Invoke(message));
        }

        public void InvokeRulesBrokenReactions(IMessage message)
        {
            _rulesBrokenReactions.ForEach(reaction => reaction.Invoke(message));
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
    }

    

    

    // Reactions

    public interface IRuleReaction
    {
        void Invoke(IMessage message);
    }

    public class RuleReaction : IRuleReaction
    {
        private Action<IMessage> _reactionAction;

        public RuleReaction(Action<IMessage> reactionAction)
        {
            _reactionAction = reactionAction;
        }

        public void Invoke(IMessage message)
        {
            _reactionAction.Invoke(message);
        }
    }

    public class RuleReaction<T> : IRuleReaction
    {
        private T _reactionParameter;
        private Action<IMessage, T> _reactionAction;

        public RuleReaction(Action<IMessage, T> reactionAction, T reactionParameter)
        {
            _reactionAction = reactionAction;
            _reactionParameter = reactionParameter;
        }

        public void Invoke(IMessage message)
        {
            _reactionAction.Invoke(message, _reactionParameter);
        }
    }

    public class ChannelRuleReactions
    {
        public static void RespondToMessage(IMessage message, string response)
        {
            message.Channel.SendMessageAsync(response).GetAwaiter().GetResult();
        }

        public static void DeleteMessage(IMessage message)
        {
            message.DeleteAsync().GetAwaiter().GetResult();
        }

        public static void PinMessage(IMessage message)
        {
            ((IUserMessage)message).PinAsync().GetAwaiter().GetResult();
        }

        public static void ReactToMessage(IMessage message, string reactionEmoji)
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
                ((IUserMessage)message).AddReactionAsync(emote).GetAwaiter().GetResult();
            }
        }

        public static void AnnounceChannelRules(IMessage message, string channelRules)
        {
            //Check if rules have been posted lately (within the last 5 posts).
            var previousMessages = message.Channel.GetMessagesAsync(5).Flatten().ToList().GetAwaiter().GetResult();
            var shouldPostRules = previousMessages.Any(msg => msg.Author.IsBot && msg.Content == channelRules) == false;

            if (shouldPostRules) 
            {
                message.Channel.SendMessageAsync(channelRules).GetAwaiter().GetResult();    
            }
        }
    }





    public enum RulesetTriggers
    {
        Periodic,
        OnMessage
    }
}