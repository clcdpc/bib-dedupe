IF OBJECT_ID('BibDedupe.GetPairs','IF') IS NOT NULL
    DROP FUNCTION BibDedupe.GetPairs;
GO

CREATE FUNCTION BibDedupe.GetPairs (@Top INT = 1000)
RETURNS TABLE
AS
RETURN (
    SELECT TOP (@Top) MatchType, MatchValue, LeftBibId, RightBibId, PrimaryMARCTOMID
    FROM BibDedupe.Pairs
);
GO
