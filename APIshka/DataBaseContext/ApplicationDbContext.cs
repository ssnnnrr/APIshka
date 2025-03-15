using APIshka.Model;
using Microsoft.EntityFrameworkCore;

namespace APIshka.DataBaseContext
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Skin> Skins { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка связи один-ко-многим между User и Skin
            modelBuilder.Entity<Skin>()
                .HasOne(s => s.User)
                .WithMany(u => u.Skins)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Удаление скинов при удалении пользователя

            // Настройка связи для Transaction
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Skin)
                .WithMany(s => s.Transactions)
                .HasForeignKey(t => t.SkinId);

            base.OnModelCreating(modelBuilder);
        }
    }
}