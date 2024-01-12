using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core;

public class VereesaHostBuilder
{
    private readonly ServiceCollection _services;

    public VereesaHostBuilder()
    {
        _services = new ServiceCollection();
        _services.AddLogging();
        _services.AddSingleton<IJobScheduler, JobScheduler>();
    }

    public VereesaHostBuilder AddServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    public VereesaHost Start()
    {
        _services.AddBotServices();

        var provider = _services.BuildServiceProvider();

        provider.UseBotServices();

        var host = new VereesaHost { Services = provider };

        var messaging = provider.GetRequiredService<IMessagingClient>();
        _ = messaging.Start();

        return host;
    }
}

public class VereesaHost
{
    public IServiceProvider Services { get; set; }

    public async Task Shutdown()
    {
        var messaging = Services.GetRequiredService<IMessagingClient>();
        await messaging.Stop();
    }
}
