using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class AdminCareerTipsController : Controller
    {
        private bool IsAdmin()
        {
            return Session["UserID"] != null
                && Session["Role"] != null
                && Session["Role"].ToString() == "Admin";
        }

        private string SaveCareerTipImage(HttpPostedFileBase imageFile)
        {
            if (imageFile == null || imageFile.ContentLength <= 0)
            {
                return null;
            }

            string extension = Path.GetExtension(imageFile.FileName).ToLower();

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception("Images must be .jpg, .jpeg, .png, or .webp files.");
            }

            string folderPath = Server.MapPath("~/Content/images/career-tips");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = "career_tip_"
                + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                + "_"
                + Guid.NewGuid().ToString("N").Substring(0, 6)
                + extension;

            string fullPath = Path.Combine(folderPath, fileName);

            imageFile.SaveAs(fullPath);

            return "/Content/images/career-tips/" + fileName;
        }

        private string BuildImageHtml(string imagePath, string altText)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return "";
            }

            return "<figure class=\"career-tip-content-image\">" +
                   "<img src=\"" + imagePath + "\" alt=\"" + HttpUtility.HtmlEncode(altText) + "\" />" +
                   "</figure>";
        }

        private string InsertContentImages(
            string content,
            string title,
            HttpPostedFileBase contentImage1,
            HttpPostedFileBase contentImage2,
            HttpPostedFileBase contentImage3
        )
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                content = "";
            }

            string imagePath1 = SaveCareerTipImage(contentImage1);
            string imagePath2 = SaveCareerTipImage(contentImage2);
            string imagePath3 = SaveCareerTipImage(contentImage3);

            string imageHtml1 = BuildImageHtml(imagePath1, title);
            string imageHtml2 = BuildImageHtml(imagePath2, title);
            string imageHtml3 = BuildImageHtml(imagePath3, title);

            if (!string.IsNullOrEmpty(imageHtml1))
            {
                if (content.Contains("[image1]"))
                {
                    content = content.Replace("[image1]", imageHtml1);
                }
                else
                {
                    content += "\n" + imageHtml1;
                }
            }

            if (!string.IsNullOrEmpty(imageHtml2))
            {
                if (content.Contains("[image2]"))
                {
                    content = content.Replace("[image2]", imageHtml2);
                }
                else
                {
                    content += "\n" + imageHtml2;
                }
            }

            if (!string.IsNullOrEmpty(imageHtml3))
            {
                if (content.Contains("[image3]"))
                {
                    content = content.Replace("[image3]", imageHtml3);
                }
                else
                {
                    content += "\n" + imageHtml3;
                }
            }

            return content;
        }

        private string NormalizeTargetAudience(string targetAudience)
        {
            if (string.IsNullOrWhiteSpace(targetAudience))
            {
                return "Candidate";
            }

            targetAudience = targetAudience.Trim();

            if (targetAudience != "Candidate" && targetAudience != "Employer")
            {
                return "Candidate";
            }

            return targetAudience;
        }

        public ActionResult Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var tips = ArticleJsonStore.GetAll()
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            return View(tips);
        }

        public ActionResult Create()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(
            string title,
            string category,
            string targetAudience,
            string summary,
            string content,
            bool? isActive,
            HttpPostedFileBase thumbnailFile,
            HttpPostedFileBase contentImage1,
            HttpPostedFileBase contentImage2,
            HttpPostedFileBase contentImage3
        )
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                ViewBag.Error = "Please enter the career tip title.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                ViewBag.Error = "Please enter the short summary.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                ViewBag.Error = "Please enter the career tip content.";
                return View();
            }

            string thumbnailPath = null;

            try
            {
                thumbnailPath = SaveCareerTipImage(thumbnailFile);
                content = InsertContentImages(content, title, contentImage1, contentImage2, contentImage3);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }

            var tips = ArticleJsonStore.GetAll();

            var tip = new ArticleJson
            {
                ArticleID = ArticleJsonStore.GetNextId(),
                Title = title.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "Career Tips" : category.Trim(),
                TargetAudience = NormalizeTargetAudience(targetAudience),
                Summary = summary.Trim(),
                Content = content,
                Thumbnail = thumbnailPath,
                AuthorName = Session["UserName"] != null ? Session["UserName"].ToString() : "Admin",
                IsActive = isActive ?? true,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            tips.Add(tip);

            ArticleJsonStore.SaveAll(tips);

            TempData["Success"] = "Career tip added successfully.";

            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var tip = ArticleJsonStore.FindById(id);

            if (tip == null)
            {
                return HttpNotFound();
            }

            return View(tip);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(
            int id,
            string title,
            string category,
            string targetAudience,
            string summary,
            string content,
            bool? isActive,
            HttpPostedFileBase thumbnailFile,
            HttpPostedFileBase contentImage1,
            HttpPostedFileBase contentImage2,
            HttpPostedFileBase contentImage3
        )
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var tips = ArticleJsonStore.GetAll();

            var tip = tips.FirstOrDefault(a => a.ArticleID == id);

            if (tip == null)
            {
                return HttpNotFound();
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                ViewBag.Error = "Please enter the career tip title.";
                return View(tip);
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                ViewBag.Error = "Please enter the short summary.";
                return View(tip);
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                ViewBag.Error = "Please enter the career tip content.";
                return View(tip);
            }

            try
            {
                if (thumbnailFile != null && thumbnailFile.ContentLength > 0)
                {
                    if (!string.IsNullOrEmpty(tip.Thumbnail))
                    {
                        string oldPath = Server.MapPath(tip.Thumbnail);

                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    tip.Thumbnail = SaveCareerTipImage(thumbnailFile);
                }

                content = InsertContentImages(content, title, contentImage1, contentImage2, contentImage3);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(tip);
            }

            tip.Title = title.Trim();
            tip.Category = string.IsNullOrWhiteSpace(category) ? "Career Tips" : category.Trim();
            tip.TargetAudience = NormalizeTargetAudience(targetAudience);
            tip.Summary = summary.Trim();
            tip.Content = content;
            tip.IsActive = isActive ?? false;
            tip.UpdatedAt = DateTime.Now;

            ArticleJsonStore.SaveAll(tips);

            TempData["Success"] = "Career tip updated successfully.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var tips = ArticleJsonStore.GetAll();

            var tip = tips.FirstOrDefault(a => a.ArticleID == id);

            if (tip == null)
            {
                return HttpNotFound();
            }

            if (!string.IsNullOrEmpty(tip.Thumbnail))
            {
                string imagePath = Server.MapPath(tip.Thumbnail);

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            tips.Remove(tip);

            ArticleJsonStore.SaveAll(tips);

            TempData["Success"] = "Career tip deleted successfully.";

            return RedirectToAction("Index");
        }
    }
}