-- ver 0.7
-- Добавлены Contacts.HaveNewMessage и Contacts.Added
-- =============================================
-- Настройки (измените перед выполнением)
-- =============================================
DECLARE @DatabaseName NVARCHAR(128) = 'MessDB';
DECLARE @SqlLoginName NVARCHAR(128) = 'MessengerService';
DECLARE @SqlLoginPassword NVARCHAR(128) = 'StrongPassword123!';
DECLARE @DefaultDomain NVARCHAR(128) = 'RAINBOW';

-- =============================================
-- Создание новой БД и сервисного аккаунта
-- =============================================

-- Создание БД
IF NOT EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = @DatabaseName)
BEGIN
    DECLARE @CreateDB NVARCHAR(100) = 'CREATE DATABASE [' + @DatabaseName + ']';
    EXEC sp_executesql @CreateDB;
    PRINT 'Создана база данных: ' + @DatabaseName;
END
ELSE
BEGIN
    PRINT 'База данных ' + @DatabaseName + ' уже существует';
END

-- Создание логина
IF NOT EXISTS (SELECT name FROM master.sys.server_principals WHERE name = @SqlLoginName)
BEGIN
    DECLARE @CreateLogin NVARCHAR(300) = 
        'CREATE LOGIN [' + @SqlLoginName + '] 
         WITH PASSWORD = ''' + @SqlLoginPassword + ''', 
         DEFAULT_DATABASE = [' + @DatabaseName + '], 
         CHECK_EXPIRATION = OFF, 
         CHECK_POLICY = OFF';
    EXEC sp_executesql @CreateLogin;
    PRINT 'Создан новый логин: ' + @SqlLoginName;
END
ELSE
BEGIN
    PRINT 'Логин ' + @SqlLoginName + ' уже существует';
END

-- Настройка прав доступа
DECLARE @GrantPermissions NVARCHAR(500) = 
    'USE [' + @DatabaseName + '];
     IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = ''' + @SqlLoginName + ''')
     BEGIN
         CREATE USER [' + @SqlLoginName + '] FOR LOGIN [' + @SqlLoginName + '];
     END
     ALTER ROLE [db_datareader] ADD MEMBER [' + @SqlLoginName + '];
     ALTER ROLE [db_datawriter] ADD MEMBER [' + @SqlLoginName + '];
     ALTER ROLE [db_ddladmin] ADD MEMBER [' + @SqlLoginName + '];';
EXEC sp_executesql @GrantPermissions;
PRINT 'Настроены права доступа для: ' + @SqlLoginName;

-- =============================================
-- Создание структуры таблиц для Identity
-- =============================================

-- Выполняем команды в правильном порядке с явным указанием USE
DECLARE @SQL NVARCHAR(MAX);

-- 1. Сначала создаем таблицы без внешних ключей
-- Таблица ролей (AspNetRoles)
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(128) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
)';
EXEC sp_executesql @SQL;

-- Таблица пользователей (AspNetUsers)
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(128) NOT NULL,
    [UserName] nvarchar(256) NOT NULL,
    [NormalizedUserName] nvarchar(256) NOT NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    
    -- Дополнительные поля
    [FirstName] nvarchar(50) NULL,
    [LastName] nvarchar(50) NULL,
    [AvatarURL] nvarchar(255) NULL,
    [RegistrationDate] datetime2 DEFAULT GETDATE(),
    [LastOnline] datetime2 NULL,
    [Status] nvarchar(100) DEFAULT ''В сети'',
    [PasswordChangeDate] datetime2 NULL,
    [IsActiveDirectory] bit DEFAULT 0,
    [ADGuid] uniqueidentifier NULL,
    [DomainName] nvarchar(50) NULL,
    [LastADSyncDate] datetime2 NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
)';
EXEC sp_executesql @SQL;

-- 2. Создаем индексы для основных таблиц
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL';
EXEC sp_executesql @SQL;

SET @SQL = 'USE [' + @DatabaseName + '];
CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail])';
EXEC sp_executesql @SQL;

SET @SQL = 'USE [' + @DatabaseName + '];
CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL';
EXEC sp_executesql @SQL;

-- 3. Создаем таблицы с внешними ключами
-- Таблица claims ролей (AspNetRoleClaims)
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(128) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
)';
EXEC sp_executesql @SQL;

-- Таблица claims пользователей (AspNetUserClaims)
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(128) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
)';
EXEC sp_executesql @SQL;

-- Таблица логинов пользователей (AspNetUserLogins)
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(128) NOT NULL,
    [ProviderKey] nvarchar(128) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(128) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
)';
EXEC sp_executesql @SQL;

-- Таблица ролей пользователей (AspNetUserRoles)
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(128) NOT NULL,
    [RoleId] nvarchar(128) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
)';
EXEC sp_executesql @SQL;

-- Таблица токенов пользователей (AspNetUserTokens)
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(128) NOT NULL,
    [LoginProvider] nvarchar(128) NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
)';
EXEC sp_executesql @SQL;

-- 4. Создаем кастомные таблицы
-- Таблица чатов
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [Chats] (
    [ChatID] INT IDENTITY(1,1) PRIMARY KEY,
    [ChatName] NVARCHAR(100),
    [ChatType] TINYINT NOT NULL DEFAULT 1,
	[ChatDescription] NVARCHAR(128),
    [CreatedBy] NVARCHAR(128) FOREIGN KEY REFERENCES [AspNetUsers]([Id]),
    [CreationDate] DATETIME2 DEFAULT GETDATE(),
    [LastMessageDate] DATETIME2 NULL)';
EXEC sp_executesql @SQL;

-- Таблица участников чатов
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [ChatParticipants] (
    [ParticipantID] INT IDENTITY(1,1) PRIMARY KEY,
    [ChatID] INT NOT NULL FOREIGN KEY REFERENCES [Chats]([ChatID]),
    [UserId] NVARCHAR(128) NOT NULL FOREIGN KEY REFERENCES [AspNetUsers]([Id]),
    [JoinDate] DATETIME2 DEFAULT GETDATE(),
    [Role] TINYINT DEFAULT 1,
    CONSTRAINT [UQ_ChatParticipants] UNIQUE ([ChatID], [UserId]))';
EXEC sp_executesql @SQL;

-- Таблица сообщений
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [Messages] (
    [MessageID] INT IDENTITY(1,1) PRIMARY KEY,
    [ChatID] INT NOT NULL FOREIGN KEY REFERENCES [Chats]([ChatID]),
    [SenderID] NVARCHAR(128) NOT NULL FOREIGN KEY REFERENCES [AspNetUsers]([Id]),
    [MessageText] NVARCHAR(MAX),
    [SendDate] DATETIME2 DEFAULT GETDATE(),
    [IsRead] BIT DEFAULT 0,
    [IsEdited] BIT DEFAULT 0,
    [IsDeleted] BIT DEFAULT 0,
    [AttachmentURL] NVARCHAR(255))';
EXEC sp_executesql @SQL;

-- Таблица контактов
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [Contacts] (
    [ContactID] INT IDENTITY(1,1) PRIMARY KEY,
    [UserId] NVARCHAR(128) NOT NULL FOREIGN KEY REFERENCES [AspNetUsers]([Id]),
    [ChatType] TINYINT NOT NULL DEFAULT 1,
    [ChatID] INT NULL FOREIGN KEY REFERENCES [Chats]([ChatID]),
    [ContactUserID] NVARCHAR(128) NULL FOREIGN KEY REFERENCES [AspNetUsers]([Id]),
    [Added] BIT DEFAULT 0,
    [AddedDate] DATETIME2 DEFAULT GETDATE(),
    [CustomDisplayName] NVARCHAR(100) NULL,
    [LastMessageLoadedDate] DATETIME2 NULL,
    [HaveNewMessage] BIT DEFAULT 0,
    [IsBlocked] BIT DEFAULT 0,
    [IsIgnored] BIT DEFAULT 0,
    [DoNotDisturb] BIT DEFAULT 0)';
EXEC sp_executesql @SQL;

-- Таблица уведомлений
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE TABLE [Notifications] (
    [NotificationID] INT IDENTITY(1,1) PRIMARY KEY,
    [UserId] NVARCHAR(128) NOT NULL FOREIGN KEY REFERENCES [AspNetUsers]([Id]),
    [NotificationType] TINYINT NOT NULL,
    [SourceID] INT,
    [NotificationText] NVARCHAR(255),
    [IsRead] BIT DEFAULT 0,
    [CreatedDate] DATETIME2 DEFAULT GETDATE())';
EXEC sp_executesql @SQL;

-- 5. Создаем индексы
SET @SQL = 'USE [' + @DatabaseName + '];
CREATE INDEX [IX_Messages_ChatID] ON [Messages]([ChatID])';
EXEC sp_executesql @SQL;

SET @SQL = 'USE [' + @DatabaseName + '];
CREATE INDEX [IX_Messages_SenderID] ON [Messages]([SenderID])';
EXEC sp_executesql @SQL;

SET @SQL = 'USE [' + @DatabaseName + '];
CREATE INDEX [IX_Messages_SendDate] ON [Messages]([SendDate])';
EXEC sp_executesql @SQL;

SET @SQL = 'USE [' + @DatabaseName + '];
CREATE INDEX [IX_ChatParticipants_UserID] ON [ChatParticipants]([UserId])';
EXEC sp_executesql @SQL;

SET @SQL = 'USE [' + @DatabaseName + '];
CREATE INDEX [IX_Contacts_UserID] ON [Contacts]([UserId])';
EXEC sp_executesql @SQL;

PRINT 'Созданы таблицы и индексы';

-- =============================================
-- Тестовые данные
-- =============================================

DECLARE @InsertData NVARCHAR(MAX) = '
USE [' + @DatabaseName + '];

-- Обычные пользователи (с паролями)
INSERT INTO [AspNetUsers] (
    [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
    [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumberConfirmed],
    [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount],
    [FirstName], [LastName], [RegistrationDate], [LastOnline], [PasswordChangeDate],
    [IsActiveDirectory], [DomainName]  -- Добавлены обязательные поля
)
VALUES 
    (NEWID(), ''user1'', ''USER1'', ''user1@example.com'', ''USER1@EXAMPLE.COM'', 1,
    ''AQAAAAIAAYagAAAAEJTVFTWvgnVjaiDsTnzyIwRp+Eqe4NjO5lybwsQ6vPKsKiX1CZJQt8jPysuq0tANpA=='', 
    ''5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F'', NEWID(), 0, 0, 0, 0,
    ''Иван'', ''Иванов'', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(),
    0, ''''),  -- Добавлены значения по умолчанию
    
    (NEWID(), ''user2'', ''USER2'', ''user2@example.com'', ''USER2@EXAMPLE.COM'', 1,
    ''AQAAAAIAAYagAAAAEJTVFTWvgnVjaiDsTnzyIwRp+Eqe4NjO5lybwsQ6vPKsKiX1CZJQt8jPysuq0tANpA=='', 
    ''5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F'', NEWID(), 0, 0, 0, 0,
    ''Петр'', ''Петров'', DATEADD(day, -30, GETUTCDATE()), DATEADD(day, -1, GETUTCDATE()), DATEADD(day, -30, GETUTCDATE()),
    0, '''');  -- Добавлены значения по умолчанию


-- AD-пользователи (без паролей)
INSERT INTO [AspNetUsers] (
    [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
    [SecurityStamp], [ConcurrencyStamp], [PhoneNumberConfirmed],
    [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount],
    [FirstName], [LastName], [RegistrationDate], [LastOnline],
    [IsActiveDirectory], [ADGuid], [DomainName], [LastADSyncDate]
)
VALUES 
    (NEWID(), ''' + @DefaultDomain + '\aduser1'', UPPER(''' + @DefaultDomain + '\ADUSER1''), ''aduser1@company.com'', ''ADUSER1@COMPANY.COM'', 1,
    ''5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F'', NEWID(), 0, 0, 0, 0,
    ''Алексей'', ''Смирнов'', GETUTCDATE(), GETUTCDATE(),
    1, NEWID(), ''' + @DefaultDomain + ''', GETUTCDATE()),
    
    (NEWID(), ''' + @DefaultDomain + '\aduser2'', UPPER(''' + @DefaultDomain + '\ADUSER2''), ''aduser2@company.com'', ''ADUSER2@COMPANY.COM'', 1,
    ''5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F5Y6F'', NEWID(), 0, 0, 0, 0,
    ''Ольга'', ''Кузнецова'', GETUTCDATE(), GETUTCDATE(),
    1, NEWID(), ''' + @DefaultDomain + ''', GETUTCDATE());

-- Тестовые чаты
DECLARE @User1Id NVARCHAR(128) = (SELECT TOP 1 [Id] FROM [AspNetUsers] WHERE [UserName] = ''user1'');
DECLARE @AdUser1Id NVARCHAR(128) = (SELECT TOP 1 [Id] FROM [AspNetUsers] WHERE [UserName] = ''' + @DefaultDomain + '\aduser1'');

INSERT INTO [Chats] ([ChatType], [CreatedBy], [ChatName])
VALUES 
    (1, @User1Id, NULL),
    (2, @AdUser1Id, ''Общий чат отдела'');

-- Участники чатов
DECLARE @User2Id NVARCHAR(128) = (SELECT TOP 1 [Id] FROM [AspNetUsers] WHERE [UserName] = ''user2'');
DECLARE @AdUser2Id NVARCHAR(128) = (SELECT TOP 1 [Id] FROM [AspNetUsers] WHERE [UserName] = ''' + @DefaultDomain + '\aduser2'');
DECLARE @Chat1Id INT = (SELECT TOP 1 [ChatID] FROM [Chats] WHERE [ChatName] IS NULL);
DECLARE @Chat2Id INT = (SELECT TOP 1 [ChatID] FROM [Chats] WHERE [ChatName] = ''Общий чат отдела'');

INSERT INTO [ChatParticipants] ([ChatID], [UserId], [Role])
VALUES 
    (@Chat1Id, @User1Id, 1), (@Chat1Id, @User2Id, 1),
    (@Chat2Id, @User1Id, 1), (@Chat2Id, @AdUser1Id, 2), (@Chat2Id, @AdUser2Id, 1);

-- Тестовые сообщения
INSERT INTO [Messages] ([ChatID], [SenderID], [MessageText])
VALUES 
    (@Chat1Id, @User1Id, ''Привет, как дела?''),
    (@Chat1Id, @User2Id, ''Привет! Все отлично!''),
    (@Chat2Id, @AdUser1Id, ''Добро пожаловать в общий чат отдела!''),
    (@Chat2Id, @User1Id, ''Спасибо! Рад быть здесь!'');

-- Контакты
INSERT INTO [Contacts] ([UserId], [ContactUserID], [CustomDisplayName], [Added], [AddedDate])
VALUES 
    (@User1Id, @User2Id, ''Петр Петров'', 1, GETUTCDATE()),
    (@User1Id, @AdUser1Id, ''Алексей Смирнов'', 1, GETUTCDATE()),
    (@User2Id, @User1Id, ''Иван Иванов'', 1, GETUTCDATE()),
    (@AdUser1Id, @AdUser2Id, ''Ольга Кузнецова'', 1, GETUTCDATE());
';
EXEC sp_executesql @InsertData;
PRINT 'Добавлены тестовые данные';

PRINT 'Скрипт успешно выполнен! База данных готова к использованию.';