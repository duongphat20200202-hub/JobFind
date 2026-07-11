using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BasicProject.Models;

namespace BasicProject.Controllers
{
    public class EmployerCompanyController : Controller
    {
        private QLTimViecContext db = new QLTimViecContext();

        private Employer GetCurrentEmployer()
        {
            if (Session["UserID"] == null ||
                Session["Role"] == null ||
                Session["Role"].ToString() != "Employer")
            {
                return null;
            }

            int userId = int.Parse(Session["UserID"].ToString());

            var user = db.Users.FirstOrDefault(u =>
                u.UserID == userId &&
                u.IsActive == true
            );

            if (user == null)
            {
                Session.Clear();
                return null;
            }

            return db.Employers.FirstOrDefault(e => e.UserID == userId);
        }

        public ActionResult Edit()
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var company = db.Companies.FirstOrDefault(c => c.CompanyID == employer.CompanyID);

            if (company == null)
            {
                return HttpNotFound();
            }

            return View(company);
        }

        [HttpPost]
        public ActionResult Edit(
            string companyName,
            string location,
            string companySize,
            string email,
            string phone,
            string website,
            string description,
            HttpPostedFileBase logoFile,
            HttpPostedFileBase bannerFile
        )
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var company = db.Companies.FirstOrDefault(c => c.CompanyID == employer.CompanyID);

            if (company == null)
            {
                return HttpNotFound();
            }

            company.CompanyName = companyName;
            company.Location = location;
            company.CompanySize = companySize;
            company.Email = email;
            company.Phone = phone;
            company.Website = website;
            company.Description = description;

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

            // Upload company logo
            if (logoFile != null && logoFile.ContentLength > 0)
            {
                string extension = Path.GetExtension(logoFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ViewBag.Error = "Logo images must be .jpg, .jpeg, .png, or .webp files.";
                    return View(company);
                }

                string folderPath = Server.MapPath("~/Content/images/companies");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = "company_" + company.CompanyID + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;

                string fullPath = Path.Combine(folderPath, fileName);

                logoFile.SaveAs(fullPath);

                company.Logo = "/Content/images/companies/" + fileName;
            }

            // Upload company banner
            if (bannerFile != null && bannerFile.ContentLength > 0)
            {
                string extension = Path.GetExtension(bannerFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ViewBag.Error = "Banner images must be .jpg, .jpeg, .png, or .webp files.";
                    return View(company);
                }

                string bannerFolder = Server.MapPath("~/Content/images/company-banners");

                if (!Directory.Exists(bannerFolder))
                {
                    Directory.CreateDirectory(bannerFolder);
                }

                // Delete old company banner if it exists
                string[] oldBannerFiles = Directory.GetFiles(bannerFolder, "banner_" + company.CompanyID + ".*");

                foreach (string oldFile in oldBannerFiles)
                {
                    System.IO.File.Delete(oldFile);
                }

                // Save new banner by CompanyID
                string bannerName = "banner_" + company.CompanyID + extension;
                string bannerFullPath = Path.Combine(bannerFolder, bannerName);

                bannerFile.SaveAs(bannerFullPath);
            }

            db.SaveChanges();

            TempData["Success"] = "Company information updated successfully.";

            return RedirectToAction("Edit");
        }

        public ActionResult Banner(int id)
        {
            string bannerFolder = Server.MapPath("~/Content/images/company-banners");
            string defaultBanner = Server.MapPath("~/Content/images/default-company-banner.jpg");

            if (Directory.Exists(bannerFolder))
            {
                string[] files = Directory.GetFiles(bannerFolder, "banner_" + id + ".*");

                if (files.Length > 0)
                {
                    string filePath = files[0];
                    string extension = Path.GetExtension(filePath).ToLower();

                    string contentType = "image/jpeg";

                    if (extension == ".png")
                    {
                        contentType = "image/png";
                    }
                    else if (extension == ".webp")
                    {
                        contentType = "image/webp";
                    }
                    else if (extension == ".jpg" || extension == ".jpeg")
                    {
                        contentType = "image/jpeg";
                    }

                    return File(filePath, contentType);
                }
            }

            if (System.IO.File.Exists(defaultBanner))
            {
                return File(defaultBanner, "image/jpeg");
            }

            return Content("");
        }

        [HttpPost]
        public ActionResult DeleteLogo()
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var company = db.Companies.FirstOrDefault(c => c.CompanyID == employer.CompanyID);

            if (company == null)
            {
                return HttpNotFound();
            }

            if (!string.IsNullOrEmpty(company.Logo))
            {
                string logoPath = Server.MapPath(company.Logo);

                if (System.IO.File.Exists(logoPath))
                {
                    System.IO.File.Delete(logoPath);
                }

                company.Logo = null;
                db.SaveChanges();
            }

            TempData["Success"] = "Company logo deleted successfully.";
            return RedirectToAction("Edit");
        }

        [HttpPost]
        public ActionResult DeleteBanner()
        {
            var employer = GetCurrentEmployer();

            if (employer == null)
            {
                return RedirectToAction("Login", "Account", new { role = "Employer" });
            }

            var company = db.Companies.FirstOrDefault(c => c.CompanyID == employer.CompanyID);

            if (company == null)
            {
                return HttpNotFound();
            }

            string bannerFolder = Server.MapPath("~/Content/images/company-banners");

            if (Directory.Exists(bannerFolder))
            {
                string[] oldBannerFiles = Directory.GetFiles(bannerFolder, "banner_" + company.CompanyID + ".*");

                foreach (string oldFile in oldBannerFiles)
                {
                    System.IO.File.Delete(oldFile);
                }
            }

            TempData["Success"] = "Company banner deleted successfully.";
            return RedirectToAction("Edit");
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