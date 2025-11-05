using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neasden.Repository.Database;
using Neasden.Repository.Redis.Options;
using Neasden.Repository.Write;
using Neasden.Saver;
using Neasden.Saver.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

var insightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

builder.Logging.ClearProviders();

if (builder.Environment.IsProduction())
{
    var resourceBuilder = ResourceBuilder.CreateDefault()
       .AddService(
           serviceName: builder.Environment.ApplicationName,
           serviceVersion: "1.0.0");

    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.SetResourceBuilder(resourceBuilder);

        logging.AddAzureMonitorLogExporter(o =>
        {
            o.ConnectionString = insightsConnectionString;
        });
    });

    builder.Services.AddOpenTelemetry()
      .ConfigureResource(rb => rb.AddService(builder.Environment.ApplicationName))
      .WithTracing(tracing => tracing
          .AddSqlClientInstrumentation()
          .AddAzureMonitorTraceExporter(o =>
          {
              o.ConnectionString = insightsConnectionString;
          }))
      .WithMetrics(metrics => metrics
          .AddRuntimeInstrumentation()
          .AddAzureMonitorMetricExporter(o =>
          {
              o.ConnectionString = insightsConnectionString;
          }));
}
else
{
    builder.Logging.AddConsole();
}

builder.Services.AddOptions<RedisOptions>()
  .Configure<IConfiguration>((settings, configuration) =>
  {
      configuration.GetSection("Redis").Bind(settings);
  });

builder.Services.AddOptions<PostgresOptions>()
  .Configure<IConfiguration>((settings, configuration) =>
   {
     configuration.GetSection("Postgres").Bind(settings);
   });

builder.Services.AddOptions<SaverOptions>()
  .Configure<IConfiguration>((settings, configuration) =>
  {
      configuration.GetSection("Saver").Bind(settings);
  });

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
    configOptions.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(configOptions);
});

builder.Services.AddDbContext<WriteDbContext>((sp, options) =>
{
    var postgresOptions = sp.GetRequiredService<IOptions<PostgresOptions>>().Value;
    options.UseNpgsql(postgresOptions.ConnectionString);
});

builder.Services.AddScoped<WriteDisruptionRepository>();
builder.Services.AddScoped<WriteNotificationRepository>();
builder.Services.AddScoped<Neasden.Repository.Redis.DisruptionRepository>();
builder.Services.AddScoped<Neasden.Repository.Redis.NotificationRepository>();
builder.Services.AddScoped<Saver>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
