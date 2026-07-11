using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicProject.Models
{
    [Table("Employers")]
    public class Employer
    {
        [Key]
        public int EmployerID { get; set; }

        public int UserID { get; set; }
        public int CompanyID { get; set; }

        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Position { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; }
    }
}