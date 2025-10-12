IF OBJECT_ID('BibDedupe.ProcessDecisionBatch','P') IS NOT NULL
    DROP PROCEDURE BibDedupe.ProcessDecisionBatch;
GO

CREATE PROCEDURE BibDedupe.ProcessDecisionBatch
    @UserEmail NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LeftBibId INT;
    DECLARE @RightBibId INT;
    DECLARE @ActionId INT;
    DECLARE @BatchId INT = NULL;
    DECLARE @Succeeded BIT;
    DECLARE @ErrorMessage NVARCHAR(1024);

    DECLARE decision_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT LeftBibId, RightBibId, ActionId
        FROM BibDedupe.DecisionQueue
        WHERE UserEmail = @UserEmail
        ORDER BY LeftBibId, RightBibId;

    OPEN decision_cursor;
    FETCH NEXT FROM decision_cursor INTO @LeftBibId, @RightBibId, @ActionId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Succeeded = 0;
        SET @ErrorMessage = NULL;

        BEGIN TRY
            IF (@ActionId = 1)
            BEGIN
                EXEC BibDedupe.MergePair @KeepBibId = @LeftBibId, @DeleteBibId = @RightBibId, @UserEmail = @UserEmail, @ActionId = @ActionId;
            END
            ELSE IF (@ActionId = 2)
            BEGIN
                EXEC BibDedupe.MarkNotDuplicate @LeftBibId = @LeftBibId, @RightBibId = @RightBibId, @UserEmail = @UserEmail;
            END
            ELSE IF (@ActionId = 3)
            BEGIN
                EXEC BibDedupe.Skip @LeftBibId = @LeftBibId, @RightBibId = @RightBibId, @UserEmail = @UserEmail;
            END
            ELSE IF (@ActionId = 4)
            BEGIN
                EXEC BibDedupe.MergePair @KeepBibId = @RightBibId, @DeleteBibId = @LeftBibId, @UserEmail = @UserEmail, @ActionId = @ActionId;
            END
            ELSE
            BEGIN
                SET @ErrorMessage = CONCAT('Unsupported action id ', @ActionId, '.');
            END

            IF (@ErrorMessage IS NULL)
            BEGIN
                SET @Succeeded = 1;
            END
        END TRY
        BEGIN CATCH
            SET @ErrorMessage = ERROR_MESSAGE();
        END CATCH;

        IF (@BatchId IS NULL)
        BEGIN
            SELECT TOP 1 @BatchId = BatchId
            FROM BibDedupe.DecisionBatches
            WHERE UserEmail = @UserEmail AND CompletedAt IS NULL AND FailedAt IS NULL
            ORDER BY StartedAt DESC;
        END

        IF (@BatchId IS NOT NULL)
        BEGIN
            INSERT INTO BibDedupe.DecisionBatchResults
                (BatchId, LeftBibId, RightBibId, ActionId, Succeeded, ErrorMessage, ProcessedAt)
            VALUES
                (@BatchId, @LeftBibId, @RightBibId, @ActionId, @Succeeded, CASE WHEN @ErrorMessage IS NULL THEN NULL ELSE LEFT(@ErrorMessage, 1024) END, SYSUTCDATETIME());
        END

        FETCH NEXT FROM decision_cursor INTO @LeftBibId, @RightBibId, @ActionId;
    END

    CLOSE decision_cursor;
    DEALLOCATE decision_cursor;

    DELETE FROM BibDedupe.DecisionQueue WHERE UserEmail = @UserEmail;
END
GO
