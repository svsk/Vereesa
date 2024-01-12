using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Extensions;

namespace Vereesa.Core.Infrastructure
{
    public enum WellknownRole : ulong
    {
        Officer = 124251615489294337
    }

    public class VereesaEmoji
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
    }

    public class VereesaReaction
    {
        public IUser User { get; set; }
        public IEmote Emote { get; set; }
    }

    public interface IEmojiClient
    {
        string GetCustomEmoji(string emojiName);
        IReadOnlyCollection<VereesaEmoji> GetCustomEmojiByServerId(ulong neonGuildId);
        Task<VereesaEmoji> CreateCustomEmoji(ulong guildId, string emojiName, Image emoteImage);
    }

    public interface IMessagingClient
    {
        Task<IMessage> Prompt(IUser author, string prompt, IMessageChannel channel, int timeout = 15000);
        Task<IMessage> Prompt(WellknownRole role, string prompt, IMessageChannel channel, int timeout = 15000);
        Task<IMessage> Prompt(ulong roleId, string prompt, IMessageChannel channel, int timeout = 15000);
        Task SendMessageToChannelByIdAsync(ulong channelId, string message);
        IReadOnlyCollection<IGuild> GetServers();
        Task<IMessage> GetMessageById(ulong channelId, ulong messageId);
        IChannel GetChannelById(ulong channelId);

        // Maybe move out?
        List<IRole> GetRolesByName(string roleName, bool ignoreCase = false);
        IEnumerable<IUser> GetServerUsersById(ulong serverId);
        IMessageChannel GetChannelById(object notificationMessageChannelId);
        string EscapeSelfMentions(string message);
    }

    public interface IBotService { }

    /// <summary>
    /// Inheriting this class causes a singleton instance of it to automatically start in VereesaClient.cs
    /// </summary>
    public class BotServiceBase<T>
        where T : IBotService
    {
        private T _service;

        protected DiscordSocketClient Discord { get; }
        private readonly ILogger<BotServiceBase<T>> _logger;

        public BotServiceBase(T service, DiscordSocketClient discord, ILogger<BotServiceBase<T>> logger)
        {
            _service = service;
            Discord = discord;
            _logger = logger;
            BindCommands();
        }

        private void BindCommands()
        {
            var commandMethods = new List<(string command, MethodInfo method)>();
            var onButtonClickMethods = new List<(string buttonId, MethodInfo method)>();
            var onSelectMenuMethods = new List<(string componentId, MethodInfo method)>();
            var memberUpdatedMethods = new List<MethodInfo>();
            var onMessageMethods = new List<MethodInfo>();
            var onReadyMethods = new List<MethodInfo>();
            var onReactionMethods = new List<MethodInfo>();
            var onMentionMethods = new List<MethodInfo>();
            var onResponseMethods = new List<MethodInfo>();
            var onUserJoinedMethods = new List<MethodInfo>();
            var onIntervalMethods = new List<MethodInfo>();
            var onVoiceStateChangeMethods = new List<MethodInfo>();

            var allMethods = _service.GetType().GetMethods();

            Console.WriteLine(_service.GetType().Name);

            foreach (var method in allMethods)
            {
                var commandsAttributes = method.GetCustomAttributes(true).OfType<OnCommandAttribute>();
                commandMethods.AddRange(commandsAttributes.Select(c => (c.Command, method)));

                var buttonClickAttributes = method.GetCustomAttributes(true).OfType<OnButtonClickAttribute>();
                onButtonClickMethods.AddRange(buttonClickAttributes.Select(c => (c.ButtonId, method)));

                var selectMenuAttributes = method.GetCustomAttributes(true).OfType<OnSelectMenuExecutedAttribute>();
                onSelectMenuMethods.AddRange(selectMenuAttributes.Select(c => (c.CustomId, method)));

                if (method.GetCustomAttributes(true).OfType<OnMemberUpdatedAttribute>().Any())
                {
                    memberUpdatedMethods.Add(method);
                }

                if (method.GetCustomAttribute<OnMessageAttribute>(true) != null)
                {
                    onMessageMethods.Add(method);
                }

                if (method.GetCustomAttribute<OnReadyAttribute>(true) != null)
                {
                    onReadyMethods.Add(method);
                }

                if (method.GetCustomAttribute<OnReactionAttribute>(true) != null)
                {
                    onReactionMethods.Add(method);
                }

                if (method.GetCustomAttribute<OnMentionAttribute>(true) != null)
                {
                    onMentionMethods.Add(method);
                }

                if (method.GetCustomAttribute<OnUserJoinedAttribute>(true) != null)
                {
                    onUserJoinedMethods.Add(method);
                }

                if (method.GetCustomAttribute<OnIntervalAttribute>(true) != null)
                {
                    onIntervalMethods.Add(method);
                }

                if (method.GetCustomAttribute<OnVoiceStateChangeAttribute>(true) != null)
                {
                    onVoiceStateChangeMethods.Add(method);
                }
            }

            var commandHandlers = commandMethods.GroupBy(c => c.command).ToList();
            if (commandHandlers.Any())
            {
                Discord.MessageReceived += async (messageForEvaluation) =>
                {
                    if (GetBestMatchingCommandHandler(messageForEvaluation, commandHandlers) is { } commandHandler)
                    {
                        await ExecuteMessageHandlerAsync(messageForEvaluation, commandHandler);
                    }
                };
            }

            if (onButtonClickMethods.Any())
            {
                Discord.ButtonExecuted += async (interaction) =>
                {
                    var methods = GetBestMatchingMethods(onButtonClickMethods, interaction.Data.CustomId);
                    await interaction.DeferAsync();
                    await ExecuteHandlersAsync(methods, new[] { interaction });
                };
            }

            if (onSelectMenuMethods.Any())
            {
                Discord.SelectMenuExecuted += async (interaction) =>
                {
                    var methods = GetBestMatchingMethods(onSelectMenuMethods, interaction.Data.CustomId);
                    await interaction.DeferAsync();
                    await ExecuteHandlersAsync(methods, new[] { interaction });
                };
            }

            if (onMentionMethods.Any())
            {
                Discord.MessageReceived += async (message) =>
                {
                    if (message.MentionedUsers.Any(u => u.Id == Discord.CurrentUser.Id))
                    {
                        await ExecuteHandlersAsync(onMentionMethods, new[] { message });
                    }
                };
            }

            if (memberUpdatedMethods.Any())
            {
                Discord.GuildMemberUpdated += async (cacheUserBefore, userAfter) =>
                {
                    var userBefore = await cacheUserBefore.GetOrDownloadAsync();
                    await ExecuteHandlersAsync(memberUpdatedMethods, new[] { userBefore, userAfter });
                };
            }

            if (onMessageMethods.Any())
            {
                Discord.MessageReceived += async (message) =>
                {
                    await ExecuteHandlersAsync(onMessageMethods, new[] { message });
                };
            }

            if (onReadyMethods.Any())
            {
                Discord.Ready += async () =>
                {
                    await ExecuteHandlersAsync(onReadyMethods, new object[0]);
                };
            }

            if (onReactionMethods.Any())
            {
                Discord.ReactionAdded += async (message, channel, reaction) =>
                {
                    var vReaction = new VereesaReaction { User = reaction.User.Value, Emote = reaction.Emote };
                    await ExecuteHandlersAsync(
                        onReactionMethods,
                        new object[] { message.Id, channel.Value, vReaction }
                    );
                };
            }

            if (onUserJoinedMethods.Any())
            {
                Discord.UserJoined += async (user) =>
                {
                    await ExecuteHandlersAsync(onUserJoinedMethods, new object[] { user });
                };
            }

            if (onVoiceStateChangeMethods.Any())
            {
                Discord.UserVoiceStateUpdated += async (user, oldState, newState) =>
                {
                    await ExecuteHandlersAsync(onVoiceStateChangeMethods, new object[] { user, oldState, newState });
                };
            }
        }

        private List<MethodInfo> GetBestMatchingMethods(List<(string, MethodInfo)> handlers, string methodKey)
        {
            return handlers
                .OrderByDescending(cd => cd.Item1.Length)
                .Where(ch => methodKey.Equals(ch.Item1, StringComparison.CurrentCultureIgnoreCase))
                .Select(p => p.Item2)
                .ToList();
        }

        // this code is super similar to the message handler
        private async Task ExecuteHandlersAsync(List<MethodInfo> methods, object[] parameters)
        {
            async Task ExecuteHandler(MethodInfo method, object[] invocationParameters)
            {
                try
                {
                    if (method.ReturnType == typeof(Task))
                    {
                        await (Task)method.Invoke(_service, invocationParameters);
                    }
                    else
                    {
                        method.Invoke(_service, invocationParameters);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "Failed to invoke handler {HandlerName} on {Class}",
                        method.Name,
                        method.DeclaringType.Name
                    );
                }
            }

            foreach (var method in methods)
            {
                async Task DoExecuteHandler()
                {
                    try
                    {
                        await ExecuteHandler(method, parameters);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(
                            ex,
                            "Failed to invoke handler {HandlerName} on {Class}",
                            method.Name,
                            method.DeclaringType.Name
                        );
                    }
                }

                if (method.GetCustomAttribute<AsyncHandlerAttribute>() != null)
                {
                    _ = Task.Run(async () =>
                    {
                        await DoExecuteHandler();
                    });
                }
                else
                {
                    await DoExecuteHandler();
                }
            }
        }

        private async Task ExecuteMessageHandlerAsync(
            IMessage messageToHandle,
            (string command, MethodInfo method) commandHandler
        )
        {
            async Task ExecuteCommand(string command, MethodInfo handler)
            {
                try
                {
                    var handlerParams = BuildHandlerParamList(command, handler, messageToHandle);
                    var parameters = handler
                        .GetParameters()
                        .Select((para, index) => handlerParams.ElementAtOrDefault(index))
                        .ToArray();

                    await (Task)handler.Invoke(_service, parameters);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "Failed to invoke handler {HandlerName} on {Class}",
                        handler.Name,
                        handler.DeclaringType.Name
                    );

                    if (
                        handler.GetCustomAttribute(typeof(CommandUsageAttribute))
                        is CommandUsageAttribute usageAttribute
                    )
                    {
                        await messageToHandle.Channel.SendMessageAsync(
                            $"`{command}` usage: {usageAttribute.UsageDescription}"
                        );
                    }
                    else
                    {
                        await messageToHandle.Channel.SendMessageAsync(
                            $"I wasn't able to do that. You sure you did that right? Tell Vein to write a usage description for `{command}` to help."
                        );
                    }
                }
            }

            if (!UserCanExecute(messageToHandle.Author, commandHandler.method))
            {
                return;
            }

            // If the command is marked Async, we just fire and forget, because it may be really long
            // running.
            if (commandHandler.method.GetCustomAttribute<AsyncHandlerAttribute>() != null)
            {
                _ = ExecuteCommand(commandHandler.command, commandHandler.method);
            }
            else
            {
                await ExecuteCommand(commandHandler.command, commandHandler.method);
            }
        }

        private List<object> BuildHandlerParamList(string command, MethodInfo handler, IMessage sourceMessage)
        {
            var commandArgString = sourceMessage.Content.Substring(command.Length).Trim().ToArray();

            var args = new List<string>();
            if (commandArgString.Length > 0)
            {
                args.Add("");
                var isInQuotes = false;
                for (var i = 0; i < commandArgString.Length; i++)
                {
                    if (commandArgString[i] == '"')
                    {
                        isInQuotes = !isInQuotes;
                    }
                    else if (commandArgString[i] == ' ' && !isInQuotes)
                    {
                        // new argument
                        args.Add("");
                    }
                    else
                    {
                        args[args.Count - 1] += commandArgString[i];
                    }
                }
            }

            var argAttributes = handler
                .GetCustomAttributes<WithArgumentAttribute>()
                .OrderByDescending(attr => attr.ArgumentIndex)
                .ToList();

            var handlerParams = new List<object>();
            if (!argAttributes.Any())
            {
                handlerParams.AddRange(args);
            }
            else
            {
                foreach (var attr in argAttributes)
                {
                    handlerParams.Add(string.Join(' ', args.Skip(attr.ArgumentIndex)));
                    args = args.Take(attr.ArgumentIndex).ToList();
                }

                handlerParams.Reverse();
            }

            handlerParams.Insert(0, sourceMessage);

            return handlerParams;
        }

        private (string command, MethodInfo method)? GetBestMatchingCommandHandler(
            IMessage messageForEvaluation,
            List<IGrouping<string, (string command, MethodInfo method)>> commandHandlers
        )
        {
            var messageContent = messageForEvaluation.Content;
            return commandHandlers
                .OrderByDescending(cd => cd.Key.Length)
                .FirstOrDefault(ch => messageContent.StartsWith(ch.Key, StringComparison.CurrentCultureIgnoreCase))
                ?.FirstOrDefault();
        }

        private bool UserCanExecute(IUser caller, MethodInfo method)
        {
            var isAuthorized = true;
            var authorizeAttributes = method.GetCustomAttributes(true).OfType<AuthorizeAttribute>();
            var guildUser = caller as IGuildUser;
            var userRoles = guildUser?.RoleIds.Select(rid => Discord.GetRole(rid)).ToList();

            foreach (var authorizeAttribute in authorizeAttributes)
            {
                if (!userRoles.Any(r => r.Name == authorizeAttribute.RoleName || r.Id == authorizeAttribute.RoleId))
                {
                    isAuthorized = false;
                }
            }

            return isAuthorized;
        }
    }

    public class DiscordMessagingClient : IMessagingClient
    {
        public DiscordSocketClient Discord { get; }

        public DiscordMessagingClient(DiscordSocketClient discord)
        {
            Discord = discord;
        }

        /// <summary>
        /// Prompts a role for a response. Potentially a long lasting request. This should always be put in an
        /// async event handler.
        /// </summary>
        /// <param name="role">The role responsible for responding to the prompt.</param>
        /// <param name="promptMessage">The message sent to the prompted role.</param>
        /// <param name="channel">The channel in which to prompt the role.</param>
        /// <param name="timeoutMs">Duration in milliseconds to wait for a response.</param>
        /// <returns>The first message sent by a person with the prompted role in the selected channel.
        /// Null if no one in the responsible role responds before the timeout.</returns>
        public Task<IMessage> Prompt(
            WellknownRole role,
            string promptMessage,
            IMessageChannel channel,
            int timeoutMs = 15000
        ) => Prompt((ulong)role, promptMessage, channel, timeoutMs);

        /// <summary>
        /// Prompts a role for a response. Potentially a long lasting request. This should always be put in an
        /// async event handler.
        /// </summary>
        /// <param name="role">The role responsible for responding to the prompt.</param>
        /// <param name="promptMessage">The message sent to the prompted role.</param>
        /// <param name="channel">The channel in which to prompt the role.</param>
        /// <param name="timeoutMs">Duration in milliseconds to wait for a response.</param>
        /// <returns>The first message sent by a person with the prompted role in the selected channel.
        /// Null if no one in the responsible role responds before the timeout.</returns>
        public Task<IMessage> Prompt(
            ulong roleId,
            string promptMessage,
            IMessageChannel channel,
            int timeoutMs = 15000
        ) =>
            Prompt(
                Discord.GetRole(roleId),
                promptMessage,
                channel,
                (u) => ((IGuildUser)u).RoleIds.Contains(roleId),
                timeoutMs
            );

        /// <summary>
        /// Prompts a user for a response. Potentially a long lasting request. This should always be put in an
        /// async event handler.
        /// </summary>
        /// <param name="user">The user responsible for responding to the prompt.</param>
        /// <param name="promptMessage">The message sent to the prompted user.</param>
        /// <param name="channel">The channel in which to prompt the user.</param>
        /// <param name="timeoutMs">Duration in milliseconds to wait for a response.</param>
        /// <returns>The first message sent by the prompted user in the selected channel. Null if there is no response
        /// from the responsible user before the timeout.</returns>
        public Task<IMessage> Prompt(
            IUser user,
            string promptMessage,
            IMessageChannel channel,
            int timeoutMs = 15000
        ) => Prompt(user, promptMessage, channel, (u) => u.Id == user.Id, timeoutMs);

        private Task<IMessage> Prompt(
            IMentionable responsible,
            string promptMessage,
            IMessageChannel channel,
            Func<IUser, bool> authorIsResponsible,
            int timeoutMs
        )
        {
            return Task.Run(async () =>
            {
                IMessage response = null;

                Task AwaitResponse(IMessage msg)
                {
                    if (msg.Channel.Id == channel.Id && authorIsResponsible(msg.Author))
                    {
                        response = msg;
                    }

                    return Task.CompletedTask;
                }

                Discord.MessageReceived += AwaitResponse;
                await channel.SendMessageAsync($"{responsible.Mention} {promptMessage}");

                var sw = new Stopwatch();
                sw.Start();

                while (response == null && sw.ElapsedMilliseconds < timeoutMs)
                {
                    await Task.Delay(250);
                }

                Discord.MessageReceived -= AwaitResponse;
                sw.Stop();

                return response;
            });
        }

        public async Task SendMessageToChannelByIdAsync(ulong channelId, string message)
        {
            var channel = await Discord.GetChannelAsync(channelId);

            if (channel is IMessageChannel messageChannel)
            {
                await messageChannel.SendMessageAsync(message);
            }
            else
            {
                throw new Exception($"Channel with id {channelId} is not a message channel.");
            }
        }

        public List<IRole> GetRolesByName(string roleName, bool ignoreCase)
        {
            return Discord.Guilds
                .SelectMany(g => g.Roles)
                .Where(
                    r =>
                        r.Name.Equals(
                            roleName,
                            ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture
                        )
                )
                .OfType<IRole>()
                .ToList();
        }

        public async Task<IMessage> GetMessageById(ulong channelId, ulong messageId)
        {
            var channel = await Discord.GetChannelAsync(channelId);

            if (channel is IMessageChannel messageChannel)
            {
                var message = await messageChannel.GetMessageAsync(messageId);
                return message;
            }

            throw new Exception($"Channel with id {channelId} is not a message channel.");
        }

        public IReadOnlyCollection<IGuild> GetServers()
        {
            return Discord.Guilds.OfType<IGuild>().ToList().AsReadOnly();
        }

        public IEnumerable<IUser> GetServerUsersById(ulong serverId)
        {
            return Discord.Guilds.FirstOrDefault(g => g.Id == serverId)?.Users;
        }

        public IChannel GetChannelById(ulong channelId)
        {
            return Discord.GetChannel(channelId);
        }

        public IMessageChannel GetChannelById(object notificationMessageChannelId)
        {
            throw new NotImplementedException();
        }

        public string EscapeSelfMentions(string message)
        {
            if (message == null)
                return null;

            return message.Replace("@Vereesa", "Vereesa").Replace($"<@{Discord.CurrentUser.Id}>", "Vereesa");
        }
    }

    public class DiscordEmojiClient : IEmojiClient
    {
        public DiscordSocketClient Discord { get; }

        public DiscordEmojiClient(DiscordSocketClient discord)
        {
            Discord = discord;
        }

        public string GetCustomEmoji(string emojiName)
        {
            // Make this dynamic?
            var guildName = "Neon";

            var emoji = Discord.Guilds
                .FirstOrDefault(g => g.Name == guildName)
                ?.Emotes.FirstOrDefault(e => e.Name.ToLower() == emojiName.ToLower());

            if (emoji == null)
                return null;

            return $"<:{emoji.Name}:{emoji.Id}>";
        }

        public IReadOnlyCollection<VereesaEmoji> GetCustomEmojiByServerId(ulong guildId)
        {
            var emotes = Discord.GetGuild(guildId).Emotes;
            return emotes.Select(e => new VereesaEmoji { Id = e.Id, Name = e.Name }).ToList();
        }

        public async Task<VereesaEmoji> CreateCustomEmoji(ulong guildId, string emojiName, Image emoteImage)
        {
            var emote = await Discord.GetGuild(guildId).CreateEmoteAsync(emojiName, emoteImage);
            return new VereesaEmoji { Id = emote.Id, Name = emote.Name };
        }
    }
}
