/*
Assigns BibDedupe runtime permissions for one database principal.

Usage examples:
1) SQL login or Windows/AD login already created at the server level:
   - Set @PrincipalType = 'LOGIN'
   - Set @PrincipalName = 'my_sql_login' or 'DOMAIN\\some.user'

2) Azure SQL / Entra principal (no server login mapping):
   - Set @PrincipalType = 'EXTERNAL'
   - Set @PrincipalName = 'user@contoso.com' (or Entra group display name)

3) Contained SQL database user:
   - Set @PrincipalType = 'CONTAINED'
   - Set @ContainedUserPassword

This script grants per-object rights in the Polaris database for cross-database
objects used by BibDedupe procedures.
*/

DECLARE @DatabaseName SYSNAME = N'clcdb';
DECLARE @PrincipalName SYSNAME = N'REPLACE_ME';
DECLARE @PrincipalType NVARCHAR(20) = N'LOGIN'; -- LOGIN | EXTERNAL | CONTAINED
DECLARE @ContainedUserPassword NVARCHAR(256) = N'ChangeMe_StrongPassword!'; -- used only for CONTAINED
DECLARE @PolarisDatabaseName SYSNAME = N'Polaris';

IF DB_ID(@DatabaseName) IS NULL
    THROW 50001, 'Target database does not exist.', 1;

DECLARE @Sql NVARCHAR(MAX) = N'USE ' + QUOTENAME(@DatabaseName) + N';

IF USER_ID(@PrincipalName) IS NULL
BEGIN
    IF @PrincipalType = N''LOGIN''
        EXEC(N''CREATE USER '' + QUOTENAME(@PrincipalName) + N'' FOR LOGIN '' + QUOTENAME(@PrincipalName));
    ELSE IF @PrincipalType = N''EXTERNAL''
        EXEC(N''CREATE USER '' + QUOTENAME(@PrincipalName) + N'' FROM EXTERNAL PROVIDER'');
    ELSE IF @PrincipalType = N''CONTAINED''
        EXEC(N''CREATE USER '' + QUOTENAME(@PrincipalName) + N'' WITH PASSWORD = '' + QUOTENAME(@ContainedUserPassword, ''''''''));
    ELSE
        THROW 50002, ''Unsupported @PrincipalType. Use LOGIN, EXTERNAL, or CONTAINED.'', 1;
END

-- Core runtime access for BibDedupe + Hangfire tables.
IF IS_ROLEMEMBER(N''db_datareader'', @PrincipalName) <> 1
    EXEC(N''ALTER ROLE [db_datareader] ADD MEMBER '' + QUOTENAME(@PrincipalName));

IF IS_ROLEMEMBER(N''db_datawriter'', @PrincipalName) <> 1
    EXEC(N''ALTER ROLE [db_datawriter] ADD MEMBER '' + QUOTENAME(@PrincipalName));

-- Table-valued functions and any stored procedures in BibDedupe schema.
EXEC(N''GRANT EXECUTE ON SCHEMA::[BibDedupe] TO '' + QUOTENAME(@PrincipalName));

-- Hangfire SQL storage initial schema creation/upgrade requires elevated DDL rights.
IF IS_ROLEMEMBER(N''db_ddladmin'', @PrincipalName) <> 1
    EXEC(N''ALTER ROLE [db_ddladmin] ADD MEMBER '' + QUOTENAME(@PrincipalName));
';

EXEC sys.sp_executesql
    @Sql,
    N'@PrincipalName SYSNAME, @PrincipalType NVARCHAR(20), @ContainedUserPassword NVARCHAR(256)',
    @PrincipalName = @PrincipalName,
    @PrincipalType = @PrincipalType,
    @ContainedUserPassword = @ContainedUserPassword;

IF DB_ID(@PolarisDatabaseName) IS NULL
    THROW 50003, 'Polaris database does not exist.', 1;

DECLARE @PolarisSql NVARCHAR(MAX) = N'USE ' + QUOTENAME(@PolarisDatabaseName) + N';

IF USER_ID(@PrincipalName) IS NULL
BEGIN
    IF @PrincipalType = N''LOGIN''
        EXEC(N''CREATE USER '' + QUOTENAME(@PrincipalName) + N'' FOR LOGIN '' + QUOTENAME(@PrincipalName));
    ELSE IF @PrincipalType = N''EXTERNAL''
        EXEC(N''CREATE USER '' + QUOTENAME(@PrincipalName) + N'' FROM EXTERNAL PROVIDER'');
    ELSE IF @PrincipalType = N''CONTAINED''
        EXEC(N''CREATE USER '' + QUOTENAME(@PrincipalName) + N'' WITH PASSWORD = '' + QUOTENAME(@ContainedUserPassword, ''''''''));
    ELSE
        THROW 50004, ''Unsupported @PrincipalType. Use LOGIN, EXTERNAL, or CONTAINED.'', 1;
END

-- Per-object permissions used by BibDedupe cross-database queries and merge workflow.
EXEC(N''GRANT SELECT ON OBJECT::[Polaris].[BibliographicRecords] TO '' + QUOTENAME(@PrincipalName));
EXEC(N''GRANT SELECT ON OBJECT::[Polaris].[MARCTypeOfMaterial] TO '' + QUOTENAME(@PrincipalName));
EXEC(N''GRANT SELECT ON OBJECT::[Polaris].[SysHoldRequests] TO '' + QUOTENAME(@PrincipalName));
EXEC(N''GRANT SELECT ON OBJECT::[Polaris].[PolarisUsers] TO '' + QUOTENAME(@PrincipalName));
EXEC(N''GRANT SELECT ON OBJECT::[Polaris].[GroupUsers] TO '' + QUOTENAME(@PrincipalName));
EXEC(N''GRANT SELECT ON OBJECT::[Polaris].[Groups] TO '' + QUOTENAME(@PrincipalName));

EXEC(N''GRANT SELECT, INSERT, UPDATE ON OBJECT::[Polaris].[BibliographicTags] TO '' + QUOTENAME(@PrincipalName));
EXEC(N''GRANT INSERT ON OBJECT::[Polaris].[BibliographicSubfields] TO '' + QUOTENAME(@PrincipalName));

-- Entry procedures called by BibDedupe merge processing.
EXEC(N''GRANT EXECUTE ON OBJECT::[Polaris].[Cat_RetainBibRecordDataByID] TO '' + QUOTENAME(@PrincipalName));
EXEC(N''GRANT EXECUTE ON OBJECT::[Polaris].[UnIndexBib] TO '' + QUOTENAME(@PrincipalName));
EXEC(N''GRANT EXECUTE ON OBJECT::[Polaris].[Cat_ReassignBibRecordLinks] TO '' + QUOTENAME(@PrincipalName));
EXEC(N''GRANT EXECUTE ON OBJECT::[Polaris].[Cat_DeleteBibRecordProcessing] TO '' + QUOTENAME(@PrincipalName));
EXEC(N''GRANT EXECUTE ON OBJECT::[Polaris].[IndexBib] TO '' + QUOTENAME(@PrincipalName));

-- Also grant EXECUTE on nested modules referenced by the seeded entry procedures.
DECLARE @ModuleName NVARCHAR(517);

    DECLARE nested_module_cursor CURSOR LOCAL FAST_FORWARD FOR
    WITH SeedModules AS (
        SELECT OBJECT_ID(N''[Polaris].[Cat_RetainBibRecordDataByID]'') AS ObjectId
        UNION ALL SELECT OBJECT_ID(N''[Polaris].[UnIndexBib]'')
        UNION ALL SELECT OBJECT_ID(N''[Polaris].[Cat_ReassignBibRecordLinks]'')
        UNION ALL SELECT OBJECT_ID(N''[Polaris].[Cat_DeleteBibRecordProcessing]'')
        UNION ALL SELECT OBJECT_ID(N''[Polaris].[IndexBib]'')
    ), DependencyTree AS (
        SELECT sm.ObjectId
        FROM SeedModules sm
        WHERE sm.ObjectId IS NOT NULL

        UNION

        SELECT sed.referenced_id
        FROM DependencyTree dt
        JOIN sys.sql_expression_dependencies sed
            ON sed.referencing_id = dt.ObjectId
        JOIN sys.objects o
            ON o.object_id = sed.referenced_id
        WHERE sed.referenced_id IS NOT NULL
          AND o.type IN (N''P'', N''PC'', N''FN'', N''IF'', N''TF'', N''FS'', N''FT'')
    )
    SELECT DISTINCT QUOTENAME(OBJECT_SCHEMA_NAME(dt.ObjectId)) + N''.'' + QUOTENAME(OBJECT_NAME(dt.ObjectId)) AS ModuleName
    FROM DependencyTree dt
    WHERE dt.ObjectId IS NOT NULL;

    OPEN nested_module_cursor;

    FETCH NEXT FROM nested_module_cursor INTO @ModuleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC(N''GRANT EXECUTE ON OBJECT::'' + @ModuleName + N'' TO '' + QUOTENAME(@PrincipalName));
        FETCH NEXT FROM nested_module_cursor INTO @ModuleName;
    END

    CLOSE nested_module_cursor;
    DEALLOCATE nested_module_cursor;
';

    EXEC sys.sp_executesql
        @PolarisSql,
        N'@PrincipalName SYSNAME, @PrincipalType NVARCHAR(20), @ContainedUserPassword NVARCHAR(256)',
        @PrincipalName = @PrincipalName,
        @PrincipalType = @PrincipalType,
        @ContainedUserPassword = @ContainedUserPassword;
