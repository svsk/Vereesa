namespace Vereesa.Core.Configuration
{
    public class NewsFeedServiceSettings
    {
        public string SiteRoot { get; set; }
        public ulong ChannelId { get; set; }
        public int CheckInterval { get; set; }

        public string CheckUrl { get; set; }
        public string CheckElementSelector { get; set; }
        public string ReactionLinkSelector { get; set; }

        public string NewsItemHeaderSelector { get; set; }
        public string NewsItemImageSelector { get; set; }
        public string NewsItemTextSelector { get; set; }
    }
}