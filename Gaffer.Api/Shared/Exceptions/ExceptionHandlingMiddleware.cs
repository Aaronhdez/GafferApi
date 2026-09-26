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
        ProblemDetails problemDetails;
        var converters = GetConverterDictionary();
        if (!converters.ContainsKey(exception.GetType()))
        {
            problemDetails = UnknownError(context);
        }
        else
        {
            problemDetails = converters[exception.GetType()](exception);
            if (problemDetails == null)
            {
                problemDetails = UnknownError(context);
            }
            else
            {
                problemDetails.Instance = context.Request.Path.Value;
            }
        }

        return WriteErrorResponse(context, problemDetails);
        
        
    }
    
    private ProblemDetails UnknownError(HttpContext context)
    {
        return new ProblemDetails
        {
            Title = "Unkwnown error",
            Status = (int)HttpStatusCode.InternalServerError,
            Instance = context.Request.Path.Value,
        };
    }

    private Task WriteErrorResponse(HttpContext context, ProblemDetails problemDetails)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetResponseStatus(problemDetails.Status);
        var result = JsonConvert.SerializeObject(problemDetails);
        return context.Response.WriteAsync(result);
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
}