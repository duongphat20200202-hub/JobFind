using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace BasicProject.Services
{
    public class CvAiResult
    {
        public bool IsCv { get; set; }
        public int Score { get; set; }
        public string Message { get; set; }
        public string ExtractedText { get; set; }
    }

    public class CvMatchResult
    {
        public int Score { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }

        public List<string> MatchedSkills { get; set; }
        public List<string> MissingSkills { get; set; }
        public string ExtractedText { get; set; }

        public CvMatchResult()
        {
            MatchedSkills = new List<string>();
            MissingSkills = new List<string>();
            ExtractedText = "";
        }
    }

    public class CvAiService
    {
        public CvAiResult AnalyzeCvFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            string text = "";

            if (extension == ".pdf")
            {
                text = ExtractTextFromPdf(filePath);
            }
            else if (extension == ".docx")
            {
                text = ExtractTextFromDocx(filePath);
            }
            else
            {
                return new CvAiResult
                {
                    IsCv = false,
                    Score = 0,
                    Message = "The system only supports PDF or DOCX CV files.",
                    ExtractedText = ""
                };
            }

            return AnalyzeText(text);
        }

        private CvAiResult AnalyzeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new CvAiResult
                {
                    IsCv = false,
                    Score = 0,
                    Message = "The system could not read the content of this file. Please upload a valid CV file.",
                    ExtractedText = ""
                };
            }

            string lower = NormalizeText(text);

            int wordCount = lower
                .Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Length;

            if (wordCount < 50)
            {
                return new CvAiResult
                {
                    IsCv = false,
                    Score = 0,
                    Message = "This file is too short to be recognized as a valid CV. Please upload a complete CV file.",
                    ExtractedText = text
                };
            }

            string[] negativeKeywords =
            {
        "invoice",
        "receipt",
        "payment",
        "contract",
        "agreement",
        "report",
        "assignment",
        "homework",
        "essay",
        "article",
        "chapter",
        "table of contents",
        "meeting minutes",
        "purchase order",
        "quotation",
        "bill",
        "tax",
        "statement",
        "slide",
        "presentation",
        "research paper",
        "đơn hàng",
        "hóa đơn",
        "hoa don",
        "bài tập",
        "bai tap",
        "báo cáo",
        "bao cao",
        "hợp đồng",
        "hop dong",
        "biên bản",
        "bien ban"
    };

            int negativeCount = 0;

            foreach (var keyword in negativeKeywords)
            {
                if (lower.Contains(keyword))
                {
                    negativeCount++;
                }
            }

            if (negativeCount >= 2)
            {
                return new CvAiResult
                {
                    IsCv = false,
                    Score = 0,
                    Message = "This file looks like another type of document, not a CV. Please upload a valid CV file.",
                    ExtractedText = text
                };
            }

            bool hasEmail = lower.Contains("@");

            bool hasPhone =
                lower.Contains("phone") ||
                lower.Contains("mobile") ||
                lower.Contains("tel") ||
                lower.Contains("contact") ||
                lower.Contains("số điện thoại") ||
                lower.Contains("so dien thoai") ||
                System.Text.RegularExpressions.Regex.IsMatch(lower, @"\b0\d{8,10}\b") ||
                System.Text.RegularExpressions.Regex.IsMatch(lower, @"\b\+?\d{9,12}\b");

            bool hasName =
                lower.Contains("full name") ||
                lower.Contains("name") ||
                lower.Contains("candidate") ||
                lower.Contains("họ tên") ||
                lower.Contains("ho ten");

            bool hasSkills =
                lower.Contains("skills") ||
                lower.Contains("technical skills") ||
                lower.Contains("soft skills") ||
                lower.Contains("kỹ năng") ||
                lower.Contains("ky nang");

            bool hasExperience =
                lower.Contains("experience") ||
                lower.Contains("work experience") ||
                lower.Contains("employment history") ||
                lower.Contains("internship") ||
                lower.Contains("kinh nghiệm") ||
                lower.Contains("kinh nghiem");

            bool hasEducation =
                lower.Contains("education") ||
                lower.Contains("university") ||
                lower.Contains("college") ||
                lower.Contains("degree") ||
                lower.Contains("major") ||
                lower.Contains("gpa") ||
                lower.Contains("học vấn") ||
                lower.Contains("hoc van");

            bool hasProject =
                lower.Contains("project") ||
                lower.Contains("projects") ||
                lower.Contains("portfolio") ||
                lower.Contains("dự án") ||
                lower.Contains("du an");

            bool hasObjective =
                lower.Contains("career objective") ||
                lower.Contains("objective") ||
                lower.Contains("summary") ||
                lower.Contains("profile") ||
                lower.Contains("about me") ||
                lower.Contains("mục tiêu nghề nghiệp") ||
                lower.Contains("muc tieu nghe nghiep");

            bool hasCertificate =
                lower.Contains("certificate") ||
                lower.Contains("certification") ||
                lower.Contains("certifications") ||
                lower.Contains("award") ||
                lower.Contains("chứng chỉ") ||
                lower.Contains("chung chi");

            int score = 0;
            int sectionCount = 0;

            if (hasEmail)
            {
                score += 20;
            }

            if (hasPhone)
            {
                score += 15;
            }

            if (hasName)
            {
                score += 10;
            }

            if (hasSkills)
            {
                score += 20;
                sectionCount++;
            }

            if (hasExperience)
            {
                score += 20;
                sectionCount++;
            }

            if (hasEducation)
            {
                score += 15;
                sectionCount++;
            }

            if (hasProject)
            {
                score += 10;
                sectionCount++;
            }

            if (hasObjective)
            {
                score += 10;
                sectionCount++;
            }

            if (hasCertificate)
            {
                score += 5;
                sectionCount++;
            }

            if (lower.Contains("curriculum vitae") || lower.Contains("resume") || lower.Contains("cv"))
            {
                score += 10;
            }

            if (score > 100)
            {
                score = 100;
            }

            bool hasContactInfo = hasEmail || hasPhone;

            bool isCv =
                score >= 60 &&
                hasContactInfo &&
                sectionCount >= 2;

            if (!isCv && score >= 45 && hasContactInfo && sectionCount >= 3)
            {
                isCv = true;
            }

            return new CvAiResult
            {
                IsCv = isCv,
                Score = score,
                Message = isCv
                    ? "The system confirmed that this file looks like a valid CV."
                    : "This file does not have enough CV information. A valid CV should include contact information, skills, education, and work experience or projects.",
                ExtractedText = text
            };
        }

        // Overload cũ để controller cũ vẫn chạy nếu còn gọi 4 tham số
        public CvMatchResult AnalyzeCvMatch(
            string cvFilePath,
            string jobTitle,
            string jobDescription,
            string jobRequirement)
        {
            return AnalyzeCvMatch(
                cvFilePath,
                jobTitle,
                jobDescription,
                jobRequirement,
                "",
                "",
                ""
            );
        }

        // Hàm chính để chấm CV theo Job
        public CvMatchResult AnalyzeCvMatch(
            string cvFilePath,
            string jobTitle,
            string jobDescription,
            string jobRequirement,
            string jobExperience,
            string jobType,
            string jobCategory)
        {
            string extension = System.IO.Path.GetExtension(cvFilePath).ToLower();
            string cvText = "";

            if (extension == ".pdf")
            {
                cvText = ExtractTextFromPdf(cvFilePath);
            }
            else if (extension == ".docx")
            {
                cvText = ExtractTextFromDocx(cvFilePath);
            }
            else
            {
                return new CvMatchResult
                {
                    Score = 0,
                    Level = "Unsupported File",
                    Message = "The system only supports matching PDF or DOCX CV files.",
                    ExtractedText = ""
                };
            }

            if (string.IsNullOrWhiteSpace(cvText))
            {
                return new CvMatchResult
                {
                    Score = 0,
                    Level = "Unreadable CV",
                    Message = "The system could not read the CV content.",
                    ExtractedText = ""
                };
            }

            string cvLower = NormalizeText(cvText);

            string titleText = NormalizeText(jobTitle);
            string categoryText = NormalizeText(jobCategory);
            string experienceText = NormalizeText(jobExperience);
            string typeText = NormalizeText(jobType);
            string descriptionText = NormalizeText(jobDescription);
            string requirementText = NormalizeText(jobRequirement);

            string jobText = (
                titleText + " " +
                categoryText + " " +
                experienceText + " " +
                typeText + " " +
                requirementText + " " +
                descriptionText
            ).Trim();

            if (string.IsNullOrWhiteSpace(jobText))
            {
                return new CvMatchResult
                {
                    Score = 0,
                    Level = "Missing Job Data",
                    Message = "This job post does not have enough description or requirements to compare with candidate CVs.",
                    ExtractedText = cvText
                };
            }

            List<string> importantKeywords = ExtractImportantJobKeywords(
                titleText,
                categoryText,
                experienceText,
                typeText,
                descriptionText,
                requirementText
            );

            if (importantKeywords.Count == 0)
            {
                return new CvMatchResult
                {
                    Score = 0,
                    Level = "Missing Keywords",
                    Message = "The system could not find useful keywords from this job post.",
                    ExtractedText = cvText
                };
            }

            var matchedKeywords = new List<string>();
            var missingKeywords = new List<string>();

            foreach (var keyword in importantKeywords)
            {
                if (cvLower.Contains(keyword))
                {
                    matchedKeywords.Add(keyword);
                }
                else
                {
                    missingKeywords.Add(keyword);
                }
            }

            int keywordScore = (int)Math.Round((matchedKeywords.Count * 100.0) / importantKeywords.Count);

            int sectionBonus = CalculateSectionBonus(cvLower, jobText);
            int score = keywordScore + sectionBonus;

            if (score > 100)
            {
                score = 100;
            }

            string level;
            string message;

            if (score >= 75)
            {
                level = "Excellent Match";
                message = "This candidate has many skills and experiences that strongly match the job requirements.";
            }
            else if (score >= 50)
            {
                level = "Good Match";
                message = "This candidate matches several important job requirements and should be reviewed carefully.";
            }
            else if (score >= 30)
            {
                level = "Need Review";
                message = "This candidate matches part of the job requirements, but still needs manual review.";
            }
            else
            {
                level = "Low Match";
                message = "This candidate has limited matching information. Please review the CV manually before making a decision.";
            }

            return new CvMatchResult
            {
                Score = score,
                Level = level,
                Message = message,
                MatchedSkills = matchedKeywords.Take(14).ToList(),
                MissingSkills = missingKeywords.Take(14).ToList(),
                ExtractedText = cvText
            };
        }

        private List<string> ExtractImportantJobKeywords(
            string jobTitle,
            string jobCategory,
            string jobExperience,
            string jobType,
            string jobDescription,
            string jobRequirement)
        {
            string importantJobText = (
                jobTitle + " " +
                jobCategory + " " +
                jobExperience + " " +
                jobType + " " +
                jobRequirement + " " +
                jobRequirement + " " +
                jobDescription
            ).Trim();

            var skillBank = new List<string>
            {
                // General
                "communication",
                "teamwork",
                "leadership",
                "problem solving",
                "time management",
                "english",
                "customer service",
                "training",
                "presentation",
                "negotiation",
                "planning",
                "reporting",
                "research",
                "analysis",

                // IT / Developer
                "html",
                "css",
                "javascript",
                "jquery",
                "bootstrap",
                "react",
                "angular",
                "vue",
                "typescript",
                "nodejs",
                "php",
                "java",
                "python",
                "c#",
                "asp.net",
                "mvc",
                "entity framework",
                "sql",
                "sql server",
                "mysql",
                "database",
                "api",
                "git",
                "github",
                "frontend",
                "backend",
                "web development",

                // Design
                "graphic",
                "graphic design",
                "designer",
                "design",
                "visual",
                "visual design",
                "visual communication",
                "branding",
                "brand identity",
                "poster",
                "banner",
                "brochure",
                "social media",
                "social media design",
                "marketing materials",
                "product visuals",
                "layout",
                "typography",
                "composition",
                "photoshop",
                "illustrator",
                "canva",
                "figma",
                "adobe photoshop",
                "adobe illustrator",
                "creative",

                // Marketing / Content
                "marketing",
                "content",
                "content marketing",
                "content writing",
                "copywriting",
                "seo",
                "facebook ads",
                "google ads",
                "tiktok",
                "social media marketing",
                "campaign",
                "digital marketing",

                // Sales
                "sales",
                "sales executive",
                "consulting",
                "lead generation",
                "customer relationship",
                "crm",
                "closing sales",

                // Accounting / Finance
                "accounting",
                "accountant",
                "finance",
                "financial",
                "tax",
                "invoice",
                "excel",
                "word",
                "powerpoint",
                "microsoft office",

                // Education
                "teaching",
                "teacher",
                "education",
                "lesson plan",
                "classroom",
                "student",
                "tutoring",

                // Vietnamese normalized keywords
                "thiet ke",
                "do hoa",
                "marketing",
                "truyen thong",
                "ban hang",
                "ke toan",
                "tai chinh",
                "lap trinh",
                "cham soc khach hang",
                "giao tiep",
                "lam viec nhom"
            };

            var result = new List<string>();

            foreach (var skill in skillBank)
            {
                string normalizedSkill = NormalizeText(skill);

                if (importantJobText.Contains(normalizedSkill))
                {
                    result.Add(normalizedSkill);
                }
            }

            var fallbackKeywords = ExtractFallbackKeywords(importantJobText);

            foreach (var keyword in fallbackKeywords)
            {
                if (!result.Contains(keyword))
                {
                    result.Add(keyword);
                }
            }

            return result
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Take(40)
                .ToList();
        }

        private List<string> ExtractFallbackKeywords(string jobText)
        {
            string[] separators =
            {
                " ", "\r", "\n", "\t", ".", ",", ";", ":", "-", "_", "/", "\\",
                "(", ")", "[", "]", "{", "}", "+", "*", "&", "|", "!", "?",
                "\"", "'", "•"
            };

            var stopWords = new List<string>
            {
                "the", "and", "for", "with", "you", "your", "are", "can", "will",
                "this", "that", "from", "have", "has", "was", "were", "been",
                "job", "work", "working", "company", "candidate", "candidates",
                "position", "requirement", "requirements", "description",
                "benefit", "benefits", "salary", "location", "experience",
                "skill", "skills", "good", "able", "must", "should", "about",
                "into", "our", "their", "they", "them", "who", "what", "where",
                "when", "why", "how", "main", "task", "tasks", "similar",
                "software", "responsible", "responsibilities", "team", "teams",
                "develop", "development", "create", "creating", "looking",
                "materials", "activities", "feedback", "deadline", "deadlines",
                "basic", "knowledge", "such", "using", "based",

                "cac", "va", "cho", "voi", "cua", "ung", "vien", "cong",
                "viec", "kinh", "nghiem", "yeu", "cau", "mo", "ta", "lam",
                "tai", "trong", "mot", "co", "la", "duoc", "se", "theo",
                "nha", "tuyen", "dung", "can", "tim", "kiem"
            };

            return jobText
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => NormalizeText(w))
                .Where(w => w.Length >= 4)
                .Where(w => !stopWords.Contains(w))
                .Distinct()
                .Take(25)
                .ToList();
        }

        private int CalculateSectionBonus(string cvText, string jobText)
        {
            int bonus = 0;

            if (cvText.Contains("experience") || cvText.Contains("work experience") || cvText.Contains("kinh nghiem"))
            {
                bonus += 4;
            }

            if (cvText.Contains("skills") || cvText.Contains("technical skills") || cvText.Contains("ky nang"))
            {
                bonus += 4;
            }

            if (cvText.Contains("education") || cvText.Contains("hoc van") || cvText.Contains("degree"))
            {
                bonus += 3;
            }

            if (cvText.Contains("project") || cvText.Contains("projects") || cvText.Contains("du an"))
            {
                bonus += 3;
            }

            if (cvText.Contains("certificate") || cvText.Contains("certification") || cvText.Contains("chung chi"))
            {
                bonus += 2;
            }

            if (jobText.Contains("senior") || jobText.Contains("3 years") || jobText.Contains("3 nam"))
            {
                if (cvText.Contains("3 years") || cvText.Contains("three years") || cvText.Contains("senior") || cvText.Contains("3 nam"))
                {
                    bonus += 5;
                }
            }

            if (bonus > 15)
            {
                bonus = 15;
            }

            return bonus;
        }

        private string ExtractTextFromPdf(string path)
        {
            StringBuilder text = new StringBuilder();

            using (PdfReader reader = new PdfReader(path))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
                    text.Append(" ");
                }
            }

            return text.ToString();
        }

        private string ExtractTextFromDocx(string path)
        {
            StringBuilder text = new StringBuilder();

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(path, false))
            {
                if (wordDoc.MainDocumentPart != null &&
                    wordDoc.MainDocumentPart.Document != null &&
                    wordDoc.MainDocumentPart.Document.Body != null)
                {
                    text.Append(wordDoc.MainDocumentPart.Document.Body.InnerText);
                }
            }

            return text.ToString();
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            text = text.Trim().ToLower();

            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new StringBuilder();

            foreach (char c in normalized)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}