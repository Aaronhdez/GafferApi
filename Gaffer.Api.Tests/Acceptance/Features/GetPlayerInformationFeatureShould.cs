using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using MySqlConnector;
using NSubstitute;

namespace Gaffer.Api.Tests.Acceptance.UseCases;

public class GetPlayerInformationFeatureShould
{
    private IDatabaseConnectionProvider _dbConnectionProvider;
    private IPlayerRepository _playerRepository;
    private IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse> _getPlayerByIdQueryHandler;
    private PlayerController _playerController;
    private CreatePlayerCommandHandler _createPlayerCommandHandler;

    private Guid _playerId = Guid.Parse("00000000-0000-0000-0000-000000000000");
    private const string FirstName = "John";
    private const string LastName = "Doe";
    private const string SecondLastName = "Doe";
    private readonly DateTime _birthDate = new(1990, 01, 01);
    private const Gender Gender = Shared.Gender.MALE;
    [SetUp]
    public void Setup()
    {
        _dbConnectionProvider = new DummyConnectionProvider();
        var player = new Player(_playerId, FirstName, LastName, SecondLastName, _birthDate, Gender);
        _playerRepository = new DummyPlayerRepository(_dbConnectionProvider, new List<Player> {player});
        _getPlayerByIdQueryHandler = new GetPlayerByIdQueryHandler(_playerRepository);
        _createPlayerCommandHandler = new CreatePlayerCommandHandler(_playerRepository);
        _playerController = new PlayerController(_getPlayerByIdQueryHandler, _createPlayerCommandHandler);
    }

    [Test]
    public async Task GetAPlayerFromRepositoryByUsingItsId()
    {
        var response = await _playerController.GetBy(_playerId);

        response.Id.Should().Be(_playerId);
        response.FirstName.Should().Be(FirstName);
        response.LastName.Should().Be(LastName);
        response.BirthDate.Should().Be(_birthDate);
        response.Gender.Should().Be(Gender);
    }
}