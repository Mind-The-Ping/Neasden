using Microsoft.Extensions.Options;
using Neasden.Repository.Database;
using Neasden.Saver;
using Neasden.Saver.Options;
using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Redis.Options;

var builder = Host.CreateApplicationBuilder(args);

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

builder.Services.AddDbContext<NeasdenDbContext>((sp, options) =>
{
    var postgresOptions = sp.GetRequiredService<IOptions<PostgresOptions>>().Value;
    options.UseNpgsql(postgresOptions.ConnectionString);
});

builder.Services.AddScoped<Neasden.Repository.Repositories.DisruptionRepository>();
builder.Services.AddScoped<Neasden.Repository.Repositories.NotificationRepository>();
builder.Services.AddScoped<Neasden.Repository.Redis.DisruptionRepository>();
builder.Services.AddScoped<Neasden.Repository.Redis.NotificationRepository>();
builder.Services.AddScoped<Saver>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
