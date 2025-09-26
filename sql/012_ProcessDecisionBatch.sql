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

    DECLARE decision_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT LeftBibId, RightBibId, ActionId
        FROM BibDedupe.DecisionQueue
        WHERE UserEmail = @UserEmail
        ORDER BY LeftBibId, RightBibId;

    OPEN decision_cursor;
    FETCH NEXT FROM decision_cursor INTO @LeftBibId, @RightBibId, @ActionId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF (@ActionId = 1)
        BEGIN
            EXEC BibDedupe.MergePair @KeepBibId = @LeftBibId, @DeleteBibId = @RightBibId, @UserEmail = @UserEmail, @ActionId = @ActionId;
        END
        ELSE IF (@ActionId = 2)
        BEGIN
            EXEC BibDedupe.KeepBoth @LeftBibId = @LeftBibId, @RightBibId = @RightBibId, @UserEmail = @UserEmail;
        END
        ELSE IF (@ActionId = 3)
        BEGIN
            EXEC BibDedupe.Skip @LeftBibId = @LeftBibId, @RightBibId = @RightBibId, @UserEmail = @UserEmail;
        END
        ELSE IF (@ActionId = 4)
        BEGIN
            EXEC BibDedupe.MergePair @KeepBibId = @RightBibId, @DeleteBibId = @LeftBibId, @UserEmail = @UserEmail, @ActionId = @ActionId;
        END

        FETCH NEXT FROM decision_cursor INTO @LeftBibId, @RightBibId, @ActionId;
    END

    CLOSE decision_cursor;
    DEALLOCATE decision_cursor;

    DELETE FROM BibDedupe.DecisionQueue WHERE UserEmail = @UserEmail;
END
GO
