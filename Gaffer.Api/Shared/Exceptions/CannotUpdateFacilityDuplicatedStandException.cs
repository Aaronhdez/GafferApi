namespace Gaffer.Api.Shared.Exceptions;

public class CannotUpdateFacilityDuplicatedStandException(Guid id, Guid standId)
    : Exception($"Stand {standId} is repeated in the update of facility {id}") {
    public Guid Id { get; } = id;
    public Guid StandId { get; } = standId;
}
