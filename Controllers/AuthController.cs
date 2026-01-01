using Microsoft.AspNetCore.Mvc;
using SecureCampusApp.Models;

namespace SecureCampusApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly DbHelper _db;

        public AuthController(IConfiguration config)
        {
            var cs = config.GetConnectionString("SecureCampusDb");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException(
                    "Missing connection string: SecureCampusDb (check appsettings.json)"
                );

            _db = new DbHelper(cs);
        }

        // =========================
        // LOGIN
        // =========================
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _db.ValidateUser(email, password);
            if (user == null)
            {
                ViewBag.Error = "Invalid login credentials";
                return View();
            }

            HttpContext.Session.SetString("UserID", user.UserID);
            HttpContext.Session.SetString("Role", user.Role);
            _db.CreateLoginSession(user.UserID);

            if (user.Role == "Student")
                return RedirectToAction("Profile", "Students");

            if (user.Role == "Lecturer")
                return RedirectToAction("Dashboard", "Lecturers");

            return RedirectToAction("Dashboard", "Admin");
        }

        // =========================
        // REGISTER (GET)
        // =========================
        public IActionResult Register()
        {
            return View();   
        }

        // =========================
        // REGISTER (POST)
        // =========================
        [HttpPost]
        public IActionResult Register(
            string role,
            string firstName,
            string lastName,
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(role) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            bool success = _db.RegisterUser(role, firstName, lastName, email, password);

            if (!success)
            {
                ViewBag.Error = "Email already exists.";
                return View();
            }

            // AUTO-create lecturer profile
            if (role == "Lecturer")
            {
                var userId = _db.GetUserIdByEmail(email);
                _db.CreateLecturerProfile(userId);
            }

            return RedirectToAction("Login");
        }

        // =========================
        // LOGOUT
        // =========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }

}

