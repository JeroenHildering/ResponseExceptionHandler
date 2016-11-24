using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace ResponseExceptionHandler
{
    public static class ResponseExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseResponseExceptionHandler(this IApplicationBuilder app) => app.UseResponseExceptionHandler(_ => { });

        public static IApplicationBuilder UseResponseExceptionHandler(this IApplicationBuilder app, Action<ResponseExceptionHandlerOptions> setupAction)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            
            var options = new ResponseExceptionHandlerOptions();

            setupAction(options);

            return app.UseMiddleware<ResponseExceptionHandlerMiddleware>(Options.Create(options));
        }
    }
}
