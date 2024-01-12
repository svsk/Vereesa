using Vereesa.Core;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Neon.Services
{
    public class StartupService : IBotService
    {
        private readonly ILogger<StartupService> _logger;

        public StartupService(ILogger<StartupService> logger)
        {
            _logger = logger;
        }

        [OnReady]
        public Task HandleReady()
        {
            _logger.LogWarning("Ready was invoked.");
            return Task.CompletedTask;
        }
    }
}
