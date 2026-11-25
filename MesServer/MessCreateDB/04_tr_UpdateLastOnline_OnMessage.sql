USE [MessDB];
GO

-- Триггер для автоматического обновления поля LastOnline
CREATE TRIGGER tr_UpdateLastOnline_OnMessage
ON [Messages]
AFTER INSERT
AS
BEGIN
    UPDATE u
    SET u.LastOnline = GETDATE()
    FROM [AspNetUsers] u
    JOIN inserted i ON u.Id = i.SenderID
    WHERE u.LockoutEnabled = 0 -- Only if not locked out
END;
GO
