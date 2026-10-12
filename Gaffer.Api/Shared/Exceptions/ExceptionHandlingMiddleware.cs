using System.Net;
using Gaffer.Api.Commands;
using Gaffer.Api.Shared.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gaffer.Api.Shared.Exceptions;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger) {
    public async Task Invoke(HttpContext context) {
        try {
            await next(context);
        } catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested) {
            HandleAbortedRequest(context, ex);
        } catch (Exception ex) {
            await HandleExceptionAsync(context, ex);
        }
    }

    // The client closed the request: nobody reads a body, so only the nginx-style 499 status is set.
    private void HandleAbortedRequest(HttpContext context, OperationCanceledException exception) {
        logger.LogInformation("Request {Path} aborted by the client: {Message}", context.Request.Path.Value, exception.Message);
        if (!context.Response.HasStarted) {
            context.Response.StatusCode = ClientClosedRequest;
        }
    }

    private const int ClientClosedRequest = 499;

    private Task HandleExceptionAsync(HttpContext context, Exception exception) {
        var problemDetails = ExceptionToHttpResponseConverters.PlayerConverters.TryGetValue(exception.GetType(), out var converter)
            ? converter(exception)
            : null;
        if (problemDetails == null) {
            problemDetails = UnknownError(context);
        } else {
            problemDetails.Instance = context.Request.Path.Value;
        }

        LogException(exception, problemDetails);
        return WriteErrorResponse(context, problemDetails);
    }

    private void LogException(Exception exception, ProblemDetails problemDetails) {
        if (problemDetails.Status == (int)HttpStatusCode.NotFound) return;
        logger.LogError(exception, "Request failed with status {Status}: {Title}", problemDetails.Status, problemDetails.Title);
    }

    private static ProblemDetails UnknownError(HttpContext context) {
        return new ProblemDetails {
            Title = "Unknown error",
            Status = (int)HttpStatusCode.InternalServerError,
            Instance = context.Request.Path.Value,
        };
    }

    private Task WriteErrorResponse(HttpContext context, ProblemDetails problemDetails) {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetResponseStatus(problemDetails.Status);
        var result = ExceptionToHttpResponseConverters.Serialize(problemDetails);
        return context.Response.WriteAsync(result);
    }

    private static int GetResponseStatus(int? problemDetailsStatus) {
        if (problemDetailsStatus.HasValue) return problemDetailsStatus.Value;
        return (int)HttpStatusCode.InternalServerError;
    }
}
