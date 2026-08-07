using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.Unit.Queries;

namespace Gaffer.Api.Tests.Acceptance.Features;

public class SearchPlayersFeatureShould
{
    private IDatabaseConnectionProvider _dummyConnectionProvider;
    private IPlayerRepository _dummyPlayerRepository;
    private IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse> _getPlayerByIdQueryHandler;
    private PlayerController _playerController;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private IQueryHandler<SearchPlayersQuery,List<SearchPlayerResult>> _searchPlayersQueryHandler;

    private Guid _playerId = Guid.Parse("00000000-0000-0000-0000-000000000000");
    private const string FirstName = "John";
    private const string LastName = "Doe";
    private const string SecondLastName = "Doe";
    private readonly DateTime _birthDate = new(1990, 01, 01);
    private List<Player> _playersList;
    private const Gender Gender = Shared.Gender.MALE;

    [SetUp]
    public void Setup()
    {
        _dummyConnectionProvider = new DummyConnectionProvider();
        _playersList = new List<Player>();
        _dummyPlayerRepository = new DummyPlayerRepository(_dummyConnectionProvider, _playersList);
        _getPlayerByIdQueryHandler = new GetPlayerByIdQueryHandler(_dummyPlayerRepository);
        _createPlayerCommandHandler = new CreatePlayerCommandHandler(_dummyPlayerRepository);
        _searchPlayersQueryHandler = new SearchPlayersQueryHandler(_dummyPlayerRepository);
        _playerController = new PlayerController(_getPlayerByIdQueryHandler, _createPlayerCommandHandler, _searchPlayersQueryHandler);
    }

    [Test]
    public async Task GetAllPlayersWhileSearchingWithoutFilters() {
        var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, _birthDate, Gender);
        var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, _birthDate, Gender);
        var player3 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, _birthDate, Gender);
        _playersList.Add(player1);
        _playersList.Add(player2);
        _playersList.Add(player3);

        var response = await _playerController.Search(new SearchPlayersRequest());

        response.Count.Should().Be(3);
        response.Should().Contain(x => x.Id == player1.Id);
        response.Should().Contain(x => x.Id == player2.Id);
        response.Should().Contain(x => x.Id == player3.Id);
    }

    [Test]
    public async Task GetAllPlayersWhileSearchingWithIdFilter() {
        var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, _birthDate, Gender);
        var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, _birthDate, Gender);
        var player3 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, _birthDate, Gender);
        _playersList.Add(player1);
        _playersList.Add(player2);
        _playersList.Add(player3);
        var namePlayerFilter = new SearchPlayersFilter {
            Name = "Id",
            Value = $"{player2.Id}"
        };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
        searchPlayersRequest.Filters.Add(namePlayerFilter);
        
        var response = await _playerController.Search(searchPlayersRequest);

        response.Count.Should().Be(1);
        response.Should().Contain(x => x.Id == player2.Id);
    }
    
    [Test]
    public async Task GetAllPlayersWhileSearchingWithFirstNameFilter() {
        var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, _birthDate, Gender);
        var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, _birthDate, Gender);
        var player3 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, _birthDate, Gender);
        _playersList.Add(player1);
        _playersList.Add(player2);
        _playersList.Add(player3);
        var namePlayerFilter = new SearchPlayersFilter {
            Name = "Name",
            Value = $"{player2.FirstName}"
        };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
        searchPlayersRequest.Filters.Add(namePlayerFilter);
        
        var response = await _playerController.Search(searchPlayersRequest);

        response.Count.Should().Be(1);
        response.Should().Contain(x => x.Id == player2.Id);
    }
}