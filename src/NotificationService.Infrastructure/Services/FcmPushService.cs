using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Settings;
using NotificationService.Domain.Entities;
using System.Text.RegularExpressions;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Firebase Cloud Messaging implementation of push notification service
/// </summary>
public class FcmPushService : IPushService
{
    private readonly PushSettings _settings;
    private readonly ILogger<FcmPushService> _logger;
    private readonly FirebaseMessaging _messaging;
    private static readonly Regex TokenRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    public FcmPushService(
        IOptions<PushSettings> settings,
        ILogger<FcmPushService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        InitializeFirebase();
        _messaging = FirebaseMessaging.DefaultInstance;
    }

    public async Task<NotificationResult> SendPushAsync(NotificationContent content, NotificationRecipient recipient, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidateRecipient(recipient))
            {
                return NotificationResult.Failure("Invalid push notification recipient");
            }

            var messageBuilder = new Message()
            {
                Token = recipient.DeviceToken,
                Notification = new Notification()
                {
                    Title = content.Subject ?? "Notification",
                    Body = content.Body
                },
                Data = new Dictionary<string, string>()
            };

            // Add variables as data
            var messageData = new Dictionary<string, string>();
            foreach (var variable in content.Variables)
            {
                messageData[$"var_{variable.Key}"] = variable.Value;
            }

            // Add metadata
            if (!string.IsNullOrEmpty(recipient.UserId))
            {
                messageData["user_id"] = recipient.UserId;
            }
            
            messageData["language"] = recipient.Language;
            
            if (!string.IsNullOrEmpty(recipient.TimeZone))
            {
                messageData["timezone"] = recipient.TimeZone;
            }

            // Set the data after building it
            messageBuilder.Data = messageData;

            // Configure platform-specific options
            messageBuilder.Android = new AndroidConfig()
            {
                Priority = Priority.High,
                Notification = new AndroidNotification()
                {
                    Title = content.Subject ?? "Notification",
                    Body = content.Body,
                    Icon = "notification_icon",
                    Color = "#FF6B6B"
                }
            };

            messageBuilder.Apns = new ApnsConfig()
            {
                Aps = new Aps()
                {
                    Alert = new ApsAlert()
                    {
                        Title = content.Subject ?? "Notification",
                        Body = content.Body
                    },
                    Badge = 1,
                    Sound = "default"
                }
            };

            _logger.LogInformation("Sending push notification to device token {DeviceToken}", 
                MaskDeviceToken(recipient.DeviceToken!));

            var response = await _messaging.SendAsync(messageBuilder, cancellationToken);

            _logger.LogInformation("Push notification sent successfully. MessageId: {MessageId}", response);

            var result = NotificationResult.Success(response);
            result.Metadata["platform"] = "fcm";
            
            return result;
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Firebase Messaging error while sending push notification to {DeviceToken}. ErrorCode: {ErrorCode}",
                MaskDeviceToken(recipient.DeviceToken!), ex.MessagingErrorCode);

            var errorMessage = ex.MessagingErrorCode switch
            {
                MessagingErrorCode.InvalidArgument => "Invalid device token or message format",
                MessagingErrorCode.Unregistered => "Device token is no longer valid",
                MessagingErrorCode.SenderIdMismatch => "Sender ID mismatch",
                MessagingErrorCode.QuotaExceeded => "FCM quota exceeded",
                MessagingErrorCode.Unavailable => "FCM service temporarily unavailable",
                _ => $"FCM error: {ex.MessagingErrorCode}"
            };

            return NotificationResult.Failure(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending push notification to {DeviceToken}", 
                MaskDeviceToken(recipient.DeviceToken!));
            return NotificationResult.Failure($"Exception: {ex.Message}");
        }
    }

    public bool ValidateRecipient(NotificationRecipient recipient)
    {
        if (string.IsNullOrEmpty(recipient.DeviceToken))
        {
            _logger.LogWarning("Push recipient validation failed: Device token is null or empty");
            return false;
        }

        if (recipient.DeviceToken.Length < 10 || recipient.DeviceToken.Length > 4096)
        {
            _logger.LogWarning("Push recipient validation failed: Invalid device token length");
            return false;
        }

        return true;
    }

    private void InitializeFirebase()
    {
        if (FirebaseApp.DefaultInstance != null)
        {
            return; // Already initialized
        }

        try
        {
            GoogleCredential credential;

            if (!string.IsNullOrEmpty(_settings.FcmServiceAccountPath) && File.Exists(_settings.FcmServiceAccountPath))
            {
                // Use service account file
                credential = GoogleCredential.FromFile(_settings.FcmServiceAccountPath);
                _logger.LogInformation("Firebase initialized with service account file");
            }
            else if (!string.IsNullOrEmpty(_settings.FcmServerKey))
            {
                // For legacy server key (not recommended for new projects)
                throw new InvalidOperationException("FCM Server Key authentication is deprecated. Please use service account JSON file.");
            }
            else
            {
                // Try default credentials (for deployed environments)
                credential = GoogleCredential.GetApplicationDefault();
                _logger.LogInformation("Firebase initialized with application default credentials");
            }

            FirebaseApp.Create(new AppOptions()
            {
                Credential = credential,
                ProjectId = _settings.FcmProjectId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase");
            throw new InvalidOperationException("Firebase initialization failed", ex);
        }
    }

    private static string MaskDeviceToken(string deviceToken)
    {
        if (string.IsNullOrEmpty(deviceToken) || deviceToken.Length < 10)
            return "***";
        
        return deviceToken.Substring(0, 6) + "***" + deviceToken.Substring(deviceToken.Length - 4);
    }
}