using System.Text.Json;

namespace Vereesa.Neon.Services;

public class NeonConKeyRetrieverService
{
    public record NeonConAttendeeKey(string AlbumUploadKey, string Id);

    public List<NeonConAttendeeKey> GetNeonConAttendeeKeys(string directory)
    {
        // Json looks like this and is in a file like attendee-124239800063492096.json. There is one file per attendee.
        // {"Id":"124239800063492096","Username":"swuden","AvatarRelativeUrl":"/2024/static/img/avatars/124239800063492096.png","AlbumUploadKey":"052b70cc-6afc-45fb-aa22-117e1e3b51f4"}

        var keys = new List<NeonConAttendeeKey>();
        foreach (var file in Directory.EnumerateFiles(directory, "attendee-*.json"))
        {
            var json = File.ReadAllText(file);
            var attendee = JsonSerializer.Deserialize<NeonConAttendeeKey>(json);

            if (attendee != null)
            {
                keys.Add(attendee);
            }
        }

        return keys;
    }
}
