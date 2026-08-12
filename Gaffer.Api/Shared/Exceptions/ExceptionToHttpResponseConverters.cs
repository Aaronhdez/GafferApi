using System.Net;
using Gaffer.Api.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Gaffer.Api.Shared.Exceptions;

public static class ExceptionToHttpResponseConverters
{
    public static readonly IReadOnlyDictionary<Type, Func<Exception, ProblemDetails>> PlayerConverters =
        new Dictionary<Type, Func<Exception, ProblemDetails>> {
            {
                typeof(PlayerNotFoundException),
                ex => BuildNotFoundProblemDetailsWithExtension(
                    "Player Not Found",
                    $"Player {((PlayerNotFoundException)ex).Id} Not Found",
                    "Error.Players.PlayerNotFound")
            },
        };

    // Helper methods to build ProblemDetails
    private static ProblemDetails BuildNotFoundProblemDetailsWithExtension(string title, string detail, string errorTranslationKey) {
        return new ProblemDetails {
            Title = title,
            Detail = detail,
            Status = (int)HttpStatusCode.NotFound,
            Instance = string.Empty,
            Type = string.Empty,
            Extensions = {
                { "ErrorTranslationKey", errorTranslationKey }
            }
        };
    }
}