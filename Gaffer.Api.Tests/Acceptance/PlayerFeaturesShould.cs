using AwesomeAssertions;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Acceptance;

public class PlayerFeaturesShould
{
    private GafferApiClientTest _client;
    
    private Guid PlayerId = TestDefaultValues.EmptyPlayerId;
    private static readonly string FirstName = TestDefaultValues.DefaultFirstName;
    private static readonly string LastName = TestDefaultValues.DefaultLastName;
    private static readonly string SecondLastName = TestDefaultValues.DefaultSecondLastName;
    private readonly DateTime BirthDate = TestDefaultValues.DefaultBirthDate;
    private const Gender Gender = TestDefaultValues.DefaultGender;
    private Player Player1;
    private Player Player2;
    private Player Player3;

    [SetUp]
    public async Task SetUp()
    {
        _client = (GafferApiClientTest) TestFixture.GafferApiClientTest;
        Player1 = new Player(Guid.NewGuid(), "Javier", "Martel", "De La Guardia", Gender.MALE, BirthDate);
        Player2 = new Player(Guid.NewGuid(), "Paul", "Gascoigne", null, Gender.MALE, BirthDate);
        Player3 = new Player(Guid.NewGuid(), "Michael", "Ballack", null, Gender.MALE, BirthDate);
    }

    [Test]
    public async Task GetAPlayer()
    {
        var createPlayerRequest = new CreatePlayerRequest(Player1.FirstName, Player1.LastName, Player1.SecondLastName, Player1.Birthdate, Player1.Gender);
        var result = await _client.PostAsync<CreatePlayerRequest, Guid>(Uris.CreatePlayer(), createPlayerRequest);
        
        var response = await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(result.ToString()));

        response.Should().NotBeNull();
        response.Id.Should().Be(result);
        response.FirstName.Should().Be(Player1.FirstName);
        response.LastName.Should().Be(Player1.LastName);
        response.BirthDate.Should().Be(Player1.Birthdate);
        response.Gender.Should().Be(Player1.Gender);
    }
    
    [Test]
    public async Task DeleteAPlayer()
    {
        var createPlayerRequest = new CreatePlayerRequest(Player1.FirstName, Player1.LastName, Player1.SecondLastName, Player1.Birthdate, Player1.Gender);
        var result = await _client.PostAsync<CreatePlayerRequest, Guid>(Uris.CreatePlayer(), createPlayerRequest);
        var filter = new SearchPlayersFilter { Name = "Id", Value = $"{PlayerId}" };
        var request = new SearchPlayersRequest();
        request.Filters.Add(filter);

        await _client.DeleteAsync(Uris.DeletePlayer(result.ToString()));
        
        var response = async () => await _client.GetAsync<SearchPlayerResponse>(Uris.GetPlayer(result.ToString()));
        await response.Should().ThrowAsync<Exception>();
    }
    
//    [Test]
//    public async Task GetAPlayerFromRepositoryByUsingItsId()
//    {
//        var player = new Player(PlayerId, FirstName, LastName, SecondLastName, Gender, BirthDate);
//        await InsertAPlayerInDb(player);
//
//        var response = await PlayerController.GetBy(PlayerId);
//
//        response.Id.Should().Be(PlayerId);
//        response.FirstName.Should().Be(FirstName);
//        response.LastName.Should().Be(LastName);
//        response.BirthDate.Should().Be(BirthDate);
//        response.Gender.Should().Be(Gender);
//    }
//    
//    [Test]
//    public async Task GetAllPlayersWhileSearchingWithoutFilters() {
//        await InsertAPlayerInDb(Player1);
//        await InsertAPlayerInDb(Player2);
//        await InsertAPlayerInDb(Player3);
//
//        var response = await PlayerController.Search(new SearchPlayersRequest());
//
//        response.Count.Should().Be(3);
//        response.Should().Contain(x => x.Id == Player1.Id);
//        response.Should().Contain(x => x.Id == Player2.Id);
//        response.Should().Contain(x => x.Id == Player3.Id);
//    }
//
//    [Test]
//    public async Task GetAllPlayersWhileSearchingWithIdFilter() {
//        await InsertAPlayerInDb(Player1);
//        await InsertAPlayerInDb(Player2);
//        await InsertAPlayerInDb(Player3);
//        var namePlayerFilter = new SearchPlayersFilter {
//            Name = "Id",
//            Value = $"{Player2.Id}"
//        };
//        var searchPlayersRequest = new SearchPlayersRequest();
//        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
//        searchPlayersRequest.Filters.Add(namePlayerFilter);
//        
//        var response = await PlayerController.Search(searchPlayersRequest);
//
//        response.Count.Should().Be(1);
//        response.Should().Contain(x => x.Id == Player2.Id);
//    }
//    
//    [Test]
//    public async Task GetAllPlayersWhileSearchingWithFirstNameFilter() {
//        await InsertAPlayerInDb(Player1);
//        await InsertAPlayerInDb(Player2);
//        await InsertAPlayerInDb(Player3);
//        var namePlayerFilter = new SearchPlayersFilter {
//            Name = "FirstName",
//            Value = $"{Player2.FirstName}"
//        };
//        var searchPlayersRequest = new SearchPlayersRequest();
//        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
//        searchPlayersRequest.Filters.Add(namePlayerFilter);
//        
//        var response = await PlayerController.Search(searchPlayersRequest);
//
//        response.Count.Should().Be(1);
//        response.Should().Contain(x => x.Id == Player2.Id);
//        response.Should().Contain(x => x.FirstName == Player2.FirstName);
//    }
//    
//    [Test]
//    public async Task GetAllPlayersWhileSearchingWithSecondNameFilter() {
//        await InsertAPlayerInDb(Player1);
//        await InsertAPlayerInDb(Player2);
//        await InsertAPlayerInDb(Player3);
//        var namePlayerFilter = new SearchPlayersFilter {
//            Name = "LastName",
//            Value = $"{Player2.LastName}"
//        };
//        var searchPlayersRequest = new SearchPlayersRequest();
//        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
//        searchPlayersRequest.Filters.Add(namePlayerFilter);
//        
//        var response = await PlayerController.Search(searchPlayersRequest);
//
//        response.Count.Should().Be(1);
//        response.Should().Contain(x => x.Id == Player2.Id);
//        response.Should().Contain(x => x.FirstName == Player2.FirstName);
//    }
}