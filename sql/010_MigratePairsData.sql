SET NOCOUNT ON;

IF SCHEMA_ID('BibDedupe') IS NULL
BEGIN
    RAISERROR('Schema BibDedupe does not exist.', 16, 1);
    RETURN;
END;

-- If the pairs table no longer contains match columns we can assume the migration already ran.
IF COL_LENGTH('BibDedupe.Pairs', 'MatchType') IS NULL
BEGIN
    PRINT 'BibDedupe.Pairs is already in the new format. Migration skipped.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @hasPrimaryMetadata BIT = CASE WHEN COL_LENGTH('BibDedupe.Pairs', 'PrimaryMARCTOMID') IS NULL THEN 0 ELSE 1 END;

    IF OBJECT_ID('BibDedupe.GetPairs', 'IF') IS NOT NULL
        DROP FUNCTION BibDedupe.GetPairs;

    CREATE TABLE #PairsOld
    (
        PairId INT NOT NULL,
        MatchType NVARCHAR(50) NULL,
        MatchValue NVARCHAR(256) NULL,
        PrimaryMARCTOMID INT NULL,
        LeftBibId INT NOT NULL,
        RightBibId INT NOT NULL
    );

    INSERT INTO #PairsOld (
        PairId,
        MatchType,
        MatchValue,
        PrimaryMARCTOMID,
        LeftBibId,
        RightBibId
    )
    SELECT
        PairId,
        MatchType,
        MatchValue,
        CASE WHEN @hasPrimaryMetadata = 1 THEN PrimaryMARCTOMID ELSE NULL END,
        LeftBibId,
        RightBibId
    FROM BibDedupe.Pairs;

    DROP TABLE BibDedupe.Pairs;

    IF OBJECT_ID('BibDedupe.PairMatches', 'U') IS NOT NULL
        DROP TABLE BibDedupe.PairMatches;

    CREATE TABLE BibDedupe.Pairs
    (
        PairId INT IDENTITY(1,1) NOT NULL,
        PrimaryMARCTOMID INT NOT NULL,
        LeftBibId INT NOT NULL,
        RightBibId INT NOT NULL,
        CONSTRAINT PK_Pairs PRIMARY KEY (PairId),
        CONSTRAINT UQ_Pairs_LeftRight UNIQUE (LeftBibId, RightBibId)
    );

    CREATE TABLE BibDedupe.PairMatches
    (
        PairMatchId INT IDENTITY(1,1) NOT NULL,
        PairId INT NOT NULL,
        MatchType NVARCHAR(50) NOT NULL,
        MatchValue NVARCHAR(256) NOT NULL,
        CONSTRAINT PK_PairMatches PRIMARY KEY (PairMatchId),
        CONSTRAINT FK_PairMatches_Pairs FOREIGN KEY (PairId) REFERENCES BibDedupe.Pairs(PairId) ON DELETE CASCADE,
        CONSTRAINT UQ_PairMatches UNIQUE (PairId, MatchType, MatchValue)
    );

    DECLARE @PairMap TABLE
    (
        PairId INT NOT NULL,
        LeftBibId INT NOT NULL,
        RightBibId INT NOT NULL
    );

    WITH PairAggregates AS
    (
        SELECT
            LeftBibId,
            RightBibId,
            PrimaryMarcTomId = COALESCE(
                MAX(CASE WHEN PrimaryMARCTOMID IS NOT NULL AND PrimaryMARCTOMID <> 0 THEN PrimaryMARCTOMID END),
                MAX(LeftBibId)
            )
        FROM #PairsOld
        GROUP BY LeftBibId, RightBibId
    )
    INSERT INTO BibDedupe.Pairs (PrimaryMARCTOMID, LeftBibId, RightBibId)
    OUTPUT inserted.PairId, inserted.LeftBibId, inserted.RightBibId INTO @PairMap(PairId, LeftBibId, RightBibId)
    SELECT
        PrimaryMarcTomId,
        LeftBibId,
        RightBibId
    FROM PairAggregates
    ORDER BY LeftBibId, RightBibId;

    INSERT INTO BibDedupe.PairMatches (PairId, MatchType, MatchValue)
    SELECT DISTINCT
        m.PairId,
        o.MatchType,
        o.MatchValue
    FROM #PairsOld o
    JOIN @PairMap m ON m.LeftBibId = o.LeftBibId AND m.RightBibId = o.RightBibId
    WHERE o.MatchType IS NOT NULL AND LTRIM(RTRIM(o.MatchType)) <> ''
      AND o.MatchValue IS NOT NULL AND LTRIM(RTRIM(o.MatchValue)) <> '';

    DROP TABLE #PairsOld;

    COMMIT TRANSACTION;

    DECLARE @createGetPairsSql NVARCHAR(MAX) = N'
CREATE FUNCTION BibDedupe.GetPairs (
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
        MatchesJson = ISNULL(pm.MatchesJson, ''[]''),
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
        )
);';

    EXEC sys.sp_executesql @createGetPairsSql;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF OBJECT_ID('tempdb..#PairsOld') IS NOT NULL DROP TABLE #PairsOld;
    THROW;
END CATCH;
