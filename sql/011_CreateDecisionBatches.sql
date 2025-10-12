IF OBJECT_ID('BibDedupe.DecisionBatches','U') IS NOT NULL
    DROP TABLE BibDedupe.DecisionBatches;
GO

CREATE TABLE BibDedupe.DecisionBatches
(
    BatchId INT IDENTITY(1,1) PRIMARY KEY,
    UserEmail NVARCHAR(256) NOT NULL,
    JobId NVARCHAR(128) NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,
    FailedAt DATETIME2 NULL,
    FailureMessage NVARCHAR(1024) NULL
);
GO
