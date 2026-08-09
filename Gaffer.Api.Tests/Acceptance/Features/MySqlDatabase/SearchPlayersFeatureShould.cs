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

namespace Gaffer.Api.Tests.Acceptance.Features.MySqlDatabase;

public class SearchPlayersFeatureShould
{
    private IDatabaseConnectionProvider _connectionProvider;
    private IPlayerRepository _dummyPlayerRepository;
    private IQueryHandler<SearchPlayerByIdQuery, SearchPlayerByIdResponse> _getPlayerByIdQueryHandler;
    private PlayerController _playerController;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private IQueryHandler<SearchPlayersQuery,List<SearchPlayerResult>> _searchPlayersQueryHandler;

    private Guid _playerId = TestDefaultValues.EmptyPlayerId;
    private static readonly string FirstName = TestDefaultValues.DefaultFirstName;
    private static readonly string LastName = TestDefaultValues.DefaultLastName;
    private static readonly string SecondLastName = TestDefaultValues.DefaultSecondLastName;
    private readonly DateTime _birthDate = TestDefaultValues.DefaultBirthDate;
    private Player player1;
    private Player player2;
    private Player player3;
    private const Gender Gender = TestDefaultValues.DefaultGender;

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
        
        player1 = new Player(Guid.NewGuid(), "Javier", "Martel", "De La Guardia", Gender.MALE, _birthDate);
        player2 = new Player(Guid.NewGuid(), "Paul", "Gascoigne", null, Gender.MALE, _birthDate);
        player3 = new Player(Guid.NewGuid(), "Michael", "Ballack", null, Gender.MALE, _birthDate);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _connectionProvider.ClearAsync([TestDefaultValues.PlayersTableName]);
    }

    [Test]
    public async Task GetAllPlayersWhileSearchingWithoutFilters() {
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