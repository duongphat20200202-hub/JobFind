using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class EmployerJobsController : Controller
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

        private Company GetEmployerCompany(Employer employer)
        {
            if (employer == null)
            {
                return null;
            }

            return db.Companies.FirstOrDefault(c => c.CompanyID == employer.CompanyID);
        }

        private bool IsCompanyApproved(Employer employer)
        {
            var company = GetEmployerCompany(employer);

            return company != null && company.Status == "Approved";
        }

        private void LoadJobFormData()
        {
            ViewBag.Categories = db.Categories
                .OrderBy(c => c.CategoryName)
                .ToList();
        }

        private string CleanText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private decimal? CleanSalary(decimal? salary)
        {
            if (!salary.HasValue || salary.Value <= 0)
            {
                return null;
            }

            return salary;
        }

        public ActionResult Index()
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var company = GetEmployerCompany(employer);

            if (company == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.CompanyStatus = company.Status;
            ViewBag.CanPostJob = company.Status == "Approved";

            var jobs = db.Jobs
                .Include("Company")
                .Include("Category")
                .Where(j => j.CompanyID == employer.CompanyID)
                .OrderByDescending(j => j.JobID)
                .ToList();

            return View(jobs);
        }

        public ActionResult Create()
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            if (!IsCompanyApproved(employer))
            {
                TempData["Error"] = "Your company has not been approved by admin yet, so you cannot post jobs.";
                return RedirectToAction("Index", "EmployerDashboard");
            }

            LoadJobFormData();
            return View();
        }

        [HttpPost]
        public ActionResult Create(
            string title,
            string description,
            string requirement,
            string benefit,
            decimal? salary,
            string location,
            string jobType,
            string experience,
            DateTime? deadline,
            int? categoryId,
            int? hiringQuantity
        )
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            if (!IsCompanyApproved(employer))
            {
                TempData["Error"] = "Your company has not been approved by admin yet, so you cannot post jobs.";
                return RedirectToAction("Index", "EmployerDashboard");
            }

            title = CleanText(title);
            description = CleanText(description);
            requirement = CleanText(requirement);
            benefit = CleanText(benefit);
            location = CleanText(location);
            jobType = CleanText(jobType);
            experience = CleanText(experience);
            salary = CleanSalary(salary);

            if (string.IsNullOrWhiteSpace(title))
            {
                ViewBag.Error = "Please enter the job title.";
                LoadJobFormData();
                return View();
            }

            if (!categoryId.HasValue)
            {
                ViewBag.Error = "Please select a job category.";
                LoadJobFormData();
                return View();
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                ViewBag.Error = "Please enter the job description.";
                LoadJobFormData();
                return View();
            }

            if (string.IsNullOrWhiteSpace(requirement))
            {
                ViewBag.Error = "Please enter the job requirements.";
                LoadJobFormData();
                return View();
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                ViewBag.Error = "Please select a location.";
                LoadJobFormData();
                return View();
            }

            if (string.IsNullOrWhiteSpace(jobType))
            {
                ViewBag.Error = "Please select a job type.";
                LoadJobFormData();
                return View();
            }

            if (string.IsNullOrWhiteSpace(experience))
            {
                ViewBag.Error = "Please select an experience level.";
                LoadJobFormData();
                return View();
            }

            if (deadline.HasValue && deadline.Value.Date < DateTime.Today)
            {
                ViewBag.Error = "Application deadline cannot be earlier than today.";
                LoadJobFormData();
                return View();
            }

            if (!hiringQuantity.HasValue || hiringQuantity.Value <= 0)
            {
                ViewBag.Error = "Please enter a valid hiring quantity.";
                LoadJobFormData();
                return View();
            }

            var job = new Job
            {
                EmployerID = employer.EmployerID,
                CompanyID = employer.CompanyID,
                CategoryID = categoryId,

                Title = title,
                Description = description,
                Requirement = requirement,
                Benefit = benefit,
                Salary = salary,
                Location = location,
                JobType = jobType,
                Experience = experience,
                Deadline = deadline,

                HiringQuantity = hiringQuantity.Value,
                RemainingSlots = hiringQuantity.Value,

                IsHot = false,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            db.Jobs.Add(job);
            db.SaveChanges();

            TempData["Success"] = "Job posted successfully. Your job post is waiting for admin approval.";

            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var job = db.Jobs.FirstOrDefault(j =>
                j.JobID == id &&
                j.CompanyID == employer.CompanyID
            );

            if (job == null)
            {
                return HttpNotFound();
            }

            LoadJobFormData();

            return View(job);
        }

        [HttpPost]
        public ActionResult Edit(
            int id,
            string title,
            string description,
            string requirement,
            string benefit,
            decimal? salary,
            string location,
            string jobType,
            string experience,
            DateTime? deadline,
            int? categoryId,
            int? hiringQuantity
        )
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var job = db.Jobs.FirstOrDefault(j =>
                j.JobID == id &&
                j.CompanyID == employer.CompanyID
            );

            if (job == null)
            {
                return HttpNotFound();
            }

            title = CleanText(title);
            description = CleanText(description);
            requirement = CleanText(requirement);
            benefit = CleanText(benefit);
            location = CleanText(location);
            jobType = CleanText(jobType);
            experience = CleanText(experience);
            salary = CleanSalary(salary);

            if (string.IsNullOrWhiteSpace(title))
            {
                ViewBag.Error = "Please enter the job title.";
                LoadJobFormData();
                return View(job);
            }

            if (!categoryId.HasValue)
            {
                ViewBag.Error = "Please select a job category.";
                LoadJobFormData();
                return View(job);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                ViewBag.Error = "Please enter the job description.";
                LoadJobFormData();
                return View(job);
            }

            if (string.IsNullOrWhiteSpace(requirement))
            {
                ViewBag.Error = "Please enter the job requirements.";
                LoadJobFormData();
                return View(job);
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                ViewBag.Error = "Please select a location.";
                LoadJobFormData();
                return View(job);
            }

            if (string.IsNullOrWhiteSpace(jobType))
            {
                ViewBag.Error = "Please select a job type.";
                LoadJobFormData();
                return View(job);
            }

            if (string.IsNullOrWhiteSpace(experience))
            {
                ViewBag.Error = "Please select an experience level.";
                LoadJobFormData();
                return View(job);
            }

            if (deadline.HasValue && deadline.Value.Date < DateTime.Today)
            {
                ViewBag.Error = "Application deadline cannot be earlier than today.";
                LoadJobFormData();
                return View(job);
            }

            if (!hiringQuantity.HasValue || hiringQuantity.Value <= 0)
            {
                ViewBag.Error = "Please enter a valid hiring quantity.";
                LoadJobFormData();
                return View(job);
            }

            int acceptedCount = db.Applications.Count(a =>
                a.JobID == job.JobID &&
                a.Status == "Accepted"
            );

            if (hiringQuantity.Value < acceptedCount)
            {
                ViewBag.Error = "Hiring quantity cannot be smaller than the number of accepted candidates.";
                LoadJobFormData();
                return View(job);
            }

            job.Title = title;
            job.Description = description;
            job.Requirement = requirement;
            job.Benefit = benefit;
            job.Salary = salary;
            job.Location = location;
            job.JobType = jobType;
            job.Experience = experience;
            job.Deadline = deadline;
            job.CategoryID = categoryId;
            job.HiringQuantity = hiringQuantity.Value;
            job.RemainingSlots = hiringQuantity.Value - acceptedCount;

            if (job.RemainingSlots < 0)
            {
                job.RemainingSlots = 0;
            }

            job.Status = "Pending";

            db.SaveChanges();

            TempData["Success"] = "Job post updated successfully. Your job post is waiting for admin approval again.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var job = db.Jobs.FirstOrDefault(j =>
                j.JobID == id &&
                j.CompanyID == employer.CompanyID
            );

            if (job == null)
            {
                return HttpNotFound();
            }

            db.Jobs.Remove(job);
            db.SaveChanges();

            TempData["Success"] = "Job post deleted successfully.";

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