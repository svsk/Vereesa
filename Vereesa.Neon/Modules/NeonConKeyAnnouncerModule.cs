using System.ComponentModel;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Vereesa.Core;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Services;

namespace Vereesa.Neon.Modules;

public class NeonConKeyAnnouncerModule : IBotModule
{
    private readonly NeonConKeyRetrieverService _service;
    private readonly IMessagingClient _messaging;
    private readonly ILogger<NeonConKeyAnnouncerModule> _logger;

    public NeonConKeyAnnouncerModule(
        NeonConKeyRetrieverService service,
        IMessagingClient messaging,
        ILogger<NeonConKeyAnnouncerModule> logger
    )
    {
        _service = service;
        _messaging = messaging;
        _logger = logger;
    }

    [SlashCommand("announce-keys", "Announce the upload keys for all NeonCon attendees.")]
    [Authorize("Guild Master")]
    public async Task AnnounceKeys(
        IDiscordInteraction interaction,
        [Description("The directory to find attendee files in")] string directory
    )
    {
        await interaction.DeferAsync();

        var keys = _service.GetNeonConAttendeeKeys(directory);

        foreach (var key in keys)
        {
            try
            {
                var ulongUserId = ulong.Parse(key.Id);

                await _messaging.SendMessageToUserByIdAsync(
                    ulongUserId,
                    $"Your NeonCon album upload User Key is:\n\n`{key.AlbumUploadKey}`\n\nGo to https://con.neon.gg/{DateTime.UtcNow.Year}/album/upload to upload your photos!"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to announce key for {UserId}", key?.Id);
            }
        }

        await interaction.FollowupAsync("All keys have been announced.");
    }
}
