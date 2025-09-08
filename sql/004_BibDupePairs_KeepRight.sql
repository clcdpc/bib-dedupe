IF OBJECT_ID('dbo.BibDupePairs_KeepRight','P') IS NOT NULL
    DROP PROCEDURE dbo.BibDupePairs_KeepRight;
GO

CREATE PROCEDURE dbo.BibDupePairs_KeepRight
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to keep right record and remove left record
    DELETE FROM dbo.BibDupePairs WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId;
END
GO
