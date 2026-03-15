using System.Text.Json;
using MicroserviceSample.Domain;
using MicroserviceSample.Infrastructure.Data;

namespace MicroserviceSample.Infrastructure.EventStore;

public interface IEventStore
{
    Task SaveEventAsync<T>(Guid correlationId, T @event) where T : class;
    Task<List<StoredEvent>> GetEventsAsync(Guid correlationId);
}

public class EventStore(ApplicationDbContext db) : IEventStore
{
    public async Task SaveEventAsync<T>(Guid correlationId, T @event) where T : class
    {
        var storedEvent = new StoredEvent
        {
            CorrelationId = correlationId,
            EventType = typeof(T).Name,
            Payload = JsonSerializer.Serialize(@event),
            Timestamp = DateTime.UtcNow
        };

        db.StoredEvents.Add(storedEvent);
        await db.SaveChangesAsync();
    }

    public async Task<List<StoredEvent>> GetEventsAsync(Guid correlationId)
    {
        return await Task.FromResult(
            db.StoredEvents
                .Where(e => e.CorrelationId == correlationId)
                .OrderBy(e => e.Timestamp)
                .ToList());
    }
}
