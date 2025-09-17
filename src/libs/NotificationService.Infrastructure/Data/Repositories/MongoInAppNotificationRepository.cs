using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Data.Repositories;

public class MongoInAppNotificationRepository : IInAppNotificationRepository
{
    private readonly IMongoCollection<InAppNotification> _collection;
    private readonly ILogger<MongoInAppNotificationRepository> _logger;

    public MongoInAppNotificationRepository(MongoDbContext context, ILogger<MongoInAppNotificationRepository> logger)
    {
        _collection = context.InAppNotifications;
        _logger = logger;
    }

    public async Task<InAppNotification> CreateAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            notification.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            notification.CreatedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _collection.InsertOneAsync(notification, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Created in-app notification {NotificationId}", notification.Id);
            
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create in-app notification");
            throw;
        }
    }

    public async Task<InAppNotification?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<InAppNotification>.Filter.Eq(x => x.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get in-app notification {NotificationId}", id);
            throw;
        }
    }

    public async Task<(List<InAppNotification> Notifications, int TotalCount)> GetByUserIdAsync(
        string userId, 
        bool unreadOnly = false, 
        int skip = 0, 
        int take = 20, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filterBuilder = Builders<InAppNotification>.Filter;
            var filter = filterBuilder.Eq(x => x.UserId, userId);

            if (unreadOnly)
            {
                filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.IsRead, false));
            }

            // Add expiry filter
            filter = filterBuilder.And(filter, 
                filterBuilder.Or(
                    filterBuilder.Eq(x => x.ExpiresAt, null),
                    filterBuilder.Gt(x => x.ExpiresAt, DateTime.UtcNow)
                ));

            var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            var notifications = await _collection
                .Find(filter)
                .SortByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Limit(take)
                .ToListAsync(cancellationToken);

            return (notifications, (int)totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<InAppNotification> UpdateAsync(InAppNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            notification.UpdatedAt = DateTime.UtcNow;
            
            var filter = Builders<InAppNotification>.Filter.Eq(x => x.Id, notification.Id);
            await _collection.ReplaceOneAsync(filter, notification, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Updated in-app notification {NotificationId}", notification.Id);
            
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update in-app notification {NotificationId}", notification.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<InAppNotification>.Filter.Eq(x => x.Id, id);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);
            
            _logger.LogInformation("Deleted in-app notification {NotificationId}", id);
            
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete in-app notification {NotificationId}", id);
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filterBuilder = Builders<InAppNotification>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(x => x.UserId, userId),
                filterBuilder.Eq(x => x.IsRead, false),
                filterBuilder.Or(
                    filterBuilder.Eq(x => x.ExpiresAt, null),
                    filterBuilder.Gt(x => x.ExpiresAt, DateTime.UtcNow)
                )
            );

            var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread count for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> MarkAsReadAsync(List<string> notificationIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<InAppNotification>.Filter.In(x => x.Id, notificationIds);
            var update = Builders<InAppNotification>.Update
                .Set(x => x.IsRead, true)
                .Set(x => x.ReadAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Marked {Count} notifications as read", result.ModifiedCount);
            
            return (int)result.ModifiedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notifications as read");
            throw;
        }
    }

    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<InAppNotification>.Filter.And(
                Builders<InAppNotification>.Filter.Ne(x => x.ExpiresAt, null),
                Builders<InAppNotification>.Filter.Lt(x => x.ExpiresAt, DateTime.UtcNow)
            );

            var result = await _collection.DeleteManyAsync(filter, cancellationToken);
            
            _logger.LogInformation("Cleaned up {Count} expired notifications", result.DeletedCount);
            
            return (int)result.DeletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired notifications");
            throw;
        }
    }
}
