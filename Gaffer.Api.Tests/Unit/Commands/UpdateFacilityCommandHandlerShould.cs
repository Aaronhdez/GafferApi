using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Commands;

public class UpdateFacilityCommandHandlerShould
{
    private IFacilityRepository _repository;
    private UpdateFacilityCommandHandler _handler;
    private readonly Guid _facilityId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IFacilityRepository>();
        _handler = new UpdateFacilityCommandHandler(_repository);
    }

    private static UpdateFacilityCommand BuildCommand(Guid id, List<UpdateFacilityStandCommand>? stands = null) => new()
    {
        Id = id,
        Name = "New Stadium Name",
        Active = false,
        BuildDate = new DateTime(2019, 6, 1),
        Type = FacilityType.Arena,
        Capacity = 30000,
        Address = new UpdateFacilityAddressCommand("New Street", "200", "08002", "Barcelona", "Catalonia", "Spain"),
        Stands = stands,
    };

    private Facility BuildExistingFacility(Guid id) => new(
        id,
        "National Stadium",
        true,
        new DateTime(2018, 1, 5),
        FacilityType.Stadium,
        52000,
        new Address(Guid.NewGuid(), "Main Avenue", "100", "28001", "Madrid", "Madrid", "Spain"),
        null);

    [Test]
    public async Task UpdateAFacilityWhileBeingExecuted()
    {
        var existing = BuildExistingFacility(_facilityId);
        var command = BuildCommand(_facilityId, [new UpdateFacilityStandCommand("North", 15000)]);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1)
            .Get(Arg.Is<FacilitySearchCriteria>(sc =>
                    sc.criteriaList.Any(c => c.Name == "Id" && c.Value == _facilityId.ToString())),
                Arg.Any<CancellationToken>());
        await _repository.Received(1).Update(Arg.Is<Facility>(facility =>
                facility.Id == existing.Id
                && facility.Name == command.Name
                && facility.Active == command.Active
                && facility.BuildDate == command.BuildDate
                && facility.Type == command.Type
                && facility.Capacity == command.Capacity
                && facility.Address.Street == command.Address.Street
                && facility.Address.StreetNumber == command.Address.StreetNumber
                && facility.Address.PostalCode == command.Address.PostalCode
                && facility.Address.City == command.Address.City
                && facility.Address.Region == command.Address.Region
                && facility.Address.Country == command.Address.Country
                && facility.Stands.Count == 1
                && facility.Stands.Any(stand => stand.StandName == "North" && stand.StandCapacity == 15000)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ThrowExceptionWhenFacilityNotFound()
    {
        var command = BuildCommand(_facilityId);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdateFacilityNotFoundException>();
    }

    [Test]
    public async Task ThrowExceptionWhenRepositoryFailsWhileGettingFacility()
    {
        var command = BuildCommand(_facilityId);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("repository unavailable"));

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdateFacilityException>()
            .WithMessage("*repository unavailable*");
    }

    [Test]
    public async Task ThrowExceptionWhenRepositoryFailsWhileUpdatingFacility()
    {
        var existing = BuildExistingFacility(_facilityId);
        var command = BuildCommand(_facilityId);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);
        _repository.Update(Arg.Any<Facility>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("update failed"));

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdateFacilityException>()
            .WithMessage("*update failed*");
    }

    [Test]
    public async Task PropagateValidationExceptionWhenStandsExceedFour()
    {
        var existing = BuildExistingFacility(_facilityId);
        var command = BuildCommand(_facilityId, [
            new UpdateFacilityStandCommand("A", 1000),
            new UpdateFacilityStandCommand("B", 1000),
            new UpdateFacilityStandCommand("C", 1000),
            new UpdateFacilityStandCommand("D", 1000),
            new UpdateFacilityStandCommand("E", 1000),
        ]);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<FacilityValidationException>()
            .WithMessage("*Stands cannot exceed 4*");
        await _repository.DidNotReceive().Update(Arg.Any<Facility>(), Arg.Any<CancellationToken>());
    }
}
