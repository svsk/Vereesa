using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Discord.WebSocket;
using Vereesa.Core.Extensions;

namespace Vereesa.Core.Services
{
    public class ConversionEngine
    {
        private const string Explaination = "";
        private DiscordSocketClient _discord;

        public ConversionEngine(DiscordSocketClient discord)
        {
            _discord = discord;

            _discord.MessageReceived += CheckMessage;
        }

        private async Task CheckMessage(SocketMessage message)
        {
            string command = message.GetCommand();

            if (command == "!convert")
            {
                string[] args = message.GetCommandArgs();

                try
                {
                    ValidateArgs(args);
                }
                catch (InvalidConversionArgsException) 
                {
                    await message.Channel.SendMessageAsync(Explaination);
                }
                catch (Exception)
                {

                }
            }
        }

        private void ValidateArgs(string[] args)
        {
            List<string> argList = args.ToList();

            if (argList.All(arg => arg != "to")) 
            {
                throw new InvalidConversionArgsException("");
            }


        }

        private class InvalidConversionArgsException : Exception
        {
            public InvalidConversionArgsException(string message) 
                : base(message)
            {
            }
        }
    }
}