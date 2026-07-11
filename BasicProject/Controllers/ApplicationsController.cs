using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BasicProject.Models;
using BasicProject.Services;

namespace BasicProject.Controllers
{
    public class ApplicationsController : Controller
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

        private bool IsJobFull(int jobId, int hiringQuantity)
        {
            int acceptedCount = db.Applications.Count(a =>
                a.JobID == jobId &&
                a.Status == "Accepted"
            );

            return acceptedCount >= hiringQuantity;
        }

        [HttpGet]
        public ActionResult Apply(int jobId)
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var job = db.Jobs
                .Include(j => j.Company)
                .FirstOrDefault(j => j.JobID == jobId);

            if (job == null)
            {
                return HttpNotFound();
            }

            if (job.Status != "Approved")
            {
                TempData["Error"] = "This job is not available for application.";
                return RedirectToAction("Details", "Jobs", new { id = jobId });
            }

            if (job.Deadline.HasValue && job.Deadline.Value.Date < DateTime.Today)
            {
                TempData["Error"] = "The application deadline for this job has passed.";
                return RedirectToAction("Details", "Jobs", new { id = jobId });
            }

            if (IsJobFull(job.JobID, job.HiringQuantity))
            {
                TempData["Error"] = "This job has reached the maximum number of accepted candidates.";
                return RedirectToAction("Details", "Jobs", new { id = jobId });
            }

            var existedApplication = db.Applications
                .Include(a => a.CV)
                .FirstOrDefault(a => a.JobID == jobId && a.CandidateID == candidate.CandidateID);

            if (existedApplication != null)
            {
                TempData["Error"] = "You have already submitted a CV for this job.";
                return RedirectToAction("ApplySuccess", new { id = existedApplication.ApplicationID });
            }

            ViewBag.Job = job;

            return View();
        }

        [HttpPost]
        public ActionResult Apply(int jobId, HttpPostedFileBase cvFile, string phone)
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

            if (job.Status != "Approved")
            {
                TempData["Error"] = "This job is not available for application.";
                return RedirectToAction("Details", "Jobs", new { id = jobId });
            }

            if (job.Deadline.HasValue && job.Deadline.Value.Date < DateTime.Today)
            {
                TempData["Error"] = "The application deadline for this job has passed.";
                return RedirectToAction("Details", "Jobs", new { id = jobId });
            }

            if (IsJobFull(job.JobID, job.HiringQuantity))
            {
                TempData["Error"] = "This job has reached the maximum number of accepted candidates.";
                return RedirectToAction("Details", "Jobs", new { id = jobId });
            }

            var existedApplication = db.Applications
                .FirstOrDefault(a => a.JobID == jobId && a.CandidateID == candidate.CandidateID);

            if (existedApplication != null)
            {
                TempData["Error"] = "You have already submitted a CV for this job.";
                return RedirectToAction("ApplySuccess", new { id = existedApplication.ApplicationID });
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                TempData["Error"] = "Please enter your phone number.";
                return RedirectToAction("Apply", new { jobId = jobId });
            }

            if (cvFile == null || cvFile.ContentLength == 0)
            {
                TempData["Error"] = "Please choose a CV file before submitting.";
                return RedirectToAction("Apply", new { jobId = jobId });
            }

            string extension = Path.GetExtension(cvFile.FileName).ToLower();

            string[] allowedExtensions = { ".pdf", ".docx" };

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only PDF or DOCX CV files are allowed.";
                return RedirectToAction("Apply", new { jobId = jobId });
            }

            string folderPath = Server.MapPath("~/Content/uploads/cvs");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = "cv_candidate_"
                + candidate.CandidateID
                + "_job_"
                + jobId
                + "_"
                + DateTime.Now.ToString("yyyyMMddHHmmss")
                + extension;

            string fullPath = Path.Combine(folderPath, fileName);

            cvFile.SaveAs(fullPath);

            var aiService = new CvAiService();
            var aiResult = aiService.AnalyzeCvFile(fullPath);

            if (!aiResult.IsCv)
            {
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                TempData["Error"] = aiResult.Message;
                return RedirectToAction("Apply", new { jobId = jobId });
            }

            string filePath = "/Content/uploads/cvs/" + fileName;

            var cv = new CV
            {
                CandidateID = candidate.CandidateID,
                CVName = Path.GetFileName(cvFile.FileName),
                FilePath = filePath,
                CreatedAt = DateTime.Now
            };

            db.CVs.Add(cv);
            db.SaveChanges();

            var application = new Application
            {
                CandidateID = candidate.CandidateID,
                JobID = jobId,
                CV_ID = cv.CV_ID,
                AppliedDate = DateTime.Now,
                Status = "Pending"
            };

            db.Applications.Add(application);

            /*
               KHÔNG trừ RemainingSlots ở đây.
               Vì CV mới nộp chỉ là Pending.
               Slot chỉ được tính là đã dùng khi application.Status = "Accepted".
            */

            db.SaveChanges();

            TempData["Success"] = "CV submitted successfully. The employer will review your application.";

            return RedirectToAction("ApplySuccess", new { id = application.ApplicationID });
        }

        public ActionResult ApplySuccess(int id)
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var application = db.Applications
                .Include(a => a.Job)
                .Include(a => a.Job.Company)
                .Include(a => a.CV)
                .FirstOrDefault(a => a.ApplicationID == id && a.CandidateID == candidate.CandidateID);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        public ActionResult MyApplications()
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var applications = db.Applications
                .Include(a => a.Job)
                .Include(a => a.Job.Company)
                .Include(a => a.CV)
                .Where(a => a.CandidateID == candidate.CandidateID)
                .OrderByDescending(a => a.AppliedDate)
                .ToList();

            return View(applications);
        }

        [HttpGet]
        public ActionResult Resubmit(int id)
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var application = db.Applications
                .Include(a => a.Job)
                .Include(a => a.Job.Company)
                .Include(a => a.CV)
                .FirstOrDefault(a => a.ApplicationID == id && a.CandidateID == candidate.CandidateID);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        [HttpPost]
        public ActionResult Resubmit(int id, HttpPostedFileBase cvFile)
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var application = db.Applications
                .Include(a => a.Job)
                .Include(a => a.CV)
                .FirstOrDefault(a => a.ApplicationID == id && a.CandidateID == candidate.CandidateID);

            if (application == null)
            {
                return HttpNotFound();
            }

            if (application.Job != null)
            {
                if (application.Job.Status != "Approved")
                {
                    TempData["Error"] = "This job is not available for application.";
                    return RedirectToAction("ApplySuccess", new { id = application.ApplicationID });
                }

                if (application.Job.Deadline.HasValue && application.Job.Deadline.Value.Date < DateTime.Today)
                {
                    TempData["Error"] = "The application deadline for this job has passed.";
                    return RedirectToAction("ApplySuccess", new { id = application.ApplicationID });
                }

                if (application.Status != "Accepted" && IsJobFull(application.JobID, application.Job.HiringQuantity))
                {
                    TempData["Error"] = "This job has reached the maximum number of accepted candidates.";
                    return RedirectToAction("ApplySuccess", new { id = application.ApplicationID });
                }
            }

            if (cvFile == null || cvFile.ContentLength == 0)
            {
                TempData["Error"] = "Please choose a new CV file.";
                return RedirectToAction("Resubmit", new { id = id });
            }

            string extension = Path.GetExtension(cvFile.FileName).ToLower();

            string[] allowedExtensions = { ".pdf", ".docx" };

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only PDF or DOCX CV files are allowed.";
                return RedirectToAction("Resubmit", new { id = id });
            }

            string folderPath = Server.MapPath("~/Content/uploads/cvs");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = "cv_candidate_"
                + candidate.CandidateID
                + "_resubmit_"
                + DateTime.Now.ToString("yyyyMMddHHmmss")
                + extension;

            string fullPath = Path.Combine(folderPath, fileName);

            cvFile.SaveAs(fullPath);

            var aiService = new CvAiService();
            var aiResult = aiService.AnalyzeCvFile(fullPath);

            if (!aiResult.IsCv)
            {
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                TempData["Error"] = aiResult.Message;
                return RedirectToAction("Resubmit", new { id = id });
            }

            string filePath = "/Content/uploads/cvs/" + fileName;

            var newCV = new CV
            {
                CandidateID = candidate.CandidateID,
                CVName = Path.GetFileName(cvFile.FileName),
                FilePath = filePath,
                CreatedAt = DateTime.Now
            };

            db.CVs.Add(newCV);
            db.SaveChanges();

            application.CV_ID = newCV.CV_ID;
            application.AppliedDate = DateTime.Now;
            application.Status = "Pending";

            /*
               Resubmit cũng KHÔNG trừ slot.
               Vì sau khi nộp lại, trạng thái quay về Pending.
            */

            db.SaveChanges();

            TempData["Success"] = "CV resubmitted successfully. The employer will review your latest CV.";

            return RedirectToAction("ApplySuccess", new { id = application.ApplicationID });
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var application = db.Applications
                .Include(a => a.Job)
                .Include(a => a.Job.Company)
                .Include(a => a.CV)
                .FirstOrDefault(a => a.ApplicationID == id && a.CandidateID == candidate.CandidateID);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var candidate = GetCurrentCandidate();

            if (candidate == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Candidate" });
            }

            var application = db.Applications
                .FirstOrDefault(a => a.ApplicationID == id && a.CandidateID == candidate.CandidateID);

            if (application == null)
            {
                return HttpNotFound();
            }

            int jobId = application.JobID;

            db.Applications.Remove(application);

            /*
               KHÔNG cộng RemainingSlots ở đây.
               Vì slot sẽ được tính theo số application Accepted.
               Nếu application bị xóa và nó từng Accepted, số Accepted tự giảm.
            */

            db.SaveChanges();

            TempData["Success"] = "Application deleted successfully. You can submit a new CV for this job.";

            return RedirectToAction("Details", "Jobs", new { id = jobId });
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