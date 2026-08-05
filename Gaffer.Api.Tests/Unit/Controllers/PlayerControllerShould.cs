using Gaffer.Api.Controllers;
using Gaffer.Api.Queries;
using NSubstitute;

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
        
        await _controller.GetBy(newGuid);
        
        await _getPlayerByIdQueryHandler.Received(1).Handle(
            Arg.Is<GetPlayerByIdQuery>(query => query != null 
                                                && query.Id == newGuid), 
            Arg.Any<CancellationToken>());
    }
}