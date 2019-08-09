namespace Vereesa.Core.Configuration
{
    public class TwitterSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string SourceTwitterUser { get; set; }
        public string TargetDiscordGuild { get; set; }
        public string TargetDiscordChannel { get; set; }
        public int CheckIntervalSeconds { get; set; }
    }
}