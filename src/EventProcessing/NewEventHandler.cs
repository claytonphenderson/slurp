using DataAccess;
using Models;

namespace EventProcessing;

public class NewEventHandler : INewEventHandler
{
    private readonly IEventRepository _eventRepository;
    public NewEventHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task InsertNewEvent(Event newEvent)
    {
        await _eventRepository.Insert(newEvent);
    }
}