using Microsoft.Extensions.DependencyInjection;

namespace EventProcessing;

public static class EventProcessingRegistration
{
    public static IServiceCollection AddEventProcessing(this IServiceCollection services)
    {
        services.AddSingleton<INewEventHandler, NewEventHandler>();
        return services;
    }
}