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

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IPlayerRepository>();
        _handler = new SearchPlayersQueryHandler(_repository);
    }

    [Test]
    public async Task ReturnEmptyInformationWhenNoPlayersFound()
    {
        var searchCriteria = new SearchPlayersQuery();
        var players = new List<Player>();
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(players);
        
        var result = await _handler.Handle(searchCriteria, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(0);
    }
    
    [Test]
    public async Task ReturnAllPlayersInformationWhenNoCriteriaIsSpecified()
    {
        var searchCriteria = new SearchPlayersQuery();
        var players = new List<Player>();
        var player1 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", new DateTime(2001,1,1), Gender.MALE);
        var player2 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", new DateTime(2001,1,1), Gender.MALE);
        var player3 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", new DateTime(2001,1,1), Gender.MALE);
        players.Add(player1);
        players.Add(player2);
        players.Add(player3);
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(players);
        
        var result = await _handler.Handle(searchCriteria, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(players.Count);
        result.Should().Contain(spr => spr.Id == player1.Id);
        result.Should().Contain(spr => spr.Id == player2.Id);
        result.Should().Contain(spr => spr.Id == player3.Id);
    }
    
    [Test]
    public async Task ReturnOnePlayerInformationWhenThereIsOnePlayersFound()
    {
        var searchCriteria = new SearchPlayersQuery();
        var players = new List<Player>();
        var player1 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", new DateTime(2001,1,1), Gender.MALE);
        players.Add(player1);
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(players);
        
        var result = await _handler.Handle(searchCriteria, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(1);
        result.Should().Contain(spr => spr.Id == player1.Id);
    }
    
    [Test]
    public async Task ReturnTwoPlayersInformationWhenThereAreTwoPlayersFound()
    {
        var searchCriteria = new SearchPlayersQuery();
        var players = new List<Player>();
        var player1 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", new DateTime(2001,1,1), Gender.MALE);
        var player2 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", new DateTime(2001,1,1), Gender.MALE);
        players.Add(player1);
        players.Add(player2);
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(players);
        
        var result = await _handler.Handle(searchCriteria, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(2);
        result.Should().Contain(spr => spr.Id == player1.Id);
        result.Should().Contain(spr => spr.Id == player2.Id);
    }
    
    [Test]
    public async Task ReturnThreePlayersInformationWhenThereAreThreePlayersFound()
    {
        var searchCriteria = new SearchPlayersQuery();
        var players = new List<Player>();
        var player1 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", new DateTime(2001,1,1), Gender.MALE);
        var player2 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", new DateTime(2001,1,1), Gender.MALE);
        var player3 = new Player(Guid.NewGuid(), "John", "Doe", "Smith", new DateTime(2001,1,1), Gender.MALE);
        players.Add(player1);
        players.Add(player2);
        players.Add(player3);
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns(players);
        
        var result = await _handler.Handle(searchCriteria, CancellationToken.None);

        await _repository.Received(1).Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>());
        result.Count.Should().Be(3);
        result.Should().Contain(spr => spr.Id == player1.Id);
        result.Should().Contain(spr => spr.Id == player2.Id);
        result.Should().Contain(spr => spr.Id == player3.Id);
    }
}