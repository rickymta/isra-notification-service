using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Repositories;

/// <summary>
/// MongoDB implementation of UserNotificationPreference repository
/// </summary>
public class MongoUserNotificationPreferenceRepository : IUserNotificationPreferenceRepository
{
    private readonly IMongoCollection<UserNotificationPreference> _collection;
    private readonly ILogger<MongoUserNotificationPreferenceRepository> _logger;

    public MongoUserNotificationPreferenceRepository(
        IMongoDatabase database,
        ILogger<MongoUserNotificationPreferenceRepository> logger)
    {
        _collection = database.GetCollection<UserNotificationPreference>("UserNotificationPreferences");
        _logger = logger;
        
        // Create indexes
        CreateIndexes();
    }

    public async Task<UserNotificationPreference> AddAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default)
    {
        try
        {
            await _collection.InsertOneAsync(preference, cancellationToken: cancellationToken);
            _logger.LogDebug("User notification preference {PreferenceId} added for user {UserId}", 
                preference.Id, preference.UserId);
            return preference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user notification preference for user {UserId}", preference.UserId);
            throw;
        }
    }

    public async Task<UserNotificationPreference> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<UserNotificationPreference>.Filter.Eq(x => x.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user notification preference {PreferenceId}", id);
            throw;
        }
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
            _logger.LogError(ex, "Error getting user notification preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserNotificationPreference> UpdateAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default)
    {
        try
        {
            preference.UpdatedAt = DateTime.UtcNow;
            
            var filter = Builders<UserNotificationPreference>.Filter.Eq(x => x.Id, preference.Id);
            await _collection.ReplaceOneAsync(filter, preference, cancellationToken: cancellationToken);
            
            _logger.LogDebug("User notification preference {PreferenceId} updated for user {UserId}", 
                preference.Id, preference.UserId);
            return preference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user notification preference for user {UserId}", preference.UserId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<UserNotificationPreference>.Filter.Eq(x => x.Id, id);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);
            
            _logger.LogDebug("User notification preference {PreferenceId} deleted, success: {Success}", 
                id, result.DeletedCount > 0);
            
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user notification preference {PreferenceId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<UserNotificationPreference>.Filter.Eq(x => x.UserId, userId);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);
            
            _logger.LogDebug("User notification preference for user {UserId} deleted, success: {Success}", 
                userId, result.DeletedCount > 0);
            
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user notification preference for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserNotificationPreference>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection
                .Find(Builders<UserNotificationPreference>.Filter.Empty)
                .Skip(skip)
                .Limit(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all user notification preferences");
            throw;
        }
    }

    public async Task<UserNotificationPreference> UpsertAsync(UserNotificationPreference preference, CancellationToken cancellationToken = default)
    {
        try
        {
            preference.UpdatedAt = DateTime.UtcNow;
            
            var filter = Builders<UserNotificationPreference>.Filter.Eq(x => x.UserId, preference.UserId);
            var options = new ReplaceOptions { IsUpsert = true };
            
            await _collection.ReplaceOneAsync(filter, preference, options, cancellationToken);
            
            _logger.LogDebug("User notification preference upserted for user {UserId}", preference.UserId);
            
            return preference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting user notification preference for user {UserId}", preference.UserId);
            throw;
        }
    }

    private void CreateIndexes()
    {
        try
        {
            // Unique index for userId
            var userIdIndexModel = new CreateIndexModel<UserNotificationPreference>(
                Builders<UserNotificationPreference>.IndexKeys.Ascending(x => x.UserId),
                new CreateIndexOptions { Unique = true }
            );

            _collection.Indexes.CreateMany(new[]
            {
                userIdIndexModel
            });

            _logger.LogInformation("User notification preference indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user notification preference indexes");
        }
    }
}
