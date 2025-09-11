IF OBJECT_ID('BibDedupe.MergePair','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.MergePair;
GO

CREATE PROCEDURE BibDedupe.MergePair
    @KeepBibId INT,
    @DeleteBibId INT,
    @UserEmail NVARCHAR(256),
    @ActionId INT
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to merge records
    INSERT INTO BibDedupe.PairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, ActionId)
    VALUES (SYSDATETIME(), @UserEmail, @KeepBibId, @DeleteBibId, @ActionId);
END
GO
