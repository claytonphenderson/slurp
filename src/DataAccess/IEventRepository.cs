using Models;

namespace DataAccess;

public interface IEventRepository
{
    Task Insert(Event newEvent);
}