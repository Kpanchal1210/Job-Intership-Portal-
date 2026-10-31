using Job_InternshipPortal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Job_InternshipPortal.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }
        public DbSet<Interview> Interviews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure 1-to-1 relationship between Recruiter and Company
            builder.Entity<Company>()
                .HasOne(c => c.Recruiter)
                .WithOne(u => u.Company)
                .HasForeignKey<Company>(c => c.RecruiterId)
                .OnDelete(DeleteBehavior.Restrict);

            // JobApplication relationships to prevent cascade delete cycles in SQL Server
            builder.Entity<JobApplication>()
                .HasOne(ja => ja.Job)
                .WithMany(j => j.JobApplications)
                .HasForeignKey(ja => ja.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<JobApplication>()
                .HasOne(ja => ja.Applicant)
                .WithMany(u => u.JobApplications)
                .HasForeignKey(ja => ja.ApplicantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate applications for the same job and user
            builder.Entity<JobApplication>()
                .HasIndex(ja => new { ja.JobId, ja.ApplicantId })
                .IsUnique();

            // SavedJob relationships
            builder.Entity<SavedJob>()
                .HasOne(sj => sj.Job)
                .WithMany(j => j.SavedJobs)
                .HasForeignKey(sj => sj.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SavedJob>()
                .HasOne(sj => sj.Applicant)
                .WithMany(u => u.SavedJobs)
                .HasForeignKey(sj => sj.ApplicantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate saved jobs for the same user and job
            builder.Entity<SavedJob>()
                .HasIndex(sj => new { sj.JobId, sj.ApplicantId })
                .IsUnique();
        }
    }
}
