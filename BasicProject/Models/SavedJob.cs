using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicProject.Models
{
    [Table("SavedJobs")]
    public class SavedJob
    {
        [Key]
        public int SavedJobID { get; set; }

        public int CandidateID { get; set; }
        public int JobID { get; set; }

        public DateTime? SavedAt { get; set; }

        [ForeignKey("CandidateID")]
        public virtual Candidate Candidate { get; set; }

        [ForeignKey("JobID")]
        public virtual Job Job { get; set; }
    }
}