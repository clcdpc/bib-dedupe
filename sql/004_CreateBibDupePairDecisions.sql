IF OBJECT_ID('dbo.BibDupeActions','U') IS NOT NULL
    DROP TABLE dbo.BibDupeActions;
GO
CREATE TABLE dbo.BibDupeActions (
    ActionId INT NOT NULL PRIMARY KEY,
    ActionName NVARCHAR(50) NOT NULL
);
INSERT INTO dbo.BibDupeActions (ActionId, ActionName) VALUES (1, 'merge'), (2, 'skip'), (3, 'keep both');
GO

IF OBJECT_ID('dbo.BibDupePairDecisions','U') IS NOT NULL
    DROP TABLE dbo.BibDupePairDecisions;
GO
CREATE TABLE dbo.BibDupePairDecisions (
    DecisionTimestamp DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UserEmail NVARCHAR(256) NOT NULL,
    KeptBibId INT NOT NULL,
    DeletedBibId INT NULL,
    ActionId INT NOT NULL,
    CONSTRAINT FK_BibDupePairDecisions_ActionId FOREIGN KEY (ActionId)
        REFERENCES dbo.BibDupeActions(ActionId)
);
GO
