using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class AdminCompaniesController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        private bool IsAdmin()
        {
            return Session["UserID"] != null
                && Session["Role"] != null
                && Session["Role"].ToString() == "Admin";
        }

        public ActionResult Index(string status)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var companies = db.Companies.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                companies = companies.Where(c => c.Status == status);
            }

            ViewBag.CurrentStatus = status;
            ViewBag.PendingCount = db.Companies.Count(c => c.Status == "Pending");
            ViewBag.ApprovedCount = db.Companies.Count(c => c.Status == "Approved");
            ViewBag.RejectedCount = db.Companies.Count(c => c.Status == "Rejected");

            return View(companies.OrderByDescending(c => c.CreatedAt).ToList());
        }

        public ActionResult Details(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var company = db.Companies.Find(id);

            if (company == null)
            {
                return HttpNotFound();
            }

            return View(company);
        }

        [HttpPost]
        public ActionResult Approve(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var company = db.Companies.Find(id);

            if (company == null)
            {
                return HttpNotFound();
            }

            company.Status = "Approved";
            db.SaveChanges();

            TempData["Success"] = "Company approved successfully.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Reject(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var company = db.Companies.Find(id);

            if (company == null)
            {
                return HttpNotFound();
            }

            company.Status = "Rejected";
            db.SaveChanges();

            TempData["Success"] = "Company rejected successfully.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult SetPending(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var company = db.Companies.Find(id);

            if (company == null)
            {
                return HttpNotFound();
            }

            company.Status = "Pending";
            db.SaveChanges();

            TempData["Success"] = "Company has been moved back to pending status.";

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