using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class EmployersController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        public ActionResult Index(string keyword, string location, string companySize)
        {
            var companies = db.Companies
                .Where(c => c.Status == "Approved");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                companies = companies.Where(c =>
                    c.CompanyName.Contains(keyword) ||
                    c.Description.Contains(keyword) ||
                    c.Location.Contains(keyword)
                );

                ViewBag.Keyword = keyword;
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                companies = companies.Where(c =>
                    c.Location.Contains(location) ||
                    db.Jobs.Any(j =>
                        j.CompanyID == c.CompanyID &&
                        j.Status == "Approved" &&
                        (!j.Deadline.HasValue || j.Deadline.Value >= DateTime.Today) &&
                        j.RemainingSlots > 0 &&
                        j.Location.Contains(location)
                    )
                );

                ViewBag.Location = location;
            }

            if (!string.IsNullOrWhiteSpace(companySize))
            {
                companies = companies.Where(c => c.CompanySize == companySize);
                ViewBag.CompanySize = companySize;
            }

            var companyList = companies
                .OrderBy(c => c.CompanyName)
                .ToList();

            var companyIds = companyList.Select(c => c.CompanyID).ToList();

            var activeJobs = db.Jobs
                .Where(j =>
                    companyIds.Contains(j.CompanyID) &&
                    j.Status == "Approved" &&
                    (!j.Deadline.HasValue || j.Deadline.Value >= DateTime.Today) &&
                    j.RemainingSlots > 0
                )
                .ToList();

            var openJobCounts = activeJobs
                .GroupBy(j => j.CompanyID)
                .ToDictionary(g => g.Key, g => g.Count());

            var locationSummaries = new Dictionary<int, string>();

            foreach (var company in companyList)
            {
                var locations = new List<string>();

                if (!string.IsNullOrWhiteSpace(company.Location))
                {
                    locations.Add(company.Location.Trim());
                }

                var jobLocations = activeJobs
                    .Where(j => j.CompanyID == company.CompanyID)
                    .Where(j => !string.IsNullOrWhiteSpace(j.Location))
                    .Select(j => j.Location.Trim())
                    .ToList();

                locations.AddRange(jobLocations);

                locations = locations
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Distinct()
                    .ToList();

                locationSummaries[company.CompanyID] = BuildLocationSummary(locations);
            }

            ViewBag.OpenJobCounts = openJobCounts;
            ViewBag.LocationSummaries = locationSummaries;

            return View(companyList);
        }

        public ActionResult Details(int id)
        {
            var company = db.Companies.FirstOrDefault(c =>
                c.CompanyID == id &&
                c.Status == "Approved"
            );

            if (company == null)
            {
                return HttpNotFound();
            }

            var jobs = db.Jobs
                .Include(j => j.Category)
                .Where(j =>
                    j.CompanyID == id &&
                    j.Status == "Approved" &&
                    (!j.Deadline.HasValue || j.Deadline.Value >= DateTime.Today) &&
                    j.RemainingSlots > 0
                )
                .OrderByDescending(j => j.JobID)
                .ToList();

            var locations = new List<string>();

            if (!string.IsNullOrWhiteSpace(company.Location))
            {
                locations.Add(company.Location.Trim());
            }

            locations.AddRange(
                jobs
                    .Where(j => !string.IsNullOrWhiteSpace(j.Location))
                    .Select(j => j.Location.Trim())
                    .ToList()
            );

            locations = locations.Distinct().ToList();

            var model = new CompanyDetailViewModel
            {
                Company = company,
                Jobs = jobs,
                OpenJobCount = jobs.Count,
                LocationSummary = BuildLocationSummary(locations)
            };

            return View(model);
        }

        private string BuildLocationSummary(List<string> locations)
        {
            if (locations == null || locations.Count == 0)
            {
                return "Updating";
            }

            if (locations.Count == 1)
            {
                return locations[0];
            }

            return locations[0] + " + " + (locations.Count - 1) + " locations";
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