using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class CVTemplatesController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        public ActionResult Index(string category, string keyword)
        {
            var templates = db.CVTemplates
                .Where(t => t.IsActive);

            if (!string.IsNullOrWhiteSpace(category))
            {
                templates = templates.Where(t => t.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string keywordLower = keyword.Trim().ToLower();

                templates = templates.Where(t =>
                    t.TemplateName.ToLower().Contains(keywordLower) ||
                    t.Category.ToLower().Contains(keywordLower) ||
                    (t.Description != null && t.Description.ToLower().Contains(keywordLower))
                );
            }

            ViewBag.CurrentCategory = category;
            ViewBag.Keyword = keyword;

            return View(templates.OrderByDescending(t => t.CreatedAt).ToList());
        }

        public ActionResult Details(int id)
        {
            var template = db.CVTemplates
                .FirstOrDefault(t => t.TemplateID == id && t.IsActive);

            if (template == null)
            {
                return HttpNotFound();
            }

            ViewBag.RelatedTemplates = db.CVTemplates
                .Where(t =>
                    t.IsActive &&
                    t.TemplateID != id &&
                    t.Category == template.Category
                )
                .OrderByDescending(t => t.CreatedAt)
                .Take(3)
                .ToList();

            return View(template);
        }

        public ActionResult Download(int id)
        {
            var template = db.CVTemplates
                .FirstOrDefault(t => t.TemplateID == id && t.IsActive);

            if (template == null)
            {
                return HttpNotFound();
            }

            string filePath = Server.MapPath(template.FilePath);

            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound("CV template file not found.");
            }

            string extension = System.IO.Path.GetExtension(filePath);
            string fileName = template.TemplateName + extension;

            string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return File(filePath, contentType, fileName);
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