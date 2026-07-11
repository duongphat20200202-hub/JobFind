using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class CareerTipsController : Controller
    {
        public ActionResult Index(string category, string keyword)
        {
            var allTips = ArticleJsonStore.GetAll()
                .Where(a => a.IsActive)
                .Where(a => string.IsNullOrEmpty(a.TargetAudience) || a.TargetAudience == "Candidate")
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            IEnumerable<ArticleJson> tips = allTips;

            if (!string.IsNullOrWhiteSpace(category))
            {
                tips = tips.Where(a => a.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string keywordLower = keyword.Trim().ToLower();

                tips = tips.Where(a =>
                    (!string.IsNullOrEmpty(a.Title) && a.Title.ToLower().Contains(keywordLower)) ||
                    (!string.IsNullOrEmpty(a.Category) && a.Category.ToLower().Contains(keywordLower)) ||
                    (!string.IsNullOrEmpty(a.Summary) && a.Summary.ToLower().Contains(keywordLower)) ||
                    (!string.IsNullOrEmpty(a.Content) && a.Content.ToLower().Contains(keywordLower))
                );
            }

            ViewBag.CurrentCategory = category;
            ViewBag.Keyword = keyword;

            ViewBag.FeaturedTip = allTips.FirstOrDefault();

            ViewBag.LatestTips = allTips
                .Take(5)
                .ToList();

            ViewBag.PopularTips = allTips
                .Skip(1)
                .Take(4)
                .ToList();

            return View(tips.OrderByDescending(a => a.CreatedAt).ToList());
        }

        public ActionResult Details(int id)
        {
            var allTips = ArticleJsonStore.GetAll()
                .Where(a => a.IsActive && (a.TargetAudience == null || a.TargetAudience == "Candidate"))
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            var tip = allTips.FirstOrDefault(a => a.ArticleID == id);

            if (tip == null)
            {
                return HttpNotFound();
            }

            var sameAudienceTips = allTips
                .Where(a =>
                    a.ArticleID != id &&
                    (
                        a.TargetAudience == tip.TargetAudience ||
                        string.IsNullOrEmpty(a.TargetAudience) && string.IsNullOrEmpty(tip.TargetAudience)
                    )
                )
                .ToList();

            ViewBag.LatestTips = sameAudienceTips
                .Take(5)
                .ToList();

            ViewBag.RelatedTips = sameAudienceTips
                .Where(a => a.Category == tip.Category)
                .Take(6)
                .ToList();

            ViewBag.OtherTips = sameAudienceTips
                .Take(6)
                .ToList();

            return View(tip);
        }
    }
}