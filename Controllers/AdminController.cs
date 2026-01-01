using Microsoft.AspNetCore.Mvc;
using SecureCampusApp.Models;

namespace SecureCampusApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly DbHelper _db;

        public AdminController(IConfiguration config)
        {
            _db = new DbHelper(config.GetConnectionString("SecureCampusDb")!);
        }

        private bool IsAdmin()
            => HttpContext.Session.GetString("Role") == "Admin";

        // =========================
        // DASHBOARD
        // =========================
        public IActionResult Dashboard()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            var stats = _db.GetAdminStats();
            return View(stats);
        }

        // =========================
        // STUDENTS
        // =========================
        public IActionResult Students()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            return View(_db.GetStudents(null, "Admin"));
        }

        public IActionResult DeleteStudent(string id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            _db.DeleteStudentProfile(id);
            return RedirectToAction("Students");
        }

        // =========================
        // LECTURERS
        // =========================
        public IActionResult Lecturers()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            return View(_db.GetLecturers());
        }

        public IActionResult DeleteLecturer(string id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            _db.DeleteLecturerProfile(id);
            return RedirectToAction("Lecturers");
        }

        // =========================
        // COURSES
        // =========================
        public IActionResult Courses()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            ViewBag.Lecturers = _db.GetLecturers();
            return View(_db.GetCourses());
        }

        [HttpPost]
        public IActionResult CreateCourse(string CourseCode, string CourseName, string LecturerID)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(CourseCode) ||
                string.IsNullOrWhiteSpace(CourseName) ||
                string.IsNullOrWhiteSpace(LecturerID))
            {
                return RedirectToAction("Courses");
            }

            var course = new Course
            {
                CourseID = Guid.NewGuid().ToString(),   // ✅ REQUIRED
                CourseCode = CourseCode,
                CourseName = CourseName,
                LecturerID = LecturerID                // ✅ FK-safe
            };

            _db.AddCourse(course);

            _db.AddAudit(
                "Admin",
                HttpContext.Session.GetString("UserID") ?? "unknown",
                "CreateCourse",
                CourseCode,
                "Success"
            );

            return RedirectToAction("Courses");
        }



        public IActionResult DeleteCourse(string id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            _db.DeleteCourse(id);
            return RedirectToAction("Courses");
        }

        // =========================
        // AUDIT LOGS
        // =========================
        public IActionResult AuditLogs()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            return View(_db.GetAuditLogs());
        }
    }
}
