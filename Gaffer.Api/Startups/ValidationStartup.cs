using Gaffer.Api.Commands;
using Gaffer.Api.Commands.Stands;
using Gaffer.Api.Commands.Facilities;
using Gaffer.Api.Controllers;
using Gaffer.Api.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Gaffer.Api.Startups;

public static class ValidationStartup {
    public const string InvalidValueMessage = "The value is not valid.";
    public const string BodyField = "Body";

    private static readonly IReadOnlyDictionary<(Type Controller, string Action), (string Title, string Key)> Operations =
        new Dictionary<(Type, string), (string, string)> {
            [(typeof(PlayerController), nameof(PlayerController.Create))] = ("Cannot create player", ValidationErrorKeys.PlayersCreate),
            [(typeof(PlayerController), nameof(PlayerController.Update))] = ("Cannot update player", ValidationErrorKeys.PlayersUpdate),
            [(typeof(FacilitiesController), nameof(FacilitiesController.Create))] = (CreateFacilityCommandHandler.CannotCreateFacilityTitle, ValidationErrorKeys.FacilitiesCreate),
            [(typeof(FacilitiesController), nameof(FacilitiesController.Update))] = (UpdateFacilityCommandHandler.CannotUpdateFacilityTitle, ValidationErrorKeys.FacilitiesUpdate),
            [(typeof(FacilityStandsController), nameof(FacilityStandsController.Create))] = (AddFacilityStandCommandHandler.CannotCreateStandTitle, ValidationErrorKeys.FacilitiesStands),
            [(typeof(FacilityStandsController), nameof(FacilityStandsController.Update))] = (ReplaceFacilityStandCommandHandler.CannotUpdateStandTitle, ValidationErrorKeys.FacilitiesStands),
        };

    public static IServiceCollection ConfigureValidationResponses(this IServiceCollection services) {
        services.PostConfigure<ApiBehaviorOptions>(options => {
            var defaultFactory = options.InvalidModelStateResponseFactory;
            options.InvalidModelStateResponseFactory = context => BuildValidationResponse(context) ?? defaultFactory(context);
        });
        return services;
    }

    private static IActionResult? BuildValidationResponse(ActionContext context) {
        if (context.ActionDescriptor is not ControllerActionDescriptor action
            || !Operations.TryGetValue((action.ControllerTypeInfo.AsType(), action.MethodInfo.Name), out var operation)) {
            return null;
        }

        var bodyParameter = action.Parameters.FirstOrDefault(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)?.Name;
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry => FieldName(entry.Key, bodyParameter))
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(field => field, _ => new[] { InvalidValueMessage }, StringComparer.Ordinal);

        var problemDetails = ExceptionToHttpResponseConverters.BuildValidationProblemDetails(operation.Title, operation.Key, errors);
        problemDetails.Instance = context.HttpContext.Request.Path.Value;
        return new ContentResult {
            StatusCode = problemDetails.Status,
            ContentType = "application/json",
            Content = ExceptionToHttpResponseConverters.Serialize(problemDetails),
        };
    }

    private static string FieldName(string key, string? bodyParameter) {
        var path = key;
        if (bodyParameter is not null) {
            if (string.Equals(path, bodyParameter, StringComparison.OrdinalIgnoreCase)) path = string.Empty;
            else if (path.StartsWith(bodyParameter + ".", StringComparison.OrdinalIgnoreCase)) path = path[(bodyParameter.Length + 1)..];
        }

        if (path.Length == 0) return BodyField;
        return string.Join('.', path.Split('.').Select(segment =>
            segment.Length == 0 ? segment : char.ToUpperInvariant(segment[0]) + segment[1..]));
    }
}
