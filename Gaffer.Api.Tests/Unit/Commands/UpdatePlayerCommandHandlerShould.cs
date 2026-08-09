using System.Globalization;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Wordprocessing;
using Gaffer.Api.Commands;
using Gaffer.Api.Domain;
using Gaffer.Api.Shared;
using Gaffer.Api.Shared.Repositories;
using NSubstitute;

namespace Gaffer.Api.Tests.Unit.Commands;

public class UpdatePlayerCommandHandlerShould
{
    private IPlayerRepository _repository;
    private UpdatePlayerCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IPlayerRepository>();
        _handler = new UpdatePlayerCommandHandler(_repository);
    }

    [Test]
    public async Task UpdateAPlayerWhileBeingExecuted()
    {
        var playerId = Guid.NewGuid();
        const string newName = "NewName";
        const string newLastName = "ANewLastName";
        const string newSecondLastName = "SecondLastName";
        var newBirthDate = new DateTime(2000, 1, 3).ToString(CultureInfo.InvariantCulture);
        const Gender newGender = Gender.FEMALE;
        var player = new Player(playerId, "Name", "LastName", "SecondLastName", Gender.MALE, new DateTime(1999,1,1));
        var command = new UpdatePlayerCommand
        {
            Id = playerId,
            FirstName = newName,
            LastName = newLastName,
            SecondLastName = newSecondLastName,
            BirthDate = newBirthDate,
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
                && p.Birthdate.ToString(CultureInfo.InvariantCulture) == newBirthDate),
            Arg.Any<CancellationToken>());
    }
}