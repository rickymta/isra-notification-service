// MongoDB initialization script
db = db.getSiblingDB('NotificationService');

// Create collections with proper indexing
db.createCollection('NotificationTemplates');
db.createCollection('NotificationHistories');

// Create indexes for NotificationTemplates
db.NotificationTemplates.createIndex({ "templateId": 1 }, { unique: true });
db.NotificationTemplates.createIndex({ "channel": 1 });
db.NotificationTemplates.createIndex({ "language": 1 });
db.NotificationTemplates.createIndex({ "isActive": 1 });

// Create indexes for NotificationHistories
db.NotificationHistories.createIndex({ "notificationId": 1 }, { unique: true });
db.NotificationHistories.createIndex({ "status": 1 });
db.NotificationHistories.createIndex({ "channel": 1 });
db.NotificationHistories.createIndex({ "recipientEmail": 1 });
db.NotificationHistories.createIndex({ "recipientPhone": 1 });
db.NotificationHistories.createIndex({ "sentAt": 1 });
db.NotificationHistories.createIndex({ "createdAt": 1 });

// Insert sample notification templates
db.NotificationTemplates.insertMany([
  {
    templateId: "welcome-email",
    name: "Welcome Email",
    description: "Welcome email for new users",
    channel: "Email",
    language: "en",
    subject: "Welcome to {{CompanyName}}!",
    content: "Hello {{UserName}}, welcome to our platform!",
    variables: ["UserName", "CompanyName"],
    isActive: true,
    createdAt: new Date(),
    updatedAt: new Date()
  },
  {
    templateId: "password-reset-sms",
    name: "Password Reset SMS",
    description: "SMS for password reset verification",
    channel: "SMS",
    language: "en",
    subject: "",
    content: "Your password reset code is: {{ResetCode}}. Valid for 5 minutes.",
    variables: ["ResetCode"],
    isActive: true,
    createdAt: new Date(),
    updatedAt: new Date()
  },
  {
    templateId: "order-confirmation-push",
    name: "Order Confirmation Push",
    description: "Push notification for order confirmation",
    channel: "Push",
    language: "en",
    subject: "Order Confirmed",
    content: "Your order #{{OrderNumber}} has been confirmed. Total: {{Total}}",
    variables: ["OrderNumber", "Total"],
    isActive: true,
    createdAt: new Date(),
    updatedAt: new Date()
  }
]);

print("MongoDB NotificationService database initialized successfully!");