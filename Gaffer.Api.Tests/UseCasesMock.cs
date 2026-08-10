using Gaffer.Api.Commands;
using Gaffer.Api.Queries;
using NSubstitute;

namespace Gaffer.Api.Tests;

public class UseCasesMock
{
    public UseCasesMock()
    {
        AddPlayerActions();
    }

    private void AddPlayerActions()
    {
        CreatePlayerCommandHandler = Substitute.For<ICommandHandler<CreatePlayerCommand, CreatePlayerResponse>>();
        SearchPlayersQueryHandler = Substitute.For<IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>>>();
        UpdatePlayerCommandHandler = Substitute.For<ICommandHandler<UpdatePlayerCommand>>();
        DeletePlayerCommandHandler = Substitute.For<ICommandHandler<DeletePlayerCommand>>();
    }

    public IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> SearchPlayersQueryHandler { get; set; }
    public ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> CreatePlayerCommandHandler { get; set; }
    public ICommandHandler<UpdatePlayerCommand> UpdatePlayerCommandHandler { get; set; }
    public ICommandHandler<DeletePlayerCommand> DeletePlayerCommandHandler { get; set; }
}