using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
    public class ConversionEngine : IBotService
    {
        private const string Explaination = "";

        [OnCommand("!convert")]
        [Obsolete]
        public async Task CheckMessage(SocketMessage message)
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
            catch (Exception) { }
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
                : base(message) { }
        }
    }
}
