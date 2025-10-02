IF OBJECT_ID('BibDedupe.DecisionQueue','U') IS NOT NULL
    DROP TABLE BibDedupe.DecisionQueue;
GO
IF OBJECT_ID('BibDedupe.PairDecisions','U') IS NOT NULL
    DROP TABLE BibDedupe.PairDecisions;
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
