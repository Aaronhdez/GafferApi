using System.Net;
using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using NSubstitute;

namespace Gaffer.Api.Tests.Controllers;

public class PlayersControllerShould
{
    private HttpClient _client;
    private UseCasesMock _useCasesMock;
    private ICommandHandler<DeletePlayerCommand> _deletePlayerQueryHandler;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayersQueryHandler;
    private ICommandHandler<UpdatePlayerCommand> _updatePlayerQueryHandler;
    private IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> _searchPlayerQueryHandler;

    [SetUp]
    public void SetUp()
    {
        _client = TestFixture.Client;
        _useCasesMock = TestFixture.UseCasesMock;
        _createPlayersQueryHandler = _useCasesMock.CreatePlayerCommandHandler;
        _searchPlayerQueryHandler = _useCasesMock.SearchPlayersQueryHandler;
        _updatePlayerQueryHandler = _useCasesMock.UpdatePlayerCommandHandler;
        _deletePlayerQueryHandler = _useCasesMock.DeletePlayerCommandHandler;
    }

    [Test]
    public async Task GetPlayers()
    {
        var playerId = Guid.NewGuid();
        var uri = new Uri($"http://localhost:50000/api/v1.0/players/{playerId}");
        var player = new SearchPlayerResult(playerId, string.Empty, string.Empty, string.Empty, new DateTime(), Gender.OTHER);
        _searchPlayerQueryHandler
            .Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns([player]);

        var response = await _client.GetAsync(uri);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await _searchPlayerQueryHandler.Received(1)
            .Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>());
    }
}