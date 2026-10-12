namespace Gaffer.Api.Shared.Exceptions;

/// <summary>Translation keys of the validation 400, one per operation.</summary>
public static class ValidationErrorKeys
{
    public const string PlayersCreate = "Error.Players.Create.Validation";
    public const string PlayersUpdate = "Error.Players.Update.Validation";
    public const string FacilitiesCreate = "Error.Facilities.Create.Validation";
    public const string FacilitiesUpdate = "Error.Facilities.Update.Validation";
    public const string FacilitiesStands = "Error.Facilities.Stands.Validation";
}

/// <summary>
/// A request rejected because some fields are not valid. It carries the operation (title and translation key)
/// and the errors per field, and becomes a 400 <c>ProblemDetails</c> through the exception middleware.
/// </summary>
public class ValidationFailedException(string title, string errorTranslationKey, IReadOnlyDictionary<string, string[]> errors)
    : Exception(title)
{
    public const string InvalidFieldsDetail = "One or more fields are not valid.";

    public string Title { get; } = title;
    public string ErrorTranslationKey { get; } = errorTranslationKey;
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
