using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Entity Framework Core DbContext for WabbitBot with JSONB support
    /// </summary>
    public partial class WabbitBotDbContext : DbContext
    {
        public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
        public DbSet<Season> Seasons { get; set; } = null!;
        public DbSet<SeasonGroup> SeasonGroups { get; set; } = null!;

        private void ConfigureLeaderboard(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Leaderboard>(entity =>
            {
                entity.ToTable("leaderboards");

                // Configure JSONB columns for complex objects
                entity.Property(l => l.Rankings)
                    .HasColumnName("rankings")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(l => l.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(l => l.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(l => l.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(l => l.Id);
            });
        }


        private void ConfigureSeason(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Season>(entity =>
            {
                entity.ToTable("seasons");

                // Configure JSONB columns for complex objects
                entity.Property(s => s.ParticipatingTeams)
                    .HasColumnName("participating_teams")
                    .HasColumnType("jsonb");

                entity.Property(s => s.SeasonConfigId)
                    .HasColumnName("season_config_id");

                entity.Property(s => s.ConfigData)
                    .HasColumnName("config_data")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(s => s.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(s => s.SeasonGroupId)
                    .HasColumnName("season_group_id");

                entity.Property(s => s.EvenTeamFormat)
                    .HasColumnName("even_team_format");

                entity.Property(s => s.StartDate)
                    .HasColumnName("start_date");

                entity.Property(s => s.EndDate)
                    .HasColumnName("end_date");

                entity.Property(s => s.IsActive)
                    .HasColumnName("is_active");

                entity.Property(s => s.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(s => s.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(s => s.Id);

                // Foreign key relationships
                entity.HasOne<SeasonGroup>()
                    .WithMany()
                    .HasForeignKey(s => s.SeasonGroupId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<SeasonConfig>()
                    .WithMany()
                    .HasForeignKey(s => s.SeasonConfigId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }


        private void ConfigureSeasonGroup(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SeasonGroup>(entity =>
            {
                entity.ToTable("season_groups");

                // Standard columns
                entity.Property(sg => sg.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(sg => sg.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(sg => sg.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(sg => sg.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(sg => sg.Id);
            });
        }
    }
}