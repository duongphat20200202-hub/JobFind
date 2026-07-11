using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicProject.Models
{
    [Table("Companies")]
    public class Company
    {
        [Key]
        public int CompanyID { get; set; }

        public string CompanyName { get; set; }
        public string Logo { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string Website { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }

        public string CompanySize { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}