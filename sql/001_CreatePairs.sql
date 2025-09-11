IF OBJECT_ID('BibDedupe.Pairs','U') IS NOT NULL
    DROP TABLE BibDedupe.Pairs;
GO

CREATE TABLE BibDedupe.Pairs (
    PairId INT IDENTITY(1,1) NOT NULL,
    MatchType NVARCHAR(50) NOT NULL,
    MatchValue NVARCHAR(256) NOT NULL,
    PrimaryMARCTOMID INT NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    CONSTRAINT PK_Pairs PRIMARY KEY (PairId)
);
GO
