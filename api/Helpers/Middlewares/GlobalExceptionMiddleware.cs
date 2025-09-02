using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace api.Helpers.Middlewares;

public class GlobalExceptionMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in function {FunctionName}", context.FunctionDefinition.Name);

            var statusCode = HttpStatusCode.InternalServerError;


            if (ex is Exception dynEx && dynEx.GetType().GetProperty("StatusCode")?.GetValue(dynEx) is HttpStatusCode sc)
            {
                statusCode = sc;
            }
            else if (ex is UnauthorizedAccessException)
            {
                statusCode = HttpStatusCode.Unauthorized;
            }
            else if (ex is ArgumentException || ex is InvalidOperationException)
            {
                statusCode = HttpStatusCode.BadRequest;
            }

            var error = new
            {
                success = false,
                error = ex.Message
            };

            var responseData = JsonSerializer.Serialize(error);

            var req = await context.GetHttpRequestDataAsync();
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(responseData);

            context.GetInvocationResult().Value = response;
        }
    }

}