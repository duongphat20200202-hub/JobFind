using System;

namespace BasicProject.Models
{
    public class ArticleJson
    {
        public int ArticleID { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public string Summary { get; set; }

        public string Content { get; set; }

        public string Thumbnail { get; set; }

        public string AuthorName { get; set; }

        // Candidate / Employer
        public string TargetAudience { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}