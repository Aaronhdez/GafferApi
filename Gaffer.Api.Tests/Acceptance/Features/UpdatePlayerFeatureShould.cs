using AwesomeAssertions;
using Gaffer.Api.Controllers;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Tests.Acceptance.Features;

public class UpdatePlayerFeatureShould : PlayerFeatureShould
{
    [Test]
    public async Task ThrowExceptionIfPlayerIsNotFound()
    {
        var result = async () => await PlayerController.Update(Guid.NewGuid(), new UpdatePlayerRequest());

        await result.Should().ThrowAsync<PlayerNotFoundException>();
    }
}