using Asp.Versioning;
using Asp.Versioning.Conventions;
using Gaffer.Api.Controllers;
using Gaffer.Api.Domain;
using Gaffer.Api.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Gaffer.Api.Startups;

public static class VersioningStartup {
    public static IServiceCollection ConfigureVersioning(this IServiceCollection services) {
        var apiVersionsDefinition = new GafferApiVersioningDefinition();
        services.AddSingleton(apiVersionsDefinition);
        services.AddApiVersioning(options => {
                options.DefaultApiVersion = new ApiVersion(ApiVersioning.Current, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddMvc(options => {
                options.Conventions.Controller<PlayerController>().HasApiVersions(apiVersionsDefinition.Versions);
                options.Conventions.Controller<FacilitiesController>().HasApiVersions(apiVersionsDefinition.Versions);
            })
            .AddApiExplorer(options => {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
        return services;
    }
}

