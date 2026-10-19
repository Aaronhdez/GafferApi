using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Exceptions;

namespace Gaffer.Api.Commands.Stands;

internal static class StandValidation {
    /// <summary>Creates the stand of a sub-resource request; its rules become a 400 with <see cref="ValidationErrorKeys.FacilitiesStands"/>.</summary>
    public static Stand Create(Guid standId, string? standName, int standCapacity, string title) {
        try {
            return Stand.Create(standId, standName, standCapacity);
        } catch (DomainValidationException ex) {
            throw new ValidationFailedException(title, ValidationErrorKeys.FacilitiesStands, ex.Errors);
        }
    }
}
