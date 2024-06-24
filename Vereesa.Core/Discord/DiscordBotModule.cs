using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Core.Models;

namespace Vereesa.Core.Discord
{
    /// <summary>
    /// Inheriting this class causes a singleton instance of it to automatically start in VereesaClient.cs
    /// </summary>
    public class DiscordBotModule<T>
        where T : IBotModule
    {
        private T _service;

        private readonly IJobScheduler _jobScheduler;
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<DiscordBotModule<T>> _logger;
        private bool _ready = false;

        public DiscordBotModule(
            T service,
            IJobScheduler jobScheduler,
            DiscordSocketClient discord,
            ILogger<DiscordBotModule<T>> logger
        )
        {
            _jobScheduler = jobScheduler;
            _service = service;
            _discord = discord;
            _logger = logger;
            _discord.Ready += RegisterSlashCommandsToGuild;
            BindCommands();
            BindIntervalHandlers();
        }

        private async Task RegisterSlashCommandsToGuild()
        {
            _ready = true;

            var commandMethods = new List<(string command, MethodInfo method)>();

            foreach (var guild in _discord.Guilds)
            {
                var methods = _service.GetType().GetMethods();

                var slashCommandMethods = methods
                    .Where(m => m.GetCustomAttribute<SlashCommandAttribute>() != null)
                    .Select(m => new { Attributes = m.GetCustomAttribute<SlashCommandAttribute>(), Method = m })
                    .ToList();

                foreach (var command in slashCommandMethods)
                {
                    var builder = new SlashCommandBuilder()
                        .WithName(command.Attributes.Name)
                        .WithDescription(command.Attributes.Description ?? "");

                    var parameters = command.Method.GetParameters().Skip(1);
                    foreach (var parameter in parameters)
                    {
                        builder.AddOption(
                            parameter.Name.ToLower(),
                            InterpretType(parameter.ParameterType),
                            parameter.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                            parameter.GetCustomAttribute<OptionalAttribute>() == null,
                            choices: parameter
                                .GetCustomAttributes<ChoiceAttribute>()
                                .Select(c =>
                                {
                                    var choice = new ApplicationCommandOptionChoiceProperties
                                    {
                                        Name = c.Name,
                                        Value = c.Value
                                    };

                                    return choice;
                                })
                                .ToArray()
                        );
                    }

                    await guild.CreateApplicationCommandAsync(builder.Build());
                    commandMethods.Add((command.Attributes.Name, command.Method));
                }
            }

            if (commandMethods.Any())
            {
                _discord.SlashCommandExecuted += async (interaction) =>
                {
                    var invocationParameters = new List<object> { interaction };
                    var methods = GetBestMatchingMethods(commandMethods, interaction.CommandName);

                    foreach (var method in methods)
                    {
                        // Match items in interaction.Data.Options to parameters in the method
                        // and add them to invocationParameters.
                        var methodParameters = method.GetParameters().Skip(1);
                        foreach (var parameter in methodParameters)
                        {
                            var option = interaction.Data.Options.FirstOrDefault(
                                o => o.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase)
                            );

                            if (option != null)
                            {
                                invocationParameters.Add(option.Value);
                            }
                            else
                            {
                                invocationParameters.Add(null);
                            }
                        }

                        await ExecuteHandlersAsync(new() { method }, invocationParameters.ToArray());
                    }
                };
            }

            _logger.LogInformation("Slash commands registered for {Service}.", _service.GetType().Name);
        }

        private ApplicationCommandOptionType InterpretType(Type parameterType)
        {
            if (parameterType == typeof(string))
            {
                return ApplicationCommandOptionType.String;
            }
            else if (parameterType == typeof(long))
            {
                return ApplicationCommandOptionType.Integer;
            }
            else if (parameterType == typeof(double))
            {
                return ApplicationCommandOptionType.Number;
            }
            else if (parameterType == typeof(bool))
            {
                return ApplicationCommandOptionType.Boolean;
            }
            else if (parameterType == typeof(IUser))
            {
                return ApplicationCommandOptionType.User;
            }
            else if (parameterType == typeof(IChannel))
            {
                return ApplicationCommandOptionType.Channel;
            }
            else if (parameterType == typeof(IRole))
            {
                return ApplicationCommandOptionType.Role;
            }
            else if (parameterType == typeof(int))
            {
                throw new Exception("int is not supported as a parameter type for slash commands. Use long instead.");
            }
            else
            {
                throw new Exception($"Unsupported parameter type {parameterType.Name}");
            }
        }

        private Dictionary<string, DateTime?> _lastInvocationTimes = new();

        private void BindIntervalHandlers()
        {
            // Check for methods on the service that have the OnInterval attribute.
            var intervalMethods = _service
                .GetType()
                .GetMethods()
                .Where(m => m.GetCustomAttribute<OnIntervalAttribute>() != null);

            // If there are any, add a handler to jobSchedulers EverySecond event. The handler
            // should keep track of the interval of each method, and execute it each time the interval
            // has passed.
            if (intervalMethods.Any())
            {
                _jobScheduler.EverySecond += async () =>
                {
                    if (!_ready)
                    {
                        return;
                    }

                    foreach (var method in intervalMethods)
                    {
                        var intervalAttribute = method.GetCustomAttribute<OnIntervalAttribute>();
                        var interval = new TimeSpan(0, 0, intervalAttribute.Minutes, intervalAttribute.Seconds);

                        var lastInvocationTime = _lastInvocationTimes.GetValueOrDefault(method.Name);
                        if (lastInvocationTime == null || DateTime.UtcNow - lastInvocationTime > interval)
                        {
                            try
                            {
                                // Store invocation times in a dictionary, keyed by method name.
                                _lastInvocationTimes[method.Name] = DateTime.UtcNow;
                                await ExecuteHandlersAsync(new() { method }, new object[0]);
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
                    }
                };
            }
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
                _discord.MessageReceived += async (messageForEvaluation) =>
                {
                    if (GetBestMatchingCommandHandler(messageForEvaluation, commandHandlers) is { } commandHandler)
                    {
                        await ExecuteMessageHandlerAsync(messageForEvaluation, commandHandler);
                    }
                };
            }

            if (onButtonClickMethods.Any())
            {
                _discord.ButtonExecuted += async (interaction) =>
                {
                    var methods = GetBestMatchingMethods(onButtonClickMethods, interaction.Data.CustomId);
                    await interaction.DeferAsync();
                    await ExecuteHandlersAsync(methods, new[] { interaction });
                };
            }

            if (onSelectMenuMethods.Any())
            {
                _discord.SelectMenuExecuted += async (interaction) =>
                {
                    var methods = GetBestMatchingMethods(onSelectMenuMethods, interaction.Data.CustomId);
                    await interaction.DeferAsync();
                    await ExecuteHandlersAsync(methods, new[] { interaction });
                };
            }

            if (onMentionMethods.Any())
            {
                _discord.MessageReceived += async (message) =>
                {
                    if (message.MentionedUsers.Any(u => u.Id == _discord.CurrentUser.Id))
                    {
                        await ExecuteHandlersAsync(onMentionMethods, new[] { message });
                    }
                };
            }

            if (memberUpdatedMethods.Any())
            {
                _discord.GuildMemberUpdated += async (cacheUserBefore, userAfter) =>
                {
                    var userBefore = await cacheUserBefore.GetOrDownloadAsync();
                    await ExecuteHandlersAsync(memberUpdatedMethods, new[] { userBefore, userAfter });
                };
            }

            if (onMessageMethods.Any())
            {
                _discord.MessageReceived += async (message) =>
                {
                    await ExecuteHandlersAsync(onMessageMethods, new[] { message });
                };
            }

            if (onReadyMethods.Any())
            {
                _discord.Ready += async () =>
                {
                    await ExecuteHandlersAsync(onReadyMethods, new object[0]);
                };
            }

            if (onReactionMethods.Any())
            {
                _discord.ReactionAdded += async (message, channel, reaction) =>
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
                _discord.UserJoined += async (user) =>
                {
                    await ExecuteHandlersAsync(onUserJoinedMethods, new object[] { user });
                };
            }

            if (onVoiceStateChangeMethods.Any())
            {
                _discord.UserVoiceStateUpdated += async (user, oldState, newState) =>
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
            var userRoles = guildUser?.RoleIds.Select(rid => _discord.GetRole(rid)).ToList();

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
}
