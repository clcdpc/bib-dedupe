/*
    Assign BibDedupe application database permissions for an existing server login.

    Supports either:
      - SQL login names (e.g., bibdedupe_app)
      - Windows/AD login names (e.g., CONTOSO\\svc-bibdedupe)

    Notes:
      - This script assumes the server login already exists in master.
      - It creates the corresponding database user when missing.
      - Role grants are guarded so the script can be rerun safely.
*/

DECLARE @ServerLoginName sysname = N'REPLACE_WITH_LOGIN_NAME';

IF SUSER_ID(@ServerLoginName) IS NULL
BEGIN
    THROW 50000, 'Server login not found. Create the login first, then rerun this script.', 1;
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

IF USER_ID(@LoginName) IS NULL
BEGIN
    CREATE USER ' + @QuotedLoginName + N' FOR LOGIN ' + @QuotedLoginName + N';
END;

IF IS_ROLEMEMBER(N''db_datareader'', @LoginName) <> 1
BEGIN
    ALTER ROLE db_datareader ADD MEMBER ' + @QuotedLoginName + N';
END;

IF IS_ROLEMEMBER(N''db_datawriter'', @LoginName) <> 1
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
