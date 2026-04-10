-- Idempotent initialization/upgrade script for BibDedupe schema and objects.
-- Safe to run repeatedly without dropping existing data.

IF DB_ID('clcdb') IS NULL
    CREATE DATABASE [clcdb];
GO

USE [clcdb];
GO

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

    IF COL_LENGTH('BibDedupe.DecisionQueue', 'KeptBibId') IS NULL
        ALTER TABLE BibDedupe.DecisionQueue
            ADD KeptBibId AS (
                CASE ActionId
                    WHEN 1 THEN LeftBibId
                    WHEN 4 THEN RightBibId
                    ELSE NULL
                END
            ) PERSISTED;

    IF COL_LENGTH('BibDedupe.DecisionQueue', 'DeletedBibId') IS NULL
        ALTER TABLE BibDedupe.DecisionQueue
            ADD DeletedBibId AS (
                CASE ActionId
                    WHEN 1 THEN RightBibId
                    WHEN 4 THEN LeftBibId
                    ELSE NULL
                END
            ) PERSISTED;
END
GO

IF OBJECT_ID('BibDedupe.DecisionBatches', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.DecisionBatches (
        BatchId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserEmail NVARCHAR(256) NOT NULL,
        JobId NVARCHAR(128) NOT NULL,
        StartedAt DATETIME2 NOT NULL,
        CompletedAt DATETIME2 NULL,
        FailedAt DATETIME2 NULL,
        FailureMessage NVARCHAR(1024) NULL
    );
END
GO

IF COL_LENGTH('BibDedupe.DecisionBatches', 'FailedAt') IS NULL
    ALTER TABLE BibDedupe.DecisionBatches ADD FailedAt DATETIME2 NULL;

IF COL_LENGTH('BibDedupe.DecisionBatches', 'FailureMessage') IS NULL
    ALTER TABLE BibDedupe.DecisionBatches ADD FailureMessage NVARCHAR(1024) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_DecisionBatches_UserEmail_StartedAt' AND object_id = OBJECT_ID('BibDedupe.DecisionBatches')
)
    CREATE NONCLUSTERED INDEX IX_DecisionBatches_UserEmail_StartedAt
        ON BibDedupe.DecisionBatches (UserEmail, StartedAt DESC)
        INCLUDE (CompletedAt, FailedAt, FailureMessage);
GO

IF OBJECT_ID('BibDedupe.DecisionBatchResults', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.DecisionBatchResults (
        ResultId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BatchId INT NOT NULL,
        LeftBibId INT NOT NULL,
        RightBibId INT NOT NULL,
        ActionId INT NOT NULL,
        Succeeded BIT NOT NULL,
        ErrorMessage NVARCHAR(1024) NULL,
        ProcessedAt DATETIME2 NOT NULL CONSTRAINT DF_DecisionBatchResults_ProcessedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_DecisionBatchResults_Batches FOREIGN KEY (BatchId)
            REFERENCES BibDedupe.DecisionBatches (BatchId),
        CONSTRAINT FK_DecisionBatchResults_Action FOREIGN KEY (ActionId)
            REFERENCES BibDedupe.Actions (ActionId)
    );
END
ELSE
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_DecisionBatchResults_Batch'
          AND parent_object_id = OBJECT_ID('BibDedupe.DecisionBatchResults')
    )
        ALTER TABLE BibDedupe.DecisionBatchResults
            DROP CONSTRAINT FK_DecisionBatchResults_Batch;

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DecisionBatchResults_Batches' AND parent_object_id = OBJECT_ID('BibDedupe.DecisionBatchResults')
    )
        ALTER TABLE BibDedupe.DecisionBatchResults
            ADD CONSTRAINT FK_DecisionBatchResults_Batches FOREIGN KEY (BatchId)
            REFERENCES BibDedupe.DecisionBatches (BatchId);

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DecisionBatchResults_Action' AND parent_object_id = OBJECT_ID('BibDedupe.DecisionBatchResults')
    )
        ALTER TABLE BibDedupe.DecisionBatchResults
            ADD CONSTRAINT FK_DecisionBatchResults_Action FOREIGN KEY (ActionId)
            REFERENCES BibDedupe.Actions (ActionId);

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.object_id = dc.parent_object_id
           AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('BibDedupe.DecisionBatchResults')
          AND c.name = 'ProcessedAt'
    )
    BEGIN
        ALTER TABLE BibDedupe.DecisionBatchResults
            ADD CONSTRAINT DF_DecisionBatchResults_ProcessedAt DEFAULT SYSUTCDATETIME() FOR ProcessedAt;
    END
END
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_DecisionBatchResults_BatchId' AND object_id = OBJECT_ID('BibDedupe.DecisionBatchResults')
)
    DROP INDEX IX_DecisionBatchResults_BatchId ON BibDedupe.DecisionBatchResults;

CREATE NONCLUSTERED INDEX IX_DecisionBatchResults_BatchId
    ON BibDedupe.DecisionBatchResults (BatchId, ProcessedAt)
    INCLUDE (ResultId, LeftBibId, RightBibId, ActionId, Succeeded, ErrorMessage);
GO

IF OBJECT_ID('BibDedupe.PairAssignments', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.PairAssignments (
        LeftBibId INT NOT NULL,
        RightBibId INT NOT NULL,
        UserEmail NVARCHAR(256) NOT NULL,
        AssignedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_PairAssignments_AssignedAt DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT PK_PairAssignments PRIMARY KEY (LeftBibId, RightBibId)
    );
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.key_constraints WHERE name = 'PK_PairAssignments' AND parent_object_id = OBJECT_ID('BibDedupe.PairAssignments')
    )
        ALTER TABLE BibDedupe.PairAssignments ADD CONSTRAINT PK_PairAssignments PRIMARY KEY (LeftBibId, RightBibId);

    IF NOT EXISTS (
        SELECT 1
        FROM sys.columns c
        LEFT JOIN sys.default_constraints dc
            ON c.default_object_id = dc.object_id
        WHERE c.object_id = OBJECT_ID('BibDedupe.PairAssignments')
          AND c.name = 'AssignedAt'
          AND dc.object_id IS NOT NULL
    )
        ALTER TABLE BibDedupe.PairAssignments
            ADD CONSTRAINT DF_PairAssignments_AssignedAt DEFAULT SYSDATETIMEOFFSET() FOR AssignedAt;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_PairAssignments_UserEmail' AND object_id = OBJECT_ID('BibDedupe.PairAssignments')
)
    CREATE NONCLUSTERED INDEX IX_PairAssignments_UserEmail ON BibDedupe.PairAssignments (UserEmail);
GO

CREATE OR ALTER FUNCTION BibDedupe.GetPairs (
    @Top INT = 1000,
    @UserEmail NVARCHAR(256) = NULL,
    @HideDecided BIT = NULL,
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
        mtom.Description AS TOM,
        p.LeftBibId,
        br_l.BrowseTitle AS LeftTitle,
        br_l.BrowseAuthor AS LeftAuthor,
        p.RightBibId,
        br_r.BrowseTitle AS RightTitle,
        br_r.BrowseAuthor AS RightAuthor,
        MatchesJson = ISNULL(pm.MatchesJson, '[]'),
        ISNULL(leftHolds.HoldCount, 0) AS LeftHoldCount,
        ISNULL(rightHolds.HoldCount, 0) AS RightHoldCount,
        ISNULL(leftHolds.HoldCount, 0) + ISNULL(rightHolds.HoldCount, 0) AS TotalHoldCount
    FROM BibDedupe.Pairs p
    JOIN polaris.polaris.BibliographicRecords br_l
        ON br_l.BibliographicRecordID = p.LeftBibId
    JOIN polaris.polaris.BibliographicRecords br_r
        ON br_r.BibliographicRecordID = p.RightBibId
    JOIN polaris.polaris.MARCTypeOfMaterial mtom
        ON mtom.MARCTypeOfMaterialID = p.PrimaryMARCTOMID
    OUTER APPLY (
        SELECT COUNT(1) AS HoldCount
        FROM polaris.polaris.SysHoldRequests shr
        WHERE shr.BibliographicRecordID = br_l.BibliographicRecordID
          AND shr.SysHoldStatusID IN (1, 3, 4)
    ) leftHolds
    OUTER APPLY (
        SELECT COUNT(1) AS HoldCount
        FROM polaris.polaris.SysHoldRequests shr
        WHERE shr.BibliographicRecordID = br_r.BibliographicRecordID
          AND shr.SysHoldStatusID IN (1, 3, 4)
    ) rightHolds
    OUTER APPLY (
        SELECT MatchType, MatchValue
        FROM BibDedupe.PairMatches m
        WHERE m.PairId = p.PairId
        ORDER BY m.MatchType, m.MatchValue
        FOR JSON PATH
    ) pm(MatchesJson)
    WHERE (
            @HideDecided IS NULL
            OR @HideDecided = 0
            OR NOT EXISTS (
                SELECT 1
                FROM BibDedupe.PairDecisions pd
                WHERE (pd.KeptBibId = p.LeftBibId AND pd.DeletedBibId = p.RightBibId)
                   OR (pd.KeptBibId = p.RightBibId AND pd.DeletedBibId = p.LeftBibId)
            )
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
        AND (
            @TomId IS NULL
            OR p.PrimaryMARCTOMID = @TomId
        )
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
                    ISNULL(leftHolds.HoldCount, 0) > 0
                    OR ISNULL(rightHolds.HoldCount, 0) > 0
                )
            )
            OR (
                @HasHolds = 0
                AND ISNULL(leftHolds.HoldCount, 0) = 0
                AND ISNULL(rightHolds.HoldCount, 0) = 0
            )
        )
    ORDER BY br_l.BrowseTitle, br_r.BrowseTitle
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
    @ActionId INT,
    @LogonBranchId INT = 1,
    @LogonUserId INT = 1,
    @LogonWorkstationId INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @retainedTags TABLE
    (
        BibliographicTagID INT,
        TagNumber INT,
        IndicatorOne CHAR(1),
        IndicatorTwo CHAR(1),
        Subfield CHAR(1),
        Data NVARCHAR(MAX),
        AuthorizingRecordID INT
    );

    INSERT INTO @retainedTags
    EXEC [Polaris].sys.sp_executesql
        N'EXEC Polaris.Cat_RetainBibRecordDataByID @deleteBibRecordId, NULL, @logonBranchId, @logonUserId, @logonWorkstationId;',
        N'@deleteBibRecordId INT, @logonBranchId INT, @logonUserId INT, @logonWorkstationId INT',
        @deleteBibRecordId = @DeleteBibId,
        @logonBranchId = @LogonBranchId,
        @logonUserId = @LogonUserId,
        @logonWorkstationId = @LogonWorkstationId;

    DECLARE @tagId INT;
    DECLARE @isUnindexed BIT = 0;
    DECLARE tagCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT rt.BibliographicTagID
    FROM @retainedTags rt
    ORDER BY rt.BibliographicTagID;

    BEGIN TRY
        EXEC [Polaris].sys.sp_executesql
            N'EXEC Polaris.UnIndexBib @keepBibRecordId;',
            N'@keepBibRecordId INT',
            @keepBibRecordId = @KeepBibId;
        SET @isUnindexed = 1;

        BEGIN TRANSACTION;

        OPEN tagCursor;
        FETCH NEXT FROM tagCursor INTO @tagId;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @newSequence INT;
            SELECT @newSequence = ISNULL(MAX(bt.Sequence), 0) + 1
            FROM @retainedTags rt
            JOIN Polaris.Polaris.BibliographicTags bt
                ON bt.TagNumber = rt.TagNumber
            WHERE bt.BibliographicRecordID = @KeepBibId
              AND rt.BibliographicTagID = @tagId;

            UPDATE Polaris.Polaris.BibliographicTags
            SET Sequence = Sequence + 1
            WHERE BibliographicRecordID = @KeepBibId
              AND Sequence >= @newSequence;

            INSERT INTO Polaris.Polaris.BibliographicTags
            SELECT TOP 1 @KeepBibId, @newSequence, rt.TagNumber, rt.IndicatorOne, rt.IndicatorTwo, rt.TagNumber
            FROM @retainedTags rt
            WHERE rt.BibliographicTagID = @tagId;

            DECLARE @newTagId INT = CAST(SCOPE_IDENTITY() AS INT);
            DECLARE @subfield CHAR(1);
            DECLARE @data NVARCHAR(MAX);
            DECLARE @subfieldSequence INT = 1;

            DECLARE subfieldCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT rt.Subfield, rt.Data
            FROM @retainedTags rt
            WHERE rt.BibliographicTagID = @tagId;

            OPEN subfieldCursor;
            FETCH NEXT FROM subfieldCursor INTO @subfield, @data;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                INSERT INTO Polaris.Polaris.BibliographicSubfields
                SELECT @newTagId, @subfieldSequence, @subfield, @data, 0;

                SET @subfieldSequence = @subfieldSequence + 1;
                FETCH NEXT FROM subfieldCursor INTO @subfield, @data;
            END
            CLOSE subfieldCursor;
            DEALLOCATE subfieldCursor;

            FETCH NEXT FROM tagCursor INTO @tagId;
        END
        CLOSE tagCursor;
        DEALLOCATE tagCursor;

        EXEC [Polaris].sys.sp_executesql
            N'EXEC Polaris.Cat_ReassignBibRecordLinks @keepBibRecordId, @deleteBibRecordId, @logonBranchId, @logonUserId, @logonWorkstationId;',
            N'@keepBibRecordId INT, @deleteBibRecordId INT, @logonBranchId INT, @logonUserId INT, @logonWorkstationId INT',
            @keepBibRecordId = @KeepBibId,
            @deleteBibRecordId = @DeleteBibId,
            @logonBranchId = @LogonBranchId,
            @logonUserId = @LogonUserId,
            @logonWorkstationId = @LogonWorkstationId;

        DECLARE @recordDeleted BIT;
        DECLARE @recordMarkedForDeletion BIT;
        DECLARE @widowList NVARCHAR(MAX);

        EXEC [Polaris].sys.sp_executesql
            N'EXEC Polaris.Cat_DeleteBibRecordProcessing
                @deleteBibRecordId, @logonBranchId, @logonUserId, @logonWorkstationId,
                @keepBibRecordId, NULL, @recordDeleted OUTPUT, @recordMarkedForDeletion OUTPUT, @widowList OUTPUT;',
            N'@deleteBibRecordId INT, @logonBranchId INT, @logonUserId INT, @logonWorkstationId INT, @keepBibRecordId INT, @recordDeleted BIT OUTPUT, @recordMarkedForDeletion BIT OUTPUT, @widowList NVARCHAR(MAX) OUTPUT',
            @deleteBibRecordId = @DeleteBibId,
            @logonBranchId = @LogonBranchId,
            @logonUserId = @LogonUserId,
            @logonWorkstationId = @LogonWorkstationId,
            @keepBibRecordId = @KeepBibId,
            @recordDeleted = @recordDeleted OUTPUT,
            @recordMarkedForDeletion = @recordMarkedForDeletion OUTPUT,
            @widowList = @widowList OUTPUT;

        INSERT INTO BibDedupe.PairDecisions (DecisionTimestamp, UserEmail, KeptBibId, DeletedBibId, ActionId)
        VALUES (SYSDATETIME(), @UserEmail, @KeepBibId, @DeleteBibId, @ActionId);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'subfieldCursor') >= -1
        BEGIN
            CLOSE subfieldCursor;
            DEALLOCATE subfieldCursor;
        END

        IF CURSOR_STATUS('local', 'tagCursor') >= -1
        BEGIN
            CLOSE tagCursor;
            DEALLOCATE tagCursor;
        END

        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        IF @isUnindexed = 1
        BEGIN
            BEGIN TRY
                EXEC [Polaris].sys.sp_executesql
                    N'EXEC Polaris.IndexBib @keepBibRecordId;',
                    N'@keepBibRecordId INT',
                    @keepBibRecordId = @KeepBibId;
            END TRY
            BEGIN CATCH
                -- keep original merge error as the thrown error
            END CATCH
        END

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH

    IF @isUnindexed = 1
        EXEC [Polaris].sys.sp_executesql
            N'EXEC Polaris.IndexBib @keepBibRecordId;',
            N'@keepBibRecordId INT',
            @keepBibRecordId = @KeepBibId;
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

IF OBJECT_ID('BibDedupe.UserClaims', 'V') IS NOT NULL
    DROP VIEW BibDedupe.UserClaims;
GO

IF OBJECT_ID('BibDedupe.UserClaims', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.UserClaims
    (
        UserEmail NVARCHAR(256) NOT NULL,
        Claim NVARCHAR(256) NOT NULL,
        CONSTRAINT PK_UserClaims PRIMARY KEY NONCLUSTERED (UserEmail, Claim)
    );
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints
        WHERE name = 'PK_UserClaims'
          AND parent_object_id = OBJECT_ID('BibDedupe.UserClaims')
    )
    BEGIN
        ALTER TABLE BibDedupe.UserClaims
            ADD CONSTRAINT PK_UserClaims PRIMARY KEY NONCLUSTERED (UserEmail, Claim);
    END
END
GO
