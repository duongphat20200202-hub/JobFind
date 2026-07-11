using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicProject.Models
{
    [Table("CVs")]
    public class CV
    {
        [Key]
        public int CV_ID { get; set; }

        public int CandidateID { get; set; }

        public string CVName { get; set; }
        public string FilePath { get; set; }

        public bool IsDefault { get; set; }
        public DateTime? CreatedAt { get; set; }

        [ForeignKey("CandidateID")]
        public virtual Candidate Candidate { get; set; }
    }
}