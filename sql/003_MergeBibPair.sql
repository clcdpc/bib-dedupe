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
    -- TODO: implement logic to merge records
    INSERT INTO dbo.BibDupePairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId)
    VALUES (SYSDATETIME(), @UserEmail, @KeepBibId, @DeleteBibId);
END
GO
