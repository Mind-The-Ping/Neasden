using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Neasden.API;
using Neasden.API.Options;
using Neasden.Library.Clients;
using Neasden.Repository.NotificationCount;
using Neasden.Repository.Read;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
          .AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation()
          .AddSqlClientInstrumentation()
          .AddAzureMonitorTraceExporter(o =>
          {
              o.ConnectionString = insightsConnectionString;
          }))
      .WithMetrics(metrics => metrics
          .AddAspNetCoreInstrumentation()
          .AddHttpClientInstrumentation()
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

builder.Services.Configure<JwtOptions>(
   builder.Configuration.GetSection("Jwt"));

builder.Services.Configure<WaterlooOptions>(
    builder.Configuration.GetSection("Waterloo"));

builder.Services.Configure<DatabaseOptions>(
   builder.Configuration.GetSection("Database"));

builder.Services.AddDbContextFactory<ReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

    var client = new MongoClient(options.ConnectionString);
    var database = client.GetDatabase(options.Name);

    return database;
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<IWaterlooClient, WaterlooClient>();
builder.Services.AddScoped<INotificationCountRepository, NotificationCountRepository>();
builder.Services.AddScoped<ReadDisruptionRepository>();
builder.Services.AddScoped<ReadNotificationRepository>();
builder.Services.AddScoped<NotificationRetriever>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

public partial class Program { }