using Serilog;
using ShopeeVideo.Worker;
using ShopeeVideo.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

// Register Services
builder.Services.AddSingleton<IGoogleSheetService, GoogleSheetService>();
builder.Services.AddSingleton<ICrawlService, CrawlService>();
builder.Services.AddSingleton<IVideoService, VideoService>();
builder.Services.AddHostedService<Worker>();


var host = builder.Build();
host.Run();

