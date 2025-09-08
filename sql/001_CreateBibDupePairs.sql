IF OBJECT_ID('dbo.BibDupePairs','U') IS NOT NULL
    DROP TABLE dbo.BibDupePairs;
GO

CREATE TABLE dbo.BibDupePairs (
    MatchType NVARCHAR(50) NOT NULL,
    MatchValue NVARCHAR(256) NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    CONSTRAINT PK_BibDupePairs PRIMARY KEY (LeftBibId, RightBibId)
);
GO
