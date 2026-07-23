using Microsoft.Extensions.DependencyInjection;
using SaasPlatform.Application.Organizations;
using SaasPlatform.Application.Subscriptions;

namespace SaasPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<OrganizationService>();
        services.AddScoped<SubscriptionService>();
        return services;
    }
}
