using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Data;

namespace NotificationService.Infrastructure.Data.Repositories;

public class MongoUserNotificationPreferenceRepository : IUserNotificationPreferenceRepository
{
    private readonly IMongoCollection<UserNotificationPreference> _collection;
    private readonly ILogger<MongoUserNotificationPreferenceRepository> _logger;

    public MongoUserNotificationPreferenceRepository(MongoDbContext context, ILogger<MongoUserNotificationPreferenceRepository> logger)
    {
        _collection = context.GetCollection<UserNotificationPreference>("user_notification_preferences");
        _logger = logger;
    }

    public async Task<List<UserNotificationPreference>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<UserNotificationPreference>.Filter.Eq(x => x.UserId, userId);
            var preferences = await _collection.Find(filter).ToListAsync(cancellationToken);
            
            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserNotificationPreference> UpsertAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default)
    {
        try
        {
            preference.UpdatedAt = DateTime.UtcNow;
            
            var filter = Builders<UserNotificationPreference>.Filter.And(
                Builders<UserNotificationPreference>.Filter.Eq(x => x.UserId, preference.UserId),
                Builders<UserNotificationPreference>.Filter.Eq(x => x.Channel, preference.Channel)
            );

            var options = new ReplaceOptions { IsUpsert = true };
            await _collection.ReplaceOneAsync(filter, preference, options, cancellationToken);
            
            _logger.LogInformation("Upserted notification preference for user {UserId}, channel {Channel}", 
                preference.UserId, preference.Channel);
                
            return preference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert notification preference for user {UserId}", preference.UserId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<UserNotificationPreference>.Filter.Eq(x => x.Id, id);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);
            
            _logger.LogInformation("Deleted notification preference {PreferenceId}", id);
            
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification preference {PreferenceId}", id);
            throw;
        }
    }
}
