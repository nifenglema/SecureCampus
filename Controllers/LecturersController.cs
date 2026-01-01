using Microsoft.AspNetCore.Mvc;
using SecureCampusApp.Models;

namespace SecureCampusApp.Controllers
{
    public class LecturersController : Controller
    {
        private readonly DbHelper _db;

        public LecturersController(IConfiguration config)
        {
            _db = new DbHelper(config.GetConnectionString("SecureCampusDb")!);
        }

        private bool IsLecturer()
            => HttpContext.Session.GetString("Role") == "Lecturer";

        // ======================
        // DASHBOARD
        // ======================
        public IActionResult Dashboard()
        {
            if (!IsLecturer())
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // ======================
        // MY COURSES
        // ======================
        public IActionResult Courses()
        {
            if (HttpContext.Session.GetString("Role") != "Lecturer")
                return RedirectToAction("Login", "Auth");

            var userId = HttpContext.Session.GetString("UserID")!;
            var lecturerId = _db.GetLecturerIdByUserId(userId);

            if (lecturerId == null)
                return View(new List<Course>());

            return View(_db.GetCoursesForLecturerUser(lecturerId));
        }

        public IActionResult Students()
        {
            if (HttpContext.Session.GetString("Role") != "Lecturer")
                return RedirectToAction("Login", "Auth");

            var userId = HttpContext.Session.GetString("UserID")!;
            var courses = _db.GetCoursesForLecturerUser(userId);

            ViewBag.Courses = courses;
            return View(); // View will show course dropdown + student list
        }

        [HttpPost]
        public IActionResult EnrollStudent(string courseId, string studentId)
        {
            if (HttpContext.Session.GetString("Role") != "Lecturer")
                return RedirectToAction("Login", "Auth");

            var userId = HttpContext.Session.GetString("UserID")!;
            _db.EnrollStudentByLecturer(userId, courseId, studentId);
            return RedirectToAction("Students");
        }

        // ======================
        // GRADES
        // ======================
        public IActionResult Grades()
        {
            if (HttpContext.Session.GetString("Role") != "Lecturer")
                return RedirectToAction("Login", "Auth");

            var userId = HttpContext.Session.GetString("UserID")!;
            var data = _db.GetEnrollmentsForLecturer(userId);

            return View(data);
        }

        [HttpPost]
        public IActionResult SaveGrade(string enrollmentId, string gradeValue)
        {
            if (HttpContext.Session.GetString("Role") != "Lecturer")
                return RedirectToAction("Login", "Auth");

            _db.SaveGrade(enrollmentId, gradeValue);
            return RedirectToAction("Grades");
        }

        [HttpPost]
        public IActionResult UpdateGrade(string studentId, string courseId, string grade)
        {
            if (!IsLecturer())
                return RedirectToAction("Login", "Auth");

            _db.UpdateGrade(studentId, courseId, grade);
            return RedirectToAction("Grades");
        }
    }
}
