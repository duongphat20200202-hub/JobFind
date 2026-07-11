using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class AdminUsersController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        private bool IsAdmin()
        {
            return Session["UserID"] != null
                && Session["Role"] != null
                && Session["Role"].ToString() == "Admin";
        }

        public ActionResult Index(string role, string status, string keyword)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var users = db.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                users = users.Where(u => u.Role == role);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active")
                {
                    users = users.Where(u => u.IsActive == true);
                }
                else if (status == "Locked")
                {
                    users = users.Where(u => u.IsActive == false);
                }
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                users = users.Where(u =>
                    u.UserName.Contains(keyword) ||
                    u.Email.Contains(keyword)
                );
            }

            ViewBag.CurrentRole = role;
            ViewBag.CurrentStatus = status;
            ViewBag.Keyword = keyword;

            ViewBag.TotalUsers = db.Users.Count();
            ViewBag.CandidateCount = db.Users.Count(u => u.Role == "Candidate");
            ViewBag.EmployerCount = db.Users.Count(u => u.Role == "Employer");
            ViewBag.AdminCount = db.Users.Count(u => u.Role == "Admin");
            ViewBag.ActiveCount = db.Users.Count(u => u.IsActive == true);
            ViewBag.LockedCount = db.Users.Count(u => u.IsActive == false);

            return View(users.OrderByDescending(u => u.CreatedAt).ToList());
        }

        public ActionResult Details(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = db.Users.Find(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            ViewBag.Candidate = db.Candidates.FirstOrDefault(c => c.UserID == id);
            ViewBag.Employer = db.Employers.FirstOrDefault(e => e.UserID == id);
            ViewBag.Staff = db.Staffs.FirstOrDefault(s => s.UserID == id);

            return View(user);
        }

        [HttpPost]
        public ActionResult Lock(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = int.Parse(Session["UserID"].ToString());

            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot lock the admin account that is currently logged in.";
                return RedirectToAction("Index");
            }

            var user = db.Users.Find(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            user.IsActive = false;
            db.SaveChanges();

            TempData["Success"] = "Account locked successfully.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Unlock(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = db.Users.Find(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            user.IsActive = true;
            db.SaveChanges();

            TempData["Success"] = "Account unlocked successfully.";

            return RedirectToAction("Index");
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