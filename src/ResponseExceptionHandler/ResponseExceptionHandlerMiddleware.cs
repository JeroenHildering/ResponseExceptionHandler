using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace ResponseExceptionHandler
{
    public class ResponseExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ResponseExceptionHandlerOptions _options;
        private readonly ILogger _logger;
        private readonly Func<object, Task> _clearCacheHeadersDelegate;
        
        public ResponseExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory,
            IOptions<ResponseExceptionHandlerOptions> options)
        {
            _next = next;
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<ResponseExceptionHandlerMiddleware>();
            _clearCacheHeadersDelegate = ClearCacheHeaders;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("The response has already started, the error handler will not be executed.");
                    throw;
                }

                var originalPath = context.Request.Path;

                try
                {
                    context.Response.Clear();
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = new MediaTypeHeaderValue("application/json").ToString();
                    
                    ExceptionResponse exceptionResponse;
                    if (_options.Responses.TryGetValue(ex.GetType(), out exceptionResponse))
                    {
                        context.Response.StatusCode = exceptionResponse.StatusCode;

                        await WriteResponseAsync(context, GetExceptionResponse(ex, exceptionResponse));

                        _logger.LogError(0, ex, $"An exception has occurred: {ex.Message}");
                    }
                    else
                    {
                        var errorCode = GenerateErrorCode(_options.ErrorCodePrefix);

                        await WriteResponseAsync(context, CreateErrorResponse(errorCode, _options.DefaultErrorMessage));

                        _logger.LogError(new EventId(0, errorCode), ex, $"An unhandled exception has occurred [{errorCode}]: {ex.Message}");
                    }
                    
                    context.Response.OnStarting(_clearCacheHeadersDelegate, context.Response);
                    
                    return;
                }
                catch (Exception ex2)
                {
                    _logger.LogError(0, ex2, "An exception was thrown attempting to execute the error handler.");
                }
                finally
                {
                    context.Request.Path = originalPath;
                }
                throw;
            }
        }
        
        private async Task WriteResponseAsync(HttpContext context, object response)
        {
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response, _options.SerializerSettings), Encoding.UTF8);
        }

        private object GetExceptionResponse(Exception ex, ExceptionResponse exceptionResponse)
        {
            if (!string.IsNullOrWhiteSpace(exceptionResponse.Message))
            {
                return CreateErrorResponse(exceptionResponse.Message);
            }

            return exceptionResponse.Response ?? CreateErrorResponse(ex.Message.Replace(Environment.NewLine, " "));
        }

        private Error CreateErrorResponse(string message)
        {
            return CreateErrorResponse(null, message);
        }

        private Error CreateErrorResponse(string errorCode, string message)
        {
            return new Error
            {
                ErrorCode = errorCode,
                Message = message
            };
        }

        private string GenerateErrorCode(string prefix)
        {
            var uniqueValue = Guid.NewGuid()
                .ToString()
                .Replace("-", string.Empty)
                .Substring(0, 8)
                .ToUpper();

            return $"{prefix}{uniqueValue}";
        }

        private Task ClearCacheHeaders(object state)
        {
            var response = (HttpResponse)state;
            response.Headers[HeaderNames.CacheControl] = "no-cache";
            response.Headers[HeaderNames.Pragma] = "no-cache";
            response.Headers[HeaderNames.Expires] = "-1";
            response.Headers.Remove(HeaderNames.ETag);
            return Task.CompletedTask;
        }
    }
}
