using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Queries;

public class FacilityStandQueryHandlersShould
{
    private IFacilityRepository _repository;
    private readonly Guid _facilityId = Guid.NewGuid();

    [SetUp]
    public void Setup() => _repository = Substitute.For<IFacilityRepository>();

    [Test]
    public async Task ListTheStandsOfAFacility()
    {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        _repository.GetStands(_facilityId, Arg.Any<CancellationToken>()).Returns([north]);

        var result = await new SearchFacilityStandsQueryHandler(_repository).Handle(new SearchFacilityStandsQuery(_facilityId), CancellationToken.None);

        result.Should().BeEquivalentTo([new SearchFacilityStandResult(north.Id, "North", 4500)]);
    }

    [Test]
    public async Task ReadAStandOfAFacility()
    {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        _repository.GetStand(_facilityId, north.Id, Arg.Any<CancellationToken>()).Returns(north);

        var result = await new GetFacilityStandQueryHandler(_repository).Handle(new GetFacilityStandQuery(_facilityId, north.Id), CancellationToken.None);

        result.Should().BeEquivalentTo(new SearchFacilityStandResult(north.Id, "North", 4500));
    }

    [Test]
    public async Task TranslateAMissingFacility()
    {
        var missing = new FacilityNotFoundException("not found", _facilityId);
        _repository.GetStands(default, default).ThrowsAsyncForAnyArgs(missing);
        _repository.GetStand(default, default, default).ThrowsAsyncForAnyArgs(missing);

        var list = async () => await new SearchFacilityStandsQueryHandler(_repository).Handle(new SearchFacilityStandsQuery(_facilityId), CancellationToken.None);
        var read = async () => await new GetFacilityStandQueryHandler(_repository).Handle(new GetFacilityStandQuery(_facilityId, Guid.NewGuid()), CancellationToken.None);

        await list.Should().ThrowExactlyAsync<FacilityStandsFacilityNotFoundException>();
        await read.Should().ThrowExactlyAsync<FacilityStandsFacilityNotFoundException>();
    }

    [Test]
    public async Task LetAMissingStandPropagateUntranslated()
    {
        var standId = Guid.NewGuid();
        _repository.GetStand(default, default, default).ThrowsAsyncForAnyArgs(new FacilityStandNotFoundException(_facilityId, standId));

        var read = async () => await new GetFacilityStandQueryHandler(_repository).Handle(new GetFacilityStandQuery(_facilityId, standId), CancellationToken.None);

        await read.Should().ThrowExactlyAsync<FacilityStandNotFoundException>();
    }
}
