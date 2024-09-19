using VideoProcessingService.Core;
using VideoProcessingService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddCoreServices();

var app = builder.Build();
app.Run();