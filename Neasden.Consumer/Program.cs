using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neasden.Consumer.Repositories;
using Neasden.Repository.Redis;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddScoped<DisruptionRepository>();
builder.Services.AddScoped<NotificationRepository>();

builder.Services.AddScoped<DisruptionConsumerRepo>();
builder.Services.AddScoped<NotificationConsumerRepo>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
