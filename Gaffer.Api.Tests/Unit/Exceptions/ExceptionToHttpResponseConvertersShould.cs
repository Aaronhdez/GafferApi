using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
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
    public void ConvertANotPermittedFacilitySearchFilterIntoTheSameBadRequestAsPlayers()
    {
        var converter = ExceptionToHttpResponseConverters.PlayerConverters[typeof(FacilitySearchFilterNotPermittedException)];

        var problemDetails = converter(new FacilitySearchFilterNotPermittedException("Street"));

        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Error.Search.FilterNotPermitted");
        problemDetails.Detail.Should().Be("FilterNotPermitted");
        problemDetails.Extensions["ErrorTranslationKey"].Should().Be("Error.Search.FilterNotPermitted");
    }

    [Test]
    public void NotMapGenericArgumentExceptions()
    {
        ExceptionToHttpResponseConverters.PlayerConverters.Should().NotContainKey(typeof(ArgumentException));
    }

    private const string InternalServerErrorReason = "Internal Server Error";

    private sealed record Expectation(
        Func<Guid, Exception> Create,
        int Status,
        string? Title,
        Func<Guid, string?> Detail,
        string? ErrorTranslationKey);

    // One expectation per converter. A converter added without an entry here fails CoverEveryConverter.
    private static readonly IReadOnlyDictionary<Type, Expectation> Expectations = new Dictionary<Type, Expectation>
    {
        [typeof(CannotDeletePlayerNotFoundException)] = new(id => new CannotDeletePlayerNotFoundException("not found", id),
            404, "Cannot delete player", id => $"Player {id} not found", "Error.Players.Delete.PlayerNotFound"),
        [typeof(CannotDeletePlayerException)] = new(id => new CannotDeletePlayerException("boom", id),
            400, "Cannot delete player", id => $"Player {id} cannot be deleted", "Error.Players.Delete.Internal Server Error"),
        [typeof(CannotUpdatePlayerNotFoundException)] = new(id => new CannotUpdatePlayerNotFoundException("not found", id),
            404, "Cannot update player", id => $"Player {id} not found", "Error.Players.Update.PlayerNotFound"),
        [typeof(CannotUpdatePlayerException)] = new(id => new CannotUpdatePlayerException("boom", id),
            400, "Cannot update player", id => $"Player {id} cannot be updated", "Error.Players.Update.Internal Server Error"),
        [typeof(PlayerNotFoundException)] = new(id => new PlayerNotFoundException("Player not found", id),
            404, null, _ => null, null),
        [typeof(PlayerSearchFilterNotPermittedException)] = new(_ => new PlayerSearchFilterNotPermittedException("Gender"),
            400, "Error.Search.FilterNotPermitted", _ => "FilterNotPermitted", "Error.Search.FilterNotPermitted"),
        [typeof(FacilitySearchFilterNotPermittedException)] = new(_ => new FacilitySearchFilterNotPermittedException("Street"),
            400, "Error.Search.FilterNotPermitted", _ => "FilterNotPermitted", "Error.Search.FilterNotPermitted"),
        [typeof(FacilityValidationException)] = new(_ => new FacilityValidationException("Stands cannot exceed 4"),
            400, "Cannot create facility", _ => "Stands cannot exceed 4", "Error.Facilities.Create.Validation"),
        [typeof(FacilityNotFoundException)] = new(id => new FacilityNotFoundException("Facility not found", id),
            404, "Facility not found", _ => "Facility not found", "Error.Facilities.NotFound"),
        [typeof(CannotDeleteFacilityNotFoundException)] = new(id => new CannotDeleteFacilityNotFoundException("not found", id),
            404, "Cannot delete facility", id => $"Facility {id} not found", "Error.Facilities.Delete.FacilityNotFound"),
        [typeof(CannotDeleteFacilityException)] = new(id => new CannotDeleteFacilityException("boom", id),
            400, "Cannot delete facility", id => $"Facility {id} cannot be deleted", "Error.Facilities.Delete.Internal Server Error"),
        [typeof(CannotUpdateFacilityNotFoundException)] = new(id => new CannotUpdateFacilityNotFoundException("not found", id),
            404, "Cannot update facility", id => $"Facility {id} not found", "Error.Facilities.Update.FacilityNotFound"),
        [typeof(CannotUpdateFacilityException)] = new(id => new CannotUpdateFacilityException("boom", id),
            400, "Cannot update facility", id => $"Facility {id} cannot be updated", "Error.Facilities.Update.Internal Server Error"),
        [typeof(CannotUpdateFacilityDuplicatedStandException)] = new(id => new CannotUpdateFacilityDuplicatedStandException(id, RepeatedStandId),
            400, "Cannot update facility", id => $"Stand {RepeatedStandId} is repeated in the update of facility {id}", "Error.Facilities.Update.DuplicatedStand"),
        [typeof(FacilityStandsFacilityNotFoundException)] = new(id => new FacilityStandsFacilityNotFoundException("Facility not found", id),
            404, "Facility not found", _ => "Facility not found", "Error.Facilities.Stands.FacilityNotFound"),
        [typeof(FacilityStandNotFoundException)] = new(id => new FacilityStandNotFoundException(id, RepeatedStandId),
            404, "Stand not found", id => $"Stand {RepeatedStandId} not found in facility {id}", "Error.Facilities.Stands.StandNotFound"),
        [typeof(ValidationFailedException)] = new(_ => new ValidationFailedException("Cannot create player", ValidationErrorKeys.PlayersCreate,
                new Dictionary<string, string[]> { ["FirstName"] = ["FirstName is required."] }),
            400, "Cannot create player", _ => ValidationFailedException.InvalidFieldsDetail, "Error.Players.Create.Validation"),
        [typeof(FacilityMaxStandsExceededException)] = new(id => new FacilityMaxStandsExceededException(id),
            400, "Cannot create stand", id => $"Facility {id} already has the maximum number of stands", "Error.Facilities.Stands.Create.MaxStandsExceeded"),
    };

    private static readonly Guid RepeatedStandId = Guid.Parse("7d1f5c0e-3b2a-4c7e-9f1d-2a6b8c4e0f13");

    private static IEnumerable<Type> ConvertedExceptionTypes() => Expectations.Keys;

    [Test]
    public void CoverEveryConverter()
    {
        ExceptionToHttpResponseConverters.PlayerConverters.Keys.Should().BeEquivalentTo(Expectations.Keys);
    }

    [TestCaseSource(nameof(ConvertedExceptionTypes))]
    public void ConvertEachExceptionIntoItsExpectedResponse(Type exceptionType)
    {
        var expectation = Expectations[exceptionType];
        var id = Guid.NewGuid();
        var exception = expectation.Create(id);
        exception.Should().BeOfType(exceptionType);

        var problemDetails = ExceptionToHttpResponseConverters.PlayerConverters[exceptionType](exception);

        problemDetails.Status.Should().Be(expectation.Status);
        problemDetails.Title.Should().Be(expectation.Title);
        problemDetails.Detail.Should().Be(expectation.Detail(id));
        if (expectation.ErrorTranslationKey is null)
        {
            problemDetails.Extensions.Should().NotContainKey("ErrorTranslationKey");
        }
        else
        {
            problemDetails.Extensions["ErrorTranslationKey"].Should().Be(expectation.ErrorTranslationKey);
        }
    }

    [TestCaseSource(nameof(ConvertedExceptionTypes))]
    public void NeverProduceATranslationKeyEndingWithADot(Type exceptionType)
    {
        var exception = Expectations[exceptionType].Create(Guid.NewGuid());

        var problemDetails = ExceptionToHttpResponseConverters.PlayerConverters[exceptionType](exception);

        if (problemDetails.Extensions.TryGetValue("ErrorTranslationKey", out var key))
        {
            key.Should().BeOfType<string>().Which.Should().NotEndWith(".");
        }
    }

    [Test]
    public void AssignTheInternalServerErrorReasonToTheGenericDeleteAndUpdateExceptions()
    {
        var id = Guid.NewGuid();

        new CannotDeletePlayerException("boom", id).Reason.Should().Be(InternalServerErrorReason);
        new CannotUpdatePlayerException("boom", id).Reason.Should().Be(InternalServerErrorReason);
        new CannotDeleteFacilityException("boom", id).Reason.Should().Be(InternalServerErrorReason);
        new CannotUpdateFacilityException("boom", id).Reason.Should().Be(InternalServerErrorReason);
    }

    [TestCase("Cannot create player", "Error.Players.Create.Validation")]
    [TestCase("Cannot update player", "Error.Players.Update.Validation")]
    public void ConvertAValidationFailureIntoABadRequestWithTheOperationKeyAndEveryFieldError(string title, string key)
    {
        var errors = new Dictionary<string, string[]>
        {
            ["FirstName"] = ["FirstName must be at most 100 characters."],
            ["Gender"] = ["Gender must be one of the defined values."],
        };
        var converter = ExceptionToHttpResponseConverters.PlayerConverters[typeof(ValidationFailedException)];

        var problemDetails = converter(new ValidationFailedException(title, key, errors));

        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be(title);
        problemDetails.Extensions["ErrorTranslationKey"].Should().Be(key);
        problemDetails.Extensions["Errors"].Should().BeEquivalentTo(errors);
    }

    [Test]
    public void NameThePlayerValidationKeysByOperation()
    {
        ValidationErrorKeys.PlayersCreate.Should().Be("Error.Players.Create.Validation");
        ValidationErrorKeys.PlayersUpdate.Should().Be("Error.Players.Update.Validation");
    }
}
