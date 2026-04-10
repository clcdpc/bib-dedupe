/* ============================================================
   Candidate Duplicate Bibliographic Records (Polaris / SQL Server)
   - Matches by ISBN, UPC, and normalized Title (no fuzzy/Jaccard)
   - Author-aware: pairs require matching normalized BrowseAuthor
       (if both authors present; if either missing, pair is allowed)
   - Edition-aware: pairs require same PublicationYear
       (if both years present; if either missing, pair is allowed)
   - Removes all digits from BrowseAuthor during normalization
   - Pairs only within the SAME PrimaryMARCTOMID (format)
   - Emits ONE canonical pair per group: (MinBibId → OtherBibId)
   - Style: functions on one line; JOIN flush-left; ON indented one tab; aliases aligned per statement
   ============================================================ */

set nocount on;

------------------------------------------------------------
-- 0) parameters and punctuation map
------------------------------------------------------------
declare @punct_from nvarchar(100) = N'.,;:!?''"()[]{}<>/\|-_+#*&$%@^~`=';
declare @punct_to   nvarchar(100) = replicate(N' ', len(@punct_from));
declare @digits     nvarchar(10)  = N'0123456789';
declare @spaces10   nvarchar(10)  = replicate(N' ', 10);

------------------------------------------------------------
-- 1) base bib rows (include BrowseAuthor and PublicationYear)
------------------------------------------------------------
drop table if exists #Bib;
select
    br.BibliographicRecordID [BibliographicRecordID],
    br.BrowseTitle           [BrowseTitle],
    br.BrowseAuthor          [BrowseAuthor],
    br.PrimaryMARCTOMID      [PrimaryMARCTOMID],
    br.PublicationYear       [PublicationYear]
into #Bib
from polaris.polaris.BibliographicRecords br
where br.RecordStatusID = 1
	and br.PrimaryMARCTOMID not in (36);

create index IX_Bib__Marc_Bib
    on #Bib(PrimaryMARCTOMID, BibliographicRecordID);

------------------------------------------------------------
-- 2) distinct isbn / upc
------------------------------------------------------------
drop table if exists #DistinctIsbn;
select distinct
    bii.BibliographicRecordID [BibliographicRecordID],
    b.PrimaryMARCTOMID        [PrimaryMARCTOMID],
    bii.ISBNString            [ISBNString]
into #DistinctIsbn
from polaris.polaris.BibliographicISBNIndex bii
join #Bib b
    on b.BibliographicRecordID = bii.BibliographicRecordID
where (bii.IsValidISBN is null or bii.IsValidISBN = 1)
  and bii.ISBNString is not null
  and bii.ISBNString <> N'';

create index IX_DistinctIsbn__Marc_Isbn_Bib
    on #DistinctIsbn(PrimaryMARCTOMID, ISBNString, BibliographicRecordID);

drop table if exists #DistinctUpc;
select distinct
    bui.BibliographicRecordID [BibliographicRecordID],
    b.PrimaryMARCTOMID        [PrimaryMARCTOMID],
    bui.UPCNumber             [UPCNumber]
into #DistinctUpc
from polaris.polaris.BibliographicUPCIndex bui
join #Bib b
    on b.BibliographicRecordID = bui.BibliographicRecordID
where bui.UPCNumber is not null
  and bui.UPCNumber <> N'';

create index IX_DistinctUpc__Marc_Upc_Bib
    on #DistinctUpc(PrimaryMARCTOMID, UPCNumber, BibliographicRecordID);

------------------------------------------------------------
-- 3) TITLE normalization → tokens
------------------------------------------------------------
drop table if exists #TitleTokens;
create table #TitleTokens
(
    BibliographicRecordID int not null,
    PrimaryMARCTOMID      int not null,
    token                 nvarchar(400) not null
);

insert into #TitleTokens(BibliographicRecordID, PrimaryMARCTOMID, token)
select
    b.BibliographicRecordID [BibliographicRecordID],
    b.PrimaryMARCTOMID      [PrimaryMARCTOMID],
    s.value                 [token]
from #Bib b
cross apply string_split(lower(ltrim(rtrim(translate(coalesce(b.BrowseTitle, N''), @punct_from, @punct_to)))), N' ') s
where s.value <> N''
  and s.value not in (N'a',N'an',N'the',N'and',N'of',N'for',N'to',N'in',N'on',N'by',N'with',N'vol',N'volume',N'ed',N'edition',N'pt',N'part',N'bk',N'book',N'disc',N'cd',N'dvd',N'bluray',N'blu-ray');

create index IX_TitleTokens__Marc_Bib_Token
    on #TitleTokens(PrimaryMARCTOMID, BibliographicRecordID, token);

drop table if exists #Titles;
;with DistinctTokens as
(
    select distinct BibliographicRecordID, PrimaryMARCTOMID, token from #TitleTokens
),
Keys as
(
    select
        d.BibliographicRecordID [BibliographicRecordID],
        d.PrimaryMARCTOMID      [PrimaryMARCTOMID],
        string_agg(d.token, N' ') within group (order by d.token) [TitleKey]
    from DistinctTokens d
    group by d.BibliographicRecordID, d.PrimaryMARCTOMID
    having count(*) > 0
)
select
    k.BibliographicRecordID [BibliographicRecordID],
    k.PrimaryMARCTOMID      [PrimaryMARCTOMID],
    k.TitleKey              [TitleKey],
    convert(varbinary(32), hashbytes('SHA2_256', convert(varbinary(max), k.TitleKey))) [TitleKeyHash]
into #Titles
from Keys k;

create index IX_Titles__Marc_Hash_Bib
    on #Titles(PrimaryMARCTOMID, TitleKeyHash, BibliographicRecordID)
    include (TitleKey);

------------------------------------------------------------
-- 4) AUTHOR normalization → tokens (digits removed)
------------------------------------------------------------
drop table if exists #AuthorTokens;
create table #AuthorTokens
(
    BibliographicRecordID int not null,
    PrimaryMARCTOMID      int not null,
    token                 nvarchar(200) not null
);

insert into #AuthorTokens(BibliographicRecordID, PrimaryMARCTOMID, token)
select
    b.BibliographicRecordID [BibliographicRecordID],
    b.PrimaryMARCTOMID      [PrimaryMARCTOMID],
    s.value                 [token]
from #Bib b
cross apply string_split(lower(ltrim(rtrim(translate(translate(coalesce(b.BrowseAuthor, N''), @punct_from, @punct_to), @digits, @spaces10)))), N' ') s
where s.value <> N''
  and s.value not in (N'and',N'with',N'jr',N'sr',N'iii',N'iv',N'Author', N'Illustrator', N'Performer', N'Composer');

create index IX_AuthorTokens__Marc_Bib_Token
    on #AuthorTokens(PrimaryMARCTOMID, BibliographicRecordID, token);

drop table if exists #Authors;
;with DistinctAuthTokens as
(
    select distinct BibliographicRecordID, PrimaryMARCTOMID, token from #AuthorTokens
)
select
    d.BibliographicRecordID [BibliographicRecordID],
    d.PrimaryMARCTOMID      [PrimaryMARCTOMID],
    string_agg(d.token, N' ') within group (order by d.token) [AuthorKey]
into #Authors
from DistinctAuthTokens d
group by d.BibliographicRecordID, d.PrimaryMARCTOMID;

create index IX_Authors__Marc_Key_Bib
    on #Authors(PrimaryMARCTOMID, AuthorKey, BibliographicRecordID);

------------------------------------------------------------
-- 5) canonical pairing (author- & year-aware)
------------------------------------------------------------
drop table if exists #PairsIsbn;
;with IsbnBuckets as
(
    select
        i.PrimaryMARCTOMID      [PrimaryMARCTOMID],
        i.ISBNString            [ISBNString],
        i.BibliographicRecordID [BibliographicRecordID],
        min(i.BibliographicRecordID) over (partition by i.PrimaryMARCTOMID, i.ISBNString) [MinBibId]
    from #DistinctIsbn i
)
select
    b.PrimaryMARCTOMID      [PrimaryMARCTOMID],
    b.MinBibId              [LeftBibId],
    b.BibliographicRecordID [RightBibId],
    cast('ISBN' as varchar(10)) [MatchType],
    b.ISBNString            [MatchValue]
into #PairsIsbn
from IsbnBuckets b
join #Authors la
    on la.PrimaryMARCTOMID = b.PrimaryMARCTOMID and la.BibliographicRecordID = b.MinBibId
join #Authors ra
    on ra.PrimaryMARCTOMID = b.PrimaryMARCTOMID and ra.BibliographicRecordID = b.BibliographicRecordID
join #Bib lb
    on lb.BibliographicRecordID = b.MinBibId
join #Bib rb
    on rb.BibliographicRecordID = b.BibliographicRecordID
where b.BibliographicRecordID > b.MinBibId
  and (la.AuthorKey is null or ra.AuthorKey is null or la.AuthorKey = ra.AuthorKey)
  and (lb.PublicationYear is null or rb.PublicationYear is null or lb.PublicationYear = rb.PublicationYear);

create index IX_PairsIsbn__Marc_Left_Right
    on #PairsIsbn(PrimaryMARCTOMID, LeftBibId, RightBibId);

drop table if exists #PairsUpc;
;with UpcBuckets as
(
    select
        u.PrimaryMARCTOMID      [PrimaryMARCTOMID],
        u.UPCNumber             [UPCNumber],
        u.BibliographicRecordID [BibliographicRecordID],
        min(u.BibliographicRecordID) over (partition by u.PrimaryMARCTOMID, u.UPCNumber) [MinBibId]
    from #DistinctUpc u
)
select
    b.PrimaryMARCTOMID      [PrimaryMARCTOMID],
    b.MinBibId              [LeftBibId],
    b.BibliographicRecordID [RightBibId],
    cast('UPC' as varchar(10)) [MatchType],
    cast(b.UPCNumber as varchar(32)) [MatchValue]
into #PairsUpc
from UpcBuckets b
join #Authors la
    on la.PrimaryMARCTOMID = b.PrimaryMARCTOMID and la.BibliographicRecordID = b.MinBibId
join #Authors ra
    on ra.PrimaryMARCTOMID = b.PrimaryMARCTOMID and ra.BibliographicRecordID = b.BibliographicRecordID
join #Bib lb
    on lb.BibliographicRecordID = b.MinBibId
join #Bib rb
    on rb.BibliographicRecordID = b.BibliographicRecordID
where b.BibliographicRecordID > b.MinBibId
  and (la.AuthorKey is null or ra.AuthorKey is null or la.AuthorKey = ra.AuthorKey)
  and (lb.PublicationYear is null or rb.PublicationYear is null or lb.PublicationYear = rb.PublicationYear);

create index IX_PairsUpc__Marc_Left_Right
    on #PairsUpc(PrimaryMARCTOMID, LeftBibId, RightBibId);

drop table if exists #PairsTitle;
;with TitleBuckets as
(
    select
        t.PrimaryMARCTOMID      [PrimaryMARCTOMID],
        t.TitleKey              [TitleKey],
        t.BibliographicRecordID [BibliographicRecordID],
        min(t.BibliographicRecordID) over (partition by t.PrimaryMARCTOMID, t.TitleKey) [MinBibId]
    from #Titles t
)
select
    b.PrimaryMARCTOMID      [PrimaryMARCTOMID],
    b.MinBibId              [LeftBibId],
    b.BibliographicRecordID [RightBibId],
    cast('TITLE' as varchar(10)) [MatchType],
    b.TitleKey              [MatchValue]
into #PairsTitle
from TitleBuckets b
join #Authors la
    on la.PrimaryMARCTOMID = b.PrimaryMARCTOMID and la.BibliographicRecordID = b.MinBibId
join #Authors ra
    on ra.PrimaryMARCTOMID = b.PrimaryMARCTOMID and ra.BibliographicRecordID = b.BibliographicRecordID
join #Bib lb
    on lb.BibliographicRecordID = b.MinBibId
join #Bib rb
    on rb.BibliographicRecordID = b.BibliographicRecordID
where b.BibliographicRecordID > b.MinBibId
  and (la.AuthorKey is null or ra.AuthorKey is null or la.AuthorKey = ra.AuthorKey)
  and (lb.PublicationYear is null or rb.PublicationYear is null or lb.PublicationYear = rb.PublicationYear);

create index IX_PairsTitle__Marc_Left_Right
    on #PairsTitle(PrimaryMARCTOMID, LeftBibId, RightBibId);

select
    p.MatchType
    ,p.MatchValue
    ,p.PrimaryMARCTOMID
    ,l.BibliographicRecordID [LeftBibId]
    ,r.BibliographicRecordID [RightBibId]
	,l.BrowseTitle [LeftTitle]
into #tempMatches
from
(
    select * from #PairsIsbn
    union all
    select * from #PairsUpc
    union all
    select * from #PairsTitle
) p
join #Bib l
    on l.BibliographicRecordID = p.LeftBibId
join #Bib r
    on r.BibliographicRecordID = p.RightBibId

select distinct tm.PrimaryMARCTOMID, tm.LeftBibId, tm.RightBibId
into #tempPairs
from #tempMatches tm


insert into BibDedupe.Pairs (PrimaryMARCTOMID, LeftBibId, RightBibId)
output INSERTED.PairId, INSERTED.LeftBibId, INSERTED.RightBibId into #tempPairs
select tp.PrimaryMARCTOMID, tp.LeftBibId, tp.RightBibId
from #tempPairs tp


insert into BibDedupe.PairMatches
select p.PairId
		,tm.MatchType
		,tm.MatchValue
from BibDedupe.Pairs p
join #tempMatches tm
	on tm.LeftBibId = p.LeftBibId and tm.RightBibId = p.RightBibId


drop table #tempMatches
drop table #tempPairs