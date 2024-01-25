using System.ComponentModel;
using Discord;
using Microsoft.Extensions.Logging;
using Vereesa.Core;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Helpers;

namespace Vereesa.Neon.Services
{
    public class EmojiService : IBotService
    {
        private readonly IMessagingClient _messaging;
        private readonly IEmojiClient _emoji;
        private readonly ILogger<EmojiService> _logger;
        private readonly HttpClient _httpClient;

        public EmojiService(
            IMessagingClient messaging,
            IEmojiClient emoji,
            ILogger<EmojiService> logger,
            HttpClient httpClient
        )
        {
            _messaging = messaging;
            _emoji = emoji;
            _logger = logger;
            _httpClient = httpClient;
        }

        [OnCommand("!emoji list")]
        public async Task ListEmojis(IMessage message)
        {
            var emotes = _emoji.GetCustomEmojiByServerId(WellknownGuilds.Neon);
            await message.Channel.SendMessageAsync(string.Join(" ", emotes.Select(e => $"<:{e.Name}:{e.Id}>")));
        }

        [OnCommand("!emoji suggest")]
        [WithArgument("emojiname", 0)]
        [Description(
            "Suggest a picture to turned into an emoji. Emoji name must be single word. And a picture must"
                + " be attached to the command message."
        )]
        [CommandUsage("`!emoji suggest <emoji name>`")]
        [AsyncHandler]
        public async Task SuggestEmoji(IMessage message, string emojiName)
        {
            if (message.Attachments.Count != 1)
            {
                await message.Channel.SendMessageAsync(
                    "💥 Please attach an image file to your suggestion message for evaluation."
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(emojiName))
            {
                await message.Channel.SendMessageAsync("💥 Please include an emoji name.");
                return;
            }

            var emoteUrl = message.Attachments.First().Url;
            var request = await _httpClient.GetAsync(emoteUrl);
            var stream = await request.Content.ReadAsStreamAsync();

            // Check if stream is larger than 2048kb
            if (stream.Length > 2048000)
            {
                await message.Channel.SendMessageAsync(
                    "💥 The image you attached is too large. Please try again with an image smaller than 2MB."
                );

                return;
            }

            var emoteImage = new Image(stream);

            var response = await _messaging.Prompt(
                WellknownRole.Officer,
                "Should this be an emoji? (answer `yes` to confirm)",
                message.Channel,
                60000
            );

            var responseMessage = "No officer responded. Please try again later.";

            if (response != null && response.Content.Equals("yes", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    var emote = await _emoji.CreateCustomEmoji(WellknownGuilds.Neon, emojiName, emoteImage);
                    responseMessage = $"OK, I made <:{emote.Name}:{emote.Id}>!";
                }
                catch (Exception ex)
                {
                    responseMessage = "Ugh... I failed making that an emoji... Sorry!";
                    _logger.LogError(ex, "Failed to make emoji");
                }
            }
            else if (response != null)
            {
                responseMessage = "I'm going to take that as a no.";
            }

            await message.Channel.SendMessageAsync(responseMessage);
        }
    }
}
