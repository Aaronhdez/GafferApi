using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public record AddFacilityStandResponse(Guid Id);

public record AddFacilityStandCommand(Guid FacilityId, string StandName, int StandCapacity)
    : ICommand<AddFacilityStandResponse>;

public record ReplaceFacilityStandCommand(Guid FacilityId, Guid StandId, string StandName, int StandCapacity) : ICommand;

public record RemoveFacilityStandCommand(Guid FacilityId, Guid StandId) : ICommand;

public class AddFacilityStandCommandHandler(IFacilityRepository facilityRepository)
    : ICommandHandler<AddFacilityStandCommand, AddFacilityStandResponse>
{
    public async Task<AddFacilityStandResponse> Handle(AddFacilityStandCommand command, CancellationToken cancellationToken)
    {
        var stand = new Stand(Guid.NewGuid(), command.StandName, command.StandCapacity);
        try
        {
            await facilityRepository.AddStand(command.FacilityId, stand, cancellationToken);
        }
        catch (FacilityNotFoundException ex)
        {
            throw new FacilityStandsFacilityNotFoundException(ex.Message, command.FacilityId);
        }

        return new AddFacilityStandResponse(stand.Id);
    }
}

public class ReplaceFacilityStandCommandHandler(IFacilityRepository facilityRepository)
    : ICommandHandler<ReplaceFacilityStandCommand>
{
    public async Task Handle(ReplaceFacilityStandCommand command, CancellationToken cancellationToken)
    {
        var stand = new Stand(command.StandId, command.StandName, command.StandCapacity);
        try
        {
            await facilityRepository.UpdateStand(command.FacilityId, stand, cancellationToken);
        }
        catch (FacilityNotFoundException ex)
        {
            throw new FacilityStandsFacilityNotFoundException(ex.Message, command.FacilityId);
        }
    }
}

public class RemoveFacilityStandCommandHandler(IFacilityRepository facilityRepository)
    : ICommandHandler<RemoveFacilityStandCommand>
{
    public async Task Handle(RemoveFacilityStandCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await facilityRepository.DeleteStand(command.FacilityId, command.StandId, cancellationToken);
        }
        catch (FacilityNotFoundException ex)
        {
            throw new FacilityStandsFacilityNotFoundException(ex.Message, command.FacilityId);
        }
    }
}
