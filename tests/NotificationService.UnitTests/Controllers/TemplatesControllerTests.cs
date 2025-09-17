using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Api.Controllers;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using Xunit;

namespace NotificationService.UnitTests.Controllers;

public class TemplatesControllerTests
{
    private readonly Mock<INotificationTemplateRepository> _mockRepository;
    private readonly Mock<ILogger<TemplatesController>> _mockLogger;
    private readonly TemplatesController _controller;

    public TemplatesControllerTests()
    {
        _mockRepository = new Mock<INotificationTemplateRepository>();
        _mockLogger = new Mock<ILogger<TemplatesController>>();
        _controller = new TemplatesController(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllTemplates_ShouldReturnOkWithTemplates()
    {
        // Arrange
        var templates = new List<NotificationTemplate>
        {
            CreateValidTemplate("template1"),
            CreateValidTemplate("template2")
        };

        _mockRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _controller.GetAllTemplates();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(templates);
    }

    [Fact]
    public async Task GetTemplate_WithValidId_ShouldReturnOkWithTemplate()
    {
        // Arrange
        var template = CreateValidTemplate("test-template");
        
        _mockRepository
            .Setup(x => x.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        var result = await _controller.GetTemplate(template.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(template);
    }

    [Fact]
    public async Task GetTemplate_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var templateId = "non-existent-id";
        
        _mockRepository
            .Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);

        // Act
        var result = await _controller.GetTemplate(templateId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTemplate_WithEmptyId_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.GetTemplate(string.Empty);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetTemplatesByChannel_ShouldReturnOkWithTemplates()
    {
        // Arrange
        var channel = NotificationChannel.Email;
        var templates = new List<NotificationTemplate>
        {
            CreateValidTemplate("email-template-1", channel),
            CreateValidTemplate("email-template-2", channel)
        };

        _mockRepository
            .Setup(x => x.GetByChannelAsync(channel, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _controller.GetTemplatesByChannel(channel);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(templates);
    }

    [Fact]
    public async Task CreateTemplate_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "new-template",
            Description = "Test template",
            Channel = NotificationChannel.Email,
            Language = "en",
            Subject = "Test Subject",
            Content = "Test content",
            Variables = new List<string> { "UserName" },
            IsActive = true
        };

        var createdTemplate = CreateValidTemplate(request.Name);

        _mockRepository
            .Setup(x => x.GetByNameAndLanguageAsync(request.Name, request.Language, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);

        _mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTemplate);

        // Act
        var result = await _controller.CreateTemplate(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.Value.Should().BeOfType<NotificationTemplate>();
    }

    [Fact]
    public async Task CreateTemplate_WithDuplicateNameAndLanguage_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "existing-template",
            Language = "en",
            Content = "Test content",
            Channel = NotificationChannel.Email
        };

        var existingTemplate = CreateValidTemplate(request.Name);

        _mockRepository
            .Setup(x => x.GetByNameAndLanguageAsync(request.Name, request.Language, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTemplate);

        // Act
        var result = await _controller.CreateTemplate(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("", "Content is required")]
    [InlineData(null, "Content is required")]
    public async Task CreateTemplate_WithInvalidContent_ShouldReturnBadRequest(string? content, string expectedError)
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "test-template",
            Language = "en",
            Content = content!,
            Channel = NotificationChannel.Email
        };

        // Act
        var result = await _controller.CreateTemplate(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo("Template content is required");
    }

    [Fact]
    public async Task UpdateTemplate_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var templateId = "test-template-id";
        var existingTemplate = CreateValidTemplate("test-template");
        existingTemplate.Id = templateId;

        var updateRequest = new UpdateTemplateRequest
        {
            Name = "updated-template",
            Content = "Updated content"
        };

        _mockRepository
            .Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTemplate);

        _mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate template) => template);

        // Act
        var result = await _controller.UpdateTemplate(templateId, updateRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var updatedTemplate = okResult!.Value as NotificationTemplate;
        updatedTemplate!.Name.Should().Be(updateRequest.Name);
        updatedTemplate.Body.Should().Be(updateRequest.Content);
    }

    [Fact]
    public async Task UpdateTemplate_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var templateId = "non-existent-id";
        var updateRequest = new UpdateTemplateRequest { Name = "updated-name" };

        _mockRepository
            .Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);

        // Act
        var result = await _controller.UpdateTemplate(templateId, updateRequest);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteTemplate_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var templateId = "test-template-id";

        _mockRepository
            .Setup(x => x.ExistsAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(x => x.DeleteAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteTemplate(templateId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTemplate_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var templateId = "non-existent-id";

        _mockRepository
            .Setup(x => x.ExistsAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteTemplate(templateId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static NotificationTemplate CreateValidTemplate(string name, NotificationChannel channel = NotificationChannel.Email)
    {
        return new NotificationTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = $"Description for {name}",
            Channel = channel,
            Language = "en",
            Subject = $"Subject for {name}",
            Body = $"Body content for {name}",
            Variables = new List<string> { "UserName", "CompanyName" },
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
