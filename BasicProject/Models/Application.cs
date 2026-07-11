using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicProject.Models
{
    [Table("Applications")]
    public class Application
    {
        [Key]
        public int ApplicationID { get; set; }

        public int CandidateID { get; set; }
        public int JobID { get; set; }
        public int? CV_ID { get; set; }

        public string CoverLetter { get; set; }
        public DateTime? AppliedDate { get; set; }
        public string Status { get; set; }

        [ForeignKey("CandidateID")]
        public virtual Candidate Candidate { get; set; }

        [ForeignKey("JobID")]
        public virtual Job Job { get; set; }

        [ForeignKey("CV_ID")]
        public virtual CV CV { get; set; }
    }
}