IF OBJECT_ID('BibDedupe.UserClaims', 'V') IS NOT NULL
    DROP VIEW BibDedupe.UserClaims;
GO

IF OBJECT_ID('BibDedupe.UserClaims', 'U') IS NULL
BEGIN
    CREATE TABLE BibDedupe.UserClaims
    (
        UserEmail NVARCHAR(256) NOT NULL,
        Claim NVARCHAR(256) NOT NULL,
        CONSTRAINT PK_UserClaims PRIMARY KEY NONCLUSTERED (UserEmail, Claim)
    );
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints
        WHERE name = 'PK_UserClaims'
          AND parent_object_id = OBJECT_ID('BibDedupe.UserClaims')
    )
    BEGIN
        ALTER TABLE BibDedupe.UserClaims
            ADD CONSTRAINT PK_UserClaims PRIMARY KEY NONCLUSTERED (UserEmail, Claim);
    END
END
GO
