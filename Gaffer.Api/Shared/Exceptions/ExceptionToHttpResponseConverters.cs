using System.Net;
using Gaffer.Api.Commands;
using Gaffer.Api.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Gaffer.Api.Shared.Exceptions;

public static class ExceptionToHttpResponseConverters
{
    public static readonly IReadOnlyDictionary<Type, Func<Exception, ProblemDetails>> PlayerConverters =
        new Dictionary<Type, Func<Exception, ProblemDetails>>
        {
            {
                typeof(CannotDeletePlayerNotFoundException),
                ex => BuildNotFoundProblemDetailsWithExtension(
                    "Cannot delete player",
                    $"Player {((CannotDeletePlayerNotFoundException)ex).Id} not found",
                    "Error.Players.Delete.PlayerNotFound")
            },
            {
                typeof(CannotDeletePlayerException),
                ex => BuildBadRequestProblemDetailsWithExtension(
                    "Cannot delete player",
                    $"Player {((CannotDeletePlayerException)ex).Id} cannot be deleted",
                    $"Error.Players.Delete.{((CannotDeletePlayerException)ex).Reason}")
            },
            {
                typeof(CannotUpdatePlayerNotFoundException),
                ex => BuildNotFoundProblemDetailsWithExtension(
                    "Cannot update player",
                    $"Player {((CannotUpdatePlayerNotFoundException)ex).Id} not found",
                    "Error.Players.Update.PlayerNotFound")
            },
            {
                typeof(CannotUpdatePlayerException),
                ex => BuildBadRequestProblemDetailsWithExtension(
                    "Cannot update player",
                    $"Player {((CannotUpdatePlayerException)ex).Id} cannot be updated",
                    $"Error.Players.Update.{((CannotUpdatePlayerException)ex).Reason}")
            },
        };
    
    // Helper methods to build ProblemDetails
    private static ProblemDetails BuildNotFoundProblemDetailsWithExtension(string title, string detail,
        string errorTranslationKey)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = (int)HttpStatusCode.NotFound,
            Instance = string.Empty,
            Type = string.Empty,
            Extensions =
            {
                { "ErrorTranslationKey", errorTranslationKey }
            }
        };
    }    
    private static ProblemDetails BuildBadRequestProblemDetailsWithExtension(string title, string detail,
        string errorTranslationKey)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = (int)HttpStatusCode.BadRequest,
            Instance = string.Empty,
            Type = string.Empty,
            Extensions =
            {
                { "ErrorTranslationKey", errorTranslationKey }
            }
        };
    }
}