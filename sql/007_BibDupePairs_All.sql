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

IF OBJECT_ID('dbo.BibDupePairDecisions','U') IS NOT NULL
    DROP TABLE dbo.BibDupePairDecisions;
GO
CREATE TABLE dbo.BibDupePairDecisions (
    DecisionTimestamp DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UserEmail NVARCHAR(256) NOT NULL,
    KeptBibId INT NOT NULL,
    DeletedBibId INT NULL,
    Action INT NOT NULL -- 1 = merge, 2 = skip, 3 = keep both
);
GO

IF OBJECT_ID('dbo.DecisionQueue','U') IS NOT NULL
    DROP TABLE dbo.DecisionQueue;
GO
CREATE TABLE dbo.DecisionQueue (
    UserEmail NVARCHAR(256) NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    Action INT NOT NULL,
    CONSTRAINT PK_DecisionQueue PRIMARY KEY (UserEmail, LeftBibId, RightBibId)
);
GO

IF OBJECT_ID('dbo.vwBibDupePairs','V') IS NOT NULL
    DROP VIEW dbo.vwBibDupePairs;
GO
CREATE VIEW dbo.vwBibDupePairs AS
    SELECT MatchType, MatchValue, LeftBibId, RightBibId
    FROM dbo.BibDupePairs;
GO

IF OBJECT_ID('dbo.MergeBibPair','P') IS NOT NULL
    DROP PROCEDURE dbo.MergeBibPair;
GO
CREATE PROCEDURE dbo.MergeBibPair
    @KeepBibId INT,
    @DeleteBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to merge records
    INSERT INTO dbo.BibDupePairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, Action)
    VALUES (SYSDATETIME(), @UserEmail, @KeepBibId, @DeleteBibId, 1);
END
GO

IF OBJECT_ID('dbo.BibDupePairs_KeepBoth','P') IS NOT NULL
    DROP PROCEDURE dbo.BibDupePairs_KeepBoth;
GO
CREATE PROCEDURE dbo.BibDupePairs_KeepBoth
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to keep both records as separate
    INSERT INTO dbo.BibDupePairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, Action)
    VALUES (SYSDATETIME(), @UserEmail, @LeftBibId, @RightBibId, 3);
END
GO

IF OBJECT_ID('dbo.BibDupePairs_Skip','P') IS NOT NULL
    DROP PROCEDURE dbo.BibDupePairs_Skip;
GO
CREATE PROCEDURE dbo.BibDupePairs_Skip
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to skip processing this pair
    INSERT INTO dbo.BibDupePairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, Action)
    VALUES (SYSDATETIME(), @UserEmail, @LeftBibId, @RightBibId, 2);
END
GO
