using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using BasicProject.Models;
using BasicProject.Services;

namespace BasicProject.Controllers
{
    public class EmployerApplicationsController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        private Employer GetCurrentEmployer()
        {
            if (Session["UserID"] == null || Session["Role"] == null || Session["Role"].ToString() != "Employer")
            {
                return null;
            }

            int userId = int.Parse(Session["UserID"].ToString());

            return db.Employers.FirstOrDefault(e => e.UserID == userId);
        }

        public ActionResult Index()
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var jobs = db.Jobs
                .Include(j => j.Company)
                .Where(j => j.CompanyID == employer.CompanyID)
                .OrderByDescending(j => j.CreatedAt)
                .ToList();

            ViewBag.ApplicationCounts = db.Applications
                .Where(a => a.Job.CompanyID == employer.CompanyID)
                .GroupBy(a => a.JobID)
                .ToDictionary(g => g.Key, g => g.Count());

            return View(jobs);
        }

        public ActionResult ByJob(
            int id,
            string keyword,
            string status,
            string matchLevel,
            string sort
        )
        {
            if (Session["UserID"] == null || Session["Role"] == null || Session["Role"].ToString() != "Employer")
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            int userId = int.Parse(Session["UserID"].ToString());

            var employer = db.Employers.FirstOrDefault(e => e.UserID == userId);

            if (employer == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var job = db.Jobs
    .Include("Company")
    .Include("Category")
    .FirstOrDefault(j => j.JobID == id && j.CompanyID == employer.CompanyID);

            if (job == null)
            {
                return HttpNotFound();
            }

            var applications = db.Applications
                .Include("Candidate")
                .Include("CV")
                .Include("Job")
                .Where(a => a.JobID == id)
                .ToList();

            var aiScores = new Dictionary<int, CvMatchResult>();
            var aiService = new CvAiService();

            foreach (var app in applications)
            {
                if (app.CV != null && !string.IsNullOrEmpty(app.CV.FilePath))
                {
                    try
                    {
                        string fullCvPath = Server.MapPath(app.CV.FilePath);

                        if (System.IO.File.Exists(fullCvPath))
                        {
                            var result = aiService.AnalyzeCvMatch(
    fullCvPath,
    job.Title,
    job.Description,
    job.Requirement,
    job.Experience,
    job.JobType,
    job.Category != null ? job.Category.CategoryName : ""
);

                            aiScores[app.ApplicationID] = result;
                        }
                        else
                        {
                            aiScores[app.ApplicationID] = new CvMatchResult
                            {
                                Score = 0,
                                Level = "File Not Found",
                                Message = "The CV file could not be found in the system."
                            };
                        }
                    }
                    catch
                    {
                        aiScores[app.ApplicationID] = new CvMatchResult
                        {
                            Score = 0,
                            Level = "Analysis Error",
                            Message = "The system could not analyze this CV."
                        };
                    }
                }
                else
                {
                    aiScores[app.ApplicationID] = new CvMatchResult
                    {
                        Score = 0,
                        Level = "No CV",
                        Message = "The candidate has not submitted a CV file."
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                applications = applications
                    .Where(a => a.Status != null && a.Status.ToLower() == status.ToLower())
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(matchLevel))
            {
                applications = applications
                    .Where(a =>
                        aiScores.ContainsKey(a.ApplicationID) &&
                        aiScores[a.ApplicationID].Level == matchLevel
                    )
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string lowerKeyword = keyword.ToLower();

                applications = applications
                    .Where(a =>
                    {
                        string candidateSkills = a.Candidate != null && a.Candidate.Skills != null ? a.Candidate.Skills.ToLower() : "";
                        string candidateExperience = a.Candidate != null && a.Candidate.Experience != null ? a.Candidate.Experience.ToLower() : "";
                        string candidateEducation = a.Candidate != null && a.Candidate.Education != null ? a.Candidate.Education.ToLower() : "";

                        string cvText = "";

                        if (aiScores.ContainsKey(a.ApplicationID) && aiScores[a.ApplicationID].ExtractedText != null)
                        {
                            cvText = aiScores[a.ApplicationID].ExtractedText.ToLower();
                        }

                        string matchedSkills = "";

                        if (aiScores.ContainsKey(a.ApplicationID) && aiScores[a.ApplicationID].MatchedSkills != null)
                        {
                            matchedSkills = string.Join(" ", aiScores[a.ApplicationID].MatchedSkills).ToLower();
                        }

                        return candidateSkills.Contains(lowerKeyword)
                            || candidateExperience.Contains(lowerKeyword)
                            || candidateEducation.Contains(lowerKeyword)
                            || cvText.Contains(lowerKeyword)
                            || matchedSkills.Contains(lowerKeyword);
                    })
                    .ToList();
            }

            switch (sort)
            {
                case "Newest":
                    applications = applications
                        .OrderByDescending(a => a.AppliedDate)
                        .ToList();
                    break;

                case "Oldest":
                    applications = applications
                        .OrderBy(a => a.AppliedDate)
                        .ToList();
                    break;

                case "Pending":
                    applications = applications
                        .OrderBy(a => a.Status == "Pending" ? 0 : 1)
                        .ThenByDescending(a => a.AppliedDate)
                        .ToList();
                    break;

                case "Accepted":
                    applications = applications
                        .OrderBy(a => a.Status == "Accepted" ? 0 : 1)
                        .ThenByDescending(a => a.AppliedDate)
                        .ToList();
                    break;

                case "Rejected":
                    applications = applications
                        .OrderBy(a => a.Status == "Rejected" ? 0 : 1)
                        .ThenByDescending(a => a.AppliedDate)
                        .ToList();
                    break;

                default:
                    applications = applications
                        .OrderByDescending(a => aiScores.ContainsKey(a.ApplicationID) ? aiScores[a.ApplicationID].Score : 0)
                        .ThenByDescending(a => a.AppliedDate)
                        .ToList();
                    break;
            }

            ViewBag.Job = job;
            ViewBag.AiScores = aiScores;

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.MatchLevel = matchLevel;
            ViewBag.Sort = string.IsNullOrWhiteSpace(sort) ? "BestMatch" : sort;

            ViewBag.TotalApplications = applications.Count;
            ViewBag.ExcellentCount = applications.Count(a => aiScores.ContainsKey(a.ApplicationID) && aiScores[a.ApplicationID].Score >= 75);
            ViewBag.GoodCount = applications.Count(a => aiScores.ContainsKey(a.ApplicationID) && aiScores[a.ApplicationID].Score >= 50 && aiScores[a.ApplicationID].Score < 75);
            ViewBag.PendingCount = applications.Count(a => a.Status == "Pending");
            ViewBag.AcceptedCount = applications.Count(a => a.Status == "Accepted");
            ViewBag.RejectedCount = applications.Count(a => a.Status == "Rejected");

            return View(applications);
        }

        public ActionResult Accept(int id)
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var application = db.Applications
                .Include("Job")
                .FirstOrDefault(a =>
                    a.ApplicationID == id &&
                    a.Job.CompanyID == employer.CompanyID
                );

            if (application == null)
            {
                return HttpNotFound();
            }

            if (application.Status == "Accepted")
            {
                TempData["Info"] = "This application has already been accepted.";
                return RedirectToAction("ByJob", new { id = application.JobID });
            }

            if (application.Job.RemainingSlots <= 0)
            {
                TempData["Error"] = "This job is already full. You cannot accept more candidates.";
                return RedirectToAction("ByJob", new { id = application.JobID });
            }

            application.Status = "Accepted";
            application.Job.RemainingSlots = application.Job.RemainingSlots - 1;

            db.SaveChanges();

            TempData["Success"] = "Candidate accepted successfully. Remaining slots have been updated.";

            return RedirectToAction("ByJob", new { id = application.JobID });
        }

        public ActionResult Reject(int id)
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var application = db.Applications
                .Include(a => a.Job)
                .FirstOrDefault(a =>
                    a.ApplicationID == id &&
                    a.Job.CompanyID == employer.CompanyID
                );

            if (application == null)
            {
                return HttpNotFound();
            }

            if (application.Status == "Accepted")
            {
                application.Job.RemainingSlots = application.Job.RemainingSlots + 1;

                if (application.Job.RemainingSlots > application.Job.HiringQuantity)
                {
                    application.Job.RemainingSlots = application.Job.HiringQuantity;
                }

                if (application.Job.Status == "Closed")
                {
                    application.Job.Status = "Approved";
                }
            }

            application.Status = "Rejected";

            db.SaveChanges();

            TempData["Success"] = "Candidate rejected successfully. Remaining slots have been updated.";

            return RedirectToAction("ByJob", new { id = application.JobID });
        }

        private ApplicationMatchInfo CalculateRuleMatch(Job job, Candidate candidate)
        {
            var result = new ApplicationMatchInfo();

            if (job == null || candidate == null)
            {
                result.Score = 0;
                result.Level = "Low Match";
                result.Message = "Not enough data to calculate match score.";
                return result;
            }

            string jobText = NormalizeText(
                (job.Title ?? "") + " " +
                (job.Description ?? "") + " " +
                (job.Requirement ?? "") + " " +
                (job.JobType ?? "") + " " +
                (job.Experience ?? "") + " " +
                (job.Category != null ? job.Category.CategoryName : "")
            );

            string candidateText = NormalizeText(
                (candidate.Skills ?? "") + " " +
                (candidate.Experience ?? "") + " " +
                (candidate.Education ?? "") + " " +
                (candidate.Address ?? "")
            );

            var requiredSkills = ExtractRequiredSkills(jobText);

            foreach (var skill in requiredSkills)
            {
                if (candidateText.Contains(skill.ToLower()))
                {
                    result.MatchedSkills.Add(skill);
                }
                else
                {
                    result.MissingSkills.Add(skill);
                }
            }

            int skillScore = 0;

            if (requiredSkills.Count > 0)
            {
                skillScore = (int)Math.Round((double)result.MatchedSkills.Count / requiredSkills.Count * 50);
            }
            else
            {
                skillScore = 20;
            }

            int experienceScore = CalculateExperienceScore(job.Experience, candidate.Experience);
            int locationScore = CalculateLocationScore(job.Location, candidate.Address);
            int profileScore = CalculateProfileScore(job, candidate, result.MatchedSkills.Count);

            result.Score = skillScore + experienceScore + locationScore + profileScore;

            if (result.Score > 100)
            {
                result.Score = 100;
            }

            result.Level = GetMatchLevel(result.Score);
            result.Message = "Score calculated from candidate skills, experience, location, and profile information.";

            return result;
        }

        private List<string> ExtractRequiredSkills(string jobText)
        {
            var skillBank = new List<string>
            {
                "html", "css", "javascript", "jquery", "bootstrap",
                "c#", "asp.net", "mvc", "sql", "sql server", "entity framework",
                "java", "python", "php", "react", "angular", "vue",
                "sales", "customer service", "communication", "negotiation",
                "marketing", "content", "seo", "facebook ads", "google ads",
                "accounting", "finance", "excel", "word", "powerpoint",
                "design", "photoshop", "illustrator", "figma",
                "english", "teaching", "training", "teamwork", "leadership"
            };

            var result = new List<string>();

            foreach (var skill in skillBank)
            {
                if (jobText.Contains(skill))
                {
                    result.Add(skill);
                }
            }

            return result.Distinct().ToList();
        }

        private int CalculateExperienceScore(string jobExperience, string candidateExperience)
        {
            string jobExpText = NormalizeText(jobExperience);
            string candidateExpText = NormalizeText(candidateExperience);

            if (string.IsNullOrWhiteSpace(jobExpText) ||
                jobExpText.Contains("no experience") ||
                jobExpText.Contains("fresher") ||
                jobExpText.Contains("intern"))
            {
                return 20;
            }

            int requiredYears = ExtractFirstNumber(jobExpText);
            int candidateYears = ExtractFirstNumber(candidateExpText);

            if (requiredYears <= 0)
            {
                return 15;
            }

            if (candidateYears >= requiredYears)
            {
                return 20;
            }

            if (candidateYears > 0 && candidateYears + 1 >= requiredYears)
            {
                return 12;
            }

            if (!string.IsNullOrWhiteSpace(candidateExpText))
            {
                return 7;
            }

            return 0;
        }

        private int CalculateLocationScore(string jobLocation, string candidateAddress)
        {
            string jobLoc = NormalizeText(jobLocation);
            string candLoc = NormalizeText(candidateAddress);

            if (string.IsNullOrWhiteSpace(jobLoc) || string.IsNullOrWhiteSpace(candLoc))
            {
                return 5;
            }

            if (jobLoc.Contains(candLoc) || candLoc.Contains(jobLoc))
            {
                return 15;
            }

            if (jobLoc.Contains("remote") || candLoc.Contains("remote"))
            {
                return 12;
            }

            return 0;
        }

        private int CalculateProfileScore(Job job, Candidate candidate, int matchedSkillCount)
        {
            int score = 0;

            string candidateText = NormalizeText(
                (candidate.Skills ?? "") + " " +
                (candidate.Experience ?? "") + " " +
                (candidate.Education ?? "")
            );

            if (matchedSkillCount > 0)
            {
                score += 10;
            }

            if (job.Category != null && !string.IsNullOrEmpty(job.Category.CategoryName))
            {
                string category = NormalizeText(job.Category.CategoryName);

                if (candidateText.Contains(category))
                {
                    score += 5;
                }
            }

            return score;
        }

        private int ExtractFirstNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            var match = Regex.Match(text, @"\d+");

            if (match.Success)
            {
                int number = 0;

                if (int.TryParse(match.Value, out number))
                {
                    return number;
                }
            }

            return 0;
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            return text.Trim().ToLower();
        }

        private string GetMatchLevel(int score)
        {
            if (score >= 80)
            {
                return "Excellent Match";
            }

            if (score >= 60)
            {
                return "Good Match";
            }

            if (score >= 40)
            {
                return "Average Match";
            }

            return "Low Match";
        }

        private string BuildMatchMessage(int score, string aiLevel, string aiMessage)
        {
            if (score >= 80)
            {
                return "This candidate is highly suitable for the job based on CV analysis and profile information.";
            }

            if (score >= 60)
            {
                return "This candidate matches several important requirements for the job.";
            }

            if (score >= 40)
            {
                return "This candidate has some relevant information but may need further review.";
            }

            return "This candidate has limited matching information. Please review the CV manually before making a decision.";
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