USE SecureCampus;
GO

-- Drop old policy + function if exists
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = 'StudentProfileRLS')
    DROP SECURITY POLICY StudentProfileRLS;
GO

IF OBJECT_ID('dbo.fn_student_rls', 'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_student_rls;
GO

-- New predicate uses SESSION_CONTEXT('UserID') + allows Admin to see all
CREATE FUNCTION dbo.fn_student_rls(@UserID NVARCHAR(50))
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS fn_access
    WHERE
        -- student can see only their own row
        @UserID = CONVERT(NVARCHAR(50), SESSION_CONTEXT(N'UserID'))
        OR
        -- admin can see all rows
        CONVERT(NVARCHAR(30), SESSION_CONTEXT(N'Role')) = N'Admin';
GO

CREATE SECURITY POLICY StudentProfileRLS
ADD FILTER PREDICATE dbo.fn_student_rls(UserID)
ON dbo.StudentProfile
WITH (STATE = ON);
GO
