IF OBJECT_ID('BibDedupe.DecisionBatchResults','U') IS NOT NULL
    DROP TABLE BibDedupe.DecisionBatchResults;
GO

CREATE TABLE BibDedupe.DecisionBatchResults
(
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    BatchId INT NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    ActionId INT NOT NULL,
    Succeeded BIT NOT NULL,
    ErrorMessage NVARCHAR(1024) NULL,
    ProcessedAt DATETIME2 NOT NULL CONSTRAINT DF_DecisionBatchResults_ProcessedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_DecisionBatchResults_Batches FOREIGN KEY (BatchId)
        REFERENCES BibDedupe.DecisionBatches(BatchId),
    CONSTRAINT FK_DecisionBatchResults_Action FOREIGN KEY (ActionId)
        REFERENCES BibDedupe.Actions(ActionId)
);
GO

CREATE NONCLUSTERED INDEX IX_DecisionBatchResults_BatchId
    ON BibDedupe.DecisionBatchResults(BatchId, ProcessedAt);
GO
