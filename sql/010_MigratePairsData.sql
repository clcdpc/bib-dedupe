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

    DECLARE @hasLeftMetadata BIT = CASE WHEN COL_LENGTH('BibDedupe.Pairs', 'LeftTitle') IS NULL THEN 0 ELSE 1 END;
    DECLARE @hasRightMetadata BIT = CASE WHEN COL_LENGTH('BibDedupe.Pairs', 'RightTitle') IS NULL THEN 0 ELSE 1 END;

    DECLARE @sql NVARCHAR(MAX) =
        N'SELECT PairId, MatchType, MatchValue, PrimaryMARCTOMID, LeftBibId, RightBibId' +
        CASE WHEN @hasLeftMetadata = 1 THEN N', LeftTitle, LeftAuthor' ELSE N', NULL AS LeftTitle, NULL AS LeftAuthor' END +
        CASE WHEN @hasRightMetadata = 1 THEN N', RightTitle, RightAuthor' ELSE N', NULL AS RightTitle, NULL AS RightAuthor' END +
        N' INTO #PairsOld FROM BibDedupe.Pairs;';

    EXEC sp_executesql @sql;

    DROP TABLE BibDedupe.Pairs;

    IF OBJECT_ID('BibDedupe.PairMatches', 'U') IS NOT NULL
        DROP TABLE BibDedupe.PairMatches;

    CREATE TABLE BibDedupe.Pairs
    (
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
            ),
            LeftTitle = MAX(CASE WHEN LeftTitle IS NOT NULL AND LTRIM(RTRIM(LeftTitle)) <> '' THEN LeftTitle END),
            LeftAuthor = MAX(CASE WHEN LeftAuthor IS NOT NULL AND LTRIM(RTRIM(LeftAuthor)) <> '' THEN LeftAuthor END),
            RightTitle = MAX(CASE WHEN RightTitle IS NOT NULL AND LTRIM(RTRIM(RightTitle)) <> '' THEN RightTitle END),
            RightAuthor = MAX(CASE WHEN RightAuthor IS NOT NULL AND LTRIM(RTRIM(RightAuthor)) <> '' THEN RightAuthor END)
        FROM #PairsOld
        GROUP BY LeftBibId, RightBibId
    )
    INSERT INTO BibDedupe.Pairs (PrimaryMARCTOMID, LeftBibId, RightBibId, LeftTitle, LeftAuthor, RightTitle, RightAuthor)
    OUTPUT inserted.PairId, inserted.LeftBibId, inserted.RightBibId INTO @PairMap(PairId, LeftBibId, RightBibId)
    SELECT
        PrimaryMarcTomId,
        LeftBibId,
        RightBibId,
        COALESCE(LeftTitle, ''),
        LeftAuthor,
        COALESCE(RightTitle, ''),
        RightAuthor
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
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF OBJECT_ID('tempdb..#PairsOld') IS NOT NULL DROP TABLE #PairsOld;
    THROW;
END CATCH;
