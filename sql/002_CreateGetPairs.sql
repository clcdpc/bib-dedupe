IF OBJECT_ID('BibDedupe.GetPairs','IF') IS NOT NULL
    DROP FUNCTION BibDedupe.GetPairs;
GO

CREATE FUNCTION [BibDedupe].[GetPairs] (@Top INT = 1000)
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
	join polaris.polaris.BibliographicRecords br_l
		on br_l.BibliographicRecordID = p.LeftBibId
	join polaris.polaris.BibliographicRecords br_r
		on br_r.BibliographicRecordID = p.RightBibId
	join polaris.polaris.MARCTypeOfMaterial mtom
		on mtom.MARCTypeOfMaterialID = p.PrimaryMARCTOMID
	outer apply ( select count(1) [HoldCount] from polaris.polaris.SysHoldRequests shr where shr.BibliographicRecordID = br_l.BibliographicRecordID and shr.SysHoldStatusID in (1,3,4) ) leftHolds
	outer apply ( select count(1) [HoldCount] from polaris.polaris.SysHoldRequests shr where shr.BibliographicRecordID = br_r.BibliographicRecordID and shr.SysHoldStatusID in (1,3,4) ) rightHolds
    OUTER APPLY (
        SELECT MatchType, MatchValue
        FROM BibDedupe.PairMatches m
        WHERE m.PairId = p.PairId
        ORDER BY m.MatchType, m.MatchValue
        FOR JSON PATH
    ) pm(MatchesJson)
	order by br_l.BrowseTitle, br_r.BrowseTitle
);
GO
