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

public class GetPlayerInformationFeatureShould
{
    private IDatabaseConnectionProvider _connectionProvider;
    private IPlayerRepository _playerRepository;
    private IQueryHandler<SearchPlayerByIdQuery, SearchPlayerByIdResponse> _getPlayerByIdQueryHandler;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    private IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> _searchPlayersQueryHandler;
    private PlayerController _playerController;

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
        var connectionString = TestDefaultValues.TestConnectionString;
        _connectionProvider = new SqlConnectionProvider(connectionString);
        _playerRepository = new SqlPlayerRepository(_connectionProvider);
        _getPlayerByIdQueryHandler = new SearchPlayerByIdQueryHandler(_playerRepository);
        _createPlayerCommandHandler = new CreatePlayerCommandHandler(_playerRepository);
        _searchPlayersQueryHandler = new SearchPlayersQueryHandler(_playerRepository);
        _playerController = new PlayerController(_getPlayerByIdQueryHandler, _createPlayerCommandHandler, _searchPlayersQueryHandler);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _connectionProvider.ClearAsync(["Players"]);
    }

    [Test]
    public async Task GetAPlayerFromRepositoryByUsingItsId()
    {
        var player = new Player(_playerId, FirstName, LastName, SecondLastName, Gender, _birthDate);
        await InsertAPlayerInDb(player);

        var response = await _playerController.GetBy(_playerId);

        response.Id.Should().Be(_playerId);
        response.FirstName.Should().Be(FirstName);
        response.LastName.Should().Be(LastName);
        response.BirthDate.Should().Be(_birthDate);
        response.Gender.Should().Be(Gender);
    }

    [Test]
    public void ThrowExceptionIfPlayerDoesNotExist()
    {
        var result = async () => await _playerController.GetBy(_playerId);

        result.Should().ThrowAsync<PlayerNotFoundException>();
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