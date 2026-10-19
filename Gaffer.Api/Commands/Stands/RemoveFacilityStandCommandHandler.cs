using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands.Stands;

public record RemoveFacilityStandCommand(Guid FacilityId, Guid StandId) : ICommand;

public class RemoveFacilityStandCommandHandler(IFacilityRepository facilityRepository)
    : ICommandHandler<RemoveFacilityStandCommand> {
    public async Task Handle(RemoveFacilityStandCommand command, CancellationToken cancellationToken) {
        try {
            await facilityRepository.DeleteStand(command.FacilityId, command.StandId, cancellationToken);
        } catch (FacilityNotFoundException ex) {
            throw new FacilityStandsFacilityNotFoundException(ex.Message, command.FacilityId);
        }
    }
}
