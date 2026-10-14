using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Criteria;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Queries;

public class SearchPlayersQueryHandlerShould {
    private IPlayerRepository repository;
    private SearchPlayersQueryHandler handler;
    private List<Player> players;
    private readonly SearchPlayersQuery emptySearchCriteria = new();

    [SetUp]
    public void Setup() {
        players = new List<Player>();
        repository = Substitute.For<IPlayerRepository>();
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns(players);
        handler = new SearchPlayersQueryHandler(repository);
    }

    [Test]
    public async Task ReturnEmptyInformationWhenNoPlayersFound() {
        var result = await handler.Handle(emptySearchCriteria, CancellationToken.None);

        await repository.Received(1).Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(0);
    }

    [Test]
    public async Task ReturnAllPlayersInformationWhenNoCriteriaIsSpecified() {
        var player1 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001, 1, 1));
        var player2 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001, 1, 1));
        var player3 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001, 1, 1));
        players.Add(player1);
        players.Add(player2);
        players.Add(player3);

        var result = await handler.Handle(emptySearchCriteria, CancellationToken.None);

        await repository.Received(1).Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(players.Count);
        result.Should().Contain(spr => spr.Id == player1.Id);
        result.Should().Contain(spr => spr.Id == player2.Id);
        result.Should().Contain(spr => spr.Id == player3.Id);
    }

    [Test]
    public async Task ReturnAPlayerInformationWhenIdCriteriaIsSpecified() {
        var player1 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001, 1, 1));
        var player2 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001, 1, 1));
        var player3 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001, 1, 1));
        players.Add(player1);
        players.Add(player2);
        players.Add(player3);
        var filteredPlayers = new List<Player>();
        filteredPlayers.Add(player1);
        var query = new SearchPlayersQuery();
        query.Filters.Add(new QueryFilter { Name = "Id", Value = $"{player1.Id}" });
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns(filteredPlayers);

        var result = await handler.Handle(query, CancellationToken.None);


        await repository.Received(1).Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(1);
        result.Should().Contain(spr => spr.Id == player1.Id);
    }

    [Test]
    public async Task PropagateExceptionWhenRepositoryFails() {
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("repository unavailable"));

        var result = async () => await handler.Handle(emptySearchCriteria, CancellationToken.None);

        await result.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*repository unavailable*");
    }

    [Test]
    public async Task MapEveryQueryFilterToRepositorySearchCriteria() {
        var player = new Player(Guid.NewGuid(), "Jane", "Doe", "Smith", Gender.FEMALE, new DateTime(2002, 2, 2));
        var query = new SearchPlayersQuery();
        query.Filters.Add(new QueryFilter { Name = "FirstName", Value = "Jane" });
        query.Filters.Add(new QueryFilter { Name = "Gender", Value = nameof(Gender.FEMALE) });
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns([player]);

        var result = await handler.Handle(query, CancellationToken.None);

        await repository.Received(1).Get(Arg.Is<CriteriaSet>(criteria =>
            criteria != null && criteria.ValueCriteria.Count == 2
            && criteria.ValueCriteria.Any(filter => IsSingleAndValue(filter, "FirstName", "Jane"))
            && criteria.ValueCriteria.Any(filter => IsSingleAndValue(filter, "Gender", nameof(Gender.FEMALE)))),
            Arg.Any<CancellationToken>());
        result.Should().ContainSingle();
        result[0].Gender.Should().Be(Gender.FEMALE);
        result[0].BirthDate.Should().Be(player.Birthdate);
    }

    private static bool IsSingleAndValue(ValueCriteria criteria, string name, string value) =>
        criteria.Name == name
        && criteria.Type == ValueCriteriaType.AND
        && criteria.Values.Count == 1
        && criteria.Values[0] == value;
}
