IF OBJECT_ID('dbo.tvfBibDupePairs','IF') IS NOT NULL
    DROP FUNCTION dbo.tvfBibDupePairs;
GO

CREATE FUNCTION dbo.tvfBibDupePairs (@Top INT = 1000)
RETURNS TABLE
AS
RETURN (
    SELECT TOP (@Top) MatchType, MatchValue, LeftBibId, RightBibId
    FROM dbo.BibDupePairs
);
GO
