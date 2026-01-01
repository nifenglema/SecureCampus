using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using SecureCampusApp.Models;

namespace SecureCampusApp.Models
{
    public class DbHelper
    {
        private readonly string _connectionString;

        public DbHelper(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null", nameof(connectionString));

            _connectionString = connectionString;
        }

        public string? GetUserIdByEmail(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT TOP 1 UserID FROM dbo.[User] WHERE Email = @email;",
                conn);

            cmd.Parameters.AddWithValue("@email", email);

            var result = cmd.ExecuteScalar();
            return result == null ? null : result.ToString();
        }

        public string? GetLecturerIdByUserId(string userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(
                "SELECT LecturerID FROM LecturerProfile WHERE UserID = @uid",
                conn
            );
            cmd.Parameters.AddWithValue("@uid", userId);

            return cmd.ExecuteScalar()?.ToString();
        }


        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        // ==================================================
        // RLS SESSION CONTEXT
        // ==================================================
        private void SetRlsContext(SqlConnection conn, string? userId, string? role)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
                return;

            using var cmd = new SqlCommand(@"
                EXEC sys.sp_set_session_context @key=N'UserID', @value=@uid;
                EXEC sys.sp_set_session_context @key=N'Role',   @value=@role;
            ", conn);

            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@role", role);
            cmd.ExecuteNonQuery();
        }

        // ==================================================
        // AUTH
        // ==================================================
        public bool RegisterUser(string role, string firstName, string lastName, string email, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var check = new SqlCommand(
                "SELECT COUNT(*) FROM [User] WHERE Email=@e", conn);
            check.Parameters.AddWithValue("@e", email);

            if ((int)check.ExecuteScalar() > 0)
                return false;

            string hashedPassword = HashPassword(password); // ✅ HASH HERE

            var cmd = new SqlCommand(@"
        INSERT INTO [User]
        (UserID, Role, FirstName, LastName, Email, PasswordHash)
        VALUES
        (@id, @role, @fn, @ln, @email, @pwd)", conn);

            cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@role", role);
            cmd.Parameters.AddWithValue("@fn", firstName);
            cmd.Parameters.AddWithValue("@ln", lastName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@pwd", hashedPassword); // ✅

            cmd.ExecuteNonQuery();
            return true;
        }


        public User? ValidateUser(string email, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string hashed = HashPassword(password);

            var cmd = new SqlCommand(
                "SELECT UserID, Role FROM [User] WHERE Email=@e AND PasswordHash=@p", conn);

            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", hashed);

            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                return new User
                {
                    UserID = r["UserID"].ToString()!,
                    Role = r["Role"].ToString()!
                };
            }
            return null;
        }


        // ==================================================
        // STUDENT PROFILE
        // ==================================================
        public List<StudentProfile> GetStudents(string? userId, string? role)
        {
            var list = new List<StudentProfile>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            SetRlsContext(conn, userId, role);

            var cmd = new SqlCommand(@"
                SELECT StudentID, UserID, MatricNo, Programme, IntakeYear, Address
                FROM StudentProfile
                ORDER BY StudentID", conn);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new StudentProfile
                {
                    StudentID = r["StudentID"]?.ToString() ?? "",
                    UserID = r["UserID"]?.ToString() ?? "",
                    MatricNo = r["MatricNo"]?.ToString() ?? "",
                    Programme = r["Programme"] == DBNull.Value ? "" : r["Programme"]?.ToString() ?? "",
                    IntakeYear = r["IntakeYear"] == DBNull.Value ? 0 : Convert.ToInt32(r["IntakeYear"]),
                    Address = r["Address"] == DBNull.Value ? "" : r["Address"]?.ToString() ?? ""
                });
            }
            return list;
        }

        public void UpdateStudentProfile(string userId, string ic, string address, string programme, string role)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            SetRlsContext(conn, userId, role);

            var cmd = new SqlCommand(@"
                OPEN SYMMETRIC KEY StudentICKey
                DECRYPTION BY CERTIFICATE StudentICCert;

                IF NOT EXISTS (SELECT 1 FROM StudentProfile WHERE UserID=@uid)
                BEGIN
                    DECLARE @sid NVARCHAR(50) = 'S' + LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', ''), 6);
                    DECLARE @matric NVARCHAR(30) =
                        CAST(YEAR(GETDATE()) AS NVARCHAR(4)) +
                        RIGHT('000000' + CAST(ABS(CHECKSUM(NEWID())) % 1000000 AS NVARCHAR(6)), 6);

                    INSERT INTO StudentProfile
                    (StudentID, UserID, MatricNo, Programme, IntakeYear, Address, IC_Encrypted)
                    VALUES
                    (@sid, @uid, @matric, @programme, YEAR(GETDATE()), @address,
                     EncryptByKey(Key_GUID('StudentICKey'), @ic));
                END
                ELSE
                BEGIN
                    UPDATE StudentProfile
                    SET Programme=@programme,
                        Address=@address,
                        IC_Encrypted=EncryptByKey(Key_GUID('StudentICKey'), @ic)
                    WHERE UserID=@uid;
                END

                CLOSE SYMMETRIC KEY StudentICKey;
            ", conn);

            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@ic", ic);
            cmd.Parameters.AddWithValue("@address", address);
            cmd.Parameters.AddWithValue("@programme", programme);

            cmd.ExecuteNonQuery();
        }

        public void DeleteStudentProfile(string studentId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand("DELETE FROM StudentProfile WHERE StudentID=@id", conn);
            cmd.Parameters.AddWithValue("@id", studentId);
            cmd.ExecuteNonQuery();
        }

        // ==================================================
        // LECTURERS
        // ==================================================
        public List<LecturerProfile> GetLecturers()
        {
            var list = new List<LecturerProfile>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT LecturerID, StaffNo, Department
        FROM LecturerProfile
        ORDER BY StaffNo
    ", conn);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new LecturerProfile
                {
                    LecturerID = r["LecturerID"].ToString()!,
                    StaffNo = r["StaffNo"].ToString()!,
                    Department = r["Department"].ToString()!
                });
            }

            return list;
        }

        public List<Course> GetCoursesForLecturerUser(string lecturerUserId)
        {
            var list = new List<Course>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT c.CourseID, c.CourseCode, c.CourseName, c.LecturerID
        FROM Courses c
        JOIN LecturerProfile lp ON c.LecturerID = lp.LecturerID
        WHERE lp.UserID = @uid
    ", conn);

            cmd.Parameters.AddWithValue("@uid", lecturerUserId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Course
                {
                    CourseID = r["CourseID"].ToString()!,
                    CourseCode = r["CourseCode"].ToString()!,
                    CourseName = r["CourseName"].ToString()!,
                    LecturerID = r["LecturerID"].ToString()!
                });
            }
            return list;
        }



        public List<Grade> GetEnrollmentsForLecturer(string userId)
        {
            var list = new List<Grade>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT 
            e.EnrollmentID,
            e.StudentID,
            e.CourseID,
            ISNULL(g.Grade, '') AS Grade
        FROM Enrollments e
        JOIN Courses c ON e.CourseID = c.CourseID
        JOIN LecturerProfile l ON c.LecturerID = l.LecturerID
        LEFT JOIN Grades g ON g.EnrollmentID = e.EnrollmentID
        WHERE l.UserID = @uid
    ", conn);

            cmd.Parameters.AddWithValue("@uid", userId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Grade
                {
                    EnrollmentID = r["EnrollmentID"].ToString()!,
                    StudentID = r["StudentID"].ToString()!,
                    CourseID = r["CourseID"].ToString()!,
                    GradeValue = r["Grade"].ToString()!
                });
            }

            return list;
        }
        public void EnrollStudentByLecturer(string lecturerUserId, string courseId, string studentId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Safety check: lecturer must own the course
            var check = new SqlCommand(@"
        SELECT COUNT(*)
        FROM Courses c
        JOIN LecturerProfile lp ON c.LecturerID = lp.LecturerID
        WHERE c.CourseID = @cid AND lp.UserID = @uid
    ", conn);

            check.Parameters.AddWithValue("@cid", courseId);
            check.Parameters.AddWithValue("@uid", lecturerUserId);

            int ok = (int)check.ExecuteScalar()!;
            if (ok == 0) throw new Exception("Unauthorized: course not owned by this lecturer.");

            // Insert enrollment (prevent duplicate)
            var cmd = new SqlCommand(@"
        IF NOT EXISTS (SELECT 1 FROM Enrollments WHERE StudentID=@sid AND CourseID=@cid)
        BEGIN
            INSERT INTO Enrollments(EnrollmentID, StudentID, CourseID)
            VALUES (CONVERT(NVARCHAR(50), NEWID()), @sid, @cid)
        END
    ", conn);

            cmd.Parameters.AddWithValue("@sid", studentId);
            cmd.Parameters.AddWithValue("@cid", courseId);
            cmd.ExecuteNonQuery();
        }

        public List<Grade> GetGradeSheetForLecturer(string userId)
        {
            var list = new List<Grade>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT
            ISNULL(g.GradeID, '') AS GradeID,
            e.EnrollmentID,
            e.StudentID,
            e.CourseID,
            ISNULL(g.Grade, '') AS Grade
        FROM Enrollments e
        JOIN Courses c ON e.CourseID = c.CourseID
        JOIN LecturerProfile l ON c.LecturerID = l.LecturerID
        LEFT JOIN Grades g ON g.EnrollmentID = e.EnrollmentID
        WHERE l.UserID = @uid
    ", conn);

            cmd.Parameters.AddWithValue("@uid", userId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Grade
                {
                    GradeID = r["GradeID"].ToString()!,
                    EnrollmentID = r["EnrollmentID"].ToString()!,
                    StudentID = r["StudentID"].ToString()!,
                    CourseID = r["CourseID"].ToString()!,
                    GradeValue = r["Grade"].ToString()!
                });
            }

            return list;
        }

        public void SaveGrade(string enrollmentId, string grade)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        IF EXISTS (SELECT 1 FROM Grades WHERE EnrollmentID = @eid)
            UPDATE Grades 
            SET Grade = @g, UpdatedAt = GETDATE()
            WHERE EnrollmentID = @eid
        ELSE
            INSERT INTO Grades (GradeID, EnrollmentID, Grade)
            VALUES (NEWID(), @eid, @g)
    ", conn);

            cmd.Parameters.AddWithValue("@eid", enrollmentId);
            cmd.Parameters.AddWithValue("@g", grade);

            cmd.ExecuteNonQuery();
        }
        public List<Course> GetStudentCourses(string studentId)
        {
            var list = new List<Course>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT c.*
        FROM Courses c
        JOIN Enrollments e ON c.CourseID = e.CourseID
        WHERE e.StudentID = @sid", conn);

            cmd.Parameters.AddWithValue("@sid", studentId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Course
                {
                    CourseCode = r["CourseCode"].ToString()!,
                    CourseName = r["CourseName"].ToString()!
                });
            }
            return list;
        }

        public List<Course> GetMyCourses(string userId)
        {
            var list = new List<Course>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT c.CourseID, c.CourseCode, c.CourseName, c.LecturerID
        FROM StudentProfile sp
        JOIN Enrollments e ON sp.StudentID = e.StudentID
        JOIN Courses c ON e.CourseID = c.CourseID
        WHERE sp.UserID = @uid
    ", conn);

            cmd.Parameters.AddWithValue("@uid", userId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Course
                {
                    CourseID = r["CourseID"].ToString()!,
                    CourseCode = r["CourseCode"].ToString()!,
                    CourseName = r["CourseName"].ToString()!,
                    LecturerID = r["LecturerID"].ToString()!
                });
            }

            return list;
        }

        public void UpdateGrade(string studentId, string courseId, string grade)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        UPDATE Grades
        SET Grade = @g
        WHERE StudentID = @sid AND CourseID = @cid
    ", conn);

            cmd.Parameters.AddWithValue("@g", grade);
            cmd.Parameters.AddWithValue("@sid", studentId);
            cmd.Parameters.AddWithValue("@cid", courseId);

            cmd.ExecuteNonQuery();
        }

        // ==================================================
        // COURSES
        // ==================================================
        public List<Course> GetCourses()
        {
            var list = new List<Course>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT CourseID, CourseCode, CourseName, LecturerID FROM Courses", conn);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new Course
                {
                    CourseID = r["CourseID"]?.ToString() ?? "",
                    CourseCode = r["CourseCode"]?.ToString() ?? "",
                    CourseName = r["CourseName"]?.ToString() ?? "",
                    LecturerID = r["LecturerID"] == DBNull.Value ? "" : r["LecturerID"]?.ToString() ?? ""
                });
            }
            return list;
        }

        public void AddCourse(Course c)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
                INSERT INTO Courses (CourseID, CourseCode, CourseName, LecturerID)
                VALUES (@id, @code, @name, @lid)", conn);

            cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@code", c.CourseCode);
            cmd.Parameters.AddWithValue("@name", c.CourseName);
            cmd.Parameters.AddWithValue("@lid", (object?)c.LecturerID ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public void DeleteCourse(string courseId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand("DELETE FROM Courses WHERE CourseID=@id", conn);
            cmd.Parameters.AddWithValue("@id", courseId);
            cmd.ExecuteNonQuery();
        }




        // ==================================================
        // AUDIT LOGS
        // ==================================================
        public void AddAudit(string role, string userId, string action, string target, string status)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
                INSERT INTO Audit_Logs
                (LogID, ActorRole, ActorUserID, Action, TargetEntity, Status)
                VALUES
                (@id, @role, @uid, @action, @target, @status)", conn);

            cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@role", role);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@action", action);
            cmd.Parameters.AddWithValue("@target", target);
            cmd.Parameters.AddWithValue("@status", status);

            cmd.ExecuteNonQuery();
        }

        public List<AuditLog> GetAuditLogs()
        {
            var list = new List<AuditLog>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT * FROM Audit_Logs ORDER BY Timestamp DESC", conn);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new AuditLog
                {
                    LogID = r["LogID"]?.ToString() ?? "",
                    Timestamp = Convert.ToDateTime(r["Timestamp"]),
                    ActorRole = r["ActorRole"]?.ToString() ?? "",
                    ActorUserID = r["ActorUserID"]?.ToString() ?? "",
                    Action = r["Action"]?.ToString() ?? "",
                    TargetEntity = r["TargetEntity"] == DBNull.Value ? "" : r["TargetEntity"]?.ToString() ?? "",
                    Status = r["Status"]?.ToString() ?? ""
                });
            }
            return list;
        }

        public AdminStats GetAdminStats()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT
            (SELECT COUNT(*) FROM [User]) AS TotalUsers,
            (SELECT COUNT(*) FROM StudentProfile) AS TotalStudents,
            (SELECT COUNT(*) FROM LecturerProfile) AS TotalLecturers,
            (SELECT COUNT(*) FROM Courses) AS TotalCourses;
    ", conn);

            using var r = cmd.ExecuteReader();
            r.Read();

            return new AdminStats
            {
                TotalUsers = Convert.ToInt32(r["TotalUsers"]),
                TotalStudents = Convert.ToInt32(r["TotalStudents"]),
                TotalLecturers = Convert.ToInt32(r["TotalLecturers"]),
                TotalCourses = Convert.ToInt32(r["TotalCourses"])
            };
        }

        public void CreateLecturerProfile(string userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        INSERT INTO LecturerProfile (LecturerID, UserID, StaffNo, Department)
        VALUES (@id, @id, 'STAFF-' + RIGHT(@id,4), 'General');
    ", conn);

            cmd.Parameters.AddWithValue("@id", userId);
            cmd.ExecuteNonQuery();
        }


        public void DeleteLecturerProfile(string lecturerId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(
                "DELETE FROM LecturerProfile WHERE LecturerID = @id", conn);

            cmd.Parameters.AddWithValue("@id", lecturerId);
            cmd.ExecuteNonQuery();
        }

        


    }
}
