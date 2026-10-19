namespace Gaffer.Api.Shared.Exceptions;

public static class ValidationErrorKeys {
    public const string PlayersCreate = "Error.Players.Create.Validation";
    public const string PlayersUpdate = "Error.Players.Update.Validation";
    public const string FacilitiesCreate = "Error.Facilities.Create.Validation";
    public const string FacilitiesUpdate = "Error.Facilities.Update.Validation";
    public const string FacilitiesStands = "Error.Facilities.Stands.Validation";
}

public class ValidationFailedException(string title, string errorTranslationKey, IReadOnlyDictionary<string, string[]> errors)
    : Exception(title) {
    public const string InvalidFieldsDetail = "One or more fields are not valid.";

    public string Title { get; } = title;
    public string ErrorTranslationKey { get; } = errorTranslationKey;
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
