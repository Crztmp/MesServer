-- =============================================
-- Настройки (измените перед выполнением)
-- =============================================
DECLARE @DatabaseName NVARCHAR(128) = 'MessDB';
DECLARE @DatabaseName1 NVARCHAR(128) = 'MessengerDB';
DECLARE @SqlLoginName NVARCHAR(128) = 'MessengerService';

-- =============================================
-- Очистка существующих объектов
-- =============================================

-- Удаление БД если существует
IF DB_ID(@DatabaseName) IS NOT NULL
BEGIN
    DECLARE @DropDB NVARCHAR(200) = 
        'ALTER DATABASE [' + @DatabaseName + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
         DROP DATABASE [' + @DatabaseName + ']';
    EXEC sp_executesql @DropDB;
    PRINT 'Удалена существующая база данных: ' + @DatabaseName;
END

IF DB_ID(@DatabaseName1) IS NOT NULL
BEGIN
    DECLARE @DropDB1 NVARCHAR(200) = 
        'ALTER DATABASE [' + @DatabaseName1 + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
         DROP DATABASE [' + @DatabaseName1 + ']';
    EXEC sp_executesql @DropDB1;
    PRINT 'Удалена существующая база данных: ' + @DatabaseName1;
END


-- Удаление логина если существует
IF EXISTS (SELECT 1 FROM master.sys.server_principals WHERE name = @SqlLoginName)
BEGIN
    DECLARE @DropLogin NVARCHAR(100) = 'DROP LOGIN [' + @SqlLoginName + ']';
    EXEC sp_executesql @DropLogin;
    PRINT 'Удален существующий логин: ' + @SqlLoginName;
END

GO
PRINT 'Скрипт успешно выполнен! База данных удалена. И юзер';