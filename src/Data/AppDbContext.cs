using GogGameDownloader.Models;
using Microsoft.EntityFrameworkCore;

namespace GogGameDownloader.Data;

public class AppDbContext : DbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<GameVersion> GameVersions { get; set; }
    public DbSet<DownloadJob> DownloadJobs { get; set; }
    public DbSet<DownloadChunk> DownloadChunks { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>().ToTable("Games");
        modelBuilder.Entity<GameVersion>().ToTable("GameVersions");
        modelBuilder.Entity<DownloadJob>().ToTable("DownloadJobs");
        modelBuilder.Entity<DownloadChunk>().ToTable("DownloadChunks");
        modelBuilder.Entity<AppSetting>().ToTable("AppSettings");

        modelBuilder.Entity<DownloadJob>()
            .Property(j => j.Status)
            .HasConversion<string>();
    }
}
