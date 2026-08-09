using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.ConnectionProviders;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;

namespace Gaffer.Api.Tests.Acceptance.Features;

public class CreatePlayerFeatureShould : PlayerFeatureShould
{
    [Test]
    public async Task CreateAPlayerOnDemand()
    {
        var createPlayerRequest = new CreatePlayerRequest(FirstName, LastName, SecondLastName, BirthDate, Gender);

        var playerId = await PlayerController.Create(createPlayerRequest);
        
        var response = await PlayerController.GetBy(playerId);
        response.Id.Should().Be(playerId);
        response.FirstName.Should().Be(FirstName);
        response.LastName.Should().Be(LastName);
        response.BirthDate.Should().Be(BirthDate);
        response.Gender.Should().Be(Gender);
    }
}