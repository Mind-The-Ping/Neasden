using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neasden.Consumer.Repositories;
using Neasden.Consumer.Settings;
using Neasden.Repository.Database;
using Neasden.Repository.Repositories;

namespace Neasden.Consumer;
public static class HostFactory
{
    public static IHost CreateHost(string[] args, bool forTesting = false)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.Configure<ServiceBusSettings>(
            builder.Configuration.GetSection("ServiceBus"));

        if (forTesting)
        {
            builder.Services.AddDbContext<NeasdenDbContext>(options =>
                options.UseNpgsql($"Host=localhost;Port=5434;Database=testdb;Username=neasdenUser;Password=password12345"));
        }
        else
        {
            builder.Services.AddDbContext<NeasdenDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetSection("PostgreSQL")["ConnectionString"]));
        }

        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ServiceBusSettings>>().Value;
            return new ServiceBusClient(options.ConnectionString);
        });

        builder.Services.AddScoped<DisruptionRepository>();
        builder.Services.AddScoped<NotificationRepository>();

        builder.Services.AddScoped<DisruptionConsumerRepo>();
        builder.Services.AddScoped<NotificationConsumerRepo>();

        builder.Services.AddHostedService<Worker>();
         
        return builder.Build();
    }
}
