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
    
    [SetUp]
    public void Setup()
    {
        _getPlayerByIdQueryHandler = Substitute.For<IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse>>();
        _controller = new PlayerController(_getPlayerByIdQueryHandler, null);
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
}