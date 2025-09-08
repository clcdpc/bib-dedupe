IF OBJECT_ID('dbo.MergeBibPair','P') IS NOT NULL
    DROP PROCEDURE dbo.MergeBibPair;
GO

CREATE PROCEDURE dbo.MergeBibPair
    @KeepBibId INT,
    @DeleteBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to merge records and remove deleted record
    DELETE FROM dbo.BibDupePairs
    WHERE (LeftBibId = @KeepBibId AND RightBibId = @DeleteBibId)
       OR (LeftBibId = @DeleteBibId AND RightBibId = @KeepBibId);
END
GO
