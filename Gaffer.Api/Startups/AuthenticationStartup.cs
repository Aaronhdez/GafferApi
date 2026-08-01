using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gaffer.Api.Startups;

public static class AuthenticationStartup
{
    public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration) {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();
        return services;
    }
}