using AwesomeAssertions;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;

namespace Gaffer.Api.Tests.Acceptance.Features_Refactor;

public class SearchPlayersFeatureShould : PlayerFeatureShould
{
    [Test]
    public async Task GetAPlayerFromRepositoryByUsingItsId()
    {
        var player = new Player(PlayerId, FirstName, LastName, SecondLastName, Gender, BirthDate);
        await InsertAPlayerInDb(player);

        var response = await PlayerController.GetBy(PlayerId);

        response.Id.Should().Be(PlayerId);
        response.FirstName.Should().Be(FirstName);
        response.LastName.Should().Be(LastName);
        response.BirthDate.Should().Be(BirthDate);
        response.Gender.Should().Be(Gender);
    }
    
    [Test]
    public async Task GetAllPlayersWhileSearchingWithoutFilters() {
        await InsertAPlayerInDb(Player1);
        await InsertAPlayerInDb(Player2);
        await InsertAPlayerInDb(Player3);

        var response = await PlayerController.Search(new SearchPlayersRequest());

        response.Count.Should().Be(3);
        response.Should().Contain(x => x.Id == Player1.Id);
        response.Should().Contain(x => x.Id == Player2.Id);
        response.Should().Contain(x => x.Id == Player3.Id);
    }

    [Test]
    public async Task GetAllPlayersWhileSearchingWithIdFilter() {
        await InsertAPlayerInDb(Player1);
        await InsertAPlayerInDb(Player2);
        await InsertAPlayerInDb(Player3);
        var namePlayerFilter = new SearchPlayersFilter {
            Name = "Id",
            Value = $"{Player2.Id}"
        };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
        searchPlayersRequest.Filters.Add(namePlayerFilter);
        
        var response = await PlayerController.Search(searchPlayersRequest);

        response.Count.Should().Be(1);
        response.Should().Contain(x => x.Id == Player2.Id);
    }
    
    [Test]
    public async Task GetAllPlayersWhileSearchingWithFirstNameFilter() {
        await InsertAPlayerInDb(Player1);
        await InsertAPlayerInDb(Player2);
        await InsertAPlayerInDb(Player3);
        var namePlayerFilter = new SearchPlayersFilter {
            Name = "FirstName",
            Value = $"{Player2.FirstName}"
        };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
        searchPlayersRequest.Filters.Add(namePlayerFilter);
        
        var response = await PlayerController.Search(searchPlayersRequest);

        response.Count.Should().Be(1);
        response.Should().Contain(x => x.Id == Player2.Id);
        response.Should().Contain(x => x.FirstName == Player2.FirstName);
    }
    
    [Test]
    public async Task GetAllPlayersWhileSearchingWithSecondNameFilter() {
        await InsertAPlayerInDb(Player1);
        await InsertAPlayerInDb(Player2);
        await InsertAPlayerInDb(Player3);
        var namePlayerFilter = new SearchPlayersFilter {
            Name = "LastName",
            Value = $"{Player2.LastName}"
        };
        var searchPlayersRequest = new SearchPlayersRequest();
        searchPlayersRequest.Filters = new List<SearchPlayersFilter>();
        searchPlayersRequest.Filters.Add(namePlayerFilter);
        
        var response = await PlayerController.Search(searchPlayersRequest);

        response.Count.Should().Be(1);
        response.Should().Contain(x => x.Id == Player2.Id);
        response.Should().Contain(x => x.FirstName == Player2.FirstName);
    }
}