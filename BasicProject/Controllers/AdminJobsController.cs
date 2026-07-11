using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class AdminJobsController : Controller
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

            DateTime today = DateTime.Today;

            var jobs = db.Jobs
                .Include(j => j.Company)
                .Include(j => j.Category)
                .AsQueryable();

            ViewBag.AllCount = jobs.Count();
            ViewBag.PendingCount = jobs.Count(j => j.Status == "Pending");
            ViewBag.ApprovedCount = jobs.Count(j => j.Status == "Approved");
            ViewBag.RejectedCount = jobs.Count(j => j.Status == "Rejected");
            ViewBag.ClosedCount = jobs.Count(j => j.Status == "Closed");
            ViewBag.ExpiredCount = jobs.Count(j => j.Deadline.HasValue && DbFunctions.TruncateTime(j.Deadline) < today);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Expired")
                {
                    jobs = jobs.Where(j =>
                        j.Deadline.HasValue &&
                        DbFunctions.TruncateTime(j.Deadline) < today
                    );
                }
                else
                {
                    jobs = jobs.Where(j => j.Status == status);
                }
            }

            ViewBag.CurrentStatus = status;

            var result = jobs
                .OrderByDescending(j => j.CreatedAt)
                .ToList();

            return View(result);
        }

        public ActionResult Details(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var job = db.Jobs
                .Include(j => j.Company)
                .Include(j => j.Category)
                .Include(j => j.Employer)
                .FirstOrDefault(j => j.JobID == id);

            if (job == null)
            {
                return HttpNotFound();
            }

            int totalApplications = db.Applications.Count(a => a.JobID == id);
            int pendingApplications = db.Applications.Count(a => a.JobID == id && a.Status == "Pending");
            int acceptedApplications = db.Applications.Count(a => a.JobID == id && a.Status == "Accepted");
            int rejectedApplications = db.Applications.Count(a => a.JobID == id && a.Status == "Rejected");

            ViewBag.TotalApplications = totalApplications;
            ViewBag.PendingApplications = pendingApplications;
            ViewBag.AcceptedApplications = acceptedApplications;
            ViewBag.RejectedApplications = rejectedApplications;

            return View(job);
        }

        [HttpPost]
        public ActionResult Approve(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var job = db.Jobs.FirstOrDefault(j => j.JobID == id);

            if (job == null)
            {
                return HttpNotFound();
            }

            job.Status = "Approved";
            db.SaveChanges();

            TempData["Success"] = "Job post approved successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Reject(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var job = db.Jobs.FirstOrDefault(j => j.JobID == id);

            if (job == null)
            {
                return HttpNotFound();
            }

            job.Status = "Rejected";
            db.SaveChanges();

            TempData["Success"] = "Job post rejected successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Close(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var job = db.Jobs.FirstOrDefault(j => j.JobID == id);

            if (job == null)
            {
                return HttpNotFound();
            }

            job.Status = "Closed";
            db.SaveChanges();

            TempData["Success"] = "Job post closed successfully.";
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