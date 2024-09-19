using CoreService.Application.Interfaces;
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.SecurityTokenService;

namespace CoreService.Api.Extensions
{
    public static class ExceptionMiddlewareExtensions
    {
        public static void ConfigureExceptionHandler(this IApplicationBuilder app,
ILoggerManager logger)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.ContentType = "application/json";
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                   
                    if (contextFeature != null)
                    {
                        var message = contextFeature.Error.Message;
                        context.Response.StatusCode = contextFeature.Error switch
                        {
                            NotFoundException => StatusCodes.Status404NotFound,
                            BadRequestException => StatusCodes.Status400BadRequest,
                            _ => StatusCodes.Status500InternalServerError
                        };
                        logger.LogError($"Something went wrong: {contextFeature.Error}");
                        await context.Response.WriteAsync(message);
                    }
                });
            });
        }
    }
}
