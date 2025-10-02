IF OBJECT_ID('BibDedupe.UserClaims', 'V') IS NOT NULL
    DROP VIEW BibDedupe.UserClaims;
GO

CREATE VIEW BibDedupe.UserClaims
AS
-- Grant application access by returning at least one row with Claim = 'Access'.
-- Assign additional roles (for example 'Administrator') by returning more rows for the same user.
SELECT TOP (0)
    CAST(NULL AS NVARCHAR(256)) AS UserEmail,
    CAST(NULL AS NVARCHAR(256)) AS Claim;
GO
