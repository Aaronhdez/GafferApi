using Gaffer.Api.Domain;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Shared.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gaffer.Api.Startups;

public static class RepositoriesStartup {
    public static IServiceCollection ConfigureRepositories(this IServiceCollection services, IConfiguration configuration) {
        services.AddScoped<IPlayerRepository, SqlPlayerRepository>();
        services.AddScoped<IFacilityRepository, SqlFacilityRepository>();
        return services;
    }
}
