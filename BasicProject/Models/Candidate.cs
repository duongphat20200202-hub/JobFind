using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicProject.Models
{
    [Table("Candidates")]
    public class Candidate
    {
        [Key]
        public int CandidateID { get; set; }

        public int UserID { get; set; }

        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Address { get; set; }
        public string Skills { get; set; }
        public string Experience { get; set; }
        public string Education { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }
}