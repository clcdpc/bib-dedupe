IF OBJECT_ID('BibDedupe.MarkNotDuplicate','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.MarkNotDuplicate;
GO

CREATE PROCEDURE BibDedupe.MarkNotDuplicate
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to record that this pair is not a duplicate
    INSERT INTO BibDedupe.PairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, ActionId)
    VALUES (SYSDATETIME(), @UserEmail, @LeftBibId, @RightBibId, 2);
END
GO
