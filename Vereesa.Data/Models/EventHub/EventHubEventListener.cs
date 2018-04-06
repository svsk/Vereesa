using System;

namespace Vereesa.Data.Models.EventHub
{
    public class EventHubEventListener
    {
        public Action<object[]> EventCallback { get; set; }
        public string EventName { get; set; }

        public EventHubEventListener(string eventName)
        {
            this.EventName = eventName;
        }

        public void Do(Action<object[]> eventCallback)
        {
            this.EventCallback = eventCallback;
        }
    }
}