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

CREATE TABLE IF NOT EXISTS Projects (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrganizationId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Description TEXT,
    Status TEXT NOT NULL,
    TeamId TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME
);

-- Tenant-first indexes: every query is scoped by OrganizationId.
CREATE INDEX IF NOT EXISTS IX_Projects_OrganizationId ON Projects(OrganizationId);
CREATE INDEX IF NOT EXISTS IX_Projects_OrganizationId_TeamId ON Projects(OrganizationId, TeamId);
CREATE INDEX IF NOT EXISTS IX_Projects_OrganizationId_TeamId_Status ON Projects(OrganizationId, TeamId, Status);
CREATE INDEX IF NOT EXISTS IX_Projects_OrganizationId_CreatedAt ON Projects(OrganizationId, CreatedAt);

CREATE TABLE IF NOT EXISTS Transactions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrganizationId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    Amount TEXT NOT NULL,          -- decimal(18,2); stored as TEXT by SQLite
    Currency TEXT NOT NULL,
    Description TEXT,
    CreatedAt DATETIME NOT NULL
);

-- Tenant-first indexes: every query is scoped by OrganizationId.
CREATE INDEX IF NOT EXISTS IX_Transactions_OrganizationId ON Transactions(OrganizationId);
CREATE INDEX IF NOT EXISTS IX_Transactions_OrganizationId_UserId ON Transactions(OrganizationId, UserId);
CREATE INDEX IF NOT EXISTS IX_Transactions_OrganizationId_UserId_CreatedAt ON Transactions(OrganizationId, UserId, CreatedAt);

CREATE TABLE IF NOT EXISTS SharedExpenses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrganizationId TEXT NOT NULL,
    GroupId TEXT NOT NULL,
    Description TEXT,
    Amount TEXT NOT NULL,          -- decimal(18,2); stored as TEXT by SQLite
    Currency TEXT NOT NULL,
    PaidByUserId TEXT NOT NULL,
    SplitType TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL
);

-- Tenant-first indexes: every query is scoped by OrganizationId.
CREATE INDEX IF NOT EXISTS IX_SharedExpenses_OrganizationId ON SharedExpenses(OrganizationId);
CREATE INDEX IF NOT EXISTS IX_SharedExpenses_OrganizationId_GroupId ON SharedExpenses(OrganizationId, GroupId);
CREATE INDEX IF NOT EXISTS IX_SharedExpenses_OrganizationId_GroupId_CreatedAt ON SharedExpenses(OrganizationId, GroupId, CreatedAt);

CREATE TABLE IF NOT EXISTS ExpenseParticipants (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SharedExpenseId INTEGER NOT NULL,
    UserId TEXT NOT NULL,
    ShareAmount TEXT NOT NULL,     -- decimal(18,2); stored as TEXT by SQLite
    FOREIGN KEY (SharedExpenseId) REFERENCES SharedExpenses(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_ExpenseParticipants_SharedExpenseId ON ExpenseParticipants(SharedExpenseId);
