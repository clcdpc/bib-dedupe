IF OBJECT_ID('BibDedupe.GetValidPairActions','IF') IS NOT NULL
    DROP FUNCTION BibDedupe.GetValidPairActions;
GO

CREATE FUNCTION BibDedupe.GetValidPairActions(
    @UserEmail NVARCHAR(256),
    @LeftBibId INT,
    @RightBibId INT
)
RETURNS TABLE
AS
RETURN (
    WITH ExistingDecisions AS (
        SELECT
            dq.KeptBibId,
            dq.DeletedBibId
        FROM BibDedupe.DecisionQueue dq
        WHERE dq.UserEmail = @UserEmail
          AND NOT (dq.LeftBibId = @LeftBibId AND dq.RightBibId = @RightBibId)
    )
    SELECT a.ActionId
    FROM BibDedupe.Actions a
    WHERE a.ActionId IN (1, 2, 3, 4)
      AND (
            a.ActionId NOT IN (1, 4)
        OR (
                a.ActionId = 1
            AND NOT EXISTS (SELECT 1 FROM ExistingDecisions WHERE DeletedBibId IN (@LeftBibId, @RightBibId))
            AND NOT EXISTS (SELECT 1 FROM ExistingDecisions WHERE KeptBibId = @RightBibId)
        )
        OR (
                a.ActionId = 4
            AND NOT EXISTS (SELECT 1 FROM ExistingDecisions WHERE DeletedBibId IN (@LeftBibId, @RightBibId))
            AND NOT EXISTS (SELECT 1 FROM ExistingDecisions WHERE KeptBibId = @LeftBibId)
        )
      )
);
GO
