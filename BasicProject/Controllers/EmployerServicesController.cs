using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class EmployerServicesController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.EmployerArticles = ArticleJsonStore.GetAll()
                .Where(a => a.IsActive && a.TargetAudience == "Employer")
                .OrderByDescending(a => a.CreatedAt)
                .Take(3)
                .ToList();

            return View();
        }

        public ActionResult Details(int id)
        {
            var allArticles = ArticleJsonStore.GetAll()
                .Where(a => a.IsActive && a.TargetAudience == "Employer")
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            var article = allArticles.FirstOrDefault(a => a.ArticleID == id);

            if (article == null)
            {
                return HttpNotFound();
            }

            ViewBag.RelatedArticles = allArticles
                .Where(a => a.ArticleID != id)
                .Take(6)
                .ToList();

            return View(article);
        }
    }
}