using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Queries;

public class SearchFacilitiesQueryHandlerShould {
    private IFacilityRepository repository;
    private SearchFacilitiesQueryHandler handler;
    private List<Facility> facilities;
    private readonly SearchFacilitiesQuery emptySearchCriteria = new();

    [SetUp]
    public void Setup() {
        facilities = new List<Facility>();
        repository = Substitute.For<IFacilityRepository>();
        repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns(facilities);
        handler = new SearchFacilitiesQueryHandler(repository);
    }

    private static Facility BuildFacility(string name = "National Stadium") => new(
        Guid.NewGuid(),
        name,
        true,
        new DateTime(2018, 1, 5),
        FacilityType.Stadium,
        52000,
        new Address(Guid.NewGuid(), "Main Avenue", "100", "28001", "Madrid", "Madrid", "Spain"),
        null);

    [Test]
    public async Task ReturnEmptyInformationWhenNoFacilitiesFound() {
        var result = await handler.Handle(emptySearchCriteria, CancellationToken.None);

        await repository.Received(1).Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>());
        result.Should().BeEmpty();
    }

    [Test]
    public async Task ReturnAllFacilitiesInformationWhenNoCriteriaIsSpecified() {
        var facility1 = BuildFacility();
        var facility2 = BuildFacility();
        var facility3 = BuildFacility();
        facilities.Add(facility1);
        facilities.Add(facility2);
        facilities.Add(facility3);

        var result = await handler.Handle(emptySearchCriteria, CancellationToken.None);

        await repository.Received(1).Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(facilities.Count);
        result.Should().Contain(sfr => sfr.Id == facility1.Id);
        result.Should().Contain(sfr => sfr.Id == facility2.Id);
        result.Should().Contain(sfr => sfr.Id == facility3.Id);
    }

    [Test]
    public async Task ReturnAFacilityInformationWhenIdCriteriaIsSpecified() {
        var facility1 = BuildFacility();
        var filteredFacilities = new List<Facility> { facility1 };
        var query = new SearchFacilitiesQuery();
        query.Filters.Add(new QueryFilter { Name = "Id", Value = $"{facility1.Id}" });
        repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns(filteredFacilities);

        var result = await handler.Handle(query, CancellationToken.None);

        await repository.Received(1).Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>());
        result.Should().ContainSingle();
        result[0].Id.Should().Be(facility1.Id);
        result[0].Address.Street.Should().Be(facility1.Address.Street);
    }

    [Test]
    public async Task ReturnEmptyInformationWhenIdCriteriaHasNoMatch() {
        var query = new SearchFacilitiesQuery();
        query.Filters.Add(new QueryFilter { Name = "Id", Value = $"{Guid.NewGuid()}" });
        repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task MapEveryQueryFilterToRepositorySearchCriteria() {
        var facility = BuildFacility("City Arena");
        var query = new SearchFacilitiesQuery();
        query.Filters.Add(new QueryFilter { Name = "Name", Value = "City Arena" });
        query.Filters.Add(new QueryFilter { Name = "Type", Value = nameof(FacilityType.Stadium) });
        repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([facility]);

        var result = await handler.Handle(query, CancellationToken.None);

        await repository.Received(1).Get(Arg.Is<FacilitySearchCriteria>(criteria =>
                criteria != null && criteria.CriteriaList.Count == 2
                && criteria.CriteriaList.Any(filter => filter.Name == "Name" && filter.Value == "City Arena")
                && criteria.CriteriaList.Any(filter => filter.Name == "Type" && filter.Value == nameof(FacilityType.Stadium))),
            Arg.Any<CancellationToken>());
        result.Should().ContainSingle();
        result[0].Name.Should().Be(facility.Name);
    }

    [Test]
    public async Task PropagateExceptionWhenRepositoryFails() {
        repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("repository unavailable"));

        var result = async () => await handler.Handle(emptySearchCriteria, CancellationToken.None);

        await result.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*repository unavailable*");
    }
}
