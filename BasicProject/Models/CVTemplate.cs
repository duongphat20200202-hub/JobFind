using System;
using System.ComponentModel.DataAnnotations;

namespace BasicProject.Models
{
    public class CVTemplate
    {
        [Key]
        public int TemplateID { get; set; }

        [Required]
        [StringLength(150)]
        public string TemplateName { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        public string PreviewImage { get; set; }

        [Required]
        public string FilePath { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}