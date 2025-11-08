using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Neasden.API;
using Neasden.API.Options;
using Neasden.Consumer;
using Neasden.Consumer.Clients.StratfordClient;
using Neasden.Consumer.Repositories;
using Neasden.Library.Clients;
using Neasden.Library.Options;
using Neasden.Repository.Write;
using StackExchange.Redis;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.Configure<JwtOptions>(
   builder.Configuration.GetSection("Jwt"));

builder.Services.Configure<WaterlooOptions>(
    builder.Configuration.GetSection("Waterloo"));

builder.Services.Configure<StratfordOptions>(
   builder.Configuration.GetSection("Stratford"));

builder.Services.Configure<ServiceBusOptions>(
    builder.Configuration.GetSection("ServiceBus"));

builder.Services.AddDbContextFactory<WriteDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    var configOptions = ConfigurationOptions.Parse(options.Connection);
    configOptions.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(configOptions);
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<IWaterlooClient, WaterlooClient>();
builder.Services.AddScoped<IStratfordClient, StratfordClient>();
builder.Services.AddScoped<IUserNotifiedRepository, UserNotifiedRepository>();
builder.Services.AddScoped<NotificationPublisher>();
builder.Services.AddScoped<DisruptionNotifier>();
builder.Services.AddScoped<WriteDisruptionRepository>();
builder.Services.AddScoped<WriteNotificationRepository>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
