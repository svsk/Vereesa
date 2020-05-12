using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Vereesa.Data.Models.EventHub;

namespace Vereesa.Core.Services
{
    public class EventHubService : BotServiceBase
    {
        private ILogger<EventHubService> _logger;
        private List<EventHubEventListener> _eventListeners;

        public EventHubService(ILogger<EventHubService> logger) 
        {
            _logger = logger;
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
            _logger.LogInformation($"New event emitted: {eventName}.");
            var eventListeners = this._eventListeners.Where(el => el.EventName == eventName).ToList();
            _logger.LogInformation($"Invoking event callbacks for {eventListeners.Count} listeners.");
            
            foreach (var eventListener in eventListeners)
            {
                eventListener.EventCallback.Invoke(param);
            }
        }
    }
}