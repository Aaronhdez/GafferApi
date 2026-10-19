namespace Gaffer.Api.Shared.Exceptions;

public class FacilityStandsFacilityNotFoundException(string message, Guid facilityId) : Exception(message) {
    public Guid FacilityId { get; } = facilityId;
}
