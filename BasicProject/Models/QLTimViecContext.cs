using System.Data.Entity;

namespace BasicProject.Models
{
    public class QLTimViecContext : DbContext
    {
        public QLTimViecContext() : base("name=QLTimViecContext")
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Employer> Employers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<CV> CVs { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }
        public DbSet<CVTemplate> CVTemplates { get; set; }
    }
}