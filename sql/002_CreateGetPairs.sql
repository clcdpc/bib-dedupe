USE [clcdb]
GO
/****** Object:  UserDefinedFunction [BibDedupe].[GetPairs]    Script Date: 10/10/2025 6:52:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER FUNCTION [BibDedupe].[GetPairs] (
    @Top INT = 1000,
    @UserEmail NVARCHAR(256) = NULL,
	@HideDecided bit = NULL,
    @TomId INT = NULL,
    @MatchType NVARCHAR(50) = NULL,
    @HasHolds BIT = NULL
)
RETURNS TABLE
AS
RETURN (
    SELECT TOP (@Top)
        p.PairId
        ,p.PrimaryMARCTOMID
		,mtom.Description [TOM]
		,LeftBibId
		,br_l.BrowseTitle	[LeftTitle]
		,br_l.BrowseAuthor	[LeftAuthor]
		,p.RightBibId
		,br_r.BrowseTitle	[RightTitle]
		,br_r.BrowseAuthor	[RightAuthor]
        ,MatchesJson = ISNULL(pm.MatchesJson, '[]')
		,isnull(leftHolds.HoldCount,0) [LeftHoldCount]
		,isnull(rightHolds.HoldCount,0) [RightHoldCount]
		,isnull(leftHolds.HoldCount,0) + isnull(rightHolds.HoldCount,0) [TotalHoldCount]
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
          AND shr.SysHoldStatusID IN (1,3,4)
    ) leftHolds
    OUTER APPLY (
        SELECT COUNT(1) AS HoldCount
        FROM polaris.polaris.SysHoldRequests shr
        WHERE shr.BibliographicRecordID = br_r.BibliographicRecordID
          AND shr.SysHoldStatusID IN (1,3,4)
    ) rightHolds
    OUTER APPLY (
        SELECT MatchType, MatchValue
        FROM BibDedupe.PairMatches m
        WHERE m.PairId = p.PairId
        ORDER BY m.MatchType, m.MatchValue
        FOR JSON PATH
    ) pm(MatchesJson)
    WHERE (@HideDecided is null or @HideDecided = 0 or NOT EXISTS (
            SELECT 1
            FROM BibDedupe.PairDecisions pd
            WHERE (pd.KeptBibId = p.LeftBibId AND pd.DeletedBibId = p.RightBibId) OR (pd.KeptBibId = p.RightBibId AND pd.DeletedBibId = p.LeftBibId)
        ))
        AND (
            @UserEmail IS NULL
            OR NOT EXISTS (
                SELECT 1
                FROM BibDedupe.DecisionQueue dq
                WHERE dq.UserEmail = @UserEmail
                  AND (
                      (dq.LeftBibId = p.LeftBibId AND dq.RightBibId = p.RightBibId)
                      OR (dq.LeftBibId = p.RightBibId AND dq.RightBibId = p.LeftBibId)
                  )
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
        order by br_l.BrowseTitle, br_r.BrowseTitle
);
