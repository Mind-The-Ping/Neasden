using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Neasden.Consumer.Repositories;
using Neasden.Repository.Redis;
using Neasden.Repository.Redis.Options;
using StackExchange.Redis;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddOptions<RedisOptions>()
     .Configure<IConfiguration>((settings, configuration) =>
     {
         configuration.GetSection("Redis").Bind(settings);
     });

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
    configOptions.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(configOptions);
});

builder.Services.AddScoped<DisruptionRepository>();
builder.Services.AddScoped<NotificationRepository>();

builder.Services.AddScoped<DisruptionConsumerRepo>();
builder.Services.AddScoped<NotificationConsumerRepo>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
