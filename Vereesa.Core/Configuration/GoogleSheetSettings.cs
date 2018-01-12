namespace Vereesa.Core.Configuration
{
    public class GoogleSheetSettings 
    {
        public string GoogleSheetCsvUrl { get; set; }
        public int CheckIntervalSeconds { get; set; }
        public string MessageToSendOnNewLine { get; set; }
        public string NotificationMessageChannelName { get; set; }
    }

}