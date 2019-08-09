using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Vereesa.Core.Integrations.Interfaces 
{
    public interface IDiscordSocketClient : IDiscordClient
    {
        event Func<Task> Ready;

        event Func<SocketMessage, Task> MessageReceived;
    }
}