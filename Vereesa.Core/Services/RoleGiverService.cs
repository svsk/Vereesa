using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
    public class RoleGiverService : IBotService
    {
        //Todo: move this to config or json storage
        private List<string> _allowedRoles = new List<string>
        {
            "Voice Chat Activity",
            "Gambler",
            "Healer",
            "Raider",
            "Tank",
            "Damage Dealer",
            "Goblin",
            "PoGo Raider",
            "Bagbois",
            "Alchemy",
            "Blacksmithing",
            "Enchanting",
            "Engineering",
            "Inscription",
            "Jewelcrafting",
            "Leatherworking",
            "Tailoring",
            "Mythic+"
        };
        private IMessagingClient _messaging;

        public RoleGiverService(IMessagingClient messaging)
        {
            _messaging = messaging;
        }

        [OnCommand("!leave")]
        [Description("Leave a role.")]
        [CommandUsage("`!leave <role name>`")]
        public async Task LeaveRoleAsync(IMessage message)
        {
            var roleName = message.GetCommandArgs().Join(" ");
            var role = GetRequestedRole(roleName);
            if (role == null)
                return;

            if (message.Author is IGuildUser guildUser)
            {
                await guildUser.RemoveRoleAsync(role);
                var response =
                    $":magic_wand: I have removed the role `{role.Name}` from you, {message.Author.Mention}.";

                await message.Channel.SendMessageAsync(response);
            }
            else
            {
                await message.Channel.SendMessageAsync("❌ You must use this command in a guild channel.");
            }
        }

        [OnCommand("!join")]
        [Description("Join a role. Role name is case insensivite. Not all roles are joinable.")]
        [CommandUsage("`!join <role name>`")]
        public async Task JoinRoleAsync(IMessage message)
        {
            var role = GetRequestedRole(message.GetCommandArgs().Join(" "));
            if (role == null)
                return;

            if (message.Author is IGuildUser guildUser)
            {
                await guildUser.AddRoleAsync(role);

                var response =
                    $":magic_wand: I've given you the role `{role.Name}`, {message.Author.Mention}."
                    + $"\n\nYou can type `!leave {role.Name}` if you ever want me to remove it again. :sparkles:";

                await message.Channel.SendMessageAsync(response);
            }
            else
            {
                await message.Channel.SendMessageAsync("❌ You must use this command in a guild channel.");
            }
        }

        private IRole GetRequestedRole(string requestedRoleName) =>
            _allowedRoles.Contains(requestedRoleName, StringComparer.InvariantCultureIgnoreCase)
                ? _messaging.GetRolesByName(requestedRoleName).FirstOrDefault()
                : null;
    }
}
