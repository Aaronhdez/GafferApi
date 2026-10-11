using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Commands;

public class FacilityStandCommandHandlersShould
{
    private IFacilityRepository _repository;
    private readonly Guid _facilityId = Guid.NewGuid();
    private readonly Guid _standId = Guid.NewGuid();

    [SetUp]
    public void Setup() => _repository = Substitute.For<IFacilityRepository>();

    [Test]
    public async Task AddAStandWithAServerGeneratedId()
    {
        var handler = new AddFacilityStandCommandHandler(_repository);

        var response = await handler.Handle(new AddFacilityStandCommand(_facilityId, "East", 3000), CancellationToken.None);

        response.Id.Should().NotBeEmpty();
        await _repository.Received(1).AddStand(_facilityId,
            Arg.Is<Stand>(stand => stand != null && stand.Id == response.Id && stand.StandName == "East" && stand.StandCapacity == 3000),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReplaceAStandKeepingTheRequestedId()
    {
        var handler = new ReplaceFacilityStandCommandHandler(_repository);

        await handler.Handle(new ReplaceFacilityStandCommand(_facilityId, _standId, "East", 3000), CancellationToken.None);

        await _repository.Received(1).UpdateStand(_facilityId,
            Arg.Is<Stand>(stand => stand != null && stand.Id == _standId && stand.StandName == "East" && stand.StandCapacity == 3000),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RemoveAStand()
    {
        var handler = new RemoveFacilityStandCommandHandler(_repository);

        await handler.Handle(new RemoveFacilityStandCommand(_facilityId, _standId), CancellationToken.None);

        await _repository.Received(1).DeleteStand(_facilityId, _standId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TranslateAMissingFacilityOnEveryCommand()
    {
        var missing = new FacilityNotFoundException("not found", _facilityId);
        _repository.AddStand(default, default!, default).ThrowsAsyncForAnyArgs(missing);
        _repository.UpdateStand(default, default!, default).ThrowsAsyncForAnyArgs(missing);
        _repository.DeleteStand(default, default, default).ThrowsAsyncForAnyArgs(missing);

        var add = async () => await new AddFacilityStandCommandHandler(_repository).Handle(new AddFacilityStandCommand(_facilityId, "East", 1), CancellationToken.None);
        var replace = async () => await new ReplaceFacilityStandCommandHandler(_repository).Handle(new ReplaceFacilityStandCommand(_facilityId, _standId, "East", 1), CancellationToken.None);
        var remove = async () => await new RemoveFacilityStandCommandHandler(_repository).Handle(new RemoveFacilityStandCommand(_facilityId, _standId), CancellationToken.None);

        foreach (var act in new[] { add, replace, remove })
        {
            (await act.Should().ThrowExactlyAsync<FacilityStandsFacilityNotFoundException>()).Which.FacilityId.Should().Be(_facilityId);
        }
    }

    [Test]
    public async Task LetTheStandRulesOfTheRepositoryPropagateUntranslated()
    {
        _repository.AddStand(default, default!, default).ThrowsAsyncForAnyArgs(new FacilityMaxStandsExceededException(_facilityId));
        _repository.DeleteStand(default, default, default).ThrowsAsyncForAnyArgs(new FacilityStandNotFoundException(_facilityId, _standId));

        var add = async () => await new AddFacilityStandCommandHandler(_repository).Handle(new AddFacilityStandCommand(_facilityId, "East", 1), CancellationToken.None);
        var remove = async () => await new RemoveFacilityStandCommandHandler(_repository).Handle(new RemoveFacilityStandCommand(_facilityId, _standId), CancellationToken.None);

        await add.Should().ThrowExactlyAsync<FacilityMaxStandsExceededException>();
        await remove.Should().ThrowExactlyAsync<FacilityStandNotFoundException>();
    }
}
