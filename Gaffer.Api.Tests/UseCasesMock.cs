using Gaffer.Api.Commands;
using Gaffer.Api.Commands.Players;
using Gaffer.Api.Queries;
using NSubstitute;

namespace Gaffer.Api.Tests;

public class UseCasesMock {
    public IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>> SearchPlayersQueryHandler { get; set; } =
        Substitute.For<IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>>>();
    public ICommandHandler<CreatePlayerCommand, CreatePlayerResponse> CreatePlayerCommandHandler { get; set; } =
        Substitute.For<ICommandHandler<CreatePlayerCommand, CreatePlayerResponse>>();
    public ICommandHandler<UpdatePlayerCommand> UpdatePlayerCommandHandler { get; set; } =
        Substitute.For<ICommandHandler<UpdatePlayerCommand>>();
    public ICommandHandler<DeletePlayerCommand> DeletePlayerCommandHandler { get; set; } =
        Substitute.For<ICommandHandler<DeletePlayerCommand>>();
}
