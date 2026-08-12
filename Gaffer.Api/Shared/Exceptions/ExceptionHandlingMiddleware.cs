using System.Net;
using Gaffer.Api.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Gaffer.Api.Shared.Exceptions;

public class ExceptionHandlingMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            //logger.LogError(ex, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        ExceptionToProblemDetails();
        var problemDetails = GetProblemDetails(exception);
        var result = JsonConvert.SerializeObject(problemDetails);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetResponseStatus(problemDetails.Status);
        return context.Response.WriteAsync(result);
    }

    private static void ExceptionToProblemDetails()
    {
        var converters = GetConverterDictionary();
    }

    private static Dictionary<Type, Func<Exception, ProblemDetails>> GetConverterDictionary()
    {
        return ExceptionToHttpResponseConverters.PlayerConverters
            .ToDictionary(converter => converter.Key, converter => converter.Value);
    }

    private static int GetResponseStatus(int? problemDetailsStatus)
    {
        if (problemDetailsStatus.HasValue) return problemDetailsStatus.Value;
        return (int)HttpStatusCode.InternalServerError;
    }

    private ProblemDetails GetProblemDetails(Exception exception)
    {
        return exception switch
        {
            PlayerNotFoundException playerNotFoundException => new ProblemDetails
            {
                Title = "Player not found",
                Status = (int)HttpStatusCode.NotFound,
                Type = exception.GetType().FullName,
                Detail = playerNotFoundException.ErrorMessage,
            },
            _ => new ProblemDetails
            {
                Title = "Unknown error",
                Status = (int)HttpStatusCode.InternalServerError,
                Type = exception.GetType().FullName,
                Detail = { }
            }
        };
    }
}