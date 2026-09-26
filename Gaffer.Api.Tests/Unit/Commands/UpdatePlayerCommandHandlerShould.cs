using System.Globalization;
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
    private DateTime _newBirthDate;
    const Gender NewGender = Gender.FEMALE;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IPlayerRepository>();
        _handler = new UpdatePlayerCommandHandler(_repository);
    }

    [Test]
    public async Task UpdateAPlayerWhileBeingExecuted()
    {
        _newBirthDate = new DateTime(2000, 1, 3);
        const Gender newGender = Gender.FEMALE;
        var player = new Player(TestDefaultValues.EmptyPlayerId,
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            Gender.MALE,
            new DateTime(1999, 1, 1));
        var command = new UpdatePlayerCommand
        {
            Id = _playerId,
            FirstName = NewName,
            LastName = NewLastName,
            SecondLastName = NewSecondLastName,
            BirthDate = _newBirthDate.ToString(CultureInfo.InvariantCulture),
            Gender = newGender.ToString(),
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
                && p.Gender == newGender
                && p.Birthdate == _newBirthDate),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void ThrowExceptionWhenPlayerNotFound()
    {
        var command = new UpdatePlayerCommand
        {
            Id = _playerId,
            FirstName = NewName,
            LastName = NewLastName,
            SecondLastName = NewSecondLastName,
            BirthDate = _newBirthDate.ToString(CultureInfo.InvariantCulture),
            Gender = NewGender.ToString(),
        };
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);
        
        var result = async () => await _handler.Handle(command, CancellationToken.None);

        result.Should().ThrowAsync<PlayerNotFoundException>();
    }

    [Test]
    public async Task ThrowExceptionWhenRepositoryFailsWhileGettingPlayer()
    {
        var command = new UpdatePlayerCommand
        {
            Id = _playerId,
            FirstName = NewName,
            LastName = NewLastName,
            SecondLastName = NewSecondLastName,
            BirthDate = new DateTime(2000, 1, 3).ToString(CultureInfo.InvariantCulture),
            Gender = NewGender.ToString(),
        };
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("repository unavailable"));

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdatePlayerException>()
            .WithMessage("*repository unavailable*");
    }

    [Test]
    public async Task ThrowExceptionWhenRepositoryFailsWhileUpdatingPlayer()
    {
        var player = new Player(
            _playerId,
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            Gender.MALE,
            new DateTime(1999, 1, 1));
        var command = new UpdatePlayerCommand
        {
            Id = _playerId,
            FirstName = NewName,
            LastName = NewLastName,
            SecondLastName = NewSecondLastName,
            BirthDate = new DateTime(2000, 1, 3).ToString(CultureInfo.InvariantCulture),
            Gender = NewGender.ToString(),
        };

        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([player]);
        _repository.Update(Arg.Any<Player>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("update failed"));

        var result = async () => await _handler.Handle(command, CancellationToken.None);

        await result.Should().ThrowAsync<CannotUpdatePlayerException>()
            .WithMessage("*update failed*");
    }
}