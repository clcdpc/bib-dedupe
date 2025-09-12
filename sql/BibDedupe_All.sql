IF SCHEMA_ID('BibDedupe') IS NULL
    EXEC('CREATE SCHEMA BibDedupe');
GO

-- Drop programmable objects first so dependent tables can be removed
IF OBJECT_ID('BibDedupe.MergePair','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.MergePair;
GO
IF OBJECT_ID('BibDedupe.KeepBoth','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.KeepBoth;
GO
IF OBJECT_ID('BibDedupe.Skip','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.Skip;
GO
IF OBJECT_ID('BibDedupe.IsAuthorizedUser','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.IsAuthorizedUser;
GO
IF OBJECT_ID('BibDedupe.GetPairs','IF') IS NOT NULL
    DROP FUNCTION BibDedupe.GetPairs;
GO

-- Drop tables in foreign key order
IF OBJECT_ID('BibDedupe.DecisionQueue','U') IS NOT NULL
    DROP TABLE BibDedupe.DecisionQueue;
GO
IF OBJECT_ID('BibDedupe.PairDecisions','U') IS NOT NULL
    DROP TABLE BibDedupe.PairDecisions;
GO
IF OBJECT_ID('BibDedupe.Pairs','U') IS NOT NULL
    DROP TABLE BibDedupe.Pairs;
GO
IF OBJECT_ID('BibDedupe.Actions','U') IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'';
    SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
                + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id))
                + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
    FROM sys.foreign_keys
    WHERE referenced_object_id = OBJECT_ID('BibDedupe.Actions');
    EXEC sp_executesql @sql;
    DROP TABLE BibDedupe.Actions;
END
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

CREATE TABLE BibDedupe.Actions (
    ActionId INT NOT NULL PRIMARY KEY,
    ActionName NVARCHAR(50) NOT NULL
);
INSERT INTO BibDedupe.Actions (ActionId, ActionName)
VALUES (1, 'keep left'), (2, 'keep both'), (3, 'skip'), (4, 'keep right');
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

CREATE TABLE BibDedupe.DecisionQueue (
    UserEmail NVARCHAR(256) NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    ActionId INT NOT NULL,
    CONSTRAINT PK_DecisionQueue PRIMARY KEY (UserEmail, LeftBibId, RightBibId),
    CONSTRAINT FK_DecisionQueue_ActionId FOREIGN KEY (ActionId)
        REFERENCES BibDedupe.Actions(ActionId)
);
GO


CREATE OR ALTER FUNCTION BibDedupe.GetPairs (@Top INT = 1000)
RETURNS TABLE
AS
RETURN (
    SELECT TOP (@Top) PairId, MatchType, MatchValue, PrimaryMARCTOMID, LeftBibId, RightBibId
    FROM BibDedupe.Pairs
);
GO

CREATE OR ALTER PROCEDURE BibDedupe.MergePair
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

CREATE OR ALTER PROCEDURE BibDedupe.KeepBoth
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

CREATE OR ALTER PROCEDURE BibDedupe.Skip
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

CREATE OR ALTER PROCEDURE BibDedupe.IsAuthorizedUser
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- Replace 'YourUserTable' with the actual table that stores user emails
    SELECT COUNT(1)
    FROM YourUserTable
    WHERE Email = @Email;
END
GO
