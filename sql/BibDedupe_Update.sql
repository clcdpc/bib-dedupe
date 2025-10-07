IF SCHEMA_ID('BibDedupe') IS NULL
    EXEC('CREATE SCHEMA BibDedupe');
GO

-- Ensure base tables exist without dropping data
IF OBJECT_ID('BibDedupe.Pairs', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.Pairs (
        PairId INT IDENTITY(1,1) NOT NULL,
        PrimaryMARCTOMID INT NOT NULL,
        LeftBibId INT NOT NULL,
        RightBibId INT NOT NULL,
        CONSTRAINT PK_Pairs PRIMARY KEY (PairId),
        CONSTRAINT UQ_Pairs_LeftRight UNIQUE (LeftBibId, RightBibId)
    );
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Pairs' AND parent_object_id = OBJECT_ID('BibDedupe.Pairs')
    )
        ALTER TABLE BibDedupe.Pairs ADD CONSTRAINT PK_Pairs PRIMARY KEY (PairId);

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes WHERE name = 'UQ_Pairs_LeftRight' AND object_id = OBJECT_ID('BibDedupe.Pairs')
    )
        ALTER TABLE BibDedupe.Pairs ADD CONSTRAINT UQ_Pairs_LeftRight UNIQUE (LeftBibId, RightBibId);
END
GO

IF OBJECT_ID('BibDedupe.PairMatches', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.PairMatches (
        PairMatchId INT IDENTITY(1,1) NOT NULL,
        PairId INT NOT NULL,
        MatchType NVARCHAR(50) NOT NULL,
        MatchValue NVARCHAR(256) NOT NULL,
        CONSTRAINT PK_PairMatches PRIMARY KEY (PairMatchId),
        CONSTRAINT FK_PairMatches_Pairs FOREIGN KEY (PairId)
            REFERENCES BibDedupe.Pairs (PairId)
            ON DELETE CASCADE,
        CONSTRAINT UQ_PairMatches UNIQUE (PairId, MatchType, MatchValue)
    );
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PairMatches' AND parent_object_id = OBJECT_ID('BibDedupe.PairMatches')
    )
        ALTER TABLE BibDedupe.PairMatches ADD CONSTRAINT PK_PairMatches PRIMARY KEY (PairMatchId);

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PairMatches_Pairs' AND parent_object_id = OBJECT_ID('BibDedupe.PairMatches')
    )
        ALTER TABLE BibDedupe.PairMatches
            ADD CONSTRAINT FK_PairMatches_Pairs FOREIGN KEY (PairId)
            REFERENCES BibDedupe.Pairs (PairId)
            ON DELETE CASCADE;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes WHERE name = 'UQ_PairMatches' AND object_id = OBJECT_ID('BibDedupe.PairMatches')
    )
        ALTER TABLE BibDedupe.PairMatches ADD CONSTRAINT UQ_PairMatches UNIQUE (PairId, MatchType, MatchValue);
END
GO

IF OBJECT_ID('BibDedupe.Actions', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.Actions (
        ActionId INT NOT NULL PRIMARY KEY,
        ActionName NVARCHAR(50) NOT NULL
    );
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID('BibDedupe.Actions')
          AND type = 'PK'
    )
        ALTER TABLE BibDedupe.Actions ADD CONSTRAINT PK_Actions PRIMARY KEY (ActionId);
END
GO

;MERGE BibDedupe.Actions AS target
USING (VALUES
    (1, N'keep left'),
    (2, N'not duplicate'),
    (3, N'skip'),
    (4, N'keep right')
) AS source (ActionId, ActionName)
    ON target.ActionId = source.ActionId
WHEN MATCHED AND target.ActionName <> source.ActionName THEN
    UPDATE SET ActionName = source.ActionName
WHEN NOT MATCHED THEN
    INSERT (ActionId, ActionName) VALUES (source.ActionId, source.ActionName);
GO

IF OBJECT_ID('BibDedupe.PairDecisions', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.PairDecisions (
        DecisionTimestamp DATETIME2 NOT NULL CONSTRAINT DF_PairDecisions_DecisionTimestamp DEFAULT SYSDATETIME(),
        UserEmail NVARCHAR(256) NOT NULL,
        KeptBibId INT NOT NULL,
        DeletedBibId INT NULL,
        ActionId INT NOT NULL,
        CONSTRAINT FK_PairDecisions_ActionId FOREIGN KEY (ActionId)
            REFERENCES BibDedupe.Actions (ActionId)
    );
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PairDecisions_ActionId' AND parent_object_id = OBJECT_ID('BibDedupe.PairDecisions')
    )
        ALTER TABLE BibDedupe.PairDecisions
            ADD CONSTRAINT FK_PairDecisions_ActionId FOREIGN KEY (ActionId)
            REFERENCES BibDedupe.Actions (ActionId);

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.object_id = dc.parent_object_id
           AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('BibDedupe.PairDecisions')
          AND c.name = 'DecisionTimestamp'
    )
    BEGIN
        ALTER TABLE BibDedupe.PairDecisions
            ADD CONSTRAINT DF_PairDecisions_DecisionTimestamp DEFAULT SYSDATETIME() FOR DecisionTimestamp;
    END
END
GO

IF OBJECT_ID('BibDedupe.DecisionQueue', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.DecisionQueue (
        UserEmail NVARCHAR(256) NOT NULL,
        LeftBibId INT NOT NULL,
        RightBibId INT NOT NULL,
        ActionId INT NOT NULL,
        CONSTRAINT PK_DecisionQueue PRIMARY KEY (UserEmail, LeftBibId, RightBibId),
        CONSTRAINT FK_DecisionQueue_ActionId FOREIGN KEY (ActionId)
            REFERENCES BibDedupe.Actions (ActionId)
    );
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.key_constraints WHERE name = 'PK_DecisionQueue' AND parent_object_id = OBJECT_ID('BibDedupe.DecisionQueue')
    )
        ALTER TABLE BibDedupe.DecisionQueue
            ADD CONSTRAINT PK_DecisionQueue PRIMARY KEY (UserEmail, LeftBibId, RightBibId);

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DecisionQueue_ActionId' AND parent_object_id = OBJECT_ID('BibDedupe.DecisionQueue')
    )
        ALTER TABLE BibDedupe.DecisionQueue
            ADD CONSTRAINT FK_DecisionQueue_ActionId FOREIGN KEY (ActionId)
            REFERENCES BibDedupe.Actions (ActionId);
END
GO

IF OBJECT_ID('BibDedupe.DecisionBatches', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.DecisionBatches (
        BatchId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserEmail NVARCHAR(256) NOT NULL,
        JobId NVARCHAR(128) NOT NULL,
        StartedAt DATETIME2 NOT NULL,
        CompletedAt DATETIME2 NULL
    );
END
GO

CREATE OR ALTER FUNCTION BibDedupe.GetPairs (
    @Top INT = 1000,
    @UserEmail NVARCHAR(256) = NULL,
    @TomId INT = NULL,
    @MatchType NVARCHAR(50) = NULL,
    @HasHolds BIT = NULL
)
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
        TOM = CAST(NULL AS NVARCHAR(256)),
        MatchesJson = ISNULL(pm.MatchesJson, '[]'),
        LeftHoldCount = CAST(0 AS INT),
        RightHoldCount = CAST(0 AS INT),
        TotalHoldCount = CAST(0 AS INT)
    FROM BibDedupe.Pairs p
    OUTER APPLY (
        SELECT MatchType, MatchValue
        FROM BibDedupe.PairMatches m
        WHERE m.PairId = p.PairId
        ORDER BY m.MatchType, m.MatchValue
        FOR JSON PATH
    ) pm (MatchesJson)
    WHERE NOT EXISTS (
            SELECT 1
            FROM BibDedupe.PairDecisions pd
            WHERE (
                (pd.KeptBibId = p.LeftBibId AND pd.DeletedBibId = p.RightBibId)
                OR (pd.KeptBibId = p.RightBibId AND pd.DeletedBibId = p.LeftBibId)
            )
              AND (@UserEmail IS NULL OR pd.UserEmail = @UserEmail)
        )
        AND (
            @UserEmail IS NULL
            OR NOT EXISTS (
                SELECT 1
                FROM BibDedupe.DecisionQueue dq
                WHERE dq.UserEmail = @UserEmail
                  AND dq.LeftBibId = p.LeftBibId
                  AND dq.RightBibId = p.RightBibId
            )
        )
        AND (@TomId IS NULL)
        AND (
            @MatchType IS NULL
            OR EXISTS (
                SELECT 1
                FROM BibDedupe.PairMatches mt
                WHERE mt.PairId = p.PairId
                  AND mt.MatchType = @MatchType
            )
        )
        AND (@HasHolds IS NULL);
GO

CREATE OR ALTER PROCEDURE BibDedupe.MergePair
    @KeepBibId INT,
    @DeleteBibId INT,
    @UserEmail NVARCHAR(256),
    @ActionId INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO BibDedupe.PairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, ActionId)
    VALUES (SYSDATETIME(), @UserEmail, @KeepBibId, @DeleteBibId, @ActionId);
END
GO

IF OBJECT_ID('BibDedupe.KeepBoth', 'P') IS NOT NULL
    DROP PROCEDURE BibDedupe.KeepBoth;
GO

CREATE OR ALTER PROCEDURE BibDedupe.MarkNotDuplicate
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

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
            EXEC BibDedupe.MarkNotDuplicate @LeftBibId = @LeftBibId, @RightBibId = @RightBibId, @UserEmail = @UserEmail;
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

IF OBJECT_ID('BibDedupe.UserClaims', 'V') IS NOT NULL
    DROP VIEW BibDedupe.UserClaims;
GO

CREATE VIEW BibDedupe.UserClaims
AS
-- Grant application access by returning at least one row with Claim = 'Access'.
-- Assign additional roles (for example 'Administrator') by returning more rows for the same user.
SELECT TOP (0)
    CAST(NULL AS NVARCHAR(256)) AS UserEmail,
    CAST(NULL AS NVARCHAR(256)) AS Claim;
GO
