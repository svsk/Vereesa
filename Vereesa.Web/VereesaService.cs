using Vereesa.Core;

internal class VereesaService : IHostedService
{
    private static VereesaClient? _client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_client != null)
            return;

        _client = new VereesaClient();
        await _client.StartupAsync(
            (services, config) => {
                // services.AddAwdeo(config);
            }
        );
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client?.Shutdown();
        return Task.CompletedTask;
    }
}
