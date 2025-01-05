using Models;

namespace EventProcessing;

public interface INewEventHandler
{
    Task InsertNewEvent(Event newEvent);
}