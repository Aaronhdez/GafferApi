using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Queries;

public class FacilityStandQueryHandlersShould {
    private IFacilityRepository repository;
    private readonly Guid facilityId = Guid.NewGuid();

    [SetUp]
    public void Setup() => repository = Substitute.For<IFacilityRepository>();

    [Test]
    public async Task ListTheStandsOfAFacility() {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        repository.GetStands(facilityId, Arg.Any<CancellationToken>()).Returns([north]);

        var result = await new SearchFacilityStandsQueryHandler(repository).Handle(new SearchFacilityStandsQuery(facilityId), CancellationToken.None);

        result.Should().BeEquivalentTo([new SearchFacilityStandResult(north.Id, "North", 4500)]);
    }

    [Test]
    public async Task ReadAStandOfAFacility() {
        var north = new Stand(Guid.NewGuid(), "North", 4500);
        repository.GetStand(facilityId, north.Id, Arg.Any<CancellationToken>()).Returns(north);

        var result = await new GetFacilityStandQueryHandler(repository).Handle(new GetFacilityStandQuery(facilityId, north.Id), CancellationToken.None);

        result.Should().BeEquivalentTo(new SearchFacilityStandResult(north.Id, "North", 4500));
    }

    [Test]
    public async Task TranslateAMissingFacility() {
        var missing = new FacilityNotFoundException("not found", facilityId);
        repository.GetStands(default, default).ThrowsAsyncForAnyArgs(missing);
        repository.GetStand(default, default, default).ThrowsAsyncForAnyArgs(missing);

        var list = async () => await new SearchFacilityStandsQueryHandler(repository).Handle(new SearchFacilityStandsQuery(facilityId), CancellationToken.None);
        var read = async () => await new GetFacilityStandQueryHandler(repository).Handle(new GetFacilityStandQuery(facilityId, Guid.NewGuid()), CancellationToken.None);

        await list.Should().ThrowExactlyAsync<FacilityStandsFacilityNotFoundException>();
        await read.Should().ThrowExactlyAsync<FacilityStandsFacilityNotFoundException>();
    }

    [Test]
    public async Task LetAMissingStandPropagateUntranslated() {
        var standId = Guid.NewGuid();
        repository.GetStand(default, default, default).ThrowsAsyncForAnyArgs(new FacilityStandNotFoundException(facilityId, standId));

        var read = async () => await new GetFacilityStandQueryHandler(repository).Handle(new GetFacilityStandQuery(facilityId, standId), CancellationToken.None);

        await read.Should().ThrowExactlyAsync<FacilityStandNotFoundException>();
    }
}
