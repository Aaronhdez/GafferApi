using AwesomeAssertions;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gaffer.Api.Tests.Unit.Commands;

public class UpdatePlayerCommandHandlerShould
{
    private IPlayerRepository _repository;
    private UpdatePlayerCommandHandler _handler;
    private readonly Guid _playerId = Guid.NewGuid();
    private const string NewName = "NewName";
    private const string NewLastName = "ANewLastName";
    private const string NewSecondLastName = "SecondLastName";
    private static readonly DateTime OriginalBirthDate = new(1999, 1, 1);
    private const Gender OriginalGender = Gender.MALE;
    private static readonly DateTime NewBirthDate = new(2000, 1, 3);
    private const Gender NewGender = Gender.FEMALE;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IPlayerRepository>();
        _handler = new UpdatePlayerCommandHandler(_repository);
    }

    [Test]
    public async Task UpdateAPlayerWhileBeingExecuted()
    {
        var player = AnExistingPlayer();
        var command = new UpdatePlayerCommand
        {
            Id = _playerId,
            FirstName = NewName,
            LastName = NewLastName,
            SecondLastName = NewSecondLastName,
            BirthDate = NewBirthDate,
            Gender = NewGender,
        };
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([player]);

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1)
            .Get(Arg.Is<PlayerSearchCriteria>(sc =>
                    sc.criteriaList.Any(c =>
                        c.Name == "Id"
                        && c.Value == _playerId.ToString())),
                Arg.Any<CancellationToken>());
        await _repository.Received(1).Update(Arg.Is<Player>(p =>
                p.Id == player.Id
                && p.FirstName == NewName
                && p.LastName == NewLastName
                && p.SecondLastName == NewSecondLastName
                && p.Gender == NewGender
                && p.Birthdate == NewBirthDate),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task KeepOmittedFieldsAndClearSecondLastNameWhenOnlyFirstNameIsSent()
    {
        var player = AnExistingPlayer();
        var command = new UpdatePlayerCommand { Id = _playerId, FirstName = NewName };
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([player]);

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1).Update(Arg.Is<Player>(p =>
                p.FirstName == NewName
                && p.LastName == TestDefaultValues.DefaultLastName
                && p.SecondLastName == null
                && p.Gender == OriginalGender
                && p.Birthdate == OriginalBirthDate),
            Arg.Any<CancellationToken>());
    }

    [TestCase("Nuevo", "Nuevo")]
    [TestCase("", null)]
    [TestCase(null, null)]
    public async Task ReplaceTheSecondLastNameAndKeepTheRest(string? sentSecondLastName, string? expectedSecondLastName)
    {
        var player = AnExistingPlayer();
        var command = new UpdatePlayerCommand { Id = _playerId, SecondLastName = sentSecondLastName };
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([player]);

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1).Update(Arg.Is<Player>(p =>
                p.FirstName == TestDefaultValues.DefaultFirstName
                && p.LastName == TestDefaultValues.DefaultLastName
                && p.SecondLastName == expectedSecondLastName
                && p.Gender == OriginalGender
                && p.Birthdate == OriginalBirthDate),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ThrowExceptionWhenPlayerNotFound()
    {
        var command = new UpdatePlayerCommand { Id = _playerId, FirstName = NewName };
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdatePlayerNotFoundException>();
    }

    [Test]
    public async Task ThrowExceptionWhenRepositoryFailsWhileGettingPlayer()
    {
        var command = new UpdatePlayerCommand { Id = _playerId, FirstName = NewName };
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("repository unavailable"));

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdatePlayerException>()
            .WithMessage("*repository unavailable*");
    }

    [Test]
    public async Task ThrowExceptionWhenRepositoryFailsWhileUpdatingPlayer()
    {
        var player = AnExistingPlayer();
        var command = new UpdatePlayerCommand
        {
            Id = _playerId,
            FirstName = NewName,
            LastName = NewLastName,
            SecondLastName = NewSecondLastName,
            BirthDate = NewBirthDate,
            Gender = NewGender,
        };

        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([player]);
        _repository.Update(Arg.Any<Player>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("update failed"));

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdatePlayerException>()
            .WithMessage("*update failed*");
    }

    private Player AnExistingPlayer() => new(
        _playerId,
        TestDefaultValues.DefaultFirstName,
        TestDefaultValues.DefaultLastName,
        TestDefaultValues.DefaultSecondLastName,
        OriginalGender,
        OriginalBirthDate);
}
