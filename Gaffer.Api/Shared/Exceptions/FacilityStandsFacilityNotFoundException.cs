namespace Gaffer.Api.Shared.Exceptions;

/// <summary>The facility addressed by a stands operation does not exist.</summary>
public class FacilityStandsFacilityNotFoundException(string message, Guid facilityId) : Exception(message) {
    public Guid FacilityId { get; } = facilityId;
}
