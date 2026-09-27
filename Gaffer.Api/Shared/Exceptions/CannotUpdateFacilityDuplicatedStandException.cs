namespace Gaffer.Api.Shared.Exceptions;

/// <summary>
/// The update of a facility repeats the <see cref="StandId"/> of one of its existing stands.
/// Converted into a 400 with the translation key <c>Error.Facilities.Update.DuplicatedStand</c>.
/// </summary>
public class CannotUpdateFacilityDuplicatedStandException(Guid id, Guid standId)
    : Exception($"Stand {standId} is repeated in the update of facility {id}")
{
    public Guid Id { get; } = id;
    public Guid StandId { get; } = standId;
}
