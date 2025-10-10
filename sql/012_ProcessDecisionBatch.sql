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
    DECLARE @BatchId INT;
    DECLARE @Succeeded BIT;
    DECLARE @ErrorMessage NVARCHAR(2000);

    SELECT TOP 1 @BatchId = BatchId
    FROM BibDedupe.DecisionBatches
    WHERE UserEmail = @UserEmail
      AND CompletedAt IS NULL
    ORDER BY StartedAt DESC;

    IF @BatchId IS NULL
    BEGIN
        RAISERROR('No pending decision batch found for %s.', 16, 1, @UserEmail);
        RETURN;
    END;

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

            SET @Succeeded = 1;
        END TRY
        BEGIN CATCH
            SET @Succeeded = 0;
            SET @ErrorMessage = LEFT(ERROR_MESSAGE(), 2000);
        END CATCH;

        INSERT INTO BibDedupe.DecisionBatchResults (BatchId, LeftBibId, RightBibId, ActionId, Succeeded, ErrorMessage)
        VALUES (@BatchId, @LeftBibId, @RightBibId, @ActionId, @Succeeded, @ErrorMessage);

        FETCH NEXT FROM decision_cursor INTO @LeftBibId, @RightBibId, @ActionId;

        IF @@FETCH_STATUS = 0
        BEGIN
            WAITFOR DELAY '00:01:00';
        END
    END

    CLOSE decision_cursor;
    DEALLOCATE decision_cursor;

    DELETE FROM BibDedupe.DecisionQueue WHERE UserEmail = @UserEmail;
END
GO
