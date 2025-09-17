using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for managing notification templates
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TemplatesController : ControllerBase
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        INotificationTemplateRepository templateRepository,
        ILogger<TemplatesController> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all notification templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all templates</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationTemplate>), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetAllTemplates(CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _templateRepository.GetAllAsync(cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all templates");
            return StatusCode(500, "Internal server error occurred while retrieving templates");
        }
    }

    /// <summary>
    /// Get template by ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NotificationTemplate), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetTemplate(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Template ID is required");
            }

            var template = await _templateRepository.GetByIdAsync(id, cancellationToken);
            
            if (template == null)
            {
                return NotFound($"Template with ID {id} not found");
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId}", id);
            return StatusCode(500, "Internal server error occurred while retrieving template");
        }
    }

    /// <summary>
    /// Get templates by channel
    /// </summary>
    /// <param name="channel">Notification channel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of templates for the specified channel</returns>
    [HttpGet("channel/{channel}")]
    [ProducesResponseType(typeof(IEnumerable<NotificationTemplate>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetTemplatesByChannel(
        [FromRoute] NotificationChannel channel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _templateRepository.GetByChannelAsync(channel, cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates for channel {Channel}", channel);
            return StatusCode(500, "Internal server error occurred while retrieving templates");
        }
    }

    /// <summary>
    /// Get template by name and language
    /// </summary>
    /// <param name="name">Template name</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template details</returns>
    [HttpGet("name/{name}/language/{language}")]
    [ProducesResponseType(typeof(NotificationTemplate), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetTemplateByNameAndLanguage(
        [FromRoute] string name,
        [FromRoute] string language,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(language))
            {
                return BadRequest("Template name and language are required");
            }

            var template = await _templateRepository.GetByNameAndLanguageAsync(name, language, cancellationToken);
            
            if (template == null)
            {
                return NotFound($"Template with name '{name}' and language '{language}' not found");
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateName} for language {Language}", name, language);
            return StatusCode(500, "Internal server error occurred while retrieving template");
        }
    }

    /// <summary>
    /// Create a new notification template
    /// </summary>
    /// <param name="request">Template creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created template</returns>
    [HttpPost]
    [ProducesResponseType(typeof(NotificationTemplate), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = ValidateCreateRequest(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ErrorMessage);
            }

            // Check if template with same name and language already exists
            var existingTemplate = await _templateRepository.GetByNameAndLanguageAsync(
                request.Name, request.Language, cancellationToken);
            
            if (existingTemplate != null)
            {
                return BadRequest($"Template with name '{request.Name}' and language '{request.Language}' already exists");
            }

            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description ?? "",
                Channel = request.Channel,
                Language = request.Language,
                Subject = request.Subject,
                Body = request.Content,
                Variables = request.Variables ?? [],
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdTemplate = await _templateRepository.CreateAsync(template, cancellationToken);
            
            _logger.LogInformation("Template {TemplateId} created successfully", createdTemplate.Id);
            
            return CreatedAtAction(
                nameof(GetTemplate), 
                new { id = createdTemplate.Id }, 
                createdTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, "Internal server error occurred while creating template");
        }
    }

    /// <summary>
    /// Update an existing notification template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="request">Template update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated template</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(NotificationTemplate), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateTemplate(
        [FromRoute] string id,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Template ID is required");
            }

            var validationResult = ValidateUpdateRequest(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ErrorMessage);
            }

            var existingTemplate = await _templateRepository.GetByIdAsync(id, cancellationToken);
            if (existingTemplate == null)
            {
                return NotFound($"Template with ID {id} not found");
            }

            // Update template properties
            existingTemplate.Name = request.Name ?? existingTemplate.Name;
            existingTemplate.Description = request.Description ?? existingTemplate.Description;
            existingTemplate.Subject = request.Subject ?? existingTemplate.Subject;
            existingTemplate.Body = request.Content ?? existingTemplate.Body;
            existingTemplate.Variables = request.Variables ?? existingTemplate.Variables;
            existingTemplate.IsActive = request.IsActive ?? existingTemplate.IsActive;
            existingTemplate.UpdatedAt = DateTime.UtcNow;

            var updatedTemplate = await _templateRepository.UpdateAsync(existingTemplate, cancellationToken);
            
            _logger.LogInformation("Template {TemplateId} updated successfully", id);
            
            return Ok(updatedTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", id);
            return StatusCode(500, "Internal server error occurred while updating template");
        }
    }

    /// <summary>
    /// Delete a notification template
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteTemplate(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Template ID is required");
            }

            var exists = await _templateRepository.ExistsAsync(id, cancellationToken);
            if (!exists)
            {
                return NotFound($"Template with ID {id} not found");
            }

            var deleted = await _templateRepository.DeleteAsync(id, cancellationToken);
            
            if (deleted)
            {
                _logger.LogInformation("Template {TemplateId} deleted successfully", id);
                return NoContent();
            }
            else
            {
                return StatusCode(500, "Failed to delete template");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", id);
            return StatusCode(500, "Internal server error occurred while deleting template");
        }
    }

    private static ValidationResult ValidateCreateRequest(CreateTemplateRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
            return ValidationResult.Error("Template name is required");

        if (string.IsNullOrEmpty(request.Content))
            return ValidationResult.Error("Template content is required");

        if (string.IsNullOrEmpty(request.Language))
            return ValidationResult.Error("Template language is required");

        return ValidationResult.Valid();
    }

    private static ValidationResult ValidateUpdateRequest(UpdateTemplateRequest request)
    {
        if (request.Name != null && string.IsNullOrEmpty(request.Name))
            return ValidationResult.Error("Template name cannot be empty");

        if (request.Content != null && string.IsNullOrEmpty(request.Content))
            return ValidationResult.Error("Template content cannot be empty");

        return ValidationResult.Valid();
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }

        public static ValidationResult Valid() => new() { IsValid = true };
        public static ValidationResult Error(string message) => new() { IsValid = false, ErrorMessage = message };
    }
}

/// <summary>
/// Request model for creating notification templates
/// </summary>
public class CreateTemplateRequest
{
    /// <summary>
    /// Template name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Notification channel
    /// </summary>
    [Required]
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Language code (e.g., 'en', 'vi')
    /// </summary>
    [Required]
    public string Language { get; set; } = "en";

    /// <summary>
    /// Template subject (for email)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Template content/body
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// List of variable names used in template
    /// </summary>
    public List<string>? Variables { get; set; }

    /// <summary>
    /// Whether template is active
    /// </summary>
    public bool? IsActive { get; set; } = true;
}

/// <summary>
/// Request model for updating notification templates
/// </summary>
public class UpdateTemplateRequest
{
    /// <summary>
    /// Template name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Template subject (for email)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Template content/body
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// List of variable names used in template
    /// </summary>
    public List<string>? Variables { get; set; }

    /// <summary>
    /// Whether template is active
    /// </summary>
    public bool? IsActive { get; set; }
}
