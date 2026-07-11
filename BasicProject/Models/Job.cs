using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicProject.Models
{
    [Table("Jobs")]
    public class Job
    {
        [Key]
        public int JobID { get; set; }

        public int EmployerID { get; set; }
        public int CompanyID { get; set; }
        public int? CategoryID { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Requirement { get; set; }
        public string Benefit { get; set; }

        public decimal? Salary { get; set; }
        public string Location { get; set; }
        public string JobType { get; set; }
        public string Experience { get; set; }
        public DateTime? Deadline { get; set; }
        public int HiringQuantity { get; set; }
        public int RemainingSlots { get; set; }

        public bool IsHot { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; }

        [ForeignKey("EmployerID")]
        public virtual Employer Employer { get; set; }

        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; }

        [ForeignKey("CategoryID")]
        public virtual Category Category { get; set; }
    }
}