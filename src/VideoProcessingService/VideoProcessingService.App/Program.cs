using Microsoft.Extensions.Options;
using VideoProcessingService.Core;
using VideoProcessingService.Core.Interfaces;
using VideoProcessingService.Infrastructure;
using VideoProcessingService.Infrastructure.Video;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddCoreServices();

var app = builder.Build();

var config = app.Services.GetRequiredService<IOptions<ConversionConfiguration>>();
var logger = app.Services.GetRequiredService<ILoggerManager>();
logger.LogInfo("Using resolutions: " + config.Value.ResolutionsJson);

app.Run();