IF OBJECT_ID('BibDedupe.Pairs','U') IS NOT NULL
    DROP TABLE BibDedupe.Pairs;
GO

CREATE TABLE BibDedupe.Pairs (
    MatchType NVARCHAR(50) NOT NULL,
    MatchValue NVARCHAR(256) NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    PrimaryMARCTOMID INT NOT NULL,
    CONSTRAINT PK_Pairs PRIMARY KEY (LeftBibId, RightBibId)
);
GO
