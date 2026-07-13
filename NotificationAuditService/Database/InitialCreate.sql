-- SQLite schema for Notification & Audit Service
-- This file is for reference; EF Core migrations handle schema creation

CREATE TABLE IF NOT EXISTS AuditLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityName TEXT NOT NULL,
    EntityId INTEGER NOT NULL,
    Action TEXT NOT NULL,
    OldValues TEXT,
    NewValues TEXT,
    UserId TEXT NOT NULL,
    UserName TEXT,
    Timestamp DATETIME NOT NULL,
    IpAddress TEXT,
    UserAgent TEXT
);

CREATE INDEX IF NOT EXISTS IX_AuditLogs_EntityName ON AuditLogs(EntityName);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_EntityName_EntityId ON AuditLogs(EntityName, EntityId);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);

CREATE TABLE IF NOT EXISTS Notifications (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Message TEXT NOT NULL,
    Type TEXT NOT NULL,
    RecipientId TEXT NOT NULL,
    RecipientEmail TEXT,
    IsRead BOOLEAN NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL,
    ReadAt DATETIME,
    RelatedEntityType TEXT,
    RelatedEntityId INTEGER
);

CREATE INDEX IF NOT EXISTS IX_Notifications_RecipientId ON Notifications(RecipientId);
CREATE INDEX IF NOT EXISTS IX_Notifications_CreatedAt ON Notifications(CreatedAt);
CREATE INDEX IF NOT EXISTS IX_Notifications_RecipientId_IsRead ON Notifications(RecipientId, IsRead);
