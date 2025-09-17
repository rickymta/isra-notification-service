using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using Xunit;

namespace NotificationService.UnitTests.Services;

public class NotificationProcessorTests
{
    private readonly Mock<INotificationTemplateRepository> _mockTemplateRepository;
    private readonly Mock<INotificationHistoryRepository> _mockHistoryRepository;
    private readonly Mock<INotificationChannelFactory> _mockChannelFactory;
    private readonly Mock<INotificationChannelStrategy> _mockChannelStrategy;
    private readonly Mock<ILogger<NotificationProcessor>> _mockLogger;
    private readonly NotificationProcessor _processor;

    public NotificationProcessorTests()
    {
        _mockTemplateRepository = new Mock<INotificationTemplateRepository>();
        _mockHistoryRepository = new Mock<INotificationHistoryRepository>();
        _mockChannelFactory = new Mock<INotificationChannelFactory>();
        _mockChannelStrategy = new Mock<INotificationChannelStrategy>();
        _mockLogger = new Mock<ILogger<NotificationProcessor>>();

        _processor = new NotificationProcessor(
            _mockTemplateRepository.Object,
            _mockHistoryRepository.Object,
            _mockChannelFactory.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessNotificationAsync_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        var template = CreateValidNotificationTemplate();
        var history = CreateNotificationHistory(request);

        _mockTemplateRepository
            .Setup(x => x.GetByNameAndLanguageAsync(request.TemplateName!, request.Recipient.Language, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _mockHistoryRepository
            .Setup(x => x.CreateAsync(It.IsAny<NotificationHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockHistoryRepository
            .Setup(x => x.UpdateAsync(It.IsAny<NotificationHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockChannelFactory
            .Setup(x => x.GetStrategy(request.Channel))
            .Returns(_mockChannelStrategy.Object);

        _mockChannelStrategy
            .Setup(x => x.ValidateRecipient(request.Recipient))
            .Returns(true);

        _mockChannelStrategy
            .Setup(x => x.SendAsync(It.IsAny<NotificationContent>(), request.Recipient, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Success("message-id-123"));

        // Act
        await _processor.ProcessNotificationAsync(request);

        // Assert
        _mockTemplateRepository.Verify(x => x.GetByNameAndLanguageAsync(
            request.TemplateName!, 
            request.Recipient.Language, 
            It.IsAny<CancellationToken>()), Times.Once);

        _mockHistoryRepository.Verify(x => x.CreateAsync(
            It.Is<NotificationHistory>(h => h.Status == NotificationStatus.Processing), 
            It.IsAny<CancellationToken>()), Times.Once);

        _mockChannelStrategy.Verify(x => x.SendAsync(
            It.IsAny<NotificationContent>(), 
            request.Recipient, 
            It.IsAny<CancellationToken>()), Times.Once);

        _mockHistoryRepository.Verify(x => x.UpdateAsync(
            It.Is<NotificationHistory>(h => h.Status == NotificationStatus.Sent), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessNotificationAsync_WithInvalidRecipient_ShouldThrowException()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        var template = CreateValidNotificationTemplate();

        _mockTemplateRepository
            .Setup(x => x.GetByNameAndLanguageAsync(request.TemplateName!, request.Recipient.Language, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _mockChannelFactory
            .Setup(x => x.GetStrategy(request.Channel))
            .Returns(_mockChannelStrategy.Object);

        _mockChannelStrategy
            .Setup(x => x.ValidateRecipient(request.Recipient))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _processor.ProcessNotificationAsync(request));

        exception.Message.Should().Contain($"Invalid recipient for channel {request.Channel}");
    }

    [Fact]
    public async Task ProcessNotificationAsync_WithTemplateNotFound_ShouldThrowException()
    {
        // Arrange
        var request = CreateValidNotificationRequest();

        _mockTemplateRepository
            .Setup(x => x.GetByNameAndLanguageAsync(request.TemplateName!, request.Recipient.Language, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _processor.ProcessNotificationAsync(request));

        exception.Message.Should().Contain($"Template '{request.TemplateName}' not found for language '{request.Recipient.Language}'");
    }

    [Fact]
    public async Task ProcessNotificationAsync_WithSendFailure_ShouldUpdateHistoryWithFailure()
    {
        // Arrange
        var request = CreateValidNotificationRequest();
        var template = CreateValidNotificationTemplate();
        var history = CreateNotificationHistory(request);

        _mockTemplateRepository
            .Setup(x => x.GetByNameAndLanguageAsync(request.TemplateName!, request.Recipient.Language, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _mockHistoryRepository
            .Setup(x => x.CreateAsync(It.IsAny<NotificationHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockHistoryRepository
            .Setup(x => x.UpdateAsync(It.IsAny<NotificationHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockChannelFactory
            .Setup(x => x.GetStrategy(request.Channel))
            .Returns(_mockChannelStrategy.Object);

        _mockChannelStrategy
            .Setup(x => x.ValidateRecipient(request.Recipient))
            .Returns(true);

        _mockChannelStrategy
            .Setup(x => x.SendAsync(It.IsAny<NotificationContent>(), request.Recipient, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationResult.Failure("Send failed"));

        // Act
        await _processor.ProcessNotificationAsync(request);

        // Assert
        _mockHistoryRepository.Verify(x => x.UpdateAsync(
            It.Is<NotificationHistory>(h => 
                h.Status == NotificationStatus.Failed && 
                h.ErrorMessage == "Send failed"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("{{UserName}}", "John Doe", "John Doe")]
    [InlineData("Hello {{UserName}}!", "John", "Hello John!")]
    [InlineData("{{UserName}} and {{CompanyName}}", "John", "John and {{CompanyName}}")]
    public void ReplaceVariables_ShouldReplaceCorrectly(string template, string userNameValue, string expected)
    {
        // Arrange
        var variables = new Dictionary<string, object> { { "UserName", userNameValue } };

        // Act
        var result = NotificationProcessor.ReplaceVariables(template, variables);

        // Assert
        result.Should().Be(expected);
    }

    private static NotificationRequest CreateValidNotificationRequest()
    {
        return new NotificationRequest
        {
            Id = Guid.NewGuid().ToString(),
            TemplateName = "welcome-email",
            Channel = NotificationChannel.Email,
            Recipient = new NotificationRecipient
            {
                Email = "test@example.com",
                Language = "en"
            },
            Variables = new Dictionary<string, object>
            {
                { "UserName", "John Doe" },
                { "CompanyName", "Test Corp" }
            },
            CreatedAt = DateTime.UtcNow
        };
    }

    private static NotificationTemplate CreateValidNotificationTemplate()
    {
        return new NotificationTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = "welcome-email",
            Channel = NotificationChannel.Email,
            Language = "en",
            Subject = "Welcome {{UserName}}!",
            Body = "Hello {{UserName}}, welcome to {{CompanyName}}!",
            Variables = new List<string> { "UserName", "CompanyName" },
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static NotificationHistory CreateNotificationHistory(NotificationRequest request)
    {
        return new NotificationHistory
        {
            Id = Guid.NewGuid().ToString(),
            NotificationId = request.Id,
            TemplateName = request.TemplateName,
            Channel = request.Channel,
            Status = NotificationStatus.Processing,
            Recipient = request.Recipient,
            CreatedAt = DateTime.UtcNow
        };
    }
}
