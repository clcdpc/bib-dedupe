IF OBJECT_ID('BibDedupe.IsAuthorizedUser','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.IsAuthorizedUser;
GO

CREATE PROCEDURE BibDedupe.IsAuthorizedUser
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- Replace 'YourUserTable' with the actual table that stores user emails
    SELECT COUNT(1)
    FROM YourUserTable
    WHERE Email = @Email;
END
GO
