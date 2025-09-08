IF OBJECT_ID('dbo.BibDupePairDecisions','U') IS NOT NULL
    DROP TABLE dbo.BibDupePairDecisions;
GO
CREATE TABLE dbo.BibDupePairDecisions (
    DecisionTimestamp DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UserEmail NVARCHAR(256) NOT NULL,
    KeptBibId INT NOT NULL,
    DeletedBibId INT NULL,
    Action INT NOT NULL -- 1 = merge, 2 = skip, 3 = keep both
);
GO
