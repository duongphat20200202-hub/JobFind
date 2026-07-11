using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicProject.Models
{
    [Table("Staffs")]
    public class Staff
    {
        [Key]
        public int StaffID { get; set; }

        public int UserID { get; set; }

        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Position { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }
}