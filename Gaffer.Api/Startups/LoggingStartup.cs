using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gaffer.Api.Startups;

public static class LoggingStartup
{
    public static void ConfigureLogging(this IServiceCollection services, IConfiguration configuration, ProductContext productContext)
    {
        
    }
}