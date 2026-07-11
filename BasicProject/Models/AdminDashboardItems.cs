namespace BasicProject.Models
{
    public class AdminTopAppliedJobItem
    {
        public int JobID { get; set; }
        public string Title { get; set; }
        public string CompanyName { get; set; }
        public int ApplicationsCount { get; set; }
    }

    public class AdminLatestJobItem
    {
        public int JobID { get; set; }
        public string Title { get; set; }
        public string CompanyName { get; set; }
        public string CategoryName { get; set; }
        public string Status { get; set; }
        public string CreatedDate { get; set; }
    }

    public class AdminExpiringJobItem
    {
        public int JobID { get; set; }
        public string Title { get; set; }
        public string CompanyName { get; set; }
        public string Deadline { get; set; }
        public int DaysLeft { get; set; }
    }
}