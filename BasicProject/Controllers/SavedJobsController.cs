using System;
using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class SavedJobsController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        private Candidate GetCurrentCandidate()
        {
            if (Session["UserID"] == null || Session["Role"] == null)
            {
                return null;
            }

            if (Session["Role"].ToString() != "Candidate")
            {
                return null;
            }

            int userId = int.Parse(Session["UserID"].ToString());

            return db.Candidates.FirstOrDefault(c => c.UserID == userId);
        }

        public ActionResult Index()
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var savedJobs = db.SavedJobs
                .Include("Job")
                .Include("Job.Company")
                .Include("Job.Category")
                .Where(s => s.CandidateID == candidate.CandidateID)
                .OrderByDescending(s => s.SavedAt)
                .ToList();

            return View(savedJobs);
        }

        [HttpGet]
        public ActionResult Save(int jobId)
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var job = db.Jobs.FirstOrDefault(j => j.JobID == jobId);

            if (job == null)
            {
                return HttpNotFound();
            }

            bool existed = db.SavedJobs.Any(s =>
                s.CandidateID == candidate.CandidateID &&
                s.JobID == jobId
            );

            if (existed)
            {
                TempData["Info"] = "You have already saved this job.";
                return RedirectToAction("Details", "Jobs", new { id = jobId });
            }

            var savedJob = new SavedJob
            {
                CandidateID = candidate.CandidateID,
                JobID = jobId,
                SavedAt = DateTime.Now
            };

            db.SavedJobs.Add(savedJob);
            db.SaveChanges();

            TempData["Success"] = "Job saved successfully.";

            return RedirectToAction("Details", "Jobs", new { id = jobId });
        }

        [HttpGet]
        public ActionResult Unsave(int jobId)
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var savedJob = db.SavedJobs.FirstOrDefault(s =>
                s.CandidateID == candidate.CandidateID &&
                s.JobID == jobId
            );

            if (savedJob != null)
            {
                db.SavedJobs.Remove(savedJob);
                db.SaveChanges();

                TempData["Success"] = "Job removed from your saved list.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Toggle(int jobId)
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var job = db.Jobs.FirstOrDefault(j => j.JobID == jobId);

            if (job == null)
            {
                return HttpNotFound();
            }

            var savedJob = db.SavedJobs.FirstOrDefault(s =>
                s.CandidateID == candidate.CandidateID &&
                s.JobID == jobId
            );

            if (savedJob == null)
            {
                db.SavedJobs.Add(new SavedJob
                {
                    CandidateID = candidate.CandidateID,
                    JobID = jobId,
                    SavedAt = DateTime.Now
                });

                db.SaveChanges();

                TempData["Success"] = "Job saved successfully.";
            }
            else
            {
                TempData["Info"] = "You have already saved this job.";
            }

            return RedirectToAction("Details", "Jobs", new { id = jobId });
        }
    }
}