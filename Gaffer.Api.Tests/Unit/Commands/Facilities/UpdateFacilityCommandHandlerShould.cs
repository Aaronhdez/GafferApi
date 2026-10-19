using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Commands.Facilities;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Commands.Facilities;

public class UpdateFacilityCommandHandlerShould {
    private IFacilityRepository repository;
    private UpdateFacilityCommandHandler handler;
    private readonly Guid facilityId = Guid.NewGuid();

    private Facility? updatedFacility;

    [SetUp]
    public void Setup() {
        updatedFacility = null;
        repository = Substitute.For<IFacilityRepository>();
        handler = new UpdateFacilityCommandHandler(repository);
    }

    // The repository applies the merge function to the facility it reads under lock; the double does the same.
    private void GivenExisting(Facility existing)
        => repository.Update(Arg.Any<Guid>(), Arg.Any<Func<Facility, Facility>>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                updatedFacility = call.Arg<Func<Facility, Facility>>()!(existing);
                return Task.CompletedTask;
            });

    private static UpdateFacilityCommand BuildCommand(Guid id, IReadOnlyList<UpdateFacilityStandCommand?>? stands = null) => new() {
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
    public async Task UpdateAFacilityWhileBeingExecuted() {
        var existing = BuildExistingFacility(facilityId);
        var command = BuildCommand(facilityId, [new UpdateFacilityStandCommand("North", 15000)]);
        GivenExisting(existing);

        await handler.Handle(command, CancellationToken.None);

        await repository.Received(1)
            .Update(facilityId, Arg.Any<Func<Facility, Facility>>(), Arg.Any<CancellationToken>());
        updatedFacility.Should().Match<Facility>(facility =>
                facility != null && facility.Id == existing.Id
                && facility.Name == command.Name
                && facility.Active == command.Active
                && facility.BuildDate == command.BuildDate
                && facility.Type == command.Type
                && facility.Capacity == command.Capacity
                && facility.Address.Street == command.Address!.Street
                && facility.Address.StreetNumber == command.Address.StreetNumber
                && facility.Address.PostalCode == command.Address.PostalCode
                && facility.Address.City == command.Address.City
                && facility.Address.Region == command.Address.Region
                && facility.Address.Country == command.Address.Country
                && facility.Stands.Count == 1
                && facility.Stands.Any(stand => stand.StandName == "North" && stand.StandCapacity == 15000));
    }

    [Test]
    public async Task ThrowExceptionWhenFacilityNotFound() {
        var command = BuildCommand(facilityId);
        repository.Update(Arg.Any<Guid>(), Arg.Any<Func<Facility, Facility>>(), Arg.Any<CancellationToken>())
            .Throws(new FacilityNotFoundException("not found", facilityId));

        var result = async () => await handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdateFacilityNotFoundException>();
    }

    [Test]
    public async Task PropagateTheRepositoryFailureWhileGettingFacility() {
        var failure = new InvalidOperationException("database unavailable");
        var command = BuildCommand(facilityId);
        repository.Update(Arg.Any<Guid>(), Arg.Any<Func<Facility, Facility>>(), Arg.Any<CancellationToken>())
            .Throws(failure);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }

    [Test]
    public async Task PropagateTheRepositoryFailureWhileUpdatingFacility() {
        var failure = new InvalidOperationException("database unavailable");
        var existing = BuildExistingFacility(facilityId);
        var command = BuildCommand(facilityId);
        repository.Update(Arg.Any<Guid>(), Arg.Any<Func<Facility, Facility>>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                call.Arg<Func<Facility, Facility>>()!(existing);
                return Task.FromException(failure);
            });

        var result = async () => await handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }

    [Test]
    public async Task PropagateValidationExceptionWhenStandsExceedFour() {
        var existing = BuildExistingFacility(facilityId);
        var command = BuildCommand(facilityId, [
            new UpdateFacilityStandCommand("A", 1000),
            new UpdateFacilityStandCommand("B", 1000),
            new UpdateFacilityStandCommand("C", 1000),
            new UpdateFacilityStandCommand("D", 1000),
            new UpdateFacilityStandCommand("E", 1000),
        ]);
        GivenExisting(existing);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesUpdate);
        exception.Errors.Keys.Should().Equal("Stands");
        exception.Errors["Stands"].Should().ContainSingle("the sent and the resulting stands both exceed the limit, reported once");
        updatedFacility.Should().BeNull();
    }

    [Test]
    public async Task KeepTheIdOfMatchedStandsUpdatingTheirValuesAndKeepTheStandsNotMentioned() {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        var south = new Stand(Guid.NewGuid(), "South", 4500);
        var existing = BuildExistingFacility(facilityId, [north, south]);
        var command = BuildCommand(facilityId, [new UpdateFacilityStandCommand("North Renovated", 5000, north.Id)]);
        GivenExisting(existing);

        await handler.Handle(command, CancellationToken.None);

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
    public async Task KeepEveryExistingStandWhenTheCommandHasNoStands(int? standCount) {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        var existing = BuildExistingFacility(facilityId, [north]);
        var command = BuildCommand(facilityId, standCount is null ? null : []);
        GivenExisting(existing);

        await handler.Handle(command, CancellationToken.None);

        UpdatedFacility().Stands.Select(Describe).Should().Equal($"{north.Id}:North:4500");
    }

    [Test]
    public async Task CreateStandsWithNewIdsForMissingUnknownOrForeignIdsWhenLessThanFourStandsExist() {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        var existing = BuildExistingFacility(facilityId, [north]);
        var unknownId = Guid.NewGuid();
        var foreignId = Guid.NewGuid();
        var command = BuildCommand(facilityId, [
            new UpdateFacilityStandCommand("Without Id", 1000),
            new UpdateFacilityStandCommand("Unknown", 2000, unknownId),
            new UpdateFacilityStandCommand("Foreign", 3000, foreignId),
        ]);
        GivenExisting(existing);

        await handler.Handle(command, CancellationToken.None);

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
    public async Task DiscardEveryUnmatchedStandWhenFourStandsExist() {
        var stands = Enumerable.Range(0, 4).Select(i => new Stand(Guid.NewGuid(), $"Stand {i}", 4500)).ToList();
        var originalDescriptions = stands.Select(Describe).ToList();
        var existing = BuildExistingFacility(facilityId, stands);
        var command = BuildCommand(facilityId, [
            new UpdateFacilityStandCommand("Renovated", 5000, stands[0].Id),
            new UpdateFacilityStandCommand("Without Id", 1000),
            new UpdateFacilityStandCommand("Unknown", 2000, Guid.NewGuid()),
            new UpdateFacilityStandCommand("Foreign", 3000, Guid.NewGuid()),
        ]);
        GivenExisting(existing);

        await handler.Handle(command, CancellationToken.None);

        var updated = UpdatedFacility();
        updated.Name.Should().Be(command.Name);
        updated.Capacity.Should().Be(command.Capacity);
        updated.Address.Street.Should().Be(command.Address!.Street);
        updated.Stands.Select(Describe).Should().BeEquivalentTo(
            [$"{stands[0].Id}:Renovated:5000", .. originalDescriptions.Skip(1)]);
    }

    [TestCase(1)]
    [TestCase(4)]
    public async Task RejectARepeatedIdOfAnExistingStandWithoutUpdating(int existingStands) {
        var stands = Enumerable.Range(0, existingStands).Select(i => new Stand(Guid.NewGuid(), $"Stand {i}", 4500)).ToList();
        var existing = BuildExistingFacility(facilityId, stands);
        var command = BuildCommand(facilityId, [
            new UpdateFacilityStandCommand("First", 1000, stands[0].Id),
            new UpdateFacilityStandCommand("Second", 2000, stands[0].Id),
        ]);
        GivenExisting(existing);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<CannotUpdateFacilityDuplicatedStandException>()).Which;
        exception.Id.Should().Be(facilityId);
        exception.StandId.Should().Be(stands[0].Id);
        updatedFacility.Should().BeNull();
    }

    [Test]
    public async Task RejectAnUpdateLeavingMoreThanFourStandsWithoutUpdating() {
        var stands = Enumerable.Range(0, 3).Select(i => new Stand(Guid.NewGuid(), $"Stand {i}", 4500)).ToList();
        var existing = BuildExistingFacility(facilityId, stands);
        var command = BuildCommand(facilityId, [
            new UpdateFacilityStandCommand("Without Id", 1000),
            new UpdateFacilityStandCommand("Unknown", 2000, Guid.NewGuid()),
        ]);
        GivenExisting(existing);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesUpdate);
        exception.Errors.Keys.Should().Equal("Stands");
        updatedFacility.Should().BeNull();
    }

    [Test]
    public async Task RejectMoreThanFourSentStandsEvenWhenTheFacilityIsFullWithoutUpdating() {
        var stands = Enumerable.Range(0, 4).Select(i => new Stand(Guid.NewGuid(), $"Stand {i}", 4500)).ToList();
        GivenExisting(BuildExistingFacility(facilityId, stands));
        var command = BuildCommand(facilityId, Enumerable.Range(0, 5).Select(i => new UpdateFacilityStandCommand($"New {i}", 10)).ToList());

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.Errors.Keys.Should().Equal("Stands");
        exception.Errors["Stands"].Should().ContainSingle();
        updatedFacility.Should().BeNull();
    }

    [Test]
    public async Task RejectInvalidSentStandsNamingThemByTheirIndexInsideTheLockWithoutUpdating() {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        GivenExisting(BuildExistingFacility(facilityId, [north]));
        var command = BuildCommand(facilityId, [
            new UpdateFacilityStandCommand("Valid", 10),
            new UpdateFacilityStandCommand("   ", 10, north.Id),
            new UpdateFacilityStandCommand("Valid", -1),
        ]);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesUpdate);
        exception.Errors.Keys.Should().BeEquivalentTo("Stands[1].StandName", "Stands[2].StandCapacity");
        updatedFacility.Should().BeNull();
    }

    [Test]
    public async Task RejectANullSentStandTogetherWithTheOtherStandErrorsWithoutUpdating() {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        GivenExisting(BuildExistingFacility(facilityId, [north]));
        var command = BuildCommand(facilityId, [new UpdateFacilityStandCommand("Valid", 10, north.Id), null, new UpdateFacilityStandCommand("Valid", -1)]);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesUpdate);
        exception.Errors.Keys.Should().BeEquivalentTo("Stands[1]", "Stands[2].StandCapacity");
        updatedFacility.Should().BeNull();
    }

    [Test]
    public async Task KeepAStoredStandThatBreaksTheRulesWhenItIsNotSent() {
        var legacyStand = new Stand(Guid.NewGuid(), "", -5);
        GivenExisting(BuildExistingFacility(facilityId, [legacyStand]));

        await handler.Handle(BuildCommand(facilityId, [new UpdateFacilityStandCommand("East", 10)]), CancellationToken.None);

        UpdatedFacility().Stands.Select(Describe).Should().Contain(Describe(legacyStand));
        UpdatedFacility().Stands.Should().HaveCount(2);
    }

    [Test]
    public async Task RejectAnInvalidBodyWithEveryInvalidFieldInsideTheLockWithoutUpdating() {
        GivenExisting(BuildExistingFacility(facilityId));
        var command = BuildCommand(facilityId);
        command.Name = new string('a', 101);
        command.BuildDate = new DateTime(1799, 12, 31);
        command.Capacity = -1;
        command.Address = new UpdateFacilityAddressCommand("", "200", "08002", "Barcelona", "Catalonia", "Spain");

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesUpdate);
        exception.Errors.Keys.Should().BeEquivalentTo("Name", "BuildDate", "Capacity", "Address.Street");
        updatedFacility.Should().BeNull();
    }

    [Test]
    public async Task RejectAnAbsentAddressWithTheUpdateKeyWithoutUpdating() {
        GivenExisting(BuildExistingFacility(facilityId));
        var command = BuildCommand(facilityId);
        command.Address = null;

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var exception = (await result.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesUpdate);
        exception.Errors.Keys.Should().Equal("Address");
        updatedFacility.Should().BeNull();
    }

    [Test]
    public async Task ReturnNotFoundBeforeValidatingAnInvalidBody() {
        repository.Update(Arg.Any<Guid>(), Arg.Any<Func<Facility, Facility>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FacilityNotFoundException("Facility not found", facilityId));
        var command = BuildCommand(facilityId);
        command.Name = "";

        var result = async () => await handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowExactlyAsync<CannotUpdateFacilityNotFoundException>();
    }

    [Test]
    public async Task UpdateAStoredFacilityThatBreaksTheRulesWhenTheBodyIsValid() {
        var legacy = new Facility(facilityId, new string('x', 150), true, new DateTime(1700, 1, 1), FacilityType.Arena, -5,
            new Address(Guid.NewGuid(), "", "1", new string('7', 30), "City", "Region", "Country"), null);
        GivenExisting(legacy);

        await handler.Handle(BuildCommand(facilityId), CancellationToken.None);

        UpdatedFacility().Name.Should().Be("New Stadium Name");
        UpdatedFacility().Address.Id.Should().Be(legacy.Address.Id);
    }

    private Facility UpdatedFacility() => updatedFacility!;

    private static string Describe(Stand stand) => $"{stand.Id}:{stand.StandName}:{stand.StandCapacity}";
}
