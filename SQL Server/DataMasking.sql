-- Email masking (if Email is in [User])
ALTER TABLE dbo.[User]
ALTER COLUMN Email NVARCHAR(255) MASKED WITH (FUNCTION = 'email()');

-- FirstName masking
ALTER TABLE dbo.[User]
ALTER COLUMN FirstName NVARCHAR(255) MASKED WITH (FUNCTION = 'partial(1,"XXXXXX",0)');

-- LastName masking 
ALTER TABLE dbo.[User]
ALTER COLUMN LastName NVARCHAR(255) MASKED WITH (FUNCTION = 'partial(1,"XXXXXX",0)');

-- Mask matric number (partial)
ALTER TABLE dbo.StudentProfile
ALTER COLUMN MatricNo NVARCHAR(30) MASKED WITH (FUNCTION = 'partial(2,"XXXXXX",2)');

-- Mask address (default mask)
ALTER TABLE dbo.StudentProfile
ALTER COLUMN Address NVARCHAR(255) MASKED WITH (FUNCTION = 'default()');
