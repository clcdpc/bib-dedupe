IF OBJECT_ID('dbo.BibDupePairs_KeepBoth','P') IS NOT NULL
    DROP PROCEDURE dbo.BibDupePairs_KeepBoth;
GO

CREATE PROCEDURE dbo.BibDupePairs_KeepBoth
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to keep both records as separate
    DELETE FROM dbo.BibDupePairs WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId;
END
GO
