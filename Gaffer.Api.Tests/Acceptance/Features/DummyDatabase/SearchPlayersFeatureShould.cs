using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Acceptance.Features.DummyDatabase;

public class SearchPlayersFeatureShould
{
    private IDatabaseConnectionProvider _connectionProvider;
    private IPlayerRepository _dummyPlayerRepository;
    private IQueryHandler<SearchPlayerByIdQuery, SearchPlayerByIdResponse> _getPlayerByIdQueryHandler;
    private PlayerController _playerController;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private IQueryHandler<SearchPlayersQuery,List<SearchPlayerResult>> _searchPlayersQueryHandler;

    private Guid _playerId = Guid.Parse("00000000-0000-0000-0000-000000000000");
    private const string FirstName = "John";
    private const string LastName = "Doe";
    private const string SecondLastName = "Doe";
    private readonly DateTime _birthDate = new(1990, 01, 01);
    private const Gender Gender = Shared.Gender.MALE;

    [SetUp]
    public void Setup()
    {
        var connectionString = TestDefaultValues.TestConnectionString;
        _connectionProvider = new SqlConnectionProvider(connectionString);
        _dummyPlayerRepository = new SqlPlayerRepository(_connectionProvider);
        _getPlayerByIdQueryHandler = new SearchPlayerByIdQueryHandler(_dummyPlayerRepository);
        _createPlayerCommandHandler = new CreatePlayerCommandHandler(_dummyPlayerRepository);
        _searchPlayersQueryHandler = new SearchPlayersQueryHandler(_dummyPlayerRepository);
        _playerController = new PlayerController(_getPlayerByIdQueryHandler, _createPlayerCommandHandler, _searchPlayersQueryHandler);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _connectionProvider.ClearAsync(["Players"]);
    }

    [Test]
    public async Task GetAllPlayersWhileSearchingWithoutFilters() {
        var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
        var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
        var player3 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
        await InsertAPlayerInDb(player1);
        await InsertAPlayerInDb(player2);
        await InsertAPlayerInDb(player3);

        var response = await _playerController.Search(new SearchPlayersRequest());

        response.Count.Should().Be(3);
        response.Should().Contain(x => x.Id == player1.Id);
        response.Should().Contain(x => x.Id == player2.Id);
        response.Should().Contain(x => x.Id == player3.Id);
    }

    [Test]
    public async Task GetAllPlayersWhileSearchingWithIdFilter() {
        var player1 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
        var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
        var player3 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
        await InsertAPlayerInDb(player1);
        await InsertAPlayerInDb(player2);
        await InsertAPlayerInDb(player3);
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
        var player1 = new Player(Guid.NewGuid(), "Other", LastName, SecondLastName, Gender, _birthDate);
        var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
        var player3 = new Player(Guid.NewGuid(), "Other2", LastName, SecondLastName, Gender, _birthDate);
        await InsertAPlayerInDb(player1);
        await InsertAPlayerInDb(player2);
        await InsertAPlayerInDb(player3);
        var namePlayerFilter = new SearchPlayersFilter {
            Name = "FirstName",
            Value = $"{player2.FirstName}"
        };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
        searchPlayersRequest.Filters.Add(namePlayerFilter);
        
        var response = await _playerController.Search(searchPlayersRequest);

        response.Count.Should().Be(1);
        response.Should().Contain(x => x.Id == player2.Id);
        response.Should().Contain(x => x.FirstName == player2.FirstName);
    }
    
    [Test]
    public async Task GetAllPlayersWhileSearchingWithSecondNameFilter() {
        var player1 = new Player(Guid.NewGuid(), FirstName, "Other", SecondLastName, Gender, _birthDate);
        var player2 = new Player(Guid.NewGuid(), FirstName, LastName, SecondLastName, Gender, _birthDate);
        var player3 = new Player(Guid.NewGuid(), FirstName, "Other2", SecondLastName, Gender, _birthDate);
        await InsertAPlayerInDb(player1);
        await InsertAPlayerInDb(player2);
        await InsertAPlayerInDb(player3);
        var namePlayerFilter = new SearchPlayersFilter {
            Name = "LastName",
            Value = $"{player2.LastName}"
        };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
        searchPlayersRequest.Filters.Add(namePlayerFilter);
        
        var response = await _playerController.Search(searchPlayersRequest);

        response.Count.Should().Be(1);
        response.Should().Contain(x => x.Id == player2.Id);
        response.Should().Contain(x => x.FirstName == player2.FirstName);
    }
    
    private async Task InsertAPlayerInDb(Player player)
    {
        var query = "INSERT INTO `Players` (Id, FirstName, LastName, SecondLastName, Gender, BirthDate) " +
                    "VALUES (@Id,  @FirstName, @LastName, @SecondLastName, @Gender, @BirthDate);";
        await _connectionProvider.ExecuteAsync(query, new
        {
            player.Id,
            player.FirstName,
            player.LastName,
            player.SecondLastName,
            Gender = player.Gender.ToString(),
            BirthDate = player.Birthdate
        });
    }
}