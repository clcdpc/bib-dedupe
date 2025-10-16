IF OBJECT_ID('BibDedupe.DecisionQueue','U') IS NOT NULL
    DROP TABLE BibDedupe.DecisionQueue;
GO
CREATE TABLE BibDedupe.DecisionQueue (
    UserEmail NVARCHAR(256) NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    ActionId INT NOT NULL,
    KeptBibId AS (
        CASE ActionId
            WHEN 1 THEN LeftBibId
            WHEN 4 THEN RightBibId
            ELSE NULL
        END
    ) PERSISTED,
    DeletedBibId AS (
        CASE ActionId
            WHEN 1 THEN RightBibId
            WHEN 4 THEN LeftBibId
            ELSE NULL
        END
    ) PERSISTED,
    CONSTRAINT PK_DecisionQueue PRIMARY KEY (UserEmail, LeftBibId, RightBibId),
    CONSTRAINT FK_DecisionQueue_ActionId FOREIGN KEY (ActionId)
        REFERENCES BibDedupe.Actions(ActionId)
);
GO
