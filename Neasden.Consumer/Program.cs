using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neasden.Consumer.Repositories;
using Neasden.Repository.Database;
using Neasden.Repository.Repositories;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddDbContext<NeasdenDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetSection("PostgreSQL")["ConnectionString"]));

builder.Services.AddScoped<DisruptionRepository>();
builder.Services.AddScoped<NotificationRepository>();

builder.Services.AddScoped<DisruptionConsumerRepo>();
builder.Services.AddScoped<NotificationConsumerRepo>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
