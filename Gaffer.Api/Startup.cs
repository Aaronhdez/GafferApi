using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Gaffer.Api.Commands;
using Gaffer.Api.Queries;
using Gaffer.Api.Startups;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NSwag.AspNetCore;

namespace Gaffer.Api;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.ConfigureLogging(configuration, Program.ProductContext);
        services.ConfigureOptions(configuration, Program.ProductContext.StartupSettingsFileName);
        services.AddMemoryCache();
        services.AddMvcCore()
            .AddMvcOptions(options =>
                options.Filters.Add(new ProducesAttribute("application/json"))
            )
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            });

        services.ConfigureAuthentication(configuration);
        services.AddAuthorization();
        services.ConfigureVersioning();
        services.ConfigureSwagger(Program.ProductContext.Name);
        services.ConfigureDatabase(configuration);
        services.AddScoped<IQueryHandler<GetPlayerByIdQuery, GetPlayerByIdResponse>, GetPlayerByIdQueryHandler>();
        //services.AddScoped<ICommandHandler<CreatePlayerCommand, CreatePlayerResponse>, CreatePlayerCommandHandler>();
        //services.AddHealthChecks(_configuration);
        //services.ConfigureSherlock(configuration);
        //services.ConfigureServices(configuration);
        //services.ConfigureClients(configuration);
        //services.AddHttpFilteringModelBinder();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app
            //.UseMiddleware<ExceptionHandlingMiddleware>()
            .UseOpenApi()
            .UseSwaggerUi(settings =>
            {
                foreach (var description in provider.ApiVersionDescriptions.Reverse())
                {
                    settings.SwaggerRoutes.Add(new SwaggerUiRoute(description.GroupName.ToUpperInvariant(),
                        $"/swagger/{description.GroupName}/swagger.json"));
                }
            })
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //.RequireAuthorization();
            });
    }
}