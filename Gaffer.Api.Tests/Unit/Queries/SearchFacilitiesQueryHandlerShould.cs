using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Queries;

public class SearchFacilitiesQueryHandlerShould
{
    private IFacilityRepository _repository;
    private SearchFacilitiesQueryHandler _handler;
    private List<Facility> _facilities;
    private readonly SearchFacilitiesQuery _emptySearchCriteria = new();

    [SetUp]
    public void Setup()
    {
        _facilities = new List<Facility>();
        _repository = Substitute.For<IFacilityRepository>();
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns(_facilities);
        _handler = new SearchFacilitiesQueryHandler(_repository);
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
    public async Task ReturnEmptyInformationWhenNoFacilitiesFound()
    {
        var result = await _handler.Handle(_emptySearchCriteria, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>());
        result.Should().BeEmpty();
    }

    [Test]
    public async Task ReturnAllFacilitiesInformationWhenNoCriteriaIsSpecified()
    {
        var facility1 = BuildFacility();
        var facility2 = BuildFacility();
        var facility3 = BuildFacility();
        _facilities.Add(facility1);
        _facilities.Add(facility2);
        _facilities.Add(facility3);

        var result = await _handler.Handle(_emptySearchCriteria, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(_facilities.Count);
        result.Should().Contain(sfr => sfr.Id == facility1.Id);
        result.Should().Contain(sfr => sfr.Id == facility2.Id);
        result.Should().Contain(sfr => sfr.Id == facility3.Id);
    }

    [Test]
    public async Task ReturnAFacilityInformationWhenIdCriteriaIsSpecified()
    {
        var facility1 = BuildFacility();
        var filteredFacilities = new List<Facility> { facility1 };
        var query = new SearchFacilitiesQuery();
        query.Filters.Add(new QueryFilter { Name = "Id", Value = $"{facility1.Id}" });
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns(filteredFacilities);

        var result = await _handler.Handle(query, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>());
        result.Should().ContainSingle();
        result[0].Id.Should().Be(facility1.Id);
        result[0].Address.Street.Should().Be(facility1.Address.Street);
    }

    [Test]
    public async Task ReturnEmptyInformationWhenIdCriteriaHasNoMatch()
    {
        var query = new SearchFacilitiesQuery();
        query.Filters.Add(new QueryFilter { Name = "Id", Value = $"{Guid.NewGuid()}" });
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task MapEveryQueryFilterToRepositorySearchCriteria()
    {
        var facility = BuildFacility("City Arena");
        var query = new SearchFacilitiesQuery();
        query.Filters.Add(new QueryFilter { Name = "Name", Value = "City Arena" });
        query.Filters.Add(new QueryFilter { Name = "Type", Value = nameof(FacilityType.Stadium) });
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>()).Returns([facility]);

        var result = await _handler.Handle(query, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Is<FacilitySearchCriteria>(criteria =>
                criteria != null && criteria.criteriaList.Count == 2
                && criteria.criteriaList.Any(filter => filter.Name == "Name" && filter.Value == "City Arena")
                && criteria.criteriaList.Any(filter => filter.Name == "Type" && filter.Value == nameof(FacilityType.Stadium))),
            Arg.Any<CancellationToken>());
        result.Should().ContainSingle();
        result[0].Name.Should().Be(facility.Name);
    }

    [Test]
    public async Task PropagateExceptionWhenRepositoryFails()
    {
        _repository.Get(Arg.Any<FacilitySearchCriteria>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("repository unavailable"));

        var result = async () => await _handler.Handle(_emptySearchCriteria, CancellationToken.None);

        await result.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*repository unavailable*");
    }
}
