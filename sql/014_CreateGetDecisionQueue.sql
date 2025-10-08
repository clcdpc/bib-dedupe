IF OBJECT_ID('BibDedupe.GetDecisionQueue','IF') IS NOT NULL
    DROP FUNCTION BibDedupe.GetDecisionQueue;
GO

CREATE FUNCTION [BibDedupe].[GetDecisionQueue](
    @UserEmail NVARCHAR(256),
    @TomId INT = NULL,
    @MatchType NVARCHAR(50) = NULL,
    @HasHolds BIT = NULL
)
RETURNS TABLE
AS
RETURN (
    SELECT
        dq.UserEmail,
        dq.LeftBibId,
        dq.RightBibId,
        dq.ActionId,
        p.PrimaryMarcTomId,
        p.LeftTitle,
        p.LeftAuthor,
        p.RightTitle,
        p.RightAuthor,
        p.TOM,
        p.MatchesJson
    FROM BibDedupe.DecisionQueue dq
    OUTER APPLY (
        SELECT
            gp.PrimaryMARCTOMID AS PrimaryMarcTomId,
            gp.LeftTitle,
            gp.LeftAuthor,
            gp.RightTitle,
            gp.RightAuthor,
            gp.TOM,
            gp.MatchesJson
        FROM BibDedupe.GetPairs(2147483647, NULL, @TomId, @MatchType, @HasHolds) gp
        WHERE gp.LeftBibId = dq.LeftBibId
          AND gp.RightBibId = dq.RightBibId
    ) p
    WHERE dq.UserEmail = @UserEmail
);
GO
