using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;
using System.Text;

namespace BasicProject.Controllers
{
    public class AdminApplicationsController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        private bool IsAdmin()
        {
            return Session["UserID"] != null
                && Session["Role"] != null
                && Session["Role"].ToString() == "Admin";
        }

        public ActionResult Index(string status, string keyword, string fromDate, string toDate)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var applications = db.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .Include(a => a.Job.Company)
                .Include(a => a.Job.Category)
                .Include(a => a.CV)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                applications = applications.Where(a => a.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();

                applications = applications.Where(a =>
                    a.Job.Title.ToLower().Contains(kw) ||
                    a.Job.Company.CompanyName.ToLower().Contains(kw) ||
                    a.Candidate.FullName.ToLower().Contains(kw)
                );
            }

            DateTime parsedFromDate;
            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out parsedFromDate))
            {
                applications = applications.Where(a =>
                    a.AppliedDate.HasValue &&
                    DbFunctions.TruncateTime(a.AppliedDate) >= parsedFromDate.Date
                );
            }

            DateTime parsedToDate;
            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out parsedToDate))
            {
                applications = applications.Where(a =>
                    a.AppliedDate.HasValue &&
                    DbFunctions.TruncateTime(a.AppliedDate) <= parsedToDate.Date
                );
            }

            ViewBag.TotalApplications = db.Applications.Count();
            ViewBag.PendingApplications = db.Applications.Count(a => a.Status == "Pending");
            ViewBag.AcceptedApplications = db.Applications.Count(a => a.Status == "Accepted");
            ViewBag.RejectedApplications = db.Applications.Count(a => a.Status == "Rejected");

            ViewBag.CurrentStatus = status;
            ViewBag.Keyword = keyword;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            var result = applications
                .OrderByDescending(a => a.AppliedDate)
                .ToList();

            return View(result);
        }

        public ActionResult ExportCsv(string status, string keyword, string fromDate, string toDate)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var applications = db.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .Include(a => a.Job.Company)
                .Include(a => a.Job.Category)
                .Include(a => a.CV)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                applications = applications.Where(a => a.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();

                applications = applications.Where(a =>
                    (a.Job != null && a.Job.Title.ToLower().Contains(kw)) ||
                    (a.Job != null && a.Job.Company != null && a.Job.Company.CompanyName.ToLower().Contains(kw)) ||
                    (a.Candidate != null && a.Candidate.FullName.ToLower().Contains(kw))
                );
            }

            DateTime parsedFromDate;
            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out parsedFromDate))
            {
                applications = applications.Where(a =>
                    a.AppliedDate.HasValue &&
                    DbFunctions.TruncateTime(a.AppliedDate) >= parsedFromDate.Date
                );
            }

            DateTime parsedToDate;
            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out parsedToDate))
            {
                applications = applications.Where(a =>
                    a.AppliedDate.HasValue &&
                    DbFunctions.TruncateTime(a.AppliedDate) <= parsedToDate.Date
                );
            }

            var result = applications
                .OrderByDescending(a => a.AppliedDate)
                .ToList();

            StringBuilder csv = new StringBuilder();

            csv.AppendLine("Candidate,Phone,Job Post,Company,Category,Status,Applied Date,CV Name");

            foreach (var item in result)
            {
                string candidateName = item.Candidate != null ? item.Candidate.FullName : "Unknown Candidate";
                string phone = item.Candidate != null ? item.Candidate.Phone : "";
                string jobTitle = item.Job != null ? item.Job.Title : "Unknown Job";
                string companyName = item.Job != null && item.Job.Company != null ? item.Job.Company.CompanyName : "Updating";
                string categoryName = item.Job != null && item.Job.Category != null ? item.Job.Category.CategoryName : "Uncategorized";
                string appStatus = item.Status;
                string appliedDate = item.AppliedDate.HasValue ? item.AppliedDate.Value.ToString("dd/MM/yyyy HH:mm") : "";
                string cvName = item.CV != null ? item.CV.CVName : "No CV";

                csv.AppendLine(
                    CsvText(candidateName) + "," +
                    CsvText(phone) + "," +
                    CsvText(jobTitle) + "," +
                    CsvText(companyName) + "," +
                    CsvText(categoryName) + "," +
                    CsvText(appStatus) + "," +
                    CsvText(appliedDate) + "," +
                    CsvText(cvName)
                );
            }

            byte[] buffer = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(csv.ToString()))
                .ToArray();

            string fileName = "applications-report-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";

            return File(buffer, "text/csv", fileName);
        }

        private string CsvText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            value = value.Replace("\"", "\"\"");

            return "\"" + value + "\"";
        }

        public ActionResult Details(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var application = db.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .Include(a => a.Job.Company)
                .Include(a => a.Job.Category)
                .Include(a => a.CV)
                .FirstOrDefault(a => a.ApplicationID == id);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
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