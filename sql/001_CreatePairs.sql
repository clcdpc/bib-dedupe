IF OBJECT_ID('BibDedupe.PairMatches','U') IS NOT NULL
    DROP TABLE BibDedupe.PairMatches;
GO
IF OBJECT_ID('BibDedupe.Pairs','U') IS NOT NULL
    DROP TABLE BibDedupe.Pairs;
GO

CREATE TABLE BibDedupe.Pairs (
    PairId INT IDENTITY(1,1) NOT NULL,
    PrimaryMARCTOMID INT NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    LeftTitle NVARCHAR(512) NOT NULL,
    LeftAuthor NVARCHAR(256) NULL,
    RightTitle NVARCHAR(512) NOT NULL,
    RightAuthor NVARCHAR(256) NULL,
    CONSTRAINT PK_Pairs PRIMARY KEY (PairId),
    CONSTRAINT UQ_Pairs_LeftRight UNIQUE (LeftBibId, RightBibId)
);
GO

CREATE TABLE BibDedupe.PairMatches (
    PairMatchId INT IDENTITY(1,1) NOT NULL,
    PairId INT NOT NULL,
    MatchType NVARCHAR(50) NOT NULL,
    MatchValue NVARCHAR(256) NOT NULL,
    CONSTRAINT PK_PairMatches PRIMARY KEY (PairMatchId),
    CONSTRAINT FK_PairMatches_Pairs FOREIGN KEY (PairId)
        REFERENCES BibDedupe.Pairs(PairId)
        ON DELETE CASCADE,
    CONSTRAINT UQ_PairMatches UNIQUE (PairId, MatchType, MatchValue)
);
GO
