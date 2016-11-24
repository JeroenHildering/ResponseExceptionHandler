using System;
using System.Collections.Concurrent;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ResponseExceptionHandler
{
    public class ResponseExceptionHandlerOptions
    {
        public string ErrorCodePrefix { get; set; }
        public string DefaultErrorMessage { get; set; }
        public JsonSerializerSettings SerializerSettings { get; set; }
        public ConcurrentDictionary<Type, ExceptionResponse> Responses { get; }

        public ResponseExceptionHandlerOptions()
        {
            ErrorCodePrefix = "ERR_";
            DefaultErrorMessage = "An unhandled exception has occurred, please check the log for details.";

            SerializerSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            Responses = new ConcurrentDictionary<Type, ExceptionResponse>();
        }

        public void Map<TException>(HttpStatusCode statusCode) where TException : Exception
        {
            Map<TException>(statusCode, null);
        }

        public void Map<TException>(HttpStatusCode statusCode, string errorMessage) where TException : Exception
        {
            Map<TException>(statusCode, errorMessage, null);
        }

        public void Map<TException>(HttpStatusCode statusCode, object errorResponse) where TException : Exception
        {
            Map<TException>(statusCode, null, errorResponse);
        }

        private void Map<TException>(HttpStatusCode statusCode, string errorMessage, object errorResponse) where TException : Exception
        {
            var type = typeof(TException);
            var response = new ExceptionResponse
            {
                StatusCode = (int)statusCode,
                Message = errorMessage,
                Response = errorResponse
            };

            if (Responses.TryAdd(type, response))
            {
                return;
            }

            ExceptionResponse oldItem;
            if (Responses.TryRemove(type, out oldItem))
            {
                Responses.TryAdd(type, response);
            }
        }
    }
}
