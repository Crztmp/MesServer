USE [MessDB];
GO

-- Триггер для отслеживания изменения пароля
CREATE TRIGGER tr_UserPasswordChange ON [AspNetUsers]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF UPDATE(PasswordHash)
    BEGIN
        UPDATE u
        SET PasswordChangeDate = GETDATE()
        FROM [AspNetUsers] u
        INNER JOIN inserted i ON u.Id = i.Id
        WHERE u.PasswordHash IS NOT NULL;
    END
END;
GO

