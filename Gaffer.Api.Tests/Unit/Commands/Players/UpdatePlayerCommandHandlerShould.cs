using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Commands.Players;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Criteria;
using Gaffer.Api.Shared.Exceptions;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Commands.Players;

public class UpdatePlayerCommandHandlerShould {
    private IPlayerRepository repository;
    private UpdatePlayerCommandHandler handler;
    private readonly Guid playerId = Guid.NewGuid();
    private const string NewName = "NewName";
    private const string NewLastName = "ANewLastName";
    private const string NewSecondLastName = "SecondLastName";
    private static readonly DateTime OriginalBirthDate = new(1999, 1, 1);
    private const Gender OriginalGender = Gender.MALE;
    private static readonly DateTime NewBirthDate = new(2000, 1, 3);
    private const Gender NewGender = Gender.FEMALE;

    [SetUp]
    public void Setup() {
        repository = Substitute.For<IPlayerRepository>();
        handler = new UpdatePlayerCommandHandler(repository);
    }

    [Test]
    public async Task UpdateAPlayerWhileBeingExecuted() {
        var player = AnExistingPlayer();
        var command = new UpdatePlayerCommand {
            Id = playerId,
            FirstName = NewName,
            LastName = NewLastName,
            SecondLastName = NewSecondLastName,
            BirthDate = NewBirthDate,
            Gender = NewGender,
        };
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns([player]);

        await handler.Handle(command, CancellationToken.None);

        await repository.Received(1)
            .Get(Arg.Is<CriteriaSet>(sc =>
                    sc != null && sc.ValueCriteria.Count == 1 && sc.ValueCriteria.Any(c =>
                        c.Name == "Id"
                        && c.Type == ValueCriteriaType.AND
                        && c.Values.Count == 1
                        && c.Values[0] == playerId.ToString())),
                Arg.Any<CancellationToken>());
        await repository.Received(1).Update(Arg.Is<Player>(p =>
                p != null && p.Id == player.Id
                && p.FirstName == NewName
                && p.LastName == NewLastName
                && p.SecondLastName == NewSecondLastName
                && p.Gender == NewGender
                && p.Birthdate == NewBirthDate),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task KeepOmittedFieldsIncludingTheSecondLastNameWhenOnlyFirstNameIsSent() {
        var player = AnExistingPlayer();
        var command = new UpdatePlayerCommand { Id = playerId, FirstName = NewName };
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns([player]);

        await handler.Handle(command, CancellationToken.None);

        await repository.Received(1).Update(Arg.Is<Player>(p =>
                p != null && p.FirstName == NewName
                && p.LastName == TestDefaultValues.DefaultLastName
                && p.SecondLastName == TestDefaultValues.DefaultSecondLastName
                && p.Gender == OriginalGender
                && p.Birthdate == OriginalBirthDate),
            Arg.Any<CancellationToken>());
    }

    [TestCase("Nuevo", "Nuevo")]
    [TestCase(null, TestDefaultValues.DefaultSecondLastName)]
    public async Task ReplaceTheSecondLastNameOnlyWhenAValueIsSentAndKeepTheRest(string? sentSecondLastName, string? expectedSecondLastName) {
        var player = AnExistingPlayer();
        var command = new UpdatePlayerCommand { Id = playerId, SecondLastName = sentSecondLastName };
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns([player]);

        await handler.Handle(command, CancellationToken.None);

        await repository.Received(1).Update(Arg.Is<Player>(p =>
                p != null && p.FirstName == TestDefaultValues.DefaultFirstName
                && p.LastName == TestDefaultValues.DefaultLastName
                && p.SecondLastName == expectedSecondLastName
                && p.Gender == OriginalGender
                && p.Birthdate == OriginalBirthDate),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ThrowExceptionWhenPlayerNotFound() {
        var command = new UpdatePlayerCommand { Id = playerId, FirstName = NewName };
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdatePlayerNotFoundException>();
    }

    [Test]
    public async Task PropagateTheRepositoryFailureWhileGettingPlayer() {
        var failure = new InvalidOperationException("database unavailable");
        var command = new UpdatePlayerCommand { Id = playerId, FirstName = NewName };
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>())
            .Throws(failure);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }

    [Test]
    public async Task PropagateTheRepositoryFailureWhileUpdatingPlayer() {
        var failure = new InvalidOperationException("database unavailable");
        var player = AnExistingPlayer();
        var command = new UpdatePlayerCommand {
            Id = playerId,
            FirstName = NewName,
            LastName = NewLastName,
            SecondLastName = NewSecondLastName,
            BirthDate = NewBirthDate,
            Gender = NewGender,
        };

        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns([player]);
        repository.Update(Arg.Any<Player>(), Arg.Any<CancellationToken>())
            .Throws(failure);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        (await result.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task RejectAnEmptySecondLastNameWithoutUpdating(string sentSecondLastName) {
        var command = new UpdatePlayerCommand { Id = playerId, SecondLastName = sentSecondLastName };
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns([AnExistingPlayer()]);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var failure = (await result.Should().ThrowAsync<ValidationFailedException>()).Which;
        failure.ErrorTranslationKey.Should().Be(ValidationErrorKeys.PlayersUpdate);
        failure.Errors.Keys.Should().Equal("SecondLastName");
        await repository.DidNotReceive().Update(Arg.Any<Player>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReportEverySentInvalidFieldAndNotUpdateWhenAnyIsInvalid() {
        var command = new UpdatePlayerCommand {
            Id = playerId,
            FirstName = "",
            LastName = NewLastName,
            SecondLastName = NewSecondLastName,
            BirthDate = DateTime.UtcNow.Date.AddYears(-15),
            Gender = (Gender)99,
        };
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns([AnExistingPlayer()]);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        var failure = (await result.Should().ThrowAsync<ValidationFailedException>()).Which;
        failure.Title.Should().Be("Cannot update player");
        failure.ErrorTranslationKey.Should().Be(ValidationErrorKeys.PlayersUpdate);
        failure.Errors.Keys.Should().BeEquivalentTo("FirstName", "BirthDate", "Gender");
        await repository.DidNotReceive().Update(Arg.Any<Player>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReturnNotFoundBeforeValidatingTheSentFields() {
        var command = new UpdatePlayerCommand { Id = playerId, FirstName = "" };
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = async () => await handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdatePlayerNotFoundException>();
    }

    [Test]
    public async Task UpdateAStoredPlayerThatBreaksTheRulesWhenOnlyValidFieldsAreSent() {
        var legacy = new Player(playerId, new string('x', 150), TestDefaultValues.DefaultLastName, "", OriginalGender,
            DateTime.UtcNow.Date.AddYears(-10));
        var command = new UpdatePlayerCommand { Id = playerId, LastName = NewLastName };
        repository.Get(Arg.Any<CriteriaSet>(), Arg.Any<CancellationToken>()).Returns([legacy]);

        await handler.Handle(command, CancellationToken.None);

        await repository.Received(1).Update(Arg.Is<Player>(p =>
                p != null && p.LastName == NewLastName && p.FirstName.Length == 150 && p.SecondLastName == ""),
            Arg.Any<CancellationToken>());
    }

    private Player AnExistingPlayer() => new(
        playerId,
        TestDefaultValues.DefaultFirstName,
        TestDefaultValues.DefaultLastName,
        TestDefaultValues.DefaultSecondLastName,
        OriginalGender,
        OriginalBirthDate);
}
