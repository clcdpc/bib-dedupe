IF OBJECT_ID('dbo.BibDupePairs_KeepLeft','P') IS NOT NULL
    DROP PROCEDURE dbo.BibDupePairs_KeepLeft;
GO

CREATE PROCEDURE dbo.BibDupePairs_KeepLeft
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to keep left record and remove right record
    DELETE FROM dbo.BibDupePairs WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId;
END
GO
