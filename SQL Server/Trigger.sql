-- Audit Trigger for Grades table

CREATE TRIGGER trg_Grades_Audit
ON Grades
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    INSERT INTO Audit_Logs
    (LogID, ActorRole, ActorUserID, Action, TargetEntity, Status)
    VALUES
    (
        NEWID(),
        'System',
        USER_NAME(),
        'Grade Modified',
        'Grades',
        'Success'
    );
END;
GO
