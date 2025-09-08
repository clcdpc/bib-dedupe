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
    -- TODO: implement logic to merge records and remove deleted record
    DELETE FROM dbo.BibDupePairs
    WHERE (LeftBibId = @KeepBibId AND RightBibId = @DeleteBibId)
       OR (LeftBibId = @DeleteBibId AND RightBibId = @KeepBibId);
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
    DELETE FROM dbo.BibDupePairs WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId;
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
    DELETE FROM dbo.BibDupePairs WHERE LeftBibId = @LeftBibId AND RightBibId = @RightBibId;
END
GO
