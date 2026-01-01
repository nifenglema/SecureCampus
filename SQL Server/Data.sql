CREATE DATABASE SecureCampus;
GO

USE SecureCampus;
GO

DROP TABLE IF EXISTS Audit_Logs;
DROP TABLE IF EXISTS LoginSession;
DROP TABLE IF EXISTS Grades;
DROP TABLE IF EXISTS Enrollments;
DROP TABLE IF EXISTS Courses;
DROP TABLE IF EXISTS LecturerProfile;
DROP TABLE IF EXISTS StudentProfile;
DROP TABLE IF EXISTS [User];

CREATE TABLE [User] (
    UserID NVARCHAR(50) PRIMARY KEY,
    Role NVARCHAR(30) CHECK (Role IN ('Student', 'Lecturer', 'Admin')),
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE StudentProfile (
    StudentID NVARCHAR(50) PRIMARY KEY,
    UserID NVARCHAR(50) NOT NULL,
    MatricNo NVARCHAR(30) UNIQUE NOT NULL,
    Programme NVARCHAR(100),
    IntakeYear INT,
    Address NVARCHAR(255),
    IC_Encrypted VARBINARY(256),
    FOREIGN KEY (UserID) REFERENCES [User](UserID) ON DELETE CASCADE
);

CREATE TABLE LecturerProfile (
    LecturerID NVARCHAR(50) PRIMARY KEY,
    UserID NVARCHAR(50) NOT NULL,
    StaffNo NVARCHAR(30) UNIQUE NOT NULL,
    Department NVARCHAR(100),
    FOREIGN KEY (UserID) REFERENCES [User](UserID) ON DELETE CASCADE
);

CREATE TABLE Courses (
    CourseID NVARCHAR(50) PRIMARY KEY,
    CourseCode NVARCHAR(20) UNIQUE NOT NULL,
    CourseName NVARCHAR(255) NOT NULL,
    LecturerID NVARCHAR(50),
    FOREIGN KEY (LecturerID) REFERENCES LecturerProfile(LecturerID)
);

CREATE TABLE Enrollments (
    EnrollmentID NVARCHAR(50) PRIMARY KEY,
    StudentID NVARCHAR(50),
    CourseID NVARCHAR(50),
    EnrollDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (StudentID) REFERENCES StudentProfile(StudentID),
    FOREIGN KEY (CourseID) REFERENCES Courses(CourseID)
);

CREATE TABLE Grades (
    GradeID NVARCHAR(50) PRIMARY KEY,
    EnrollmentID NVARCHAR(50),
    Grade NVARCHAR(5),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (EnrollmentID) REFERENCES Enrollments(EnrollmentID)
);

CREATE TABLE LoginSession (
    SessionID NVARCHAR(50) PRIMARY KEY,
    UserID NVARCHAR(50),
    ExpiresAt DATETIME NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES [User](UserID) ON DELETE CASCADE
);

CREATE TABLE Audit_Logs (
    LogID NVARCHAR(50) PRIMARY KEY,
    Timestamp DATETIME DEFAULT GETDATE(),
    ActorRole NVARCHAR(30),
    ActorUserID NVARCHAR(50),
    Action NVARCHAR(255),
    TargetEntity NVARCHAR(50),
    Status NVARCHAR(50)
);

INSERT INTO [User] VALUES
('U100', 'Student', 'Ali', 'Bin Ahmad', 'ali@mmu.edu.my', 'HASHEDPWD', GETDATE()),
('U200', 'Student', 'Siti', 'Sarah', 'siti@mmu.edu.my', 'HASHEDPWD', GETDATE()),
('U300', 'Lecturer', 'Dr Tan', 'Wei Ming', 'tanwm@mmu.edu.my', 'HASHEDPWD', GETDATE()),
('U400', 'Admin', 'System', 'Admin', 'admin@mmu.edu.my', 'HASHEDPWD', GETDATE());

INSERT INTO StudentProfile VALUES
('S100', 'U100', '1221101403', 'BIT', 2023, 'No 48, Jalan Mewah, Kuala Lumpur', 021222010102),
('S200', 'U200', '1221101402', 'BIT', 2023, 'No 5, Jalan Sutera, Shah Alam', 011001010110);

INSERT INTO LecturerProfile VALUES
('L100', 'U300', 'L1001', 'Faculty of Computing');

INSERT INTO Courses VALUES
('C100', 'CSF101', 'Database Systems', 'L100');

INSERT INTO Enrollments VALUES
('E100', 'S100', 'C100', GETDATE());

INSERT INTO Grades VALUES
('G100', 'E100', 'A', GETDATE());

INSERT INTO Audit_Logs VALUES
('LOG100', GETDATE(), 'Admin', 'U400', 'GradeUpdated', 'G100', 'Success');
