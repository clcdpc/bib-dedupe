/*
Assign runtime permissions for BibDedupe + Polaris access.
*/

DECLARE @DatabaseName SYSNAME = N'clcdb';
DECLARE @PolarisDatabaseName SYSNAME = N'Polaris';
DECLARE @PrincipalName SYSNAME = N'REPLACE_ME';
DECLARE @PrincipalType NVARCHAR(20) = N'LOGIN'; -- LOGIN | EXTERNAL | CONTAINED
DECLARE @ContainedUserPassword NVARCHAR(256) = N'ChangeMe_StrongPassword!'; -- only for CONTAINED
DECLARE @GrantHangfireSchemaManagement BIT = 0; -- set to 1 for Hangfire schema bootstrap/upgrade

DECLARE @PrincipalNameQuoted NVARCHAR(258) = QUOTENAME(@PrincipalName);
DECLARE @PrincipalNameLiteral NVARCHAR(520) = N'N''' + REPLACE(@PrincipalName, '''', '''''') + N'''';
DECLARE @ContainedPasswordLiteral NVARCHAR(520) = N'N''' + REPLACE(@ContainedUserPassword, '''', '''''') + N'''';
DECLARE @ContainedUserSid VARBINARY(85) = NULL;

IF @PrincipalType NOT IN (N'LOGIN', N'EXTERNAL', N'CONTAINED')
    THROW 50000, 'Unsupported @PrincipalType. Use LOGIN, EXTERNAL, or CONTAINED.', 1;

IF DB_ID(@DatabaseName) IS NULL
    THROW 50001, 'Target database does not exist.', 1;

IF DB_ID(@PolarisDatabaseName) IS NULL
    THROW 50003, 'Polaris database does not exist.', 1;

DECLARE @Sql NVARCHAR(MAX);

SET @Sql = N'USE ' + QUOTENAME(@DatabaseName) + N';

IF USER_ID(' + @PrincipalNameLiteral + N') IS NULL
BEGIN
    IF @PrincipalType = N''LOGIN''
        EXEC(N''CREATE USER ' + @PrincipalNameQuoted + N' FOR LOGIN ' + @PrincipalNameQuoted + N''');
    ELSE IF @PrincipalType = N''EXTERNAL''
        EXEC(N''CREATE USER ' + @PrincipalNameQuoted + N' FROM EXTERNAL PROVIDER'');
    ELSE
        EXEC(N''CREATE USER ' + @PrincipalNameQuoted + N' WITH PASSWORD = ' + @ContainedPasswordLiteral + N''');
END

IF IS_ROLEMEMBER(N''db_datareader'', ' + @PrincipalNameLiteral + N') <> 1
    ALTER ROLE [db_datareader] ADD MEMBER ' + @PrincipalNameQuoted + N';

IF IS_ROLEMEMBER(N''db_datawriter'', ' + @PrincipalNameLiteral + N') <> 1
    ALTER ROLE [db_datawriter] ADD MEMBER ' + @PrincipalNameQuoted + N';

GRANT EXECUTE ON SCHEMA::[BibDedupe] TO ' + @PrincipalNameQuoted + N';

IF @GrantHangfireSchemaManagement = 1 AND IS_ROLEMEMBER(N''db_ddladmin'', ' + @PrincipalNameLiteral + N') <> 1
    ALTER ROLE [db_ddladmin] ADD MEMBER ' + @PrincipalNameQuoted + N';';

EXEC sys.sp_executesql
    @Sql,
    N'@PrincipalType NVARCHAR(20), @GrantHangfireSchemaManagement BIT',
    @PrincipalType = @PrincipalType,
    @GrantHangfireSchemaManagement = @GrantHangfireSchemaManagement;

IF @PrincipalType = N'CONTAINED'
BEGIN
    SET @Sql = N'USE ' + QUOTENAME(@DatabaseName) + N';
    SELECT @ContainedUserSidOut = dp.sid
    FROM sys.database_principals dp
    WHERE dp.name = ' + @PrincipalNameLiteral + N';';

    EXEC sys.sp_executesql
        @Sql,
        N'@ContainedUserSidOut VARBINARY(85) OUTPUT',
        @ContainedUserSidOut = @ContainedUserSid OUTPUT;

    IF @ContainedUserSid IS NULL
        THROW 50005, 'Contained user SID could not be resolved from target database.', 1;
END

SET @Sql = N'USE ' + QUOTENAME(@PolarisDatabaseName) + N';

IF USER_ID(' + @PrincipalNameLiteral + N') IS NULL
BEGIN
    IF @PrincipalType = N''LOGIN''
        EXEC(N''CREATE USER ' + @PrincipalNameQuoted + N' FOR LOGIN ' + @PrincipalNameQuoted + N''');
    ELSE IF @PrincipalType = N''EXTERNAL''
        EXEC(N''CREATE USER ' + @PrincipalNameQuoted + N' FROM EXTERNAL PROVIDER'');
    ELSE
        EXEC(N''CREATE USER ' + @PrincipalNameQuoted + N' WITH PASSWORD = ' + @ContainedPasswordLiteral + N', SID = '' + CONVERT(NVARCHAR(170), @ContainedUserSid, 1));
END

GRANT SELECT ON OBJECT::[Polaris].[BibliographicRecords] TO ' + @PrincipalNameQuoted + N';
GRANT SELECT ON OBJECT::[Polaris].[MARCTypeOfMaterial] TO ' + @PrincipalNameQuoted + N';
GRANT SELECT ON OBJECT::[Polaris].[SysHoldRequests] TO ' + @PrincipalNameQuoted + N';
GRANT SELECT ON OBJECT::[Polaris].[PolarisUsers] TO ' + @PrincipalNameQuoted + N';
GRANT SELECT ON OBJECT::[Polaris].[GroupUsers] TO ' + @PrincipalNameQuoted + N';
GRANT SELECT ON OBJECT::[Polaris].[Groups] TO ' + @PrincipalNameQuoted + N';
GRANT SELECT, INSERT, UPDATE ON OBJECT::[Polaris].[BibliographicTags] TO ' + @PrincipalNameQuoted + N';
GRANT INSERT ON OBJECT::[Polaris].[BibliographicSubfields] TO ' + @PrincipalNameQuoted + N';

GRANT EXECUTE ON OBJECT::[Polaris].[Cat_RetainBibRecordDataByID] TO ' + @PrincipalNameQuoted + N';
GRANT EXECUTE ON OBJECT::[Polaris].[UnIndexBib] TO ' + @PrincipalNameQuoted + N';
GRANT EXECUTE ON OBJECT::[Polaris].[Cat_ReassignBibRecordLinks] TO ' + @PrincipalNameQuoted + N';
GRANT EXECUTE ON OBJECT::[Polaris].[Cat_DeleteBibRecordProcessing] TO ' + @PrincipalNameQuoted + N';
GRANT EXECUTE ON OBJECT::[Polaris].[IndexBib] TO ' + @PrincipalNameQuoted + N';

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
    JOIN sys.sql_expression_dependencies sed ON sed.referencing_id = dt.ObjectId
    JOIN sys.objects o ON o.object_id = sed.referenced_id
    WHERE sed.referenced_id IS NOT NULL
      AND o.type IN (N''P'', N''PC'', N''FN'', N''IF'', N''TF'', N''FS'', N''FT'')
)
SELECT DISTINCT QUOTENAME(OBJECT_SCHEMA_NAME(dt.ObjectId)) + N''.'' + QUOTENAME(OBJECT_NAME(dt.ObjectId))
FROM DependencyTree dt
WHERE dt.ObjectId IS NOT NULL;

OPEN nested_module_cursor;
FETCH NEXT FROM nested_module_cursor INTO @ModuleName;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC(N''GRANT EXECUTE ON OBJECT::'' + @ModuleName + N'' TO ' + @PrincipalNameQuoted + N''');
    FETCH NEXT FROM nested_module_cursor INTO @ModuleName;
END
CLOSE nested_module_cursor;
DEALLOCATE nested_module_cursor;';

EXEC sys.sp_executesql
    @Sql,
    N'@PrincipalType NVARCHAR(20), @ContainedUserSid VARBINARY(85)',
    @PrincipalType = @PrincipalType,
    @ContainedUserSid = @ContainedUserSid;
