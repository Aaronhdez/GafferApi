using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Commands.Facilities;

public class DeleteFacilityCommandHandler(IFacilityRepository facilityRepository) : ICommandHandler<DeleteFacilityCommand> {
    public async Task Handle(DeleteFacilityCommand command, CancellationToken cancellationToken) {
        try {
            var facility = await FindFacility(command, cancellationToken);
            if (facility == null) throw new FacilityNotFoundException($"Facility with id {command.Id} not found", command.Id);
            await facilityRepository.Delete(command.Id, cancellationToken);
        } catch (FacilityNotFoundException ex) {
            throw new CannotDeleteFacilityNotFoundException(ex.Message, command.Id);
        }
    }

    private async Task<Facility?> FindFacility(DeleteFacilityCommand command, CancellationToken cancellationToken) {
        var criteria = new SearchCriteria {
            Name = "Id",
            Value = command.Id.ToString()
        };
        var searchCriteria = new FacilitySearchCriteria();
        searchCriteria.CriteriaList.Add(criteria);
        var queryResult = await facilityRepository.Get(searchCriteria, cancellationToken);
        var facility = queryResult.FirstOrDefault();
        return facility;
    }
}

public class CannotDeleteFacilityNotFoundException(string message, Guid id) : Exception(message) {
    public Guid Id { get; } = id;
}

public class CannotDeleteFacilityException(string message, Guid id) : Exception(message) {
    public Guid Id { get; } = id;
    public const string InternalServerErrorReason = "Internal Server Error";
    public string Reason { get; } = InternalServerErrorReason;
}

public record DeleteFacilityCommand(Guid Id) : ICommand;
