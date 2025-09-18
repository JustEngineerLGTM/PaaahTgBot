using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaaahBot;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddHostedService<BotWorker>();
    })
    .Build()
    .Run();
