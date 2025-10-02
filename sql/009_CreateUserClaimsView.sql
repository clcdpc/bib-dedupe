IF OBJECT_ID('BibDedupe.UserClaims', 'V') IS NOT NULL
    DROP VIEW BibDedupe.UserClaims;
GO

CREATE VIEW BibDedupe.UserClaims
AS
-- Grant application access by returning at least one row with ClaimValue = 'Access'.
-- Assign additional roles (for example 'Administrator') by returning more rows for the same user.
-- Each value is added to the caller as a role claim, so the claim type column is optional.
SELECT TOP (0)
    CAST(NULL AS NVARCHAR(256)) AS UserEmail,
    CAST(NULL AS NVARCHAR(128)) AS ClaimType,
    CAST(NULL AS NVARCHAR(256)) AS ClaimValue;
GO
