using System.ComponentModel;
using System.Reflection;
using Discord;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class HelpService : IBotModule
    {
        [OnCommand("!help")]
        [Description("Prints helpful information about commands.")]
        [AsyncHandler]
        public async Task HandleMessage(IMessage message)
        {
            var services = BotServices.GetBotModules();
            var helpMessage = "";

            foreach (var service in services)
            {
                var invocableMethods = service.GetMethodsWithAttribute<OnCommandAttribute>();

                foreach (var method in invocableMethods)
                {
                    var obsoleteAttribute = method.GetCustomAttribute<ObsoleteAttribute>();
                    var commandAttribute = method.GetCustomAttribute<OnCommandAttribute>();
                    var descriptionAttribute = method.GetCustomAttribute<DescriptionAttribute>();
                    var usageAttribute = method.GetCustomAttribute<CommandUsageAttribute>();
                    var restrictionAttributes = method.GetCustomAttributes<AuthorizeAttribute>();

                    if (obsoleteAttribute != null || commandAttribute == null)
                    {
                        continue;
                    }

                    helpMessage += $"`{commandAttribute.Command}` ";

                    if (restrictionAttributes.Any())
                    {
                        helpMessage += "- 🔒 ";
                    }

                    if (descriptionAttribute != null)
                    {
                        helpMessage += $"- {descriptionAttribute.Description} ";
                    }

                    if (usageAttribute != null)
                    {
                        helpMessage += $"- {usageAttribute.UsageDescription} ";
                    }

                    helpMessage += "\n";
                }

                if (invocableMethods.Any() && !string.IsNullOrWhiteSpace(helpMessage))
                {
                    helpMessage += "\n";
                    await Task.Delay(50);
                }
            }

            // Split helpMessage into multiple messages of max 2000 characters.
            // Split by line breaks.
            var lines = helpMessage.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

            var currentMessage = "";
            foreach (var line in lines)
            {
                if (currentMessage.Length + line.Length + 2 > 2000)
                {
                    await message.Channel.SendMessageAsync(currentMessage);
                    currentMessage = "";
                }

                currentMessage += line.Replace("\n", "") + "\n";
            }

            if (!string.IsNullOrWhiteSpace(currentMessage))
            {
                await message.Channel.SendMessageAsync(currentMessage);
            }
        }
    }

    public static class TypeExtensions
    {
        public static MethodInfo[] GetMethodsWithAttribute<T>(this Type type)
            where T : Attribute
        {
            return type.GetMethods().Where(method => method.GetCustomAttribute<T>() != null).ToArray();
        }
    }
}
