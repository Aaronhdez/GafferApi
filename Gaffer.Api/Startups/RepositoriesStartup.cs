using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gaffer.Api.Startups;

public static class RepositoriesStartup
{
    public static IServiceCollection ConfigureRepositories(this IServiceCollection services, IConfiguration configuration) {
        return services;
    }

    public static IServiceCollection ConfigureDummyRepositories(this IServiceCollection services, IConfiguration configuration)
    {        
        services.AddSingleton(typeof(List<Player> ), provider => Activator.CreateInstance(typeof(List < > ).MakeGenericType(typeof(Player))));
        services.AddScoped<IPlayerRepository,DummyPlayerRepository>();
        return services;
    }
}