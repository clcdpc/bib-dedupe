IF OBJECT_ID('dbo.vwBibDupePairs','V') IS NOT NULL
    DROP VIEW dbo.vwBibDupePairs;
GO

CREATE VIEW dbo.vwBibDupePairs AS
    SELECT MatchType, MatchValue, LeftBibId, RightBibId
    FROM dbo.BibDupePairs;
GO
