using System.Net;
using Gaffer.Api.Commands;
using Gaffer.Api.Commands.Facilities;
using Gaffer.Api.Commands.Players;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gaffer.Api.Shared.Exceptions;

public static class ExceptionToHttpResponseConverters {
    private const string SearchFilterNotPermittedKey = "Error.Search.FilterNotPermitted";

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
            {
                typeof(PlayerNotFoundException),
                _ => BuildNotFoundProblemDetails()
            },
            {
                typeof(PlayerSearchFilterNotPermittedException),
                _ => BuildSearchFilterNotPermittedProblemDetails()
            },
            {
                typeof(FacilitySearchFilterNotPermittedException),
                _ => BuildSearchFilterNotPermittedProblemDetails()
            },
            {
                typeof(FacilityValidationException),
                ex => BuildBadRequestProblemDetailsWithExtension(
                    "Cannot create facility",
                    ex.Message,
                    "Error.Facilities.Create.Validation")
            },
            {
                typeof(ValidationFailedException),
                ex => BuildValidationProblemDetails(
                    ((ValidationFailedException)ex).Title,
                    ((ValidationFailedException)ex).ErrorTranslationKey,
                    ((ValidationFailedException)ex).Errors)
            },
            {
                typeof(FacilityNotFoundException),
                ex => BuildNotFoundProblemDetailsWithExtension(
                    "Facility not found",
                    ex.Message,
                    "Error.Facilities.NotFound")
            },
            {
                typeof(CannotDeleteFacilityNotFoundException),
                ex => BuildNotFoundProblemDetailsWithExtension(
                    "Cannot delete facility",
                    $"Facility {((CannotDeleteFacilityNotFoundException)ex).Id} not found",
                    "Error.Facilities.Delete.FacilityNotFound")
            },
            {
                typeof(CannotDeleteFacilityException),
                ex => BuildBadRequestProblemDetailsWithExtension(
                    "Cannot delete facility",
                    $"Facility {((CannotDeleteFacilityException)ex).Id} cannot be deleted",
                    $"Error.Facilities.Delete.{((CannotDeleteFacilityException)ex).Reason}")
            },
            {
                typeof(CannotUpdateFacilityNotFoundException),
                ex => BuildNotFoundProblemDetailsWithExtension(
                    "Cannot update facility",
                    $"Facility {((CannotUpdateFacilityNotFoundException)ex).Id} not found",
                    "Error.Facilities.Update.FacilityNotFound")
            },
            {
                typeof(CannotUpdateFacilityException),
                ex => BuildBadRequestProblemDetailsWithExtension(
                    "Cannot update facility",
                    $"Facility {((CannotUpdateFacilityException)ex).Id} cannot be updated",
                    $"Error.Facilities.Update.{((CannotUpdateFacilityException)ex).Reason}")
            },
            {
                typeof(CannotUpdateFacilityDuplicatedStandException),
                ex => BuildBadRequestProblemDetailsWithExtension(
                    "Cannot update facility",
                    ex.Message,
                    "Error.Facilities.Update.DuplicatedStand")
            },
            {
                typeof(FacilityStandsFacilityNotFoundException),
                ex => BuildNotFoundProblemDetailsWithExtension(
                    "Facility not found",
                    ex.Message,
                    "Error.Facilities.Stands.FacilityNotFound")
            },
            {
                typeof(FacilityStandNotFoundException),
                ex => BuildNotFoundProblemDetailsWithExtension(
                    "Stand not found",
                    ex.Message,
                    "Error.Facilities.Stands.StandNotFound")
            },
            {
                typeof(FacilityMaxStandsExceededException),
                ex => BuildBadRequestProblemDetailsWithExtension(
                    "Cannot create stand",
                    ex.Message,
                    "Error.Facilities.Stands.Create.MaxStandsExceeded")
            },
        };

    public static ProblemDetails BuildValidationProblemDetails(string title, string errorTranslationKey,
        IReadOnlyDictionary<string, string[]> errors) {
        var problemDetails = BuildBadRequestProblemDetailsWithExtension(title, ValidationFailedException.InvalidFieldsDetail,
            errorTranslationKey);
        problemDetails.Extensions["Errors"] = errors;
        return problemDetails;
    }

    public static string Serialize(ProblemDetails problemDetails) {
        var body = new JObject {
            ["type"] = problemDetails.Type,
            ["title"] = problemDetails.Title,
            ["status"] = problemDetails.Status,
            ["detail"] = problemDetails.Detail,
            ["instance"] = problemDetails.Instance,
        };
        foreach (var (name, value) in problemDetails.Extensions) {
            body[CamelCase(name)] = value switch {
                null => JValue.CreateNull(),
                IReadOnlyDictionary<string, string[]> errors => new JObject(
                    errors.Select(error => new JProperty(CamelCasePerSegment(error.Key), new JArray(error.Value)))),
                _ => JToken.FromObject(value),
            };
        }

        return body.ToString(Formatting.None);
    }

    private static string CamelCasePerSegment(string key) => string.Join('.', key.Split('.').Select(CamelCase));

    private static string CamelCase(string segment) =>
        segment.Length == 0 ? segment : char.ToLowerInvariant(segment[0]) + segment[1..];

    private static ProblemDetails BuildSearchFilterNotPermittedProblemDetails() {
        return BuildBadRequestProblemDetailsWithExtension(
            SearchFilterNotPermittedKey,
            "FilterNotPermitted",
            SearchFilterNotPermittedKey);
    }

    private static ProblemDetails BuildNotFoundProblemDetails() {
        return new ProblemDetails {
            Status = (int)HttpStatusCode.NotFound
        };
    }

    private static ProblemDetails BuildNotFoundProblemDetailsWithExtension(string title, string detail,
        string errorTranslationKey) {
        return new ProblemDetails {
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
        string errorTranslationKey) {
        return new ProblemDetails {
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
