using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Discord;
using Microsoft.Extensions.Logging;
using Vereesa.Neon.Configuration;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Neon.Services
{
    public class OpenAIService : IBotService
    {
        private readonly IMessagingClient _messaging;
        private readonly OpenAIClient _client;
        private int _historySkip = 0;
        private readonly ILogger<OpenAIService> _logger;
        private static ChatMessage _directives = new ChatMessage(
            ChatRole.System,
            string.Join(
                " ",
                new string[]
                {
                    "You are a Discord bot operating for the World of Warcraft guild called \"Neon\".",
                    "You are Vereesa Windrunner of the Silver Covenant.",
                    "You should behave as if you are a humble, female elf who is also a member of Neon.",
                    "Veinlash is the guild leader.",
                    "Steve is the raid leader.",
                    "Sup knows about anything relating to the economy and the guild bank.",
                    "You can sometimes be a little sassy.",
                    "You are free to use emojis at the end of your messages.",
                    "You should try to keep your answers below 300 characters. But don't be afraid to go shorter or longer if it makes sense.",
                    "Don't call people \"darling\".",
                    "You are in a group conversation with everyone in the guild.",
                    "Their messages to you will be formatted with their nicknames first, then a colon followed by their message.",
                    "Your messages should not be prepended with any name, but feel free to addreess people by their names if it makes sense.",
                    "You can also refer to people by just the first syllable in their name, when you want to appear more casual.",
                    "Do not start your messages with \"Vereesa:\"."
                }
            )
        );

        private static List<ChatMessage> _messageHistory = new List<ChatMessage>();

        public OpenAIService(IMessagingClient messaging, OpenAISettings settings, ILogger<OpenAIService> logger)
        {
            _messaging = messaging;
            _client = new OpenAIClient(settings.ApiKey);
            _logger = logger;
        }

        private string EscapeSelfMentions(string message)
        {
            return _messaging.EscapeSelfMentions(message);
        }

        [OnMention]
        [AsyncHandler]
        public async Task Respond(IMessage message)
        {
            var displayName = message.Author.GetPreferredDisplayName();
            var messageWithUsername = $"{displayName}: {EscapeSelfMentions(message.Content)}";

            _messageHistory.Add(new ChatMessage(ChatRole.User, messageWithUsername));

            var options = new ChatCompletionsOptions { Temperature = 0f };
            options.Messages.Add(_directives);

            foreach (var msg in _messageHistory.Skip(_historySkip).ToList())
                options.Messages.Add(msg);

            try
            {
                var response = await _client.GetChatCompletionsAsync("gpt-3.5-turbo", options);
                var responseContent = EscapeSelfMentions(response.Value.Choices.FirstOrDefault()?.Message?.Content);

                _messageHistory.Add(new ChatMessage(ChatRole.Assistant, responseContent));

                await message.Channel.SendMessageAsync(
                    response.Value.Choices.FirstOrDefault()?.Message?.Content,
                    messageReference: new MessageReference(message.Id)
                );
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 400 && ex.ErrorCode == "context_length_exceeded")
                {
                    _historySkip += 4;
                    _ = Respond(message);
                    _logger.LogError(ex, "Context Length Exceeded - Increasing history skip.");
                }
                else
                {
                    _logger.LogError(ex, "Error in OpenAIService.Respond");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OpenAIService.Respond");
            }
        }
    }
}
