using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;

namespace Gaffer.Api.Tests.Unit.Exceptions;

public class ExceptionToHttpResponseConvertersShould
{
    [Test]
    public void ConvertANotPermittedPlayerSearchFilterIntoABadRequest()
    {
        var converter = ExceptionToHttpResponseConverters.PlayerConverters[typeof(PlayerSearchFilterNotPermittedException)];

        var problemDetails = converter(new PlayerSearchFilterNotPermittedException("Gender"));

        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Error.Search.FilterNotPermitted");
        problemDetails.Detail.Should().Be("FilterNotPermitted");
        problemDetails.Extensions["ErrorTranslationKey"].Should().Be("Error.Search.FilterNotPermitted");
    }

    [Test]
    public void ConvertAnUnknownPlayerIntoABareNotFound()
    {
        ExceptionToHttpResponseConverters.PlayerConverters.Should().ContainKey(typeof(PlayerNotFoundException));
        var converter = ExceptionToHttpResponseConverters.PlayerConverters[typeof(PlayerNotFoundException)];

        var problemDetails = converter(new PlayerNotFoundException("Player not found", Guid.NewGuid()));

        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().BeNull();
        problemDetails.Detail.Should().BeNull();
        problemDetails.Extensions.Should().NotContainKey("ErrorTranslationKey");
    }

    [Test]
    public void KeepTheTranslationKeysOfTheExistingPlayerNotFoundConversions()
    {
        var onDelete = ExceptionToHttpResponseConverters.PlayerConverters[typeof(CannotDeletePlayerNotFoundException)];
        var onUpdate = ExceptionToHttpResponseConverters.PlayerConverters[typeof(CannotUpdatePlayerNotFoundException)];

        onDelete(new CannotDeletePlayerNotFoundException("not found", Guid.NewGuid())).Extensions["ErrorTranslationKey"]
            .Should().Be("Error.Players.Delete.PlayerNotFound");
        onUpdate(new CannotUpdatePlayerNotFoundException("not found", Guid.NewGuid())).Extensions["ErrorTranslationKey"]
            .Should().Be("Error.Players.Update.PlayerNotFound");
    }

    [Test]
    public void NotMapGenericArgumentExceptionsSoFacilitySearchKeepsItsBehaviour()
    {
        ExceptionToHttpResponseConverters.PlayerConverters.Should().NotContainKey(typeof(ArgumentException));
    }
}
