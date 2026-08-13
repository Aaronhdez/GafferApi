using Gaffer.Api.Domain;

namespace Gaffer.Api.Shared.Repositories;

public interface IFacilityRepository
{
    Task Save(Facility facility, CancellationToken cancellationToken);
}