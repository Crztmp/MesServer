-- Процедура создания публичного чата
USE [MessDB];
GO

CREATE PROCEDURE sp_CreatePublicChat
    @UserID NVARCHAR(128),
    @ChatName NVARCHAR(100),
    @ChatDescription NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ChatID INT;
    DECLARE @Now DATETIME = GETUTCDATE();
    
    BEGIN TRY
        BEGIN TRANSACTION;
        -- Создаем новый публичный чат (ChatType = 2)
		INSERT INTO Chats (ChatType, ChatName, ChatDescription, CreatedBy, CreationDate)
		VALUES (2, @ChatName, @ChatDescription, @UserID, @Now);
        
        SET @ChatID = SCOPE_IDENTITY();
        
        -- Добавляем участников чата
        INSERT INTO ChatParticipants (ChatID, UserId, Role, JoinDate)
        VALUES 
            (@ChatID, @UserID, 2, @Now);    -- Создатель - админ
        
        -- Добавляем контакт
        INSERT INTO Contacts (
            UserId, ChatType, ChatID, ContactUserID, 
            Added, AddedDate, CustomDisplayName, 
            LastMessageLoadedDate, HaveNewMessage, IsBlocked, IsIgnored, DoNotDisturb
        )
        VALUES (
            @UserID, 2, @ChatID, NULL, -- ContactUserID NULL для публичного чата
            1, @Now, @ChatName, -- Используем имя чата как CustomDisplayName
            NULL, 0, 0, 0, 0
        );
        
        -- Контакт для второго участника (Added = 0)
        INSERT INTO Contacts (
            UserId, ChatType, ChatID, ContactUserID, 
            Added, AddedDate, CustomDisplayName, 
            LastMessageLoadedDate, HaveNewMessage, IsBlocked, IsIgnored, DoNotDisturb
        )
        VALUES (
            @UserID, 2, @ChatID, NULL, -- ContactUserID NULL для публичного чата
            1, @Now, @ChatName, -- Используем имя чата как CustomDisplayName
            NULL, 0, 0, 0, 0
        );
        
        -- Возвращаем ID созданного чата
        SELECT @ChatID AS ChatID;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
	SELECT @ChatID AS ChatID;
END;
GO

-- Предоставляем права на выполнение процедуры
GRANT EXECUTE ON [dbo].[sp_CreatePublicChat] TO [MessengerService];
GO