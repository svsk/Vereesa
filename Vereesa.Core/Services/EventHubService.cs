using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Vereesa.Data.Models.EventHub;

namespace Vereesa.Core.Services
{
    public class EventHubService
    {
        private List<EventHubEventListener> _eventListeners;

        public EventHubService() 
        {
            this._eventListeners = new List<EventHubEventListener>();
        }

        public EventHubEventListener On(string eventName)
        {
            var listener = new EventHubEventListener(eventName);

            this._eventListeners.Add(listener);

            return listener;
        }

        public void Emit(string eventName, params object[] param)
        {
            Console.WriteLine($"New event emitted: {eventName}.");
            var eventListeners = this._eventListeners.Where(el => el.EventName == eventName).ToList();
            Console.WriteLine($"Invoking event callbacks for {eventListeners.Count} listeners.");
            foreach (var eventListener in eventListeners)
            {
                eventListener.EventCallback.Invoke(param);
            }
        }
    }
}