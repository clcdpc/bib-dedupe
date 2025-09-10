IF OBJECT_ID('BibDedupe.KeepBoth','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.KeepBoth;
GO

CREATE PROCEDURE BibDedupe.KeepBoth
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to keep both records as separate
    INSERT INTO BibDedupe.PairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, ActionId)
    VALUES (SYSDATETIME(), @UserEmail, @LeftBibId, @RightBibId, 3);
END
GO
