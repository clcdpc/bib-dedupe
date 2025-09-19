IF OBJECT_ID('BibDedupe.GetPairs','IF') IS NOT NULL
    DROP FUNCTION BibDedupe.GetPairs;
GO

CREATE FUNCTION BibDedupe.GetPairs (@Top INT = 1000)
RETURNS TABLE
AS
RETURN (
    SELECT TOP (@Top)
        p.PairId,
        p.PrimaryMARCTOMID,
        p.LeftBibId,
        p.RightBibId,
        MatchesJson = ISNULL(pm.MatchesJson, '[]')
    FROM BibDedupe.Pairs p
    OUTER APPLY (
        SELECT MatchType, MatchValue
        FROM BibDedupe.PairMatches m
        WHERE m.PairId = p.PairId
        ORDER BY m.MatchType, m.MatchValue
        FOR JSON PATH
    ) pm(MatchesJson)
);
GO
