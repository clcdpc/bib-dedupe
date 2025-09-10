IF OBJECT_ID('dbo.DecisionQueue','U') IS NOT NULL
    DROP TABLE dbo.DecisionQueue;
GO
CREATE TABLE dbo.DecisionQueue (
    UserEmail NVARCHAR(256) NOT NULL,
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    Action INT NOT NULL,
    CONSTRAINT PK_DecisionQueue PRIMARY KEY (UserEmail, LeftBibId, RightBibId)
);
GO
