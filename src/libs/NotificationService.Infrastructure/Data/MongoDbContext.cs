using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Data;

/// <summary>
/// MongoDB database context
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _settings;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        _settings = settings.Value;
        
        var client = new MongoClient(_settings.ConnectionString);
        _database = client.GetDatabase(_settings.DatabaseName);
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<T> GetCollection<T>(string? collectionName = null)
    {
        return _database.GetCollection<T>(collectionName ?? typeof(T).Name.ToLowerInvariant());
    }

    // Collections
    public IMongoCollection<NotificationHistory> NotificationHistories => GetCollection<NotificationHistory>("notification_histories");
    public IMongoCollection<NotificationTemplate> NotificationTemplates => GetCollection<NotificationTemplate>("notification_templates");
    public IMongoCollection<InAppNotification> InAppNotifications => GetCollection<InAppNotification>("inapp_notifications");
}