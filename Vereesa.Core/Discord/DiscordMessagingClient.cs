using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Discord
{
    public class DiscordMessagingClient : IMessagingClient
    {
        private readonly DiscordSettings _settings;
        public DiscordSocketClient Discord { get; }

        public DiscordMessagingClient(DiscordSocketClient discord, DiscordSettings settings)
        {
            Discord = discord;
            _settings = settings;
        }

        public async Task Start()
        {
            await Discord.LoginAsync(TokenType.Bot, _settings.Token);
            await Discord.StartAsync();
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

        public async Task<IMessage> SendMessageToChannelByIdAsync(ulong channelId, string message, Embed embed = null)
        {
            var channel = await Discord.GetChannelAsync(channelId);

            if (channel is IMessageChannel messageChannel)
            {
                return await messageChannel.SendMessageAsync(message, embed: embed);
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

        public async Task Stop()
        {
            await Discord.LogoutAsync();
            Discord.Dispose();
        }

        public async Task<IMessage> SendMessageToUserByIdAsync(ulong userId, string message, Embed embed = null)
        {
            var user = Discord.GetUser(userId);
            if (user == null)
            {
                throw new Exception($"User with id {userId} not found.");
            }

            return await user.SendMessageAsync(message, embed: embed);
        }
    }
}
