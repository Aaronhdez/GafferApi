using System.Net;
using Gaffer.Api.Commands;
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
            CannotDeletePlayerNotFoundException => new ProblemDetails
            {
                Title = "Cannot delete player",
                Status = (int)HttpStatusCode.NotFound,
                Type = exception.GetType().FullName,
            },
            CannotDeletePlayerException cannotDeletePlayerException => new ProblemDetails
            {
                Title = "Cannot delete player",
                Status = (int)HttpStatusCode.BadRequest,
                Type = exception.GetType().FullName,
                Detail = cannotDeletePlayerException.ErrorMessage,
            },
            CannotUpdatePlayerNotFoundException => new ProblemDetails
            {
                Title = "Cannot delete player",
                Status = (int)HttpStatusCode.NotFound,
                Type = exception.GetType().FullName,
            },
            CannotUpdatePlayerException cannotDeletePlayerException => new ProblemDetails
            {
                Title = "Cannot delete player",
                Status = (int)HttpStatusCode.BadRequest,
                Type = exception.GetType().FullName,
                Detail = cannotDeletePlayerException.ErrorMessage,
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