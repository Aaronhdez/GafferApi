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

namespace Gaffer.Api.Tests.Acceptance.Features;

public class SearchPlayerFeatureShould : PlayerFeatureShould
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
}