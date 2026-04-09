IF OBJECT_ID('BibDedupe.UserClaims', 'V') IS NOT NULL
    DROP VIEW BibDedupe.UserClaims;
GO

IF OBJECT_ID('BibDedupe.UserClaims', 'U') IS NOT NULL
    DROP TABLE BibDedupe.UserClaims;
GO

CREATE TABLE BibDedupe.UserClaims
(
    UserEmail NVARCHAR(256) NOT NULL,
    Claim NVARCHAR(256) NOT NULL,
    CONSTRAINT PK_UserClaims PRIMARY KEY (UserEmail, Claim)
);
GO
