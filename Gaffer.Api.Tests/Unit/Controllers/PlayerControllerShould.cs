using Gaffer.Api.Commands;
using Gaffer.Api.Controllers;
using Gaffer.Api.Queries;
using Gaffer.Api.Shared;
using NSubstitute;
using NuGet.Protocol.Core.Types;

namespace Gaffer.Api.Tests.Unit.Controllers;

public class PlayerControllerShould
{
    private PlayerController _controller;
    private IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse> _getPlayerByIdQueryHandler;
    private ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> _createPlayerCommandHandler;
    
    [SetUp]
    public void Setup()
    {
        _getPlayerByIdQueryHandler = Substitute.For<IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse>>();
        _createPlayerCommandHandler = Substitute.For<ICommandHandler<CreatePlayerCommand, CreatePlayerResponse>>();
        _controller = new PlayerController(_getPlayerByIdQueryHandler, _createPlayerCommandHandler);
    }

    [Test]
    public async Task CallAQueryToGetAPlayerById()
    {
        var newGuid = Guid.NewGuid();
        _getPlayerByIdQueryHandler.Handle(Arg.Is<GetPlayerByIdQuery>(query => query.Id == newGuid),  
                Arg.Any<CancellationToken>()).Returns(new GetPlayerByIdResponse { Id = newGuid });
        
        await _controller.GetBy(newGuid);
        
        await _getPlayerByIdQueryHandler.Received(1).Handle(
            Arg.Is<GetPlayerByIdQuery>(query => query != null 
                                                && query.Id == newGuid), 
            Arg.Any<CancellationToken>());
    }
    
    [Test]
    public async Task CallACommandToCreateAPlayerById()
    {
        const string firstName = "John";
        const string lastName = "Doe";
        const string secondLastName = "Doe";
        DateTime birthDate = new(1990, 01, 01);
        const Gender gender = Gender.MALE;
        
        var newGuid = Guid.NewGuid();
        _createPlayerCommandHandler.Handle(Arg.Is<CreatePlayerCommand>(command =>
                command != null
                && command.FirstName == firstName
                && command.LastName == lastName
                && command.SecondLastName == secondLastName
                && command.Gender == gender
                && command.Birthdate == birthDate
                    ), Arg.Any<CancellationToken>())
            .Returns(new CreatePlayerResponse(newGuid));
        
        await _controller.GetBy(newGuid);
        
        await _getPlayerByIdQueryHandler.Received(1).Handle(
            Arg.Is<GetPlayerByIdQuery>(query => query != null 
                                                && query.Id == newGuid), 
            Arg.Any<CancellationToken>());
    }
}