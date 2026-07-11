using System;
using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class HomeController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        public ActionResult Index()
        {
            var today = DateTime.Today;

            var model = new HomeViewModel();

            model.Jobs = db.Jobs
                .Include("Company")
                .Where(j =>
                    j.Status == "Approved" &&
                    (!j.Deadline.HasValue || j.Deadline.Value >= today) &&
                    j.RemainingSlots > 0
                )
                .OrderByDescending(j => j.JobID)
                .Take(20)
                .ToList();

            model.Categories = db.Categories
                .Take(10)
                .ToList();

            model.Companies = db.Companies
                .Where(c => c.Status == "Approved")
                .OrderByDescending(c => c.CompanyID)
                .Take(10)
                .ToList();

            model.Articles = ArticleJsonStore.GetAll()
                .Where(a => a.IsActive && (a.TargetAudience == null || a.TargetAudience == "Candidate"))
                .OrderByDescending(a => a.CreatedAt)
                .Take(6)
                .ToList();

            return View(model);
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