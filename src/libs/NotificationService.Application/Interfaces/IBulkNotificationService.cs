using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

/// <summary>
/// Interface for bulk notification processing
/// </summary>
public interface IBulkNotificationService
{
    /// <summary>
    /// Submit a bulk notification request for processing
    /// </summary>
    /// <param name="request">The bulk notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The bulk notification request with assigned ID</returns>
    Task<BulkNotificationRequest> SubmitBulkNotificationAsync(BulkNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the status of a bulk notification request
    /// </summary>
    /// <param name="requestId">The bulk notification request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The bulk notification request with current status</returns>
    Task<BulkNotificationRequest?> GetBulkNotificationStatusAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated list of bulk notification requests
    /// </summary>
    /// <param name="skip">Number of items to skip</param>
    /// <param name="take">Number of items to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bulk notification requests</returns>
    Task<(List<BulkNotificationRequest> Requests, int TotalCount)> GetBulkNotificationsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a pending or processing bulk notification
    /// </summary>
    /// <param name="requestId">The bulk notification request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancelled successfully</returns>
    Task<bool> CancelBulkNotificationAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retry failed recipients in a bulk notification
    /// </summary>
    /// <param name="requestId">The bulk notification request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of recipients queued for retry</returns>
    Task<int> RetryFailedRecipientsAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed recipient status for a bulk notification
    /// </summary>
    /// <param name="requestId">The bulk notification request ID</param>
    /// <param name="status">Filter by recipient status (optional)</param>
    /// <param name="skip">Number of items to skip</param>
    /// <param name="take">Number of items to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recipients with their status</returns>
    Task<(List<BulkRecipient> Recipients, int TotalCount)> GetBulkNotificationRecipientsAsync(
        string requestId, 
        RecipientStatus? status = null, 
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for bulk notification processing engine
/// </summary>
public interface IBulkNotificationProcessor
{
    /// <summary>
    /// Process a bulk notification request
    /// </summary>
    /// <param name="request">The bulk notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing task</returns>
    Task ProcessBulkNotificationAsync(BulkNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a batch of recipients
    /// </summary>
    /// <param name="request">The bulk notification request</param>
    /// <param name="recipients">The batch of recipients to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing task</returns>
    Task ProcessBatchAsync(BulkNotificationRequest request, List<BulkRecipient> recipients, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for bulk notification repository
/// </summary>
public interface IBulkNotificationRepository
{
    /// <summary>
    /// Create a new bulk notification request
    /// </summary>
    /// <param name="request">The bulk notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created bulk notification request</returns>
    Task<BulkNotificationRequest> CreateAsync(BulkNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing bulk notification request
    /// </summary>
    /// <param name="request">The bulk notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated bulk notification request</returns>
    Task<BulkNotificationRequest> UpdateAsync(BulkNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a bulk notification request by ID
    /// </summary>
    /// <param name="id">The bulk notification request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The bulk notification request or null if not found</returns>
    Task<BulkNotificationRequest?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated list of bulk notification requests
    /// </summary>
    /// <param name="skip">Number of items to skip</param>
    /// <param name="take">Number of items to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bulk notification requests and total count</returns>
    Task<(List<BulkNotificationRequest> Requests, int TotalCount)> GetAllAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a bulk notification request
    /// </summary>
    /// <param name="id">The bulk notification request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update recipient status
    /// </summary>
    /// <param name="requestId">The bulk notification request ID</param>
    /// <param name="recipient">The recipient to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateRecipientAsync(string requestId, BulkRecipient recipient, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recipients by status
    /// </summary>
    /// <param name="requestId">The bulk notification request ID</param>
    /// <param name="status">The recipient status to filter by</param>
    /// <param name="skip">Number of items to skip</param>
    /// <param name="take">Number of items to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recipients with the specified status</returns>
    Task<(List<BulkRecipient> Recipients, int TotalCount)> GetRecipientsByStatusAsync(
        string requestId, 
        RecipientStatus status, 
        int skip = 0, 
        int take = 100, 
        CancellationToken cancellationToken = default);
}
