using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Commands.Stands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Commands.Stands;

public class FacilityStandCommandHandlersShould {
    private IFacilityRepository repository;
    private readonly Guid facilityId = Guid.NewGuid();
    private readonly Guid standId = Guid.NewGuid();

    [SetUp]
    public void Setup() => repository = Substitute.For<IFacilityRepository>();

    [Test]
    public async Task AddAStandWithAServerGeneratedId() {
        var handler = new AddFacilityStandCommandHandler(repository);

        var response = await handler.Handle(new AddFacilityStandCommand(facilityId, "East", 3000), CancellationToken.None);

        response.Id.Should().NotBeEmpty();
        await repository.Received(1).AddStand(facilityId,
            Arg.Is<Stand>(stand => stand != null && stand.Id == response.Id && stand.StandName == "East" && stand.StandCapacity == 3000),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReplaceAStandKeepingTheRequestedId() {
        var handler = new ReplaceFacilityStandCommandHandler(repository);

        await handler.Handle(new ReplaceFacilityStandCommand(facilityId, standId, "East", 3000), CancellationToken.None);

        await repository.Received(1).UpdateStand(facilityId,
            Arg.Is<Stand>(stand => stand != null && stand.Id == standId && stand.StandName == "East" && stand.StandCapacity == 3000),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RemoveAStand() {
        var handler = new RemoveFacilityStandCommandHandler(repository);

        await handler.Handle(new RemoveFacilityStandCommand(facilityId, standId), CancellationToken.None);

        await repository.Received(1).DeleteStand(facilityId, standId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TranslateAMissingFacilityOnEveryCommand() {
        var missing = new FacilityNotFoundException("not found", facilityId);
        repository.AddStand(default, default!, default).ThrowsAsyncForAnyArgs(missing);
        repository.UpdateStand(default, default!, default).ThrowsAsyncForAnyArgs(missing);
        repository.DeleteStand(default, default, default).ThrowsAsyncForAnyArgs(missing);

        var add = async () => await new AddFacilityStandCommandHandler(repository).Handle(new AddFacilityStandCommand(facilityId, "East", 1), CancellationToken.None);
        var replace = async () => await new ReplaceFacilityStandCommandHandler(repository).Handle(new ReplaceFacilityStandCommand(facilityId, standId, "East", 1), CancellationToken.None);
        var remove = async () => await new RemoveFacilityStandCommandHandler(repository).Handle(new RemoveFacilityStandCommand(facilityId, standId), CancellationToken.None);

        foreach (var act in new[] { add, replace, remove }) {
            (await act.Should().ThrowExactlyAsync<FacilityStandsFacilityNotFoundException>()).Which.FacilityId.Should().Be(facilityId);
        }
    }

    [Test]
    public async Task LetTheStandRulesOfTheRepositoryPropagateUntranslated() {
        repository.AddStand(default, default!, default).ThrowsAsyncForAnyArgs(new FacilityMaxStandsExceededException(facilityId));
        repository.DeleteStand(default, default, default).ThrowsAsyncForAnyArgs(new FacilityStandNotFoundException(facilityId, standId));

        var add = async () => await new AddFacilityStandCommandHandler(repository).Handle(new AddFacilityStandCommand(facilityId, "East", 1), CancellationToken.None);
        var remove = async () => await new RemoveFacilityStandCommandHandler(repository).Handle(new RemoveFacilityStandCommand(facilityId, standId), CancellationToken.None);

        await add.Should().ThrowExactlyAsync<FacilityMaxStandsExceededException>();
        await remove.Should().ThrowExactlyAsync<FacilityStandNotFoundException>();
    }

    // Step 03b: one case per rule and handler; the stand is built before the repository is called (400 before 404).
    [TestCase("", 10, "StandName")]
    [TestCase("East", -1, "StandCapacity")]
    public async Task RejectAnInvalidStandOnCreationWithoutCallingTheRepository(string standName, int standCapacity, string field) {
        var act = async () => await new AddFacilityStandCommandHandler(repository)
            .Handle(new AddFacilityStandCommand(facilityId, standName, standCapacity), CancellationToken.None);

        var exception = (await act.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesStands);
        exception.Title.Should().Be(AddFacilityStandCommandHandler.CannotCreateStandTitle);
        exception.Errors.Keys.Should().Equal(field);
        await repository.DidNotReceiveWithAnyArgs().AddStand(default, default!, default);
    }

    [TestCase("   ", 10, "StandName")]
    [TestCase("East", -1, "StandCapacity")]
    public async Task RejectAnInvalidStandOnReplacementWithoutCallingTheRepository(string standName, int standCapacity, string field) {
        var act = async () => await new ReplaceFacilityStandCommandHandler(repository)
            .Handle(new ReplaceFacilityStandCommand(facilityId, standId, standName, standCapacity), CancellationToken.None);

        var exception = (await act.Should().ThrowExactlyAsync<ValidationFailedException>()).Which;
        exception.ErrorTranslationKey.Should().Be(ValidationErrorKeys.FacilitiesStands);
        exception.Title.Should().Be(ReplaceFacilityStandCommandHandler.CannotUpdateStandTitle);
        exception.Errors.Keys.Should().Equal(field);
        await repository.DidNotReceiveWithAnyArgs().UpdateStand(default, default!, default);
    }
}
