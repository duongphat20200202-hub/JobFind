using System.Collections.Generic;

namespace BasicProject.Models
{
    public class HomeViewModel
    {
        public List<Job> Jobs { get; set; }
        public List<Category> Categories { get; set; }
        public List<Company> Companies { get; set; }
        public List<ArticleJson> Articles { get; set; }
    }
}