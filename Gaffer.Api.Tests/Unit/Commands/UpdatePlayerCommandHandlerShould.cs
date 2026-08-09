using System.Globalization;
using AwesomeAssertions;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Wordprocessing;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using Gaffer.Api.Tests.TestShared;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Commands;

public class UpdatePlayerCommandHandlerShould
{
    private IPlayerRepository _repository;
    private UpdatePlayerCommandHandler _handler;
    private Guid playerId = Guid.NewGuid();
    private const string newName = "NewName";
    private const string newLastName = "ANewLastName";
    private const string newSecondLastName = "SecondLastName";
    private DateTime newBirthDate;
    const Gender newGender = Gender.FEMALE;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IPlayerRepository>();
        _handler = new UpdatePlayerCommandHandler(_repository);
    }

    [Test]
    public async Task UpdateAPlayerWhileBeingExecuted()
    {
        newBirthDate = new DateTime(2000, 1, 3);
        const Gender newGender = Gender.FEMALE;
        var player = new Player(TestDefaultValues.EmptyPlayerId,
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            Gender.MALE,
            new DateTime(1999, 1, 1));
        var command = new UpdatePlayerCommand
        {
            Id = playerId,
            FirstName = newName,
            LastName = newLastName,
            SecondLastName = newSecondLastName,
            BirthDate = newBirthDate.ToString(CultureInfo.InvariantCulture),
            Gender = newGender.ToString(),
        };
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([player]);
        
        await _handler.Handle(command, CancellationToken.None);
        
        await _repository.Received(1)
            .Get(Arg.Is<PlayerSearchCriteria>(sc =>
                    sc.criteriaList.Any(c => 
                        c.Name == "Id" 
                        && c.Value == playerId.ToString())),
                Arg.Any<CancellationToken>());
        await _repository.Received(1).Update(Arg.Is<Player>(p =>
                p.Id == playerId
                && p.FirstName == newName
                && p.LastName == newLastName
                && p.SecondLastName == newSecondLastName
                && p.Gender == newGender
                && p.Birthdate == newBirthDate),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void ThrowExceptionWhenPlayerNotFound()
    {
        var command = new UpdatePlayerCommand
        {
            Id = playerId,
            FirstName = newName,
            LastName = newLastName,
            SecondLastName = newSecondLastName,
            BirthDate = newBirthDate.ToString(CultureInfo.InvariantCulture),
            Gender = newGender.ToString(),
        };
        _repository.Get(Arg.Any<PlayerSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([]);
        
        var result = async () => await _handler.Handle(command, CancellationToken.None);

        result.Should().ThrowAsync<PlayerNotFoundException>();
    }
}