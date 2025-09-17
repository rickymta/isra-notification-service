using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Repository interface for notification templates
/// </summary>
public interface INotificationTemplateRepository
{
    /// <summary>
    /// Get template by ID
    /// </summary>
    Task<NotificationTemplate?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get template by name and language
    /// </summary>
    Task<NotificationTemplate?> GetByNameAndLanguageAsync(string name, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active templates for a specific channel
    /// </summary>
    Task<IEnumerable<NotificationTemplate>> GetByChannelAsync(Domain.Enums.NotificationChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all templates
    /// </summary>
    Task<IEnumerable<NotificationTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new template
    /// </summary>
    Task<NotificationTemplate> CreateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing template
    /// </summary>
    Task<NotificationTemplate> UpdateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a template
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if template exists
    /// </summary>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}