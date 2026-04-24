/*
    Assign BibDedupe application database permissions for an existing server login.

    Supports either:
      - SQL login names (e.g., bibdedupe_app)
      - Windows/AD login names (e.g., CONTOSO\svc-bibdedupe)

    Notes:
      - This script assumes the server login already exists in master.
      - It creates the corresponding database user when missing.
      - Role grants are guarded so the script can be rerun safely.
*/

DECLARE @ServerLoginName sysname = N'REPLACE_WITH_LOGIN_NAME';
-- Use the login name exactly as shown in sys.server_principals (single backslash for AD logins).

SET @ServerLoginName = LTRIM(RTRIM(@ServerLoginName));

IF NOT EXISTS (
    SELECT 1
    FROM sys.server_principals sp
    WHERE sp.name = @ServerLoginName
)
BEGIN
    THROW 50000, 'Server login not found in sys.server_principals. Use the exact login name as stored on the SQL Server instance, then rerun this script.', 1;
END;

DECLARE @QuotedLoginName nvarchar(258) = QUOTENAME(@ServerLoginName);
DECLARE @DatabaseName sysname;
DECLARE @Sql nvarchar(max);

DECLARE @TargetDatabases TABLE (DatabaseName sysname PRIMARY KEY);
INSERT INTO @TargetDatabases (DatabaseName)
VALUES
    (N'clcdb'),
    (N'Polaris');

DECLARE db_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT d.DatabaseName
FROM @TargetDatabases d
WHERE DB_ID(d.DatabaseName) IS NOT NULL;

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @DatabaseName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = N'
USE ' + QUOTENAME(@DatabaseName) + N';

DECLARE @ExistingUserSid varbinary(85) = (
    SELECT dp.sid
    FROM sys.database_principals dp
    WHERE dp.name = @LoginName
);
DECLARE @LoginSid varbinary(85) = SUSER_SID(@LoginName);

IF @ExistingUserSid IS NULL
BEGIN
    CREATE USER ' + @QuotedLoginName + N' FOR LOGIN ' + @QuotedLoginName + N';
END
ELSE IF @ExistingUserSid <> @LoginSid
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.database_principals dp
        WHERE dp.name = @LoginName
          AND dp.authentication_type_desc = N''DATABASE''
    )
    BEGIN
        THROW 50001, ''Contained database user exists with the same name. Drop/rename that user before mapping this server login.'', 1;
    END;

    ALTER USER ' + @QuotedLoginName + N' WITH LOGIN = ' + @QuotedLoginName + N';
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members drm
    INNER JOIN sys.database_principals roles ON roles.principal_id = drm.role_principal_id
    INNER JOIN sys.database_principals members ON members.principal_id = drm.member_principal_id
    WHERE roles.name = N''db_datareader''
      AND members.name = @LoginName
)
BEGIN
    ALTER ROLE db_datareader ADD MEMBER ' + @QuotedLoginName + N';
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members drm
    INNER JOIN sys.database_principals roles ON roles.principal_id = drm.role_principal_id
    INNER JOIN sys.database_principals members ON members.principal_id = drm.member_principal_id
    WHERE roles.name = N''db_datawriter''
      AND members.name = @LoginName
)
BEGIN
    ALTER ROLE db_datawriter ADD MEMBER ' + @QuotedLoginName + N';
END;

GRANT EXECUTE TO ' + @QuotedLoginName + N';
';

    EXEC sys.sp_executesql
        @Sql,
        N'@LoginName sysname',
        @LoginName = @ServerLoginName;

    FETCH NEXT FROM db_cursor INTO @DatabaseName;
END;

CLOSE db_cursor;
DEALLOCATE db_cursor;
