using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class EmployerDashboardController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        private Employer GetCurrentEmployer()
        {
            if (Session["UserID"] == null ||
                Session["Role"] == null ||
                Session["Role"].ToString() != "Employer")
            {
                return null;
            }

            int userId = int.Parse(Session["UserID"].ToString());

            var user = db.Users.FirstOrDefault(u =>
                u.UserID == userId &&
                u.IsActive == true
            );

            if (user == null)
            {
                Session.Clear();
                return null;
            }

            return db.Employers.FirstOrDefault(e => e.UserID == userId);
        }

        public ActionResult Index()
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            int companyId = employer.CompanyID;

            var company = db.Companies.FirstOrDefault(c => c.CompanyID == companyId);

            if (company == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.CompanyName = company.CompanyName;
            ViewBag.CompanyStatus = company.Status;

            ViewBag.TotalJobs = db.Jobs.Count(j => j.CompanyID == companyId);

            ViewBag.PendingJobs = db.Jobs.Count(j =>
                j.CompanyID == companyId &&
                j.Status == "Pending"
            );

            ViewBag.ApprovedJobs = db.Jobs.Count(j =>
                j.CompanyID == companyId &&
                j.Status == "Approved"
            );

            ViewBag.RejectedJobs = db.Jobs.Count(j =>
                j.CompanyID == companyId &&
                j.Status == "Rejected"
            );

            ViewBag.ClosedJobs = db.Jobs.Count(j =>
                j.CompanyID == companyId &&
                j.Status == "Closed"
            );

            ViewBag.TotalApplications = db.Applications.Count(a =>
                a.Job.CompanyID == companyId
            );

            ViewBag.PendingApplications = db.Applications.Count(a =>
                a.Job.CompanyID == companyId &&
                a.Status == "Pending"
            );

            return View();
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