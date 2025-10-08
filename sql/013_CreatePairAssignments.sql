IF OBJECT_ID('BibDedupe.PairAssignments','U') IS NOT NULL
    DROP TABLE BibDedupe.PairAssignments;
GO
CREATE TABLE BibDedupe.PairAssignments (
    LeftBibId INT NOT NULL,
    RightBibId INT NOT NULL,
    UserEmail NVARCHAR(256) NOT NULL,
    AssignedAt DATETIMEOFFSET NOT NULL CONSTRAINT DF_PairAssignments_AssignedAt DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT PK_PairAssignments PRIMARY KEY (LeftBibId, RightBibId)
);
GO
CREATE NONCLUSTERED INDEX IX_PairAssignments_UserEmail ON BibDedupe.PairAssignments (UserEmail);
GO
