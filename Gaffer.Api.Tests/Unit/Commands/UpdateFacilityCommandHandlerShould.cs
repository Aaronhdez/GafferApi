using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Exceptions;
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

    private Facility BuildExistingFacility(Guid id, List<Stand>? stands = null) => new(
        id,
        "National Stadium",
        true,
        new DateTime(2018, 1, 5),
        FacilityType.Stadium,
        52000,
        new Address(Guid.NewGuid(), "Main Avenue", "100", "28001", "Madrid", "Madrid", "Spain"),
        stands);

    [Test]
    public async Task UpdateAFacilityWhileBeingExecuted()
    {
        var existing = BuildExistingFacility(_facilityId);
        var command = BuildCommand(_facilityId, [new UpdateFacilityStandCommand("North", 15000)]);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1)
            .Get(Arg.Is<FacilitySearchCriteria>(sc =>
                    sc != null && sc.criteriaList.Any(c => c.Name == "Id" && c.Value == _facilityId.ToString())),
                Arg.Any<CancellationToken>());
        await _repository.Received(1).Update(Arg.Is<Facility>(facility =>
                facility != null && facility.Id == existing.Id
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
    public async Task PropagateTheRepositoryFailureWhileGettingFacility()
    {
        var failure = new InvalidOperationException("database unavailable");
        var command = BuildCommand(_facilityId);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>())
            .Throws(failure);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }

    [Test]
    public async Task PropagateTheRepositoryFailureWhileUpdatingFacility()
    {
        var failure = new InvalidOperationException("database unavailable");
        var existing = BuildExistingFacility(_facilityId);
        var command = BuildCommand(_facilityId);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);
        _repository.Update(Arg.Any<Facility>(), Arg.Any<CancellationToken>())
            .Throws(failure);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
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

    [Test]
    public async Task KeepTheIdOfMatchedStandsUpdatingTheirValuesAndKeepTheStandsNotMentioned()
    {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        var south = new Stand(Guid.NewGuid(), "South", 4500);
        var existing = BuildExistingFacility(_facilityId, [north, south]);
        var command = BuildCommand(_facilityId, [new UpdateFacilityStandCommand("North Renovated", 5000, north.Id)]);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);

        await _handler.Handle(command, CancellationToken.None);

        var updated = UpdatedFacility();
        updated.Stands.Select(Describe).Should().BeEquivalentTo([
            $"{north.Id}:North Renovated:5000",
            $"{south.Id}:South:4500"
        ]);
        updated.Address.Id.Should().Be(existing.Address.Id);
        updated.Name.Should().Be(command.Name);
    }

    [TestCase(null)]
    [TestCase(0)]
    public async Task KeepEveryExistingStandWhenTheCommandHasNoStands(int? standCount)
    {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        var existing = BuildExistingFacility(_facilityId, [north]);
        var command = BuildCommand(_facilityId, standCount is null ? null : []);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);

        await _handler.Handle(command, CancellationToken.None);

        UpdatedFacility().Stands.Select(Describe).Should().Equal($"{north.Id}:North:4500");
    }

    [Test]
    public async Task CreateStandsWithNewIdsForMissingUnknownOrForeignIdsWhenLessThanFourStandsExist()
    {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        var existing = BuildExistingFacility(_facilityId, [north]);
        var unknownId = Guid.NewGuid();
        var foreignId = Guid.NewGuid();
        var command = BuildCommand(_facilityId, [
            new UpdateFacilityStandCommand("Without Id", 1000),
            new UpdateFacilityStandCommand("Unknown", 2000, unknownId),
            new UpdateFacilityStandCommand("Foreign", 3000, foreignId),
        ]);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);

        await _handler.Handle(command, CancellationToken.None);

        var updated = UpdatedFacility();
        updated.Stands.Should().HaveCount(4);
        updated.Stands.Select(Describe).Should().Contain($"{north.Id}:North:4500");
        var created = updated.Stands.Where(stand => stand.Id != north.Id).ToList();
        created.Select(stand => $"{stand.StandName}:{stand.StandCapacity}")
            .Should().BeEquivalentTo(["Without Id:1000", "Unknown:2000", "Foreign:3000"]);
        created.Select(stand => stand.Id).Should().OnlyHaveUniqueItems()
            .And.NotContain([Guid.Empty, unknownId, foreignId, north.Id]);
    }

    [Test]
    public async Task DiscardEveryUnmatchedStandWhenFourStandsExist()
    {
        var stands = Enumerable.Range(0, 4).Select(i => new Stand(Guid.NewGuid(), $"Stand {i}", 4500)).ToList();
        var originalDescriptions = stands.Select(Describe).ToList();
        var existing = BuildExistingFacility(_facilityId, stands);
        var command = BuildCommand(_facilityId, [
            new UpdateFacilityStandCommand("Renovated", 5000, stands[0].Id),
            new UpdateFacilityStandCommand("Without Id", 1000),
            new UpdateFacilityStandCommand("Unknown", 2000, Guid.NewGuid()),
            new UpdateFacilityStandCommand("Foreign", 3000, Guid.NewGuid()),
        ]);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);

        await _handler.Handle(command, CancellationToken.None);

        var updated = UpdatedFacility();
        updated.Name.Should().Be(command.Name);
        updated.Capacity.Should().Be(command.Capacity);
        updated.Address.Street.Should().Be(command.Address.Street);
        updated.Stands.Select(Describe).Should().BeEquivalentTo(
            [$"{stands[0].Id}:Renovated:5000", .. originalDescriptions.Skip(1)]);
    }

    [TestCase(1)]
    [TestCase(4)]
    public async Task RejectARepeatedIdOfAnExistingStandWithoutUpdating(int existingStands)
    {
        var stands = Enumerable.Range(0, existingStands).Select(i => new Stand(Guid.NewGuid(), $"Stand {i}", 4500)).ToList();
        var existing = BuildExistingFacility(_facilityId, stands);
        var command = BuildCommand(_facilityId, [
            new UpdateFacilityStandCommand("First", 1000, stands[0].Id),
            new UpdateFacilityStandCommand("Second", 2000, stands[0].Id),
        ]);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<CannotUpdateFacilityDuplicatedStandException>()).Which;
        exception.Id.Should().Be(_facilityId);
        exception.StandId.Should().Be(stands[0].Id);
        await _repository.DidNotReceive().Update(Arg.Any<Facility>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RejectAnUpdateLeavingMoreThanFourStandsWithoutUpdating()
    {
        var stands = Enumerable.Range(0, 3).Select(i => new Stand(Guid.NewGuid(), $"Stand {i}", 4500)).ToList();
        var existing = BuildExistingFacility(_facilityId, stands);
        var command = BuildCommand(_facilityId, [
            new UpdateFacilityStandCommand("Without Id", 1000),
            new UpdateFacilityStandCommand("Unknown", 2000, Guid.NewGuid()),
        ]);
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([existing]);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<FacilityValidationException>().WithMessage("*Stands cannot exceed 4*");
        await _repository.DidNotReceive().Update(Arg.Any<Facility>(), Arg.Any<CancellationToken>());
    }

    private Facility UpdatedFacility()
    {
        var call = _repository.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IFacilityRepository.Update));
        return (Facility)call.GetArguments()[0]!;
    }

    private static string Describe(Stand stand) => $"{stand.Id}:{stand.StandName}:{stand.StandCapacity}";
}
