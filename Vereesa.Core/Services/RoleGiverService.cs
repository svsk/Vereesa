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
        
        private Dictionary<string, Func<SocketMessage, Task>> _commandMap => new Dictionary<string, Func<SocketMessage, Task>> 
        { 
            { "!join", JoinRoleAsync },
            { "!leave", LeaveRoleAsync }
        };

        private List<string> _allowedRoles => new List<string> { "Voice Chat Activity", "Gambler", "Healer", "Raider", "Tank", "Damage Dealer", "Goblin" }; //Todo: move this to config or json storage

        public RoleGiverService(DiscordSocketClient discord)
        {
            _discord = discord;
            _discord.MessageReceived += EvaluateMessage;
        }

        private async Task EvaluateMessage(SocketMessage message)
        {
            var command = message.GetCommand();
            
            if (command != null && _commandMap.TryGetValue(command, out var action))
                await action.Invoke(message);
        }

        private async Task LeaveRoleAsync(SocketMessage message)
        {
            var role = GetRequestedRole(message);
            if (role == null)
                return;

            var guild = _discord.Guilds.FirstOrDefault(g => g.Channels.Any(c => c.Id == message.Channel.Id));
            await guild.GetUser(message.Author.Id).RemoveRoleAsync(role);
            await message.Channel.SendMessageAsync($"I have removed the role `{role.Name}` from you, {message.Author.Username}.");
        }

        private async Task JoinRoleAsync(SocketMessage message)
        {
            var role = GetRequestedRole(message);
            if (role == null)
                return;

            var guild = _discord.Guilds.FirstOrDefault(g => g.Channels.Any(c => c.Id == message.Channel.Id));
            await guild.GetUser(message.Author.Id).AddRoleAsync(role);
            await message.Channel.SendMessageAsync($"I have granted you the role `{role.Name}`, {message.Author.Username}.");
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