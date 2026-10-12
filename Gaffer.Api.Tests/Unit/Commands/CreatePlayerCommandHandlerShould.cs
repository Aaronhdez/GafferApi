using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Commands;

public class CreatePlayerCommandHandlerShould {
    private IPlayerRepository repository;
    private CreatePlayerCommandHandler handler;

    [SetUp]
    public void Setup() {
        repository = Substitute.For<IPlayerRepository>();
        handler = new CreatePlayerCommandHandler(repository);
    }

    [Test]
    public async Task CreateAPlayerWhileBeingExecuted() {
        var createPlayerCommand = new CreatePlayerCommand(
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            TestDefaultValues.DefaultBirthDate,
            TestDefaultValues.DefaultGender);

        await handler.Handle(createPlayerCommand, CancellationToken.None);

        await repository.Received(1).Save(Arg.Is<Player>(p =>
            p != null && p.FirstName == TestDefaultValues.DefaultFirstName
            && p.LastName == TestDefaultValues.DefaultLastName
            && p.SecondLastName == TestDefaultValues.DefaultSecondLastName
            && p.Birthdate == TestDefaultValues.DefaultBirthDate
            && p.Gender == TestDefaultValues.DefaultGender
        ), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReturnCreatedPlayerResponseWithGeneratedId() {
        var command = new CreatePlayerCommand(
            "Jane",
            "Smith",
            null,
            new DateTime(1995, 4, 12),
            Gender.FEMALE);
        Player? savedPlayer = null;

        repository.When(x => x.Save(Arg.Any<Player>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => savedPlayer = callInfo.Arg<Player>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        savedPlayer.Should().NotBeNull();
        result.Id.Should().Be(savedPlayer!.Id);
        await repository.Received(1).Save(Arg.Is<Player>(p =>
            p != null && p.Id == result.Id
            && p.FirstName == command.FirstName
            && p.LastName == command.LastName
            && p.SecondLastName == command.SecondLastName
            && p.Birthdate == command.Birthdate
            && p.Gender == command.Gender
        ), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RejectAnInvalidPlayerWithEveryInvalidFieldAndNotSaveIt() {
        var command = new CreatePlayerCommand("", new string('l', 101), "", DateTime.UtcNow.Date.AddDays(1), (Gender)99);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var failure = (await result.Should().ThrowAsync<ValidationFailedException>()).Which;
        failure.Title.Should().Be("Cannot create player");
        failure.ErrorTranslationKey.Should().Be(ValidationErrorKeys.PlayersCreate);
        failure.Errors.Keys.Should().BeEquivalentTo("FirstName", "LastName", "SecondLastName", "BirthDate", "Gender");
        await repository.DidNotReceive().Save(Arg.Any<Player>(), Arg.Any<CancellationToken>());
    }
}
