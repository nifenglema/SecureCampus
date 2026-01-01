using Microsoft.AspNetCore.Mvc;
using SecureCampusApp.Models;
using Microsoft.AspNetCore.Http;

namespace SecureCampusApp.Controllers
{
    public class StudentsController : Controller
    {
        private readonly DbHelper _db;

        public StudentsController(IConfiguration config)
        {
            var cs = config.GetConnectionString("SecureCampusDb");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Missing connection string: SecureCampusDb (check appsettings.json)");

            _db = new DbHelper(cs);
        }

        // ==================================================
        // ADMIN: View All Students
        // ==================================================
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Auth");

            var uid = HttpContext.Session.GetString("UserID");
            var role = HttpContext.Session.GetString("Role");

            return View(_db.GetStudents(uid, role));
        }

    

        // ==================================================
        // ADMIN: Delete Student Profile
        // ==================================================
        public IActionResult Delete(string id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Auth");

            _db.DeleteStudentProfile(id);
            return RedirectToAction("Index");
        }

        // ==================================================
        // STUDENT: View Profile Page
        // ==================================================
        public IActionResult Profile()
        {
            if (HttpContext.Session.GetString("Role") != "Student")
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // ==================================================
        // STUDENT: Update Own Profile
        // ==================================================
        [HttpPost]
        public IActionResult Profile(string Programme, string IC, string Address)
        {
            var userId = HttpContext.Session.GetString("UserID");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Auth");

            // Basic validation
            if (string.IsNullOrWhiteSpace(Programme) ||
                string.IsNullOrWhiteSpace(IC) ||
                string.IsNullOrWhiteSpace(Address))
            {
                ViewBag.Error = "Please fill in all fields.";
                return View();
            }

            // IMPORTANT: pass role so RLS context works
            _db.UpdateStudentProfile(userId, IC, Address, Programme, role);

            ViewBag.Success = "Profile updated successfully.";
            return View();
        }

        public IActionResult MyCourses()
        {
            if (HttpContext.Session.GetString("Role") != "Student")
                return RedirectToAction("Login", "Auth");

            var userId = HttpContext.Session.GetString("UserID")!;
            return View(_db.GetMyCourses(userId));
        }

    }
}
