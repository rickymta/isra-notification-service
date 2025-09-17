using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Data.Repositories;

/// <summary>
/// MongoDB implementation of the notification template repository
/// </summary>
public class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly IMongoCollection<NotificationTemplate> _collection;

    public NotificationTemplateRepository(IMongoDatabase database, IOptions<MongoDbSettings> settings)
    {
        _collection = database.GetCollection<NotificationTemplate>(settings.Value.TemplatesCollection);
        
        // Create indexes for better performance
        CreateIndexes();
    }

    public async Task<NotificationTemplate?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(t => t.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<NotificationTemplate?> GetByNameAndLanguageAsync(string name, string language, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(t => t.Name == name && t.Language == language && t.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationTemplate>> GetByChannelAsync(NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        var templates = await _collection
            .Find(t => t.Channel == channel && t.IsActive)
            .ToListAsync(cancellationToken);
        
        return templates;
    }

    public async Task<IEnumerable<NotificationTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _collection
            .Find(_ => true)
            .ToListAsync(cancellationToken);
        
        return templates;
    }

    public async Task<NotificationTemplate> CreateAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        
        await _collection.InsertOneAsync(template, null, cancellationToken);
        return template;
    }

    public async Task<NotificationTemplate> UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        template.UpdatedAt = DateTime.UtcNow;
        
        await _collection.ReplaceOneAsync(
            t => t.Id == template.Id,
            template,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        
        return template;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteOneAsync(
            t => t.Id == id,
            cancellationToken);
        
        return result.DeletedCount > 0;
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        var count = await _collection.CountDocumentsAsync(
            t => t.Id == id,
            null,
            cancellationToken);
        
        return count > 0;
    }

    private void CreateIndexes()
    {
        var indexKeys = Builders<NotificationTemplate>.IndexKeys
            .Ascending(t => t.Name)
            .Ascending(t => t.Language);
        
        var indexModel = new CreateIndexModel<NotificationTemplate>(indexKeys);
        _collection.Indexes.CreateOne(indexModel);

        var channelIndexKeys = Builders<NotificationTemplate>.IndexKeys
            .Ascending(t => t.Channel)
            .Ascending(t => t.IsActive);
        
        var channelIndexModel = new CreateIndexModel<NotificationTemplate>(channelIndexKeys);
        _collection.Indexes.CreateOne(channelIndexModel);
    }
}