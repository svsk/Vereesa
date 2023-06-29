using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Configuration;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
    public class OpenAIService : BotServiceBase
    {
        private readonly OpenAIClient _client;
        private readonly ILogger<OpenAIService> _logger;
        private static string _directives = string.Join(
            " ",
            new string[]
            {
                "You are a Discord bot operating for the World of Warcraft guild called \"Neon\".",
                "Your name is Vereesa.",
                "Veinlash is the guild leader.",
                "Steve is the raid leader.",
                "Sup knows about anything relating to the economy and the guild bank.",
                "You should behave as if you are a humble, female elf who is a member of Neon.",
                "You can sometimes be a little sassy.",
                "You are free to use emojis at the end of your messages.",
                "You should try to keep your answers below 300 characters. But don't be afraid to go shorter or longer if it makes sense.",
                "You should not start responses with \"Oh, darling\"."
            }
        );

        private static List<ChatMessage> _messageHistory = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, _directives),
        };

        public OpenAIService(OpenAISettings settings, DiscordSocketClient discord, ILogger<OpenAIService> logger)
            : base(discord)
        {
            _client = new OpenAIClient(settings.ApiKey);
            _logger = logger;
        }

        [OnMention]
        [AsyncHandler]
        public async Task Respond(IMessage message)
        {
            _messageHistory.Add(new ChatMessage(ChatRole.User, message.Content));

            var options = new ChatCompletionsOptions { Temperature = 0f };
            foreach (var msg in _messageHistory)
                options.Messages.Add(msg);

            try
            {
                var response = await _client.GetChatCompletionsAsync("gpt-3.5-turbo", options);

                var responseContent = response.Value.Choices.FirstOrDefault()?.Message?.Content;

                _messageHistory.Add(new ChatMessage(ChatRole.Assistant, responseContent));

                await message.Channel.SendMessageAsync(
                    response.Value.Choices.FirstOrDefault()?.Message?.Content,
                    messageReference: new MessageReference(message.Id)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OpenAIService.Respond");
            }
        }
    }
}
