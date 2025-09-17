using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Data.Repositories;

/// <summary>
/// MongoDB implementation of the notification history repository
/// </summary>
public class NotificationHistoryRepository : INotificationHistoryRepository
{
    private readonly IMongoCollection<NotificationHistory> _collection;

    public NotificationHistoryRepository(IMongoDatabase database, IOptions<MongoDbSettings> settings)
    {
        _collection = database.GetCollection<NotificationHistory>(settings.Value.HistoryCollection);
        
        // Create indexes for better performance
        CreateIndexes();
    }

    public async Task<NotificationHistory?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(h => h.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<NotificationHistory?> GetByExternalMessageIdAsync(string externalMessageId, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(h => h.ExternalMessageId == externalMessageId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationHistory>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var histories = await _collection
            .Find(h => h.Recipient.UserId == userId)
            .SortByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
        
        return histories;
    }

    public async Task<IEnumerable<NotificationHistory>> GetByStatusAsync(NotificationStatus status, CancellationToken cancellationToken = default)
    {
        var histories = await _collection
            .Find(h => h.Status == status)
            .ToListAsync(cancellationToken);
        
        return histories;
    }

    public async Task<IEnumerable<NotificationHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var histories = await _collection
            .Find(h => h.CreatedAt >= startDate && h.CreatedAt <= endDate)
            .SortByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
        
        return histories;
    }

    public async Task<IEnumerable<NotificationHistory>> GetFailedForRetryAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationHistory>.Filter.And(
            Builders<NotificationHistory>.Filter.Eq(h => h.Status, NotificationStatus.Failed),
            Builders<NotificationHistory>.Filter.Where(h => h.RetryCount < h.MaxRetries)
        );

        var histories = await _collection
            .Find(filter)
            .ToListAsync(cancellationToken);
        
        return histories;
    }

    public async Task<NotificationHistory> CreateAsync(NotificationHistory history, CancellationToken cancellationToken = default)
    {
        history.CreatedAt = DateTime.UtcNow;
        history.UpdatedAt = DateTime.UtcNow;
        
        await _collection.InsertOneAsync(history, null, cancellationToken);
        return history;
    }

    public async Task<NotificationHistory> UpdateAsync(NotificationHistory history, CancellationToken cancellationToken = default)
    {
        history.UpdatedAt = DateTime.UtcNow;
        
        await _collection.ReplaceOneAsync(
            h => h.Id == history.Id,
            history,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        
        return history;
    }

    public async Task<bool> UpdateStatusAsync(string id, NotificationStatus status, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var updateBuilder = Builders<NotificationHistory>.Update
            .Set(h => h.Status, status)
            .Set(h => h.UpdatedAt, DateTime.UtcNow);

        if (status == NotificationStatus.Sent)
        {
            updateBuilder = updateBuilder.Set(h => h.SentAt, DateTime.UtcNow);
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            updateBuilder = updateBuilder.Set(h => h.ErrorMessage, errorMessage);
        }

        var result = await _collection.UpdateOneAsync(
            h => h.Id == id,
            updateBuilder,
            null,
            cancellationToken);
        
        return result.ModifiedCount > 0;
    }

    public async Task<bool> IncrementRetryCountAsync(string id, CancellationToken cancellationToken = default)
    {
        var update = Builders<NotificationHistory>.Update
            .Inc(h => h.RetryCount, 1)
            .Set(h => h.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            h => h.Id == id,
            update,
            null,
            cancellationToken);
        
        return result.ModifiedCount > 0;
    }

    public async Task<long> DeleteOldRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteManyAsync(
            h => h.CreatedAt < olderThan,
            cancellationToken);
        
        return result.DeletedCount;
    }

    private void CreateIndexes()
    {
        // Index for user queries
        var userIndexKeys = Builders<NotificationHistory>.IndexKeys
            .Ascending(h => h.Recipient.UserId)
            .Descending(h => h.CreatedAt);
        var userIndexModel = new CreateIndexModel<NotificationHistory>(userIndexKeys);
        _collection.Indexes.CreateOne(userIndexModel);

        // Index for status queries
        var statusIndexKeys = Builders<NotificationHistory>.IndexKeys
            .Ascending(h => h.Status);
        var statusIndexModel = new CreateIndexModel<NotificationHistory>(statusIndexKeys);
        _collection.Indexes.CreateOne(statusIndexModel);

        // Index for external message ID
        var externalIdIndexKeys = Builders<NotificationHistory>.IndexKeys
            .Ascending(h => h.ExternalMessageId);
        var externalIdIndexModel = new CreateIndexModel<NotificationHistory>(externalIdIndexKeys);
        _collection.Indexes.CreateOne(externalIdIndexModel);

        // Index for date range queries
        var dateIndexKeys = Builders<NotificationHistory>.IndexKeys
            .Descending(h => h.CreatedAt);
        var dateIndexModel = new CreateIndexModel<NotificationHistory>(dateIndexKeys);
        _collection.Indexes.CreateOne(dateIndexModel);

        // Compound index for failed notifications ready for retry
        var retryIndexKeys = Builders<NotificationHistory>.IndexKeys
            .Ascending(h => h.Status)
            .Ascending(h => h.RetryCount);
        var retryIndexModel = new CreateIndexModel<NotificationHistory>(retryIndexKeys);
        _collection.Indexes.CreateOne(retryIndexModel);
    }
}