using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class AdminCVTemplatesController : Controller
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

            var templates = db.CVTemplates
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            return View(templates);
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
        public ActionResult Create(
            string templateName,
            string category,
            string description,
            bool? isActive,
            HttpPostedFileBase previewFile,
            HttpPostedFileBase cvFile
        )
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(templateName))
            {
                ViewBag.Error = "Please enter the CV template name.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                ViewBag.Error = "Please select the CV template category.";
                return View();
            }

            if (cvFile == null || cvFile.ContentLength == 0)
            {
                ViewBag.Error = "Please choose a Word CV template file.";
                return View();
            }

            string previewPath = null;
            string cvFilePath = null;

            string[] allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
            string[] allowedCvExtensions = { ".docx" };

            if (previewFile != null && previewFile.ContentLength > 0)
            {
                string imageExtension = Path.GetExtension(previewFile.FileName).ToLower();

                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    ViewBag.Error = "Preview images must be .jpg, .jpeg, .png, or .webp files.";
                    return View();
                }

                string imageFolder = Server.MapPath("~/Content/images/cv-templates");

                if (!Directory.Exists(imageFolder))
                {
                    Directory.CreateDirectory(imageFolder);
                }

                string imageName = "preview_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + imageExtension;
                string imageFullPath = Path.Combine(imageFolder, imageName);

                previewFile.SaveAs(imageFullPath);

                previewPath = "/Content/images/cv-templates/" + imageName;
            }

            string cvExtension = Path.GetExtension(cvFile.FileName).ToLower();

            if (!allowedCvExtensions.Contains(cvExtension))
            {
                ViewBag.Error = "CV template file must be a .docx file.";
                return View();
            }

            string cvFolder = Server.MapPath("~/Content/files/cv-templates");

            if (!Directory.Exists(cvFolder))
            {
                Directory.CreateDirectory(cvFolder);
            }

            string cvName = "cv_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + cvExtension;
            string cvFullPath = Path.Combine(cvFolder, cvName);

            cvFile.SaveAs(cvFullPath);

            cvFilePath = "/Content/files/cv-templates/" + cvName;

            var template = new CVTemplate
            {
                TemplateName = templateName,
                Category = category,
                Description = description,
                PreviewImage = previewPath,
                FilePath = cvFilePath,
                IsActive = isActive ?? true,
                CreatedAt = DateTime.Now
            };

            db.CVTemplates.Add(template);
            db.SaveChanges();

            TempData["Success"] = "CV template added successfully.";

            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var template = db.CVTemplates.Find(id);

            if (template == null)
            {
                return HttpNotFound();
            }

            return View(template);
        }

        [HttpPost]
        public ActionResult Edit(
            int id,
            string templateName,
            string category,
            string description,
            bool? isActive,
            HttpPostedFileBase previewFile,
            HttpPostedFileBase cvFile
        )
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var template = db.CVTemplates.Find(id);

            if (template == null)
            {
                return HttpNotFound();
            }

            if (string.IsNullOrWhiteSpace(templateName))
            {
                ViewBag.Error = "Please enter the CV template name.";
                return View(template);
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                ViewBag.Error = "Please select the CV template category.";
                return View(template);
            }

            template.TemplateName = templateName;
            template.Category = category;
            template.Description = description;
            template.IsActive = isActive ?? false;

            string[] allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
            string[] allowedCvExtensions = { ".docx" };

            if (previewFile != null && previewFile.ContentLength > 0)
            {
                string imageExtension = Path.GetExtension(previewFile.FileName).ToLower();

                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    ViewBag.Error = "Preview images must be .jpg, .jpeg, .png, or .webp files.";
                    return View(template);
                }

                if (!string.IsNullOrEmpty(template.PreviewImage))
                {
                    string oldImagePath = Server.MapPath(template.PreviewImage);

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string imageFolder = Server.MapPath("~/Content/images/cv-templates");

                if (!Directory.Exists(imageFolder))
                {
                    Directory.CreateDirectory(imageFolder);
                }

                string imageName = "preview_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + imageExtension;
                string imageFullPath = Path.Combine(imageFolder, imageName);

                previewFile.SaveAs(imageFullPath);

                template.PreviewImage = "/Content/images/cv-templates/" + imageName;
            }

            if (cvFile != null && cvFile.ContentLength > 0)
            {
                string cvExtension = Path.GetExtension(cvFile.FileName).ToLower();

                if (!allowedCvExtensions.Contains(cvExtension))
                {
                    ViewBag.Error = "CV template file must be a .docx file.";
                    return View(template);
                }

                if (!string.IsNullOrEmpty(template.FilePath))
                {
                    string oldCvPath = Server.MapPath(template.FilePath);

                    if (System.IO.File.Exists(oldCvPath))
                    {
                        System.IO.File.Delete(oldCvPath);
                    }
                }

                string cvFolder = Server.MapPath("~/Content/files/cv-templates");

                if (!Directory.Exists(cvFolder))
                {
                    Directory.CreateDirectory(cvFolder);
                }

                string cvName = "cv_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + cvExtension;
                string cvFullPath = Path.Combine(cvFolder, cvName);

                cvFile.SaveAs(cvFullPath);

                template.FilePath = "/Content/files/cv-templates/" + cvName;
            }

            db.SaveChanges();

            TempData["Success"] = "CV template updated successfully.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var template = db.CVTemplates.Find(id);

            if (template == null)
            {
                return HttpNotFound();
            }

            if (!string.IsNullOrEmpty(template.PreviewImage))
            {
                string imagePath = Server.MapPath(template.PreviewImage);

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            if (!string.IsNullOrEmpty(template.FilePath))
            {
                string filePath = Server.MapPath(template.FilePath);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            db.CVTemplates.Remove(template);
            db.SaveChanges();

            TempData["Success"] = "CV template deleted successfully.";

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