using System;
using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class AccountController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        public ActionResult Login(string role)
        {
            ViewBag.Role = role;
            return View();
        }

        [HttpPost]
        public ActionResult Login(string userName, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter both username and password.";
                ViewBag.Role = role;
                return View();
            }

            var user = db.Users.FirstOrDefault(u =>
                (u.UserName == userName || u.Email == userName)
                && u.PasswordHash == password
            );

            if (user == null)
            {
                ViewBag.Error = "Username or password is incorrect.";
                ViewBag.Role = role;
                return View();
            }

            if (user.IsActive == false)
            {
                ViewBag.Error = "Your account has been locked. Please contact the administrator.";
                ViewBag.Role = role;
                return View();
            }

            if (!string.IsNullOrEmpty(role) && user.Role != role && user.Role != "Admin")
            {
                ViewBag.Error = "This account does not match the selected account type.";
                ViewBag.Role = role;
                return View();
            }

            Session["UserID"] = user.UserID;
            Session["UserName"] = user.UserName;
            Session["Email"] = user.Email;
            Session["Role"] = user.Role;

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "AdminDashboard");
            }

            if (user.Role == "Employer")
            {
                return RedirectToAction("Index", "EmployerDashboard");
            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Register(string role)
        {
            if (role == "Employer")
            {
                return RedirectToAction("RegisterEmployer");
            }

            return RedirectToAction("RegisterCandidate");
        }

        public ActionResult RegisterCandidate()
        {
            return View();
        }

        [HttpPost]
        public ActionResult RegisterCandidate(
            string userName,
            string email,
            string password,
            string confirmPassword,
            string fullName,
            string phone
        )
        {
            if (string.IsNullOrEmpty(userName) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(confirmPassword) ||
                string.IsNullOrEmpty(fullName))
            {
                ViewBag.Error = "Please enter all required information.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            var checkEmail = db.Users.FirstOrDefault(u => u.Email == email);
            if (checkEmail != null)
            {
                ViewBag.Error = "This email is already in use.";
                return View();
            }

            var checkUserName = db.Users.FirstOrDefault(u => u.UserName == userName);
            if (checkUserName != null)
            {
                ViewBag.Error = "This username is already in use.";
                return View();
            }

            var user = new User
            {
                UserName = userName,
                Email = email,
                PasswordHash = password,
                Role = "Candidate",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();

            var candidate = new Candidate
            {
                UserID = user.UserID,
                FullName = fullName,
                Phone = phone
            };

            db.Candidates.Add(candidate);
            db.SaveChanges();

            TempData["Success"] = "Candidate registration successful. Please log in.";
            return RedirectToAction("Login", new { role = "Candidate" });
        }

        public ActionResult RegisterEmployer()
        {
            return View();
        }

        [HttpPost]
        public ActionResult RegisterEmployer(
            string userName,
            string email,
            string password,
            string confirmPassword,

            string fullName,
            string phone,
            string position,

            string companyName,
            string companyLocation,
            string companyEmail,
            string companyPhone,
            string website,
            string description
        )
        {
            if (string.IsNullOrEmpty(userName) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(confirmPassword) ||
                string.IsNullOrEmpty(fullName) ||
                string.IsNullOrEmpty(companyName))
            {
                ViewBag.Error = "Please enter all required information.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            var checkEmail = db.Users.FirstOrDefault(u => u.Email == email);
            if (checkEmail != null)
            {
                ViewBag.Error = "This email is already in use.";
                return View();
            }

            var checkUserName = db.Users.FirstOrDefault(u => u.UserName == userName);
            if (checkUserName != null)
            {
                ViewBag.Error = "This username is already in use.";
                return View();
            }

            var user = new User
            {
                UserName = userName,
                Email = email,
                PasswordHash = password,
                Role = "Employer",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();

            var company = new Company
            {
                CompanyName = companyName,
                Location = companyLocation,
                Email = companyEmail,
                Phone = companyPhone,
                Website = website,
                Description = description,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            db.Companies.Add(company);
            db.SaveChanges();

            var employer = new Employer
            {
                UserID = user.UserID,
                CompanyID = company.CompanyID,
                FullName = fullName,
                Phone = phone,
                Position = position
            };

            db.Employers.Add(employer);
            db.SaveChanges();

            TempData["Success"] = "Employer registration successful. Your company is waiting for admin approval.";
            return RedirectToAction("Login", new { role = "Employer" });
        }

        public ActionResult Profile()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        public ActionResult Profile(string userName, string email, string newPassword)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            var user = db.Users.FirstOrDefault(u => u.UserID == userId);

            if (user == null)
            {
                Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                ViewBag.Error = "Please enter your username.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email.";
                return View();
            }

            userName = userName.Trim();
            email = email.Trim();

            bool usernameExists = db.Users.Any(u =>
                u.UserName == userName &&
                u.UserID != userId
            );

            if (usernameExists)
            {
                ViewBag.Error = "This username is already in use.";
                return View();
            }

            bool emailExists = db.Users.Any(u =>
                u.Email == email &&
                u.UserID != userId
            );

            if (emailExists)
            {
                ViewBag.Error = "This email is already in use.";
                return View();
            }

            user.UserName = userName;
            user.Email = email;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                user.PasswordHash = newPassword.Trim();
            }

            db.SaveChanges();

            Session["UserName"] = user.UserName;
            Session["Email"] = user.Email;
            Session["Role"] = user.Role;

            TempData["Success"] = "Your profile has been updated successfully.";

            return RedirectToAction("Profile");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public ActionResult ForgotPassword(string role)
        {
            ViewBag.Role = role;
            return View();
        }

        [HttpPost]
        public ActionResult ForgotPassword(
            string userNameOrEmail,
            string newPassword,
            string confirmPassword,
            string role
        )
        {
            ViewBag.Role = role;

            if (string.IsNullOrWhiteSpace(userNameOrEmail) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Please enter all required information.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            var user = db.Users.FirstOrDefault(u =>
                u.UserName == userNameOrEmail ||
                u.Email == userNameOrEmail
            );

            if (user == null)
            {
                ViewBag.Error = "Account not found.";
                return View();
            }

            if (!string.IsNullOrEmpty(role) && user.Role != role && user.Role != "Admin")
            {
                ViewBag.Error = "This account does not match the selected account type.";
                return View();
            }

            if (user.IsActive == false)
            {
                ViewBag.Error = "Your account has been locked. Please contact the administrator.";
                return View();
            }

            user.PasswordHash = newPassword.Trim();

            db.SaveChanges();

            TempData["Success"] = "Password reset successfully. Please log in again.";

            return RedirectToAction("Login", "Account", new { role = user.Role });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}