using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace BasicProject.Models
{
    public static class ArticleJsonStore
    {
        private static string GetJsonPath()
        {
            string folderPath = HttpContext.Current.Server.MapPath("~/App_Data");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, "articles.json");

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "[]");
            }

            return filePath;
        }

        public static List<ArticleJson> GetAll()
        {
            string path = GetJsonPath();

            string json = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<ArticleJson>();
            }

            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;

            try
            {
                var articles = serializer.Deserialize<List<ArticleJson>>(json);

                return articles ?? new List<ArticleJson>();
            }
            catch
            {
                return new List<ArticleJson>();
            }
        }

        public static void SaveAll(List<ArticleJson> articles)
        {
            string path = GetJsonPath();

            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;

            string json = serializer.Serialize(articles);

            File.WriteAllText(path, json);
        }

        public static int GetNextId()
        {
            var articles = GetAll();

            if (!articles.Any())
            {
                return 1;
            }

            return articles.Max(a => a.ArticleID) + 1;
        }

        public static ArticleJson FindById(int id)
        {
            return GetAll().FirstOrDefault(a => a.ArticleID == id);
        }
    }
}