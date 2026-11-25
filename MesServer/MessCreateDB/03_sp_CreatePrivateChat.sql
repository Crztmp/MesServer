-- Процедура создания приватного чата
USE [MessDB];
GO

CREATE PROCEDURE sp_CreatePrivateChat
    @UserID NVARCHAR(128),
    @ContactUserID NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ChatID INT;
    DECLARE @ExistingChatID INT;
    DECLARE @Now DATETIME = GETUTCDATE();
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Проверяем, существует ли уже приватный чат между этими пользователями
        SELECT TOP 1 @ExistingChatID = cp1.ChatID
        FROM ChatParticipants cp1
        INNER JOIN ChatParticipants cp2 ON cp1.ChatID = cp2.ChatID
        WHERE cp1.UserId = @UserID AND cp2.UserId = @ContactUserID
        AND EXISTS (
            SELECT 1 FROM Chats c 
            WHERE c.ChatID = cp1.ChatID AND c.ChatType = 1
        );
        
        IF @ExistingChatID IS NOT NULL
        BEGIN
            -- Чат уже существует, возвращаем его ID
            SELECT @ExistingChatID AS ChatID;
            COMMIT TRANSACTION;
            RETURN;
        END;
        
        -- Создаем новый чат
        INSERT INTO Chats (ChatType, CreatedBy, CreationDate)
        VALUES (1, @UserID, @Now);
        
        SET @ChatID = SCOPE_IDENTITY();
        
        -- Добавляем участников чата
        INSERT INTO ChatParticipants (ChatID, UserId, Role, JoinDate)
        VALUES 
            (@ChatID, @UserID, 2, @Now),    -- Создатель - админ
            (@ChatID, @ContactUserID, 1, @Now); -- Участник - обычный
        
        -- Добавляем контакты
        -- Контакт для создателя чата (Added = 1)
        INSERT INTO Contacts (
            UserId, ChatType, ChatID, ContactUserID, 
            Added, AddedDate, CustomDisplayName, 
            LastMessageLoadedDate, HaveNewMessage, IsBlocked, IsIgnored, DoNotDisturb
        )
        VALUES (
            @UserID, 1, @ChatID, @ContactUserID,
            1, @Now, NULL,
            NULL, 0, 0, 0, 0
        );
        
        -- Контакт для второго участника (Added = 0)
        INSERT INTO Contacts (
            UserId, ChatType, ChatID, ContactUserID, 
            Added, AddedDate, CustomDisplayName, 
            LastMessageLoadedDate, HaveNewMessage, IsBlocked, IsIgnored, DoNotDisturb
        )
        VALUES (
            @ContactUserID, 1, @ChatID, @UserID,
            0, @Now, NULL,
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
GRANT EXECUTE ON [dbo].[sp_CreatePrivateChat] TO [MessengerService];
GO