using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands;

public record UpdateFacilityAddressCommand(
    string Street,
    string StreetNumber,
    string PostalCode,
    string City,
    string Region,
    string Country);

/// <param name="Id">Id of an existing stand of the facility to update in place; otherwise the stand is new.</param>
public record UpdateFacilityStandCommand(string StandName, int StandCapacity, Guid? Id = null);

public class UpdateFacilityCommand : ICommand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool Active { get; set; }
    public DateTime BuildDate { get; set; }
    public FacilityType Type { get; set; }
    public int Capacity { get; set; }
    public UpdateFacilityAddressCommand? Address { get; set; }
    public List<UpdateFacilityStandCommand>? Stands { get; set; }
}

public class UpdateFacilityCommandHandler(IFacilityRepository facilityRepository) : ICommandHandler<UpdateFacilityCommand>
{
    public async Task Handle(UpdateFacilityCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await facilityRepository.Update(command.Id, existing => BuildUpdatedFacility(existing, command), cancellationToken);
        }
        catch (FacilityNotFoundException ex)
        {
            throw new CannotUpdateFacilityNotFoundException(ex.Message, command.Id);
        }
        catch (DomainValidationException ex)
        {
            throw new ValidationFailedException(CannotUpdateFacilityTitle, ValidationErrorKeys.FacilitiesUpdate, ex.Errors);
        }
    }

    public const string CannotUpdateFacilityTitle = "Cannot update facility";

    // Runs inside the repository lock: an unknown id is a 404 before the body is validated (Q21 a)), and a
    // validation error thrown here rolls back the transaction.
    private static Facility BuildUpdatedFacility(Facility existing, UpdateFacilityCommand command)
    {
        var address = command.Address is null
            ? null
            : new Address(
                existing.Address.Id,
                command.Address.Street,
                command.Address.StreetNumber,
                command.Address.PostalCode,
                command.Address.City,
                command.Address.Region,
                command.Address.Country);

        // Only the stands sent are checked (by their index in the request); stored stands kept by the merge are not
        // revalidated (Q16). Every invalid field is reported at once.
        var requested = command.Stands ?? [];
        var errors = new DomainValidationErrors();
        Facility.CheckRequestedStands(errors, requested.Select(stand => ((string?)stand.StandName, stand.StandCapacity)).ToList());
        var stands = MergeStands(existing, requested);

        Facility? updated = null;
        errors.Collect(() => updated = Facility.Create(
            existing.Id,
            command.Name,
            command.Active,
            command.BuildDate,
            command.Type,
            command.Capacity,
            address,
            stands));
        errors.ThrowIfAny();
        return updated!;
    }

    /// <summary>
    /// Merges the requested stands into the existing ones: a stand sent with the <c>Id</c> of an existing stand
    /// updates it in place; existing stands not sent are kept unchanged; any other stand (no <c>Id</c>, unknown
    /// <c>Id</c> or the <c>Id</c> of another facility's stand) is created with a new generated <c>Id</c> when the
    /// facility has fewer than <see cref="Facility.MaxStands"/> stands and silently discarded otherwise. The resulting total
    /// is validated by <see cref="Facility"/>.
    /// </summary>
    /// <exception cref="CannotUpdateFacilityDuplicatedStandException">The <c>Id</c> of an existing stand is repeated.</exception>
    private static List<Stand> MergeStands(Facility existing, List<UpdateFacilityStandCommand> requested)
    {
        var existingIds = existing.Stands.Select(stand => stand.Id).ToHashSet();
        bool IsMatched(UpdateFacilityStandCommand stand) => stand.Id is { } id && existingIds.Contains(id);
        var matched = requested.Where(IsMatched).ToList();
        var repeated = matched.GroupBy(stand => stand.Id!.Value).FirstOrDefault(group => group.Count() > 1);
        if (repeated is not null)
        {
            throw new CannotUpdateFacilityDuplicatedStandException(existing.Id, repeated.Key);
        }

        var updates = matched.ToDictionary(stand => stand.Id!.Value);
        var kept = existing.Stands.Select(stand => updates.TryGetValue(stand.Id, out var update)
            ? new Stand(stand.Id, update.StandName, update.StandCapacity)
            : stand);
        var created = existing.Stands.Count < Facility.MaxStands
            ? requested.Where(stand => !IsMatched(stand)).Select(stand => new Stand(Guid.NewGuid(), stand.StandName, stand.StandCapacity))
            : [];

        return [.. kept, .. created];
    }
}

public class CannotUpdateFacilityNotFoundException(string message, Guid id) : Exception(message)
{
    public Guid Id { get; } = id;
}

public class CannotUpdateFacilityException(string message, Guid id) : Exception(message)
{
    public Guid Id { get; } = id;
    public const string InternalServerErrorReason = "Internal Server Error";
    public string Reason { get; } = InternalServerErrorReason;
}
