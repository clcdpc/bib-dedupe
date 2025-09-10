IF SCHEMA_ID('BibDedupe') IS NULL
    EXEC('CREATE SCHEMA BibDedupe');
GO

IF OBJECT_ID('BibDedupe.Pairs','U') IS NOT NULL
    DROP TABLE BibDedupe.Pairs;
GO
CREATE TABLE BibDedupe.Pairs (
    MatchType NVARCHAR(50) NOT NULL,
    MatchValue NVARCHAR(256) NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    CONSTRAINT PK_Pairs PRIMARY KEY (LeftBibId, RightBibId)
);
GO

IF OBJECT_ID('BibDedupe.Actions','U') IS NOT NULL
    DROP TABLE BibDedupe.Actions;
GO
CREATE TABLE BibDedupe.Actions (
    ActionId INT NOT NULL PRIMARY KEY,
    ActionName NVARCHAR(50) NOT NULL
);
INSERT INTO BibDedupe.Actions (ActionId, ActionName)
VALUES (1, 'keep left'), (2, 'keep both'), (3, 'skip'), (4, 'keep right');
GO

IF OBJECT_ID('BibDedupe.PairDecisions','U') IS NOT NULL
    DROP TABLE BibDedupe.PairDecisions;
GO
CREATE TABLE BibDedupe.PairDecisions (
    DecisionTimestamp DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UserEmail NVARCHAR(256) NOT NULL,
    KeptBibId INT NOT NULL,
    DeletedBibId INT NULL,
    ActionId INT NOT NULL,
    CONSTRAINT FK_PairDecisions_ActionId FOREIGN KEY (ActionId)
        REFERENCES BibDedupe.Actions(ActionId)
);
GO

IF OBJECT_ID('BibDedupe.Queue','U') IS NOT NULL
    DROP TABLE BibDedupe.Queue;
GO
CREATE TABLE BibDedupe.Queue (
    UserEmail NVARCHAR(256) NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    ActionId INT NOT NULL,
    CONSTRAINT PK_Queue PRIMARY KEY (UserEmail, LeftBibId, RightBibId),
    CONSTRAINT FK_Queue_ActionId FOREIGN KEY (ActionId)
        REFERENCES BibDedupe.Actions(ActionId)
);
GO

IF OBJECT_ID('BibDedupe.GetPairs','IF') IS NOT NULL
    DROP FUNCTION BibDedupe.GetPairs;
GO
CREATE FUNCTION BibDedupe.GetPairs (@Top INT = 1000)
RETURNS TABLE
AS
RETURN (
    SELECT TOP (@Top) MatchType, MatchValue, LeftBibId, RightBibId
    FROM BibDedupe.Pairs
);
GO

IF OBJECT_ID('BibDedupe.MergePair','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.MergePair;
GO
CREATE PROCEDURE BibDedupe.MergePair
    @KeepBibId INT,
    @DeleteBibId INT,
    @UserEmail NVARCHAR(256),
    @ActionId INT
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to merge records
    INSERT INTO BibDedupe.PairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, ActionId)
    VALUES (SYSDATETIME(), @UserEmail, @KeepBibId, @DeleteBibId, @ActionId);
END
GO

IF OBJECT_ID('BibDedupe.KeepBoth','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.KeepBoth;
GO
CREATE PROCEDURE BibDedupe.KeepBoth
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to keep both records as separate
    INSERT INTO BibDedupe.PairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, ActionId)
    VALUES (SYSDATETIME(), @UserEmail, @LeftBibId, @RightBibId, 2);
END
GO

IF OBJECT_ID('BibDedupe.Skip','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.Skip;
GO
CREATE PROCEDURE BibDedupe.Skip
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to skip processing this pair
    INSERT INTO BibDedupe.PairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, ActionId)
    VALUES (SYSDATETIME(), @UserEmail, @LeftBibId, @RightBibId, 3);
END
GO
