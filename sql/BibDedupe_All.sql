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
IF OBJECT_ID('BibDedupe.ProcessDecisionBatch','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.ProcessDecisionBatch;
GO
IF OBJECT_ID('BibDedupe.UserClaims','V') IS NOT NULL
    DROP VIEW BibDedupe.UserClaims;
GO
IF OBJECT_ID('BibDedupe.GetPairs','IF') IS NOT NULL
    DROP FUNCTION BibDedupe.GetPairs;
GO

-- Drop tables in foreign key order
IF OBJECT_ID('BibDedupe.DecisionQueue','U') IS NOT NULL
    DROP TABLE BibDedupe.DecisionQueue;
GO
IF OBJECT_ID('BibDedupe.DecisionBatches','U') IS NOT NULL
    DROP TABLE BibDedupe.DecisionBatches;
GO
IF OBJECT_ID('BibDedupe.PairDecisions','U') IS NOT NULL
    DROP TABLE BibDedupe.PairDecisions;
GO
IF OBJECT_ID('BibDedupe.PairMatches','U') IS NOT NULL
    DROP TABLE BibDedupe.PairMatches;
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
    PrimaryMARCTOMID INT NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
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

CREATE TABLE BibDedupe.DecisionBatches
(
    BatchId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserEmail NVARCHAR(256) NOT NULL,
    JobId NVARCHAR(128) NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL
);
GO


CREATE OR ALTER FUNCTION BibDedupe.GetPairs (@Top INT = 1000)
RETURNS TABLE
AS
RETURN (
    SELECT TOP (@Top)
        p.PairId,
        p.PrimaryMARCTOMID,
        p.LeftBibId,
        p.RightBibId,
        LeftTitle = CAST(NULL AS NVARCHAR(512)),
        LeftAuthor = CAST(NULL AS NVARCHAR(256)),
        RightTitle = CAST(NULL AS NVARCHAR(512)),
        RightAuthor = CAST(NULL AS NVARCHAR(256)),
        MatchesJson = ISNULL(pm.MatchesJson, '[]')
    FROM BibDedupe.Pairs p
    OUTER APPLY (
        SELECT MatchType, MatchValue
        FROM BibDedupe.PairMatches m
        WHERE m.PairId = p.PairId
        ORDER BY m.MatchType, m.MatchValue
        FOR JSON PATH
    ) pm(MatchesJson)
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

CREATE OR ALTER PROCEDURE BibDedupe.ProcessDecisionBatch
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LeftBibId INT;
    DECLARE @RightBibId INT;
    DECLARE @ActionId INT;

    DECLARE decision_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT LeftBibId, RightBibId, ActionId
        FROM BibDedupe.DecisionQueue
        WHERE UserEmail = @UserEmail
        ORDER BY LeftBibId, RightBibId;

    OPEN decision_cursor;
    FETCH NEXT FROM decision_cursor INTO @LeftBibId, @RightBibId, @ActionId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF (@ActionId = 1)
        BEGIN
            EXEC BibDedupe.MergePair @KeepBibId = @LeftBibId, @DeleteBibId = @RightBibId, @UserEmail = @UserEmail, @ActionId = @ActionId;
        END
        ELSE IF (@ActionId = 2)
        BEGIN
            EXEC BibDedupe.KeepBoth @LeftBibId = @LeftBibId, @RightBibId = @RightBibId, @UserEmail = @UserEmail;
        END
        ELSE IF (@ActionId = 3)
        BEGIN
            EXEC BibDedupe.Skip @LeftBibId = @LeftBibId, @RightBibId = @RightBibId, @UserEmail = @UserEmail;
        END
        ELSE IF (@ActionId = 4)
        BEGIN
            EXEC BibDedupe.MergePair @KeepBibId = @RightBibId, @DeleteBibId = @LeftBibId, @UserEmail = @UserEmail, @ActionId = @ActionId;
        END

        FETCH NEXT FROM decision_cursor INTO @LeftBibId, @RightBibId, @ActionId;

        IF @@FETCH_STATUS = 0
        BEGIN
            WAITFOR DELAY '00:01:00';
        END
    END

    CLOSE decision_cursor;
    DEALLOCATE decision_cursor;

    DELETE FROM BibDedupe.DecisionQueue WHERE UserEmail = @UserEmail;
END
GO

CREATE VIEW BibDedupe.UserClaims
AS
-- Grant application access by returning at least one row with ClaimValue = 'Access'.
-- Assign additional roles (for example 'Administrator') by returning more rows for the same user.
-- Each value is added to the caller as a role claim, so the claim type column is optional.
SELECT TOP (0)
    CAST(NULL AS NVARCHAR(256)) AS UserEmail,
    CAST(NULL AS NVARCHAR(128)) AS ClaimType,
    CAST(NULL AS NVARCHAR(256)) AS ClaimValue;
GO
