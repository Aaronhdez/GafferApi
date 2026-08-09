using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Queries;

public class SearchPlayersQueryHandlerShould
{
    private IPlayerRepository _repository;
    private SearchPlayersQueryHandler _handler;
    private List<Player> _players;
    private readonly SearchPlayersQuery _emptySearchCriteria = new();

    [SetUp]
    public void Setup()
    {
        _players = new List<Player>();
        _repository = Substitute.For<IPlayerRepository>();
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(_players);
        _handler = new SearchPlayersQueryHandler(_repository);
    }

    [Test]
    public async Task ReturnEmptyInformationWhenNoPlayersFound()
    {
        var result = await _handler.Handle(_emptySearchCriteria, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(0);
    }
    
    [Test]
    public async Task ReturnAllPlayersInformationWhenNoCriteriaIsSpecified()
    {
        var player1 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001,1,1));
        var player2 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001,1,1));
        var player3 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001,1,1));
        _players.Add(player1);
        _players.Add(player2);
        _players.Add(player3);
        
        var result = await _handler.Handle(_emptySearchCriteria, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(_players.Count);
        result.Should().Contain(spr => spr.Id == player1.Id);
        result.Should().Contain(spr => spr.Id == player2.Id);
        result.Should().Contain(spr => spr.Id == player3.Id);
    }

    [Test]
    public async Task ReturnAPlayerInformationWhenIdCriteriaIsSpecified()
    {
        var player1 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001, 1, 1));
        var player2 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001, 1, 1));
        var player3 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", Gender.MALE, new DateTime(2001, 1, 1));
        _players.Add(player1);
        _players.Add(player2);
        _players.Add(player3);
        var filteredPlayers = new List<Player>();
        filteredPlayers.Add(player1);
        var query = new SearchPlayersQuery();
        query.Filters.Add(new QueryFilter { Name = "Id", Value = $"{player1.Id}" });
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(filteredPlayers);
        
        var result = await _handler.Handle(query, CancellationToken.None);
        

        await _repository.Received(1).Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(1);
        result.Should().Contain(spr => spr.Id == player1.Id);
    }
}