using System.Net;
using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;

namespace Gaffer.Api.Tests.Controllers;

public class PlayersControllerShould
{
    private HttpClient _client;
    private UseCasesMock _useCasesMock;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> _searchPlayersQueryHandler;
    private ICommandHandler<DeletePlayerCommand> _deletePlayerCommandHandler;
    private ICommandHandler<UpdatePlayerCommand> _updatePlayerCommandHandler;
    protected PlayerController PlayerController;
    
    protected Guid PlayerId = TestDefaultValues.EmptyPlayerId;
    protected static readonly string FirstName = TestDefaultValues.DefaultFirstName;
    protected static readonly string LastName = TestDefaultValues.DefaultLastName;
    protected static readonly string SecondLastName = TestDefaultValues.DefaultSecondLastName;
    protected readonly DateTime BirthDate = TestDefaultValues.DefaultBirthDate;
    protected const Gender Gender = TestDefaultValues.DefaultGender;
    protected Player Player1;
    protected Player Player2;
    protected Player Player3;

    [SetUp]
    public void SetUp()
    {
        _useCasesMock = TestFixture.UseCasesMock;
        _createPlayerCommandHandler = _useCasesMock.CreatePlayerCommandHandler;
        _searchPlayersQueryHandler = _useCasesMock.SearchPlayersQueryHandler;
        _updatePlayerCommandHandler = _useCasesMock.UpdatePlayerCommandHandler;
        _deletePlayerCommandHandler = _useCasesMock.DeletePlayerCommandHandler;
        PlayerController = new PlayerController(_createPlayerCommandHandler, _searchPlayersQueryHandler, _deletePlayerCommandHandler, _updatePlayerCommandHandler);
    }

    [Test]
    public async Task GetPlayerOnDemand()
    {
        var playerId = Guid.NewGuid();
        var player = new SearchPlayerResult(playerId, string.Empty, string.Empty, string.Empty, new DateTime(), Gender.OTHER);
        _searchPlayersQueryHandler
            .Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns([player]);

        await PlayerController.GetBy(playerId);

        await _searchPlayersQueryHandler.Received(1)
            .Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>());
    }
    
    [Test]
    public async Task CreateAPlayerOnDemand()
    {
        _createPlayerCommandHandler
            .Handle(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CreatePlayerResponse(Guid.NewGuid()));

        var playerId = await PlayerController.Create(new CreatePlayerRequest(FirstName, LastName, null, BirthDate, Gender));
        
        await _createPlayerCommandHandler.Received(1)
            .Handle(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>());
    }
    
    [Test]
    public async Task UpdateAPlayerOnDemand()
    {
        var playerId = Guid.NewGuid();
        var player = new SearchPlayerResult(playerId, string.Empty, string.Empty, string.Empty, new DateTime(), Gender.OTHER);
        _searchPlayersQueryHandler
            .Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns([player]);
        
        await PlayerController.Update(playerId, new UpdatePlayerRequest());
        
        await _updatePlayerCommandHandler.Received(1)
            .Handle(Arg.Any<UpdatePlayerCommand>(), Arg.Any<CancellationToken>());
    }
    
    [Test]
    public async Task DeleteAPlayerOnCommand()
    {
        var playerId = Guid.NewGuid();
        var player = new SearchPlayerResult(playerId, string.Empty, string.Empty, string.Empty, new DateTime(), Gender.OTHER);
        _searchPlayersQueryHandler
            .Handle(Arg.Any<SearchPlayersQuery>(), Arg.Any<CancellationToken>())
            .Returns([player]);
        
        await PlayerController.Delete(new DeletePlayerRequest(playerId));
        
        await _deletePlayerCommandHandler.Received(1)
            .Handle(Arg.Any<DeletePlayerCommand>(), Arg.Any<CancellationToken>());
    }
    
}