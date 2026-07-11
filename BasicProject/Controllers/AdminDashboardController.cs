using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class AdminDashboardController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        private bool IsAdmin()
        {
            return Session["UserID"] != null
                && Session["Role"] != null
                && Session["Role"].ToString() == "Admin";
        }

        public ActionResult Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            DateTime today = DateTime.Today;
            DateTime currentMonthStart = new DateTime(today.Year, today.Month, 1);
            DateTime nextMonthStart = currentMonthStart.AddMonths(1);
            DateTime prevMonthStart = currentMonthStart.AddMonths(-1);

            // =========================
            // KPI COUNTS
            // =========================
            ViewBag.TotalUsers = db.Users.Count();
            ViewBag.TotalJobs = db.Jobs.Count();
            ViewBag.TotalApplications = db.Applications.Count();
            ViewBag.TotalCompanies = db.Companies.Count();

            int usersThisMonth = db.Users.Count(u => u.CreatedAt >= currentMonthStart && u.CreatedAt < nextMonthStart);
            int usersPrevMonth = db.Users.Count(u => u.CreatedAt >= prevMonthStart && u.CreatedAt < currentMonthStart);

            int jobsThisMonth = db.Jobs.Count(j => j.CreatedAt >= currentMonthStart && j.CreatedAt < nextMonthStart);
            int jobsPrevMonth = db.Jobs.Count(j => j.CreatedAt >= prevMonthStart && j.CreatedAt < currentMonthStart);

            int applicationsThisMonth = db.Applications.Count(a => a.AppliedDate >= currentMonthStart && a.AppliedDate < nextMonthStart);
            int applicationsPrevMonth = db.Applications.Count(a => a.AppliedDate >= prevMonthStart && a.AppliedDate < currentMonthStart);

            int companiesThisMonth = db.Companies.Count(c => c.CreatedAt >= currentMonthStart && c.CreatedAt < nextMonthStart);
            int companiesPrevMonth = db.Companies.Count(c => c.CreatedAt >= prevMonthStart && c.CreatedAt < currentMonthStart);

            ViewBag.UserGrowth = CalculateGrowth(usersThisMonth, usersPrevMonth);
            ViewBag.JobGrowth = CalculateGrowth(jobsThisMonth, jobsPrevMonth);
            ViewBag.ApplicationGrowth = CalculateGrowth(applicationsThisMonth, applicationsPrevMonth);
            ViewBag.CompanyGrowth = CalculateGrowth(companiesThisMonth, companiesPrevMonth);

            // =========================
            // STATUS COUNTS
            // =========================
            ViewBag.PendingApplications = db.Applications.Count(a => a.Status == "Pending");
            ViewBag.AcceptedApplications = db.Applications.Count(a => a.Status == "Accepted");
            ViewBag.RejectedApplications = db.Applications.Count(a => a.Status == "Rejected");

            ViewBag.ApprovedJobs = db.Jobs.Count(j => j.Status == "Approved");
            ViewBag.PendingJobs = db.Jobs.Count(j => j.Status == "Pending");
            ViewBag.RejectedJobs = db.Jobs.Count(j => j.Status == "Rejected");
            ViewBag.ClosedJobs = db.Jobs.Count(j => j.Status == "Closed");

            // =========================
            // TREND CHART DATA
            // =========================
            LoadTrendChartData(today);

            // =========================
            // TOP APPLIED JOBS
            // =========================
            var topAppliedJobsRaw = db.Jobs
    .Include(j => j.Company)
    .ToList();

            var topAppliedJobs = topAppliedJobsRaw
                .Select(j => new AdminTopAppliedJobItem
                {
                    JobID = j.JobID,
                    Title = j.Title,
                    CompanyName = j.Company != null ? j.Company.CompanyName : "Updating",
                    ApplicationsCount = db.Applications.Count(a => a.JobID == j.JobID)
                })
                .OrderByDescending(x => x.ApplicationsCount)
                .ThenBy(x => x.Title)
                .Take(5)
                .ToList();

            ViewBag.TopAppliedJobs = topAppliedJobs;

            // =========================
            // TOP CATEGORIES
            // =========================
            var topCategories = db.Applications
                .Include(a => a.Job.Category)
                .ToList()
                .GroupBy(a =>
                    a.Job != null && a.Job.Category != null
                        ? a.Job.Category.CategoryName
                        : "Uncategorized"
                )
                .Select(g => new
                {
                    CategoryName = g.Key,
                    ApplicationsCount = g.Count()
                })
                .OrderByDescending(x => x.ApplicationsCount)
                .Take(6)
                .ToList();

            ViewBag.TopCategoryLabels = topCategories.Select(x => x.CategoryName).ToList();
            ViewBag.TopCategoryCounts = topCategories.Select(x => x.ApplicationsCount).ToList();

            // =========================
            // LATEST JOB POSTS
            // =========================
            var latestJobs = db.Jobs
    .Include(j => j.Company)
    .Include(j => j.Category)
    .OrderByDescending(j => j.CreatedAt)
    .Take(6)
    .ToList()
    .Select(j => new AdminLatestJobItem
    {
        JobID = j.JobID,
        Title = j.Title,
        CompanyName = j.Company != null ? j.Company.CompanyName : "Updating",
        CategoryName = j.Category != null ? j.Category.CategoryName : "Uncategorized",
        Status = string.IsNullOrEmpty(j.Status) ? "Unknown" : j.Status,
        CreatedDate = j.CreatedAt.HasValue ? j.CreatedAt.Value.ToString("dd/MM/yyyy") : "Updating"
    })
    .ToList();

            ViewBag.LatestJobs = latestJobs;

            // =========================
            // JOBS EXPIRING SOON
            // =========================
            var expiringSoonJobs = db.Jobs
    .Include(j => j.Company)
    .Where(j => j.Status == "Approved" && j.Deadline.HasValue)
    .ToList()
    .Where(j => j.Deadline.Value.Date >= today && (j.Deadline.Value.Date - today).Days <= 7)
    .OrderBy(j => j.Deadline)
    .Take(6)
    .Select(j => new AdminExpiringJobItem
    {
        JobID = j.JobID,
        Title = j.Title,
        CompanyName = j.Company != null ? j.Company.CompanyName : "Updating",
        Deadline = j.Deadline.HasValue ? j.Deadline.Value.ToString("dd/MM/yyyy") : "No deadline",
        DaysLeft = j.Deadline.HasValue ? (j.Deadline.Value.Date - today).Days : 0
    })
    .ToList();

            ViewBag.ExpiringSoonJobs = expiringSoonJobs;

            return View();
        }

        [ChildActionOnly]
        public PartialViewResult NotificationMenu()
        {
            if (!IsAdmin())
            {
                return PartialView("_AdminNotificationMenu", new List<AdminNotificationItem>());
            }

            int pendingCompanyCount = db.Companies.Count(c => c.Status == "Pending");
            int pendingJobCount = db.Jobs.Count(j => j.Status == "Pending");

            ViewBag.NotificationCount = pendingCompanyCount + pendingJobCount;

            var companyItems = db.Companies
                .Where(c => c.Status == "Pending")
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList()
                .Select(c => new AdminNotificationItem
                {
                    IconClass = "fa-building",
                    Title = "Pending company",
                    Message = c.CompanyName + " is waiting for approval",
                    Url = Url.Action("Index", "AdminCompanies"),
                    CreatedAt = c.CreatedAt ?? DateTime.Now
                });

            var jobItems = db.Jobs
                .Include(j => j.Company)
                .Where(j => j.Status == "Pending")
                .OrderByDescending(j => j.CreatedAt)
                .Take(5)
                .ToList()
                .Select(j => new AdminNotificationItem
                {
                    IconClass = "fa-briefcase",
                    Title = "Pending job post",
                    Message = j.Title + " - " + (j.Company != null ? j.Company.CompanyName : "Updating"),
                    Url = Url.Action("Index", "AdminJobs"),
                    CreatedAt = j.CreatedAt ?? DateTime.Now
                });

            var items = companyItems
                .Concat(jobItems)
                .OrderByDescending(x => x.CreatedAt)
                .Take(8)
                .ToList();

            return PartialView("_AdminNotificationMenu", items);
        }

        private void LoadTrendChartData(DateTime today)
        {
            // ---------- 7 DAYS ----------
            List<string> trend7Labels = new List<string>();
            List<int> trend7Jobs = new List<int>();
            List<int> trend7Applications = new List<int>();

            for (int i = 6; i >= 0; i--)
            {
                DateTime from = today.AddDays(-i);
                DateTime to = from.AddDays(1);

                trend7Labels.Add(from.ToString("dd/MM"));
                trend7Jobs.Add(db.Jobs.Count(j => j.CreatedAt >= from && j.CreatedAt < to));
                trend7Applications.Add(db.Applications.Count(a => a.AppliedDate >= from && a.AppliedDate < to));
            }

            ViewBag.Trend7Labels = trend7Labels;
            ViewBag.Trend7Jobs = trend7Jobs;
            ViewBag.Trend7Applications = trend7Applications;

            // ---------- 30 DAYS ----------
            List<string> trend30Labels = new List<string>();
            List<int> trend30Jobs = new List<int>();
            List<int> trend30Applications = new List<int>();

            for (int i = 29; i >= 0; i--)
            {
                DateTime from = today.AddDays(-i);
                DateTime to = from.AddDays(1);

                trend30Labels.Add(from.ToString("dd/MM"));
                trend30Jobs.Add(db.Jobs.Count(j => j.CreatedAt >= from && j.CreatedAt < to));
                trend30Applications.Add(db.Applications.Count(a => a.AppliedDate >= from && a.AppliedDate < to));
            }

            ViewBag.Trend30Labels = trend30Labels;
            ViewBag.Trend30Jobs = trend30Jobs;
            ViewBag.Trend30Applications = trend30Applications;

            // ---------- 12 MONTHS ----------
            List<string> trend12Labels = new List<string>();
            List<int> trend12Jobs = new List<int>();
            List<int> trend12Applications = new List<int>();

            DateTime monthStartBase = new DateTime(today.Year, today.Month, 1);

            for (int i = 11; i >= 0; i--)
            {
                DateTime from = monthStartBase.AddMonths(-i);
                DateTime to = from.AddMonths(1);

                trend12Labels.Add(from.ToString("MM/yyyy"));
                trend12Jobs.Add(db.Jobs.Count(j => j.CreatedAt >= from && j.CreatedAt < to));
                trend12Applications.Add(db.Applications.Count(a => a.AppliedDate >= from && a.AppliedDate < to));
            }

            ViewBag.Trend12Labels = trend12Labels;
            ViewBag.Trend12Jobs = trend12Jobs;
            ViewBag.Trend12Applications = trend12Applications;
        }

        private int CalculateGrowth(int currentValue, int previousValue)
        {
            if (previousValue <= 0)
            {
                return currentValue > 0 ? 100 : 0;
            }

            return (int)Math.Round(((currentValue - previousValue) * 100.0) / previousValue);
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