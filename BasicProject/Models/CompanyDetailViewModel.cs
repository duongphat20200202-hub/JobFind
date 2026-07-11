using System.Collections.Generic;

namespace BasicProject.Models
{
    public class CompanyDetailViewModel
    {
        public Company Company { get; set; }
        public List<Job> Jobs { get; set; }

        public int OpenJobCount { get; set; }
        public string LocationSummary { get; set; }
    }
}