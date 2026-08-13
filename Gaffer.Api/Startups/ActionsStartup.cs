using Gaffer.Api.Commands;
using Gaffer.Api.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Gaffer.Api.Startups;

public static class ActionsStartup
{
    public static IServiceCollection ConfigureActions(this IServiceCollection services)
    {
        ConfigureQueries(services);
        ConfigureCommands(services);
        return services;
    }

    private static void ConfigureCommands(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreatePlayerCommand, CreatePlayerResponse>, CreatePlayerCommandHandler>();
        services.AddScoped<ICommandHandler<CreateFacilityCommand, CreateFacilityResponse>, CreateFacilityCommandHandler>();
        services.AddScoped<ICommandHandler<DeletePlayerCommand>, DeletePlayerCommandHandler>();
        services.AddScoped<ICommandHandler<UpdatePlayerCommand>, UpdatePlayerCommandHandler>();
    }

    private static void ConfigureQueries(IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<SearchPlayersQuery, List<SearchPlayerResult>>, SearchPlayersQueryHandler>();
    }
}