using Vereesa.Neon;

internal class VereesaService : IHostedService
{
    private static VereesaNeonClient? _client;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_client != null)
            return Task.CompletedTask;

        _client = new VereesaNeonClient();
        _client.Start(
            (services, config) => {
                // services.AddAwdeo(config);
            }
        );

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client == null)
            return;

        await _client.Shutdown();
    }
}
