using EventStore.Models;
using MongoDB.Driver;
using System.Text.Json;

namespace EventStore.Services;

public interface IEventStoreService
{
    Task StoreEventAsync(StoredEvent storedEvent);
    Task<List<StoredEvent>> GetEventsByAggregateIdAsync(string aggregateId, string aggregateType);
    Task<List<StoredEvent>> GetEventsByTypeAsync(string eventType);
    Task<List<StoredEvent>> GetEventsByDateRangeAsync(DateTime from, DateTime to);
    Task<long> GetEventCountAsync();
    Task<List<StoredEvent>> GetEventsForReplayAsync(string aggregateId, string aggregateType, DateTime? fromDate = null);
}

public class EventStoreService : IEventStoreService
{
    private readonly IMongoCollection<StoredEvent> _eventsCollection;
    private readonly ILogger<EventStoreService> _logger;

    public EventStoreService(IMongoDatabase database, ILogger<EventStoreService> logger)
    {
        _eventsCollection = database.GetCollection<StoredEvent>("events");
        _logger = logger;
        
        // Create indexes for better performance
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var indexKeysDefinition = Builders<StoredEvent>.IndexKeys
            .Ascending(e => e.AggregateId)
            .Ascending(e => e.AggregateType)
            .Ascending(e => e.Timestamp);

        var indexOptions = new CreateIndexOptions { Name = "AggregateId_Type_Timestamp" };
        var indexModel = new CreateIndexModel<StoredEvent>(indexKeysDefinition, indexOptions);
        _eventsCollection.Indexes.CreateOne(indexModel);

        // Index for event type queries
        var eventTypeIndex = Builders<StoredEvent>.IndexKeys.Ascending(e => e.EventType);
        var eventTypeIndexModel = new CreateIndexModel<StoredEvent>(eventTypeIndex, new CreateIndexOptions { Name = "EventType" });
        _eventsCollection.Indexes.CreateOne(eventTypeIndexModel);

        // Index for timestamp queries
        var timestampIndex = Builders<StoredEvent>.IndexKeys.Ascending(e => e.Timestamp);
        var timestampIndexModel = new CreateIndexModel<StoredEvent>(timestampIndex, new CreateIndexOptions { Name = "Timestamp" });
        _eventsCollection.Indexes.CreateOne(timestampIndexModel);
    }

    public async Task StoreEventAsync(StoredEvent storedEvent)
    {
        try
        {
            await _eventsCollection.InsertOneAsync(storedEvent);
            _logger.LogInformation("Event stored successfully: {EventType} for aggregate {AggregateType}:{AggregateId}",
                storedEvent.EventType, storedEvent.AggregateType, storedEvent.AggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store event {EventType} for aggregate {AggregateType}:{AggregateId}",
                storedEvent.EventType, storedEvent.AggregateType, storedEvent.AggregateId);
            throw;
        }
    }

    public async Task<List<StoredEvent>> GetEventsByAggregateIdAsync(string aggregateId, string aggregateType)
    {
        var filter = Builders<StoredEvent>.Filter.And(
            Builders<StoredEvent>.Filter.Eq(e => e.AggregateId, aggregateId),
            Builders<StoredEvent>.Filter.Eq(e => e.AggregateType, aggregateType)
        );

        var sort = Builders<StoredEvent>.Sort.Ascending(e => e.Timestamp);

        return await _eventsCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<List<StoredEvent>> GetEventsByTypeAsync(string eventType)
    {
        var filter = Builders<StoredEvent>.Filter.Eq(e => e.EventType, eventType);
        var sort = Builders<StoredEvent>.Sort.Descending(e => e.Timestamp);

        return await _eventsCollection.Find(filter).Sort(sort).Limit(100).ToListAsync();
    }

    public async Task<List<StoredEvent>> GetEventsByDateRangeAsync(DateTime from, DateTime to)
    {
        var filter = Builders<StoredEvent>.Filter.And(
            Builders<StoredEvent>.Filter.Gte(e => e.Timestamp, from),
            Builders<StoredEvent>.Filter.Lte(e => e.Timestamp, to)
        );

        var sort = Builders<StoredEvent>.Sort.Descending(e => e.Timestamp);

        return await _eventsCollection.Find(filter).Sort(sort).Limit(1000).ToListAsync();
    }

    public async Task<long> GetEventCountAsync()
    {
        return await _eventsCollection.CountDocumentsAsync(FilterDefinition<StoredEvent>.Empty);
    }

    public async Task<List<StoredEvent>> GetEventsForReplayAsync(string aggregateId, string aggregateType, DateTime? fromDate = null)
    {
        var filterBuilder = Builders<StoredEvent>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(e => e.AggregateId, aggregateId),
            filterBuilder.Eq(e => e.AggregateType, aggregateType)
        );

        if (fromDate.HasValue)
        {
            filter = filterBuilder.And(filter, filterBuilder.Gte(e => e.Timestamp, fromDate.Value));
        }

        var sort = Builders<StoredEvent>.Sort.Ascending(e => e.Timestamp);

        return await _eventsCollection.Find(filter).Sort(sort).ToListAsync();
    }
} 