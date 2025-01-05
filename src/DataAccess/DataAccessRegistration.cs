using Microsoft.Extensions.DependencyInjection;

namespace DataAccess;

public static class DataAccessRegistration
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddSingleton<IEventRepository, EventRepository>();
        return services;
    }
}