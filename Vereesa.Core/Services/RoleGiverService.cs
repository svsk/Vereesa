using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Vereesa.Core.Extensions;

namespace Vereesa.Core.Services
{
    public class RoleGiverService
    {
        private DiscordSocketClient _discord;
        
        private Dictionary<string, Action<SocketMessage>> _commandMap => new Dictionary<string, Action<SocketMessage>> 
        { 
            { "!join", JoinRole },
            { "!leave", LeaveRole }
        };

        private List<string> _allowedRoles => new List<string> { "Voice Chat Activity" }; //Todo: move this to config or json storage

        public RoleGiverService(DiscordSocketClient discord)
        {
            _discord = discord;
            _discord.MessageReceived += EvaluateMessage;
        }

        private async Task EvaluateMessage(SocketMessage message)
        {
            var command = message.GetCommand();
            
            if (_commandMap.TryGetValue(command, out var action))
                action.Invoke(message);
        }

        private void LeaveRole(SocketMessage message)
        {
            var role = GetRequestedRole(message);
            if (role == null)
                return;

            var guild = _discord.Guilds.FirstOrDefault(g => g.Channels.Any(c => c.Id == message.Channel.Id));
            guild.GetUser(message.Author.Id).RemoveRoleAsync(role).GetAwaiter().GetResult();
            message.Channel.SendMessageAsync($"I have removed the role `{role.Name}` from you, {message.Author.Username}.").GetAwaiter().GetResult();
        }

        private void JoinRole(SocketMessage message)
        {
            var role = GetRequestedRole(message);
            if (role == null)
                return;

            var guild = _discord.Guilds.FirstOrDefault(g => g.Channels.Any(c => c.Id == message.Channel.Id));
            guild.GetUser(message.Author.Id).AddRoleAsync(role).GetAwaiter().GetResult();
            message.Channel.SendMessageAsync($"I have granted you the role `{role.Name}`, {message.Author.Username}.").GetAwaiter().GetResult();
        }

        private SocketRole GetRequestedRole(SocketMessage message)
        {
            var args = message.GetCommandArgs();
            var roleName = string.Join(" ", args);
            
            if (!_allowedRoles.Contains(roleName)) {
                return null;
            }
            
            var guild = _discord.Guilds.FirstOrDefault(g => g.Channels.Any(c => c.Id == message.Channel.Id));
            var requestedRole = guild.Roles.FirstOrDefault(role => role.Name == roleName);

            return requestedRole;
        }
    }
}