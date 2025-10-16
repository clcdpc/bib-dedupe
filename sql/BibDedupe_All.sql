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
IF OBJECT_ID('BibDedupe.MarkNotDuplicate','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.MarkNotDuplicate;
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

IF OBJECT_ID('BibDedupe.GetDecisionQueue','IF') IS NOT NULL
    DROP FUNCTION BibDedupe.GetDecisionQueue;
GO

IF OBJECT_ID('BibDedupe.GetValidPairActions','IF') IS NOT NULL
    DROP FUNCTION BibDedupe.GetValidPairActions;
GO

-- Drop tables in foreign key order
IF OBJECT_ID('BibDedupe.DecisionQueue','U') IS NOT NULL
    DROP TABLE BibDedupe.DecisionQueue;
GO
IF OBJECT_ID('BibDedupe.PairAssignments','U') IS NOT NULL
    DROP TABLE BibDedupe.PairAssignments;
GO
IF OBJECT_ID('BibDedupe.DecisionBatchResults','U') IS NOT NULL
    DROP TABLE BibDedupe.DecisionBatchResults;
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
VALUES (1, 'keep left'), (2, 'not duplicate'), (3, 'skip'), (4, 'keep right');
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
    KeptBibId AS (
        CASE ActionId
            WHEN 1 THEN LeftBibId
            WHEN 4 THEN RightBibId
            ELSE NULL
        END
    ) PERSISTED,
    DeletedBibId AS (
        CASE ActionId
            WHEN 1 THEN RightBibId
            WHEN 4 THEN LeftBibId
            ELSE NULL
        END
    ) PERSISTED,
    CONSTRAINT PK_DecisionQueue PRIMARY KEY (UserEmail, LeftBibId, RightBibId),
    CONSTRAINT FK_DecisionQueue_ActionId FOREIGN KEY (ActionId)
        REFERENCES BibDedupe.Actions(ActionId)
);
GO

CREATE TABLE BibDedupe.PairAssignments (
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    UserEmail NVARCHAR(256) NOT NULL,
    AssignedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT PK_PairAssignments PRIMARY KEY (LeftBibId, RightBibId)
);
GO
CREATE NONCLUSTERED INDEX IX_PairAssignments_UserEmail ON BibDedupe.PairAssignments (UserEmail);
GO

CREATE TABLE BibDedupe.DecisionBatches
(
    BatchId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserEmail NVARCHAR(256) NOT NULL,
    JobId NVARCHAR(128) NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    FailedAt DATETIME2 NULL,
    FailureMessage NVARCHAR(1024) NULL
);
GO

CREATE NONCLUSTERED INDEX IX_DecisionBatches_UserEmail_StartedAt
    ON BibDedupe.DecisionBatches (UserEmail, StartedAt DESC)
    INCLUDE (CompletedAt, FailedAt, FailureMessage);
GO

CREATE TABLE BibDedupe.DecisionBatchResults
(
    ResultId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    BatchId INT NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    ActionId INT NOT NULL,
    Succeeded BIT NOT NULL,
    ErrorMessage NVARCHAR(1024) NULL,
    ProcessedAt DATETIME2 NOT NULL CONSTRAINT DF_DecisionBatchResults_ProcessedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_DecisionBatchResults_Batch FOREIGN KEY (BatchId)
        REFERENCES BibDedupe.DecisionBatches(BatchId),
    CONSTRAINT FK_DecisionBatchResults_Action FOREIGN KEY (ActionId)
        REFERENCES BibDedupe.Actions(ActionId)
);
GO

CREATE NONCLUSTERED INDEX IX_DecisionBatchResults_BatchId
    ON BibDedupe.DecisionBatchResults (BatchId, ProcessedAt)
    INCLUDE (ResultId, LeftBibId, RightBibId, ActionId, Succeeded, ErrorMessage);
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
    ) pm(MatchesJson)
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
        AND (
            @UserEmail IS NULL
            OR NOT EXISTS (
                SELECT 1
                FROM BibDedupe.PairAssignments pa
                WHERE pa.LeftBibId = p.LeftBibId
                  AND pa.RightBibId = p.RightBibId
                  AND pa.UserEmail <> @UserEmail
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
        AND (
            @HasHolds IS NULL
            OR (
                @HasHolds = 1
                AND (
                    EXISTS (
                        SELECT 1
                        FROM polaris.polaris.SysHoldRequests shr
                        WHERE shr.BibliographicRecordID = p.LeftBibId
                          AND shr.SysHoldStatusID IN (1, 3, 4)
                    )
                    OR EXISTS (
                        SELECT 1
                        FROM polaris.polaris.SysHoldRequests shr
                        WHERE shr.BibliographicRecordID = p.RightBibId
                          AND shr.SysHoldStatusID IN (1, 3, 4)
                    )
                )
            )
            OR (
                @HasHolds = 0
                AND NOT EXISTS (
                    SELECT 1
                    FROM polaris.polaris.SysHoldRequests shr
                    WHERE shr.BibliographicRecordID = p.LeftBibId
                      AND shr.SysHoldStatusID IN (1, 3, 4)
                )
                AND NOT EXISTS (
                    SELECT 1
                    FROM polaris.polaris.SysHoldRequests shr
                    WHERE shr.BibliographicRecordID = p.RightBibId
                      AND shr.SysHoldStatusID IN (1, 3, 4)
                )
            )
);
GO

CREATE OR ALTER FUNCTION BibDedupe.GetDecisionQueue (
    @UserEmail NVARCHAR(256)
)
RETURNS TABLE
AS
RETURN (
    SELECT
        dq.UserEmail,
        dq.LeftBibId,
        dq.RightBibId,
        dq.ActionId,
        dq.KeptBibId,
        dq.DeletedBibId,
        p.PrimaryMarcTomId,
        p.LeftTitle,
        p.LeftAuthor,
        p.RightTitle,
        p.RightAuthor,
        p.TOM,
        p.MatchesJson
    FROM BibDedupe.DecisionQueue dq
    CROSS APPLY (
        SELECT
            gp.PrimaryMARCTOMID AS PrimaryMarcTomId,
            gp.LeftTitle,
            gp.LeftAuthor,
            gp.RightTitle,
            gp.RightAuthor,
            gp.TOM,
            gp.MatchesJson
        FROM BibDedupe.GetPairs(2147483647, NULL, 0, NULL, NULL, NULL) gp
        WHERE gp.LeftBibId = dq.LeftBibId
          AND gp.RightBibId = dq.RightBibId
    ) p
    WHERE dq.UserEmail = @UserEmail
);
GO

CREATE OR ALTER FUNCTION BibDedupe.GetValidPairActions (
    @UserEmail NVARCHAR(256),
    @LeftBibId INT,
    @RightBibId INT
)
RETURNS TABLE
AS
RETURN (
    WITH ExistingDecisions AS (
        SELECT
            dq.KeptBibId,
            dq.DeletedBibId
        FROM BibDedupe.DecisionQueue dq
        WHERE dq.UserEmail = @UserEmail
          AND NOT (dq.LeftBibId = @LeftBibId AND dq.RightBibId = @RightBibId)
    )
    SELECT a.ActionId
    FROM BibDedupe.Actions a
    WHERE a.ActionId IN (1, 2, 3, 4)
      AND (
            a.ActionId NOT IN (1, 4)
        OR (
                a.ActionId = 1
            AND NOT EXISTS (SELECT 1 FROM ExistingDecisions WHERE DeletedBibId IN (@LeftBibId, @RightBibId))
            AND NOT EXISTS (SELECT 1 FROM ExistingDecisions WHERE KeptBibId = @RightBibId)
        )
        OR (
                a.ActionId = 4
            AND NOT EXISTS (SELECT 1 FROM ExistingDecisions WHERE DeletedBibId IN (@LeftBibId, @RightBibId))
            AND NOT EXISTS (SELECT 1 FROM ExistingDecisions WHERE KeptBibId = @LeftBibId)
        )
      )
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

CREATE OR ALTER PROCEDURE BibDedupe.MarkNotDuplicate
    @LeftBibId INT,
    @RightBibId INT,
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    -- TODO: implement logic to record that this pair is not a duplicate
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
    DECLARE @BatchId INT = NULL;
    DECLARE @Succeeded BIT;
    DECLARE @ErrorMessage NVARCHAR(1024);

    DECLARE decision_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT LeftBibId, RightBibId, ActionId
        FROM BibDedupe.DecisionQueue
        WHERE UserEmail = @UserEmail
        ORDER BY LeftBibId, RightBibId;

    OPEN decision_cursor;
    FETCH NEXT FROM decision_cursor INTO @LeftBibId, @RightBibId, @ActionId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Succeeded = 0;
        SET @ErrorMessage = NULL;

        BEGIN TRY
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
            ELSE
            BEGIN
                SET @ErrorMessage = CONCAT('Unsupported action id ', @ActionId, '.');
            END

            IF (@ErrorMessage IS NULL)
            BEGIN
                SET @Succeeded = 1;
            END
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0
            BEGIN
                ROLLBACK TRANSACTION;
            END;
            SET @ErrorMessage = ERROR_MESSAGE();
        END CATCH;

        IF (@BatchId IS NULL)
        BEGIN
            SELECT TOP 1 @BatchId = BatchId
            FROM BibDedupe.DecisionBatches
            WHERE UserEmail = @UserEmail AND CompletedAt IS NULL AND FailedAt IS NULL
            ORDER BY StartedAt DESC;
        END

        IF (@BatchId IS NOT NULL)
        BEGIN
            INSERT INTO BibDedupe.DecisionBatchResults
                (BatchId, LeftBibId, RightBibId, ActionId, Succeeded, ErrorMessage, ProcessedAt)
            VALUES
                (@BatchId, @LeftBibId, @RightBibId, @ActionId, @Succeeded, CASE WHEN @ErrorMessage IS NULL THEN NULL ELSE LEFT(@ErrorMessage, 1024) END, SYSUTCDATETIME());
        END

        FETCH NEXT FROM decision_cursor INTO @LeftBibId, @RightBibId, @ActionId;
    END

    CLOSE decision_cursor;
    DEALLOCATE decision_cursor;

    DELETE FROM BibDedupe.DecisionQueue WHERE UserEmail = @UserEmail;
END
GO

CREATE VIEW BibDedupe.UserClaims
AS
-- Grant application access by returning at least one row with Claim = 'Access'.
-- Assign additional roles (for example 'Administrator') by returning more rows for the same user.
SELECT TOP (0)
    CAST(NULL AS NVARCHAR(256)) AS UserEmail,
    CAST(NULL AS NVARCHAR(256)) AS Claim;
GO
