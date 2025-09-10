IF OBJECT_ID('BibDedupe.Queue','U') IS NOT NULL
    DROP TABLE BibDedupe.Queue;
GO
CREATE TABLE BibDedupe.Queue (
    UserEmail NVARCHAR(256) NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    ActionId INT NOT NULL,
    CONSTRAINT PK_Queue PRIMARY KEY (UserEmail, LeftBibId, RightBibId),
    CONSTRAINT FK_Queue_ActionId FOREIGN KEY (ActionId)
        REFERENCES BibDedupe.Actions(ActionId)
);
GO
