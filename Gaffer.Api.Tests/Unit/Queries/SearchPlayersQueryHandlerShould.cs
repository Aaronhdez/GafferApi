using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
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
}