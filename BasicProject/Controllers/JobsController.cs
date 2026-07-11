using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class JobsController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        public ActionResult Index(
            string keyword,
            string company,
            string location,
            int? categoryId,
            string postedWithin,
            int? minSalary,
            int? maxSalary,
            string jobType,
            string experience
        )
        {
            var today = DateTime.Today;

            var jobs = db.Jobs
                .Include(j => j.Company)
                .Include(j => j.Category)
                .Where(j =>
                    j.Status == "Approved" &&
                    (!j.Deadline.HasValue || DbFunctions.TruncateTime(j.Deadline) >= today)
                );

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();

                jobs = jobs.Where(j =>
                    j.Title.ToLower().Contains(kw) ||
                    j.Description.ToLower().Contains(kw) ||
                    j.Requirement.ToLower().Contains(kw) ||
                    j.Category.CategoryName.ToLower().Contains(kw)
                );
            }

            if (!string.IsNullOrWhiteSpace(company))
            {
                string companyName = company.Trim().ToLower();

                jobs = jobs.Where(j =>
                    j.Company != null &&
                    j.Company.CompanyName.ToLower().Contains(companyName)
                );
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                jobs = jobs.Where(j => j.Location == location);
            }

            if (categoryId.HasValue)
            {
                jobs = jobs.Where(j => j.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(postedWithin))
            {
                DateTime fromDate = today;

                switch (postedWithin)
                {
                    case "today":
                        fromDate = today;
                        break;

                    case "3days":
                        fromDate = today.AddDays(-3);
                        break;

                    case "7days":
                        fromDate = today.AddDays(-7);
                        break;

                    case "14days":
                        fromDate = today.AddDays(-14);
                        break;

                    case "30days":
                        fromDate = today.AddDays(-30);
                        break;

                    default:
                        fromDate = DateTime.MinValue;
                        break;
                }

                if (fromDate != DateTime.MinValue)
                {
                    jobs = jobs.Where(j =>
                        j.CreatedAt.HasValue &&
                        DbFunctions.TruncateTime(j.CreatedAt) >= fromDate
                    );
                }
            }

            if (minSalary.HasValue && minSalary.Value > 0)
            {
                jobs = jobs.Where(j => j.Salary.HasValue && j.Salary.Value >= minSalary.Value);
            }

            if (maxSalary.HasValue && maxSalary.Value > 0)
            {
                jobs = jobs.Where(j => j.Salary.HasValue && j.Salary.Value <= maxSalary.Value);
            }

            if (!string.IsNullOrWhiteSpace(jobType))
            {
                jobs = jobs.Where(j => j.JobType == jobType);
            }

            if (!string.IsNullOrWhiteSpace(experience))
            {
                jobs = jobs.Where(j => j.Experience.Contains(experience));
            }

            var result = jobs
                .OrderByDescending(j => j.CreatedAt)
                .ToList();

            var jobIds = result.Select(j => j.JobID).ToList();

            var acceptedCounts = db.Applications
                .Where(a => jobIds.Contains(a.JobID) && a.Status == "Accepted")
                .GroupBy(a => a.JobID)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.AcceptedCounts = acceptedCounts;
            ViewBag.TotalJobs = result.Count;

            ViewBag.Keyword = keyword;
            ViewBag.Company = company;
            ViewBag.Location = location;
            ViewBag.CategoryId = categoryId;
            ViewBag.PostedWithin = postedWithin;
            ViewBag.MinSalary = minSalary;
            ViewBag.MaxSalary = maxSalary;
            ViewBag.JobType = jobType;
            ViewBag.Experience = experience;

            ViewBag.Categories = db.Categories
                .OrderBy(c => c.CategoryName)
                .ToList();

            return View(result);
        }

        public JsonResult Suggest(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Json(new string[] { }, JsonRequestBehavior.AllowGet);
            }

            string kw = keyword.Trim().ToLower();

            var today = DateTime.Today;

            var suggestions = db.Jobs
                .Where(j =>
                    j.Status == "Approved" &&
                    (!j.Deadline.HasValue || DbFunctions.TruncateTime(j.Deadline) >= today) &&
                    j.RemainingSlots > 0 &&
                    j.Title.ToLower().Contains(kw)
                )
                .Select(j => j.Title)
                .Distinct()
                .Take(8)
                .ToList();

            return Json(suggestions, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Details(int id)
        {
            var job = db.Jobs
                .Include(j => j.Company)
                .Include(j => j.Category)
                .FirstOrDefault(j =>
                    j.JobID == id &&
                    j.Status == "Approved"
                );

            if (job == null)
            {
                return HttpNotFound();
            }

            var today = DateTime.Today;

            if (job.Deadline.HasValue && job.Deadline.Value.Date < today)
            {
                TempData["Error"] = "This job posting has expired.";
                return RedirectToAction("Index", "Jobs");
            }

            if (job.RemainingSlots <= 0)
            {
                TempData["Error"] = "This job is already full.";
                return RedirectToAction("Index", "Jobs");
            }

            var similarJobs = db.Jobs
                .Include(j => j.Company)
                .Include(j => j.Category)
                .Where(j =>
                    j.JobID != job.JobID &&
                    j.Status == "Approved" &&
                    (!j.Deadline.HasValue || j.Deadline.Value >= today) &&
                    j.RemainingSlots > 0 &&
                    (
                        j.CategoryID == job.CategoryID ||
                        j.Location == job.Location ||
                        j.CompanyID == job.CompanyID
                    )
                )
                .OrderByDescending(j => j.CategoryID == job.CategoryID)
                .ThenByDescending(j => j.Location == job.Location)
                .ThenByDescending(j => j.CreatedAt)
                .Take(3)
                .ToList();

            ViewBag.SimilarJobs = similarJobs;

            return View(job);
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