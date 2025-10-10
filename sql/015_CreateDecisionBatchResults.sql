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
    ProcessedAt DATETIME2 NOT NULL CONSTRAINT DF_DecisionBatchResults_ProcessedAt DEFAULT SYSDATETIME(),
    Succeeded BIT NOT NULL,
    ErrorMessage NVARCHAR(2000) NULL,
    CONSTRAINT FK_DecisionBatchResults_Batch FOREIGN KEY (BatchId)
        REFERENCES BibDedupe.DecisionBatches (BatchId)
        ON DELETE CASCADE,
    CONSTRAINT FK_DecisionBatchResults_Action FOREIGN KEY (ActionId)
        REFERENCES BibDedupe.Actions (ActionId)
);
GO
