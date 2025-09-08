IF OBJECT_ID('dbo.BibDupePairs_Skip','P') IS NOT NULL
    DROP PROCEDURE dbo.BibDupePairs_Skip;
GO

CREATE PROCEDURE dbo.BibDupePairs_Skip
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to skip processing this pair
    DELETE FROM dbo.BibDupePairs WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId;
END
GO
