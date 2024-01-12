using System.ComponentModel;
using System.Net;
using Discord;
using Microsoft.Extensions.Logging;
using Vereesa.Core;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Neon.Services
{
    public class EmojiService : IBotService
    {
        private readonly IMessagingClient _messaging;
        private readonly IEmojiClient _emoji;
        private readonly ILogger<EmojiService> _logger;

        private ulong _neonGuildId = 124246560178438145;

        public EmojiService(IMessagingClient messaging, IEmojiClient emoji, ILogger<EmojiService> logger)
        {
            _messaging = messaging;
            _emoji = emoji;
            _logger = logger;
        }

        [OnCommand("!emoji list")]
        public async Task ListEmojis(IMessage message)
        {
            var emotes = _emoji.GetCustomEmojiByServerId(_neonGuildId);
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
                    "Please attach an image file to your suggestion message for evalulation."
                );
            }

            if (string.IsNullOrWhiteSpace(emojiName))
            {
                await message.Channel.SendMessageAsync("Please include an emoji name.");
            }

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
                    var emoteUrl = message.Attachments.First().Url;
                    var request = WebRequest.Create(emoteUrl);
                    var emoteImage = new Image(request.GetResponse().GetResponseStream());
                    var emote = await _emoji.CreateCustomEmoji(_neonGuildId, emojiName, emoteImage);
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
