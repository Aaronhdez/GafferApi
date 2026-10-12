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
    public const string CannotCreateStandTitle = "Cannot create stand";

    public async Task<AddFacilityStandResponse> Handle(AddFacilityStandCommand command, CancellationToken cancellationToken)
    {
        // Built before the repository is called: an invalid stand is a 400 even if the facility does not exist (Q21 a)).
        var stand = StandValidation.Create(Guid.NewGuid(), command.StandName, command.StandCapacity, CannotCreateStandTitle);
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
    public const string CannotUpdateStandTitle = "Cannot update stand";

    public async Task Handle(ReplaceFacilityStandCommand command, CancellationToken cancellationToken)
    {
        // Built before the repository is called: an invalid stand is a 400 before any 404 (Q21 a)).
        var stand = StandValidation.Create(command.StandId, command.StandName, command.StandCapacity, CannotUpdateStandTitle);
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

internal static class StandValidation
{
    /// <summary>Creates the stand of a sub-resource request; its rules become a 400 with <see cref="ValidationErrorKeys.FacilitiesStands"/>.</summary>
    public static Stand Create(Guid standId, string? standName, int standCapacity, string title)
    {
        try
        {
            return Stand.Create(standId, standName, standCapacity);
        }
        catch (DomainValidationException ex)
        {
            throw new ValidationFailedException(title, ValidationErrorKeys.FacilitiesStands, ex.Errors);
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
