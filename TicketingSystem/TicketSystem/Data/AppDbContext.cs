using Microsoft.EntityFrameworkCore;
using TicketSystem.Models;

namespace TicketSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.CreatedByUser)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Email unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // KanbanPin eşlemeleri
            modelBuilder.Entity<KanbanPin>(b =>
            {
                b.HasKey(p => p.KanbanPinId);

                
                b.HasIndex(p => new { p.UserId, p.TicketId }).IsUnique();

                b.HasOne(p => p.User)
                 .WithMany() 
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(p => p.Ticket)
                 .WithMany() 
                 .HasForeignKey(p => p.TicketId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<UserInsecure> UsersInsecure { get; set; }


        
        public DbSet<KanbanPin> KanbanPins { get; set; }
    }
}
