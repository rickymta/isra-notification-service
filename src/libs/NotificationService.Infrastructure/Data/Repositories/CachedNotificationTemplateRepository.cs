using NotificationService.Application.Interfaces;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Data.Repositories;

/// <summary>
/// Cached wrapper for notification template repository
/// </summary>
public class CachedNotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly INotificationTemplateRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly RedisSettings _redisSettings;
    
    private const string TEMPLATE_BY_ID_KEY = "template:id:{0}";
    private const string TEMPLATE_BY_NAME_LANG_KEY = "template:name:{0}:lang:{1}";
    private const string TEMPLATES_BY_CHANNEL_KEY = "templates:channel:{0}";
    private const string ALL_TEMPLATES_KEY = "templates:all";

    public CachedNotificationTemplateRepository(
        INotificationTemplateRepository repository,
        ICacheService cacheService,
        IOptions<RedisSettings> redisSettings)
    {
        _repository = repository;
        _cacheService = cacheService;
        _redisSettings = redisSettings.Value;
    }

    public async Task<NotificationTemplate?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TEMPLATE_BY_ID_KEY, id);
        var cached = await _cacheService.GetAsync<NotificationTemplate>(cacheKey, cancellationToken);
        
        if (cached != null)
            return cached;

        var template = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (template != null)
        {
            var expiration = TimeSpan.FromMinutes(_redisSettings.TemplateCacheExpirationMinutes);
            await _cacheService.SetAsync(cacheKey, template, expiration, cancellationToken);
        }
        
        return template;
    }

    public async Task<NotificationTemplate?> GetByNameAndLanguageAsync(string name, string language, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TEMPLATE_BY_NAME_LANG_KEY, name, language);
        var cached = await _cacheService.GetAsync<NotificationTemplate>(cacheKey, cancellationToken);
        
        if (cached != null)
            return cached;

        var template = await _repository.GetByNameAndLanguageAsync(name, language, cancellationToken);
        
        if (template != null)
        {
            var expiration = TimeSpan.FromMinutes(_redisSettings.TemplateCacheExpirationMinutes);
            await _cacheService.SetAsync(cacheKey, template, expiration, cancellationToken);
            
            // Also cache by ID
            var idCacheKey = string.Format(TEMPLATE_BY_ID_KEY, template.Id);
            await _cacheService.SetAsync(idCacheKey, template, expiration, cancellationToken);
        }
        
        return template;
    }

    public async Task<IEnumerable<NotificationTemplate>> GetByChannelAsync(NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TEMPLATES_BY_CHANNEL_KEY, channel);
        var cached = await _cacheService.GetAsync<IEnumerable<NotificationTemplate>>(cacheKey, cancellationToken);
        
        if (cached != null)
            return cached;

        var templates = await _repository.GetByChannelAsync(channel, cancellationToken);
        
        if (templates.Any())
        {
            var expiration = TimeSpan.FromMinutes(_redisSettings.TemplateCacheExpirationMinutes);
            await _cacheService.SetAsync(cacheKey, templates, expiration, cancellationToken);
            
            // Cache individual templates as well
            var tasks = templates.Select(async template =>
            {
                var idCacheKey = string.Format(TEMPLATE_BY_ID_KEY, template.Id);
                await _cacheService.SetAsync(idCacheKey, template, expiration, cancellationToken);
            });
            
            await Task.WhenAll(tasks);
        }
        
        return templates;
    }

    public async Task<IEnumerable<NotificationTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<IEnumerable<NotificationTemplate>>(ALL_TEMPLATES_KEY, cancellationToken);
        
        if (cached != null)
            return cached;

        var templates = await _repository.GetAllAsync(cancellationToken);
        
        if (templates.Any())
        {
            var expiration = TimeSpan.FromMinutes(_redisSettings.TemplateCacheExpirationMinutes);
            await _cacheService.SetAsync(ALL_TEMPLATES_KEY, templates, expiration, cancellationToken);
        }
        
        return templates;
    }

    public async Task<NotificationTemplate> CreateAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        var result = await _repository.CreateAsync(template, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateCaches(result, cancellationToken);
        
        return result;
    }

    public async Task<NotificationTemplate> UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
    {
        var result = await _repository.UpdateAsync(template, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateCaches(result, cancellationToken);
        
        return result;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        // Get template before deletion to know what to invalidate
        var template = await _repository.GetByIdAsync(id, cancellationToken);
        
        var result = await _repository.DeleteAsync(id, cancellationToken);
        
        if (result && template != null)
        {
            await InvalidateCaches(template, cancellationToken);
        }
        
        return result;
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = string.Format(TEMPLATE_BY_ID_KEY, id);
        var cached = await _cacheService.ExistsAsync(cacheKey, cancellationToken);
        
        if (cached)
            return true;

        return await _repository.ExistsAsync(id, cancellationToken);
    }

    private async Task InvalidateCaches(NotificationTemplate template, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>
        {
            // Remove specific template caches
            _cacheService.RemoveAsync(string.Format(TEMPLATE_BY_ID_KEY, template.Id), cancellationToken),
            _cacheService.RemoveAsync(string.Format(TEMPLATE_BY_NAME_LANG_KEY, template.Name, template.Language), cancellationToken),
            
            // Remove collection caches
            _cacheService.RemoveAsync(string.Format(TEMPLATES_BY_CHANNEL_KEY, template.Channel), cancellationToken),
            _cacheService.RemoveAsync(ALL_TEMPLATES_KEY, cancellationToken)
        };
        
        await Task.WhenAll(tasks);
    }
}