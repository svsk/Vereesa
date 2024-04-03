using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core
{
    public interface IMessagingClient
    {
        Task<IMessage> Prompt(IUser author, string prompt, IMessageChannel channel, int timeout = 15000);
        Task<IMessage> Prompt(WellknownRole role, string prompt, IMessageChannel channel, int timeout = 15000);
        Task<IMessage> Prompt(ulong roleId, string prompt, IMessageChannel channel, int timeout = 15000);
        Task<IMessage> SendMessageToChannelByIdAsync(ulong channelId, string message, Embed embed = null);
        Task<IMessage> SendMessageToUserByIdAsync(ulong userId, string message, Embed embed = null);
        IReadOnlyCollection<IGuild> GetServers();
        Task<IMessage> GetMessageById(ulong channelId, ulong messageId);
        IChannel GetChannelById(ulong channelId);

        // Maybe move out?
        List<IRole> GetRolesByName(string roleName, bool ignoreCase = false);
        IEnumerable<IUser> GetServerUsersById(ulong serverId);
        IMessageChannel GetChannelById(object notificationMessageChannelId);
        string EscapeSelfMentions(string message);
        Task Start();
        Task Stop();
    }
}
